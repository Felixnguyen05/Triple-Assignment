using System;
using System.Threading.Tasks;
using System.Net;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

public class HttpStartFunction
{
    private readonly string _queueConn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")!;
    private readonly string _startQueueName = Environment.GetEnvironmentVariable("START_QUEUE") ?? "start-job-queue";

    [Function("StartImages")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "start")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.Accepted);
        // create process id
        var processId = Guid.NewGuid().ToString();

        var payload = new { processId, startedAt = DateTime.UtcNow };

        // enqueue a start job with processId
        var qClient = new QueueClient(_queueConn, _startQueueName);
        await qClient.CreateIfNotExistsAsync();
        await qClient.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));

        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { processId }));
        return response;
    }
}
