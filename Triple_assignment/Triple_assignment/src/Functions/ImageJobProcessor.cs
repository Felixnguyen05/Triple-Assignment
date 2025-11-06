using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public class ImageJobProcessor
{
    private readonly ImageService _imgSvc;
    private readonly WeatherService _weather;
    private readonly StorageService _storage;

    // constructor
    public ImageJobProcessor(ImageService imgSvc, WeatherService weather, StorageService storage)
    {
        _imgSvc = imgSvc;
        _weather = weather;
        _storage = storage;
    }

    [Function("ProcessImageJob")]
    public async Task Run([QueueTrigger("%IMAGE_QUEUE%")] string queueMessage, FunctionContext context)
    {
        var logger = context.GetLogger("ProcessImageJob");

        string jobJson;

        // Try decoding Base64, fallback to plain JSON
        try
        {
            var data = Convert.FromBase64String(queueMessage);
            jobJson = System.Text.Encoding.UTF8.GetString(data);
        }
        catch (FormatException)
        {
            jobJson = queueMessage;
        }

        var jobObj = JsonSerializer.Deserialize<JsonElement>(jobJson);

        var processId = jobObj.GetProperty("processId").GetString()!;
        var stationId = jobObj.GetProperty("stationId").GetString()!;
        var stationName = jobObj.GetProperty("stationName").GetString()!;

        // get current weather for this station
        var stationWeather = await _weather.GetWeatherForStationAsync(stationId);

        // fetch public image
        using var baseImage = await _imgSvc.FetchImageAsync(stationName);

        // draw text with weather info
        var annotated = ImageHelper.DrawWeatherOnImage(baseImage, stationName, stationWeather);

        // save to stream and upload
        using var ms = new System.IO.MemoryStream();
        await annotated.SaveAsPngAsync(ms);
        ms.Position = 0;

        var blobPath = $"{processId}/{stationId}.png";
        await _storage.UploadStreamAsync(blobPath, ms, contentType: "image/png");

        logger.LogInformation($"Uploaded {blobPath}");

        // update status
        var statusText = await _storage.DownloadTextAsync($"images/{processId}/status.json");
        var status = JsonSerializer.Deserialize<JsonElement>(statusText);
        var total = status.GetProperty("total").GetInt32();
        var completed = status.GetProperty("completed").GetInt32();
        completed++;
        var newStatus = new { processId, total, completed, updatedAt = DateTime.UtcNow };
        await _storage.UploadTextAsync($"images/{processId}/status.json", JsonSerializer.Serialize(newStatus));
    }

}
