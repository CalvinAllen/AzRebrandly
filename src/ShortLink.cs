using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace CalvinAllen.AzureFunctions.AZRebrandly
{
    public class ShortLink
	{
		[FunctionName("ShortLink")]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
			HttpRequest req,
			ILogger log)
		{
			try
			{
				var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				dynamic data = JsonConvert.DeserializeObject(requestBody);

				var destination = data?.destination ??
				                  throw new ApplicationException("Destination was not provided in the payload.");
				
				var keyVaultUrl = GetSetting("KeyVaultUrl");
				var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

				var workspaceKey = secretClient.GetSecret("RebrandlyWorkspaceKey");
				var apiKey = secretClient.GetSecret("RebrandlyApiKey");
				var cosmosKey = secretClient.GetSecret("CosmosKey");

				var rebrandlyUrl = GetSetting("RebrandlyApiUrl");
				var rebrandlyDomain = GetSetting("RebrandlyDomain");
				var cosmosEndpoint = GetSetting("CosmosEndpoint");
				var cosmosDatabase = GetSetting("CosmosDatabaseName");
				var cosmosContainer = GetSetting("CosmosContainerName");

				var cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey.Value.Value);
				var container = cosmosClient.GetContainer(cosmosDatabase, cosmosContainer);

				var query =
					new QueryDefinition(
						$"SELECT c.DestinationUrl, c.ShortUrl FROM c WHERE c.DestinationUrl = '{destination}'");
				var results = container.GetItemQueryIterator<Mapping>(query);

				if (results.HasMoreResults)
				{
					var existingMapping = await results.ReadNextAsync();
					var firstResult = existingMapping?.FirstOrDefault();

					if (firstResult != null)
					{
						return new OkObjectResult($"{firstResult.ShortUrl}");
					}
				}

				var client = new RestClient(rebrandlyUrl);
				var request = new RestRequest("/links")
				{
					Method = Method.POST
				};
				request.AddHeader("Content-Type", "application/json");
				request.AddHeader("apikey", apiKey.Value.Value);
				request.AddHeader("workspace", workspaceKey.Value.Value);
				request.AddJsonBody($@"{{
                    ""destination"": ""{destination}"",
                    ""domain"":{{
                        ""fullName"": ""{rebrandlyDomain}""
                    }}
                }}");

				var response = await client.ExecutePostAsync(request);
				dynamic content = JsonConvert.DeserializeObject(response.Content);

				var mapping = new Mapping
				{
					DestinationUrl = destination,
					ShortUrl = content.shortUrl
				};

				var result = await container.CreateItemAsync(mapping, new PartitionKey(mapping.DestinationUrl));

				log.LogInformation(
					$@"Created Document: Destination = {result.Resource.DestinationUrl}, 
					Short = {result.Resource.ShortUrl}");

				return new OkObjectResult($"{content.shortUrl}");
			}
			catch (Exception ex)
			{
				return new BadRequestObjectResult(ex.Message);
			}
		}
		
		public string GetSetting(string name)
		{
			return Environment.GetEnvironmentVariable(name);
		}
	}
}