using System;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReserverInfrastructure;

namespace OrderItemsReserver
{
    public class OrderReserverByMessage
    {
        [FunctionName("OrderReserverByMessage")]
        public void Run([ServiceBusTrigger("orderreservesqueue", AutoCompleteMessages = true, Connection = "ServiceBusConnectionString")]ReservedOrderModel orderModel, ILogger log)
        {
            var client = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnectionString"));
            ServiceBusSender sender = client.CreateSender("failedorders");

            log.LogInformation($"C# ServiceBus queue trigger function processed message: {orderModel}");

            try
            {
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient("orders");

                var result = blobContainerClient.UploadBlob($"{orderModel.id}.json", BinaryData.FromString(JsonConvert.SerializeObject(orderModel)));

                if (result.GetRawResponse().IsError)
                {
                    throw new Exception($"Order ID: {orderModel.id} Reason: {result.GetRawResponse().ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                var message = new ServiceBusMessage($"The system has failed to reserve the following order: {JsonConvert.SerializeObject(orderModel)}");
                sender.SendMessageAsync(message);
            }
        }
    }
}
