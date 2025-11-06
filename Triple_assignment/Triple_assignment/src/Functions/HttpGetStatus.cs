using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace AzureWeatherImages.Functions
{
    public static class JobTracker
    {
        // Thread-safe dictionary for job statuses
        private static readonly ConcurrentDictionary<string, string> _jobs
            = new ConcurrentDictionary<string, string>();

        public static void SetStatus(string jobId, string status)
        {
            _jobs[jobId] = status;
        }

        public static string GetStatus(string jobId)
        {
            return _jobs.TryGetValue(jobId, out var status) ? status : "unknown";
        }
    }
    public class HttpGetStatus
    {
        private readonly ILogger<HttpGetStatus> _logger;

        public HttpGetStatus(ILogger<HttpGetStatus> logger)
        {
            _logger = logger;
        }

        [Function("HttpGetStatus")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/{jobId}")] HttpRequestData req,
            string jobId)
        {
            _logger.LogInformation($"Checking status for job {jobId}...");

            // TODO: Replace with actual job tracking (maybe from Table Storage)
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Job {jobId} status: completed");
            return response;
        }
    }
}
