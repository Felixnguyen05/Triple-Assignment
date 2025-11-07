using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AzureWeatherImages.Functions
{
    public class HttpGetImages
    {
        private readonly ILogger<HttpGetImages> _logger;
        private readonly StorageService _storageService;
        private readonly string _authUsername = Environment.GetEnvironmentVariable("API_USERNAME") ?? "user";
        private readonly string _authPassword = Environment.GetEnvironmentVariable("API_PASSWORD") ?? "pass";

        public HttpGetImages(ILogger<HttpGetImages> logger, StorageService storageService)
        {
            _logger = logger;
            _storageService = storageService;
        }

        [Function("HttpGetImages")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "images/{jobId}")] HttpRequestData req,
            string jobId)
        {
            // Validate Basic Auth
            if (!req.Headers.TryGetValues("Authorization", out var authHeaders) ||
                !ValidateBasicAuth(authHeaders))
            {
                var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                unauthorized.Headers.Add("WWW-Authenticate", "Basic");
                await unauthorized.WriteStringAsync("Unauthorized");
                return unauthorized;
            }

            _logger.LogInformation($"Fetching images for job {jobId}...");

            var imageUrls = await _storageService.GetImagesForJobAsync(jobId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(imageUrls);
            return response;
        }

        private bool ValidateBasicAuth(System.Collections.Generic.IEnumerable<string> headers)
        {
            foreach (var header in headers)
            {
                if (header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    var encoded = header.Substring("Basic ".Length).Trim();
                    var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
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
}
