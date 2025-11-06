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
            _logger.LogInformation($"Fetching images for job {jobId}...");

            var imageUrls = await _storageService.GetImagesForJobAsync(jobId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(imageUrls);
            return response;
        }
    }
}
