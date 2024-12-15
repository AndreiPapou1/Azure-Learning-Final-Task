using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using ReserverInfrastructure;

namespace DeliveryOrderProcessor
{
    public static class ReserveDeliveryOrder
    {
        [FunctionName("ReserveDeliveryOrder")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a ReserveDeliveryOrder request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            ReservedOrderModel orderModel = JsonConvert.DeserializeObject<ReservedOrderModel>(requestBody);

            try
            {
                var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");
                CosmosClient cosmosClient = new CosmosClient(connectionString: connectionString);

                Database deliveriesDb = await cosmosClient.CreateDatabaseIfNotExistsAsync("Deliveries");

                Container deliveriesContainer = deliveriesDb.GetContainer("Orders");

                var response = await deliveriesContainer.CreateItemAsync<ReservedOrderModel>(orderModel);

                return new OkObjectResult($"order #{orderModel.id} has been successfully delivered");

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }
    }
}
