using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace CalvinAAllen.AzRebrandly
{
    public static class ShortLink
    {
        [FunctionName("ShortLink")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            try
			{
				var keyVaultClient = new KeyVaultClient(
					new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

				string workspaceKey = await GetSecret(keyVaultClient, "RebrandlyWorkspaceKey");
				string apiKey = await GetSecret(keyVaultClient, "RebrandlyApiKey");

				var rebrandlyUrl = GetEnvironmentVariable("RebrandlyApiUrl");
				var rebrandlyDomain = GetEnvironmentVariable("RebrandlyDomain");

				var client = new RestClient(rebrandlyUrl);

				var request = new RestRequest("/links");
				request.Method = Method.POST;
				request.AddHeader("Content-Type", "application/json");
				request.AddHeader("apikey", apiKey.Value);
				request.AddHeader("workspace", workspaceKey.Value);
				request.AddJsonBody($@"{{
                    ""destination"": ""{data?.destination}"",
                    ""domain"":{{
                        ""fullName"": ""{rebrandlyDomain}""
                    }}
                }}");

				var response = client.Execute(request);
				dynamic content = JsonConvert.DeserializeObject(response.Content);

				return (ActionResult)new OkObjectResult($"{content.shortUrl}");
			}
			catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

		private static async Task<string> GetSecret(KeyVaultClient keyVaultClient, string secretName)
		{
			return await
				keyVaultClient
				.GetSecretAsync($"https://azfunctionkeyvault.vault.azure.net/secrets/{secretName}")
				.ConfigureAwait(false);
		}

		public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
