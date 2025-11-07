using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AzureWeatherImages.Functions
{
    public class HttpGetStatus
    {
        private readonly ILogger<HttpGetStatus> _logger;
        private readonly string _tableConn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")!;
        private readonly string _tableName = Environment.GetEnvironmentVariable("STATUS_TABLE") ?? "JobStatus";
        private readonly string _authUsername = Environment.GetEnvironmentVariable("API_USERNAME") ?? "user";
        private readonly string _authPassword = Environment.GetEnvironmentVariable("API_PASSWORD") ?? "pass";

        public HttpGetStatus(ILogger<HttpGetStatus> logger)
        {
            _logger = logger;
        }

        [Function("HttpGetStatus")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/{jobId}")] HttpRequestData req,
            string jobId)
        {
            // Validate Basic Auth
            if (!ValidateBasicAuth(req))
            {
                var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                unauthorized.Headers.Add("WWW-Authenticate", "Basic realm=\"Access to job status\"");
                return unauthorized;
            }

            _logger.LogInformation($"Fetching status for job {jobId} from Table Storage...");

            var response = req.CreateResponse(HttpStatusCode.OK);

            try
            {
                var tableClient = new TableClient(_tableConn, _tableName);
                await tableClient.CreateIfNotExistsAsync();

                // PartitionKey can be e.g., "Job" and RowKey = jobId
                var entityResponse = await tableClient.GetEntityAsync<TableEntity>("Job", jobId);
                var status = entityResponse.Value.GetString("Status") ?? "unknown";

                await response.WriteStringAsync($"Job {jobId} status: {status}");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                await response.WriteStringAsync($"Job {jobId} not found");
            }

            return response;
        }

        private bool ValidateBasicAuth(HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
                return false;

            var authHeader = System.Linq.Enumerable.FirstOrDefault(authHeaders);
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
                return false;

            var encoded = authHeader.Substring("Basic ".Length).Trim();
            var decoded = Encoding.UTF8.GetString(System.Convert.FromBase64String(encoded));
            var parts = decoded.Split(':');

            if (parts.Length != 2) return false;

            return parts[0] == _authUsername && parts[1] == _authPassword;
        }
    }
}
