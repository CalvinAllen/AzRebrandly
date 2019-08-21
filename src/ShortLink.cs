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

namespace CalvinAAllen.AzRebrandly
{
    public static class ShortLink
    {
        [FunctionName("ShortLink")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            try
            {
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                var secret = await 
                    keyVaultClient
                    .GetSecretAsync("https://azfunctionkeyvault.vault.azure.net/secrets/RebrandlyWorkspaceKey")
                    .ConfigureAwait(false);

                return (ActionResult)new OkObjectResult($"{secret.Value}");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);

            //App Settings?
            // Rebrandly API Uri: https://api.rebrandly.com/v1/links
            // Domain: luv2.dev
            /*
                {
                    "destination": "https://www.calvinallen.net/sdjfnahdsfaf",
                    "domain":{
                        "fullName": "luv2.dev"
                    }
                }
            */


            //{
            //    "destination": "https://www.calvinallen.net/sdjfnahdsfaf";
            //}  
            //var destinationUrl = data?.destination;

            // [SKIP] Check our spreadsheet for the destination --> short mapping
            // [SKIP] If exists, return the short url from the spreadsheet
            // If doesn't exist, call Rebrandly to shorten it, store the result/mapping in the spreadsheet
            //      return the shorturl

            //return (ActionResult)new OkObjectResult($"{secret.}");
        }
    }
}
