using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class HttpStartFunction
{
    private readonly string _tableConn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")!;
    private readonly string _tableName = "JobStatus";
    private readonly string _authUsername = Environment.GetEnvironmentVariable("API_USERNAME") ?? "user";
    private readonly string _authPassword = Environment.GetEnvironmentVariable("API_PASSWORD") ?? "pass";

    [Function("StartImages")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "start")] HttpRequestData req)
    {
        var response = req.CreateResponse();

        // --- Basic Authentication ---
        if (!req.Headers.TryGetValues("Authorization", out var authHeaders) ||
            !ValidateBasicAuth(authHeaders))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            response.Headers.Add("WWW-Authenticate", "Basic");
            await response.WriteStringAsync("Unauthorized");
            return response;
        }

        // --- Create a new process/job ID ---
        var processId = Guid.NewGuid().ToString();

        // --- Save initial status to Table Storage ---
        var tableClient = new TableClient(_tableConn, _tableName);
        await tableClient.CreateIfNotExistsAsync();
        var entity = new TableEntity(processId, "status")
        {
            { "Status", "Started" },
            { "StartedAt", DateTime.UtcNow }
        };
        await tableClient.AddEntityAsync(entity);

        // --- Return response ---
        response.StatusCode = HttpStatusCode.Accepted;
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { processId }));

        return response;
    }

    private bool ValidateBasicAuth(System.Collections.Generic.IEnumerable<string> headers)
    {
        foreach (var header in headers)
        {
            if (header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                var encoded = header.Substring("Basic ".Length).Trim();
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var parts = decoded.Split(':');
                if (parts.Length == 2 &&
                    parts[0] == _authUsername &&
                    parts[1] == _authPassword)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
