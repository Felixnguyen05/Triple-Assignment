using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class StartJobQueueProcessor
{
    private readonly WeatherService _weather;
    private readonly string _queueConn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")!;
    private readonly string _imageQueueName = Environment.GetEnvironmentVariable("IMAGE_QUEUE") ?? "image-job-queue";
    private readonly StorageService _storage;

    public StartJobQueueProcessor(WeatherService weather, StorageService storage)
    {
        _weather = weather;
        _storage = storage;
    }

    [Function("ProcessStartJob")]
    public async Task Run([QueueTrigger("%START_QUEUE%")] string queueMessage, FunctionContext context)
    {
        var logger = context.GetLogger("ProcessStartJob");
        logger.LogInformation("Start job message received.");

        // Decode Base64 or fallback to plain JSON
        byte[] data;
        try
        {
            data = Convert.FromBase64String(queueMessage);
        }
        catch (FormatException)
        {
            data = Encoding.UTF8.GetBytes(queueMessage);
        }

        // Parse JSON into JsonDocument
        var jsonText = Encoding.UTF8.GetString(data);
        var startObj = JsonDocument.Parse(jsonText).RootElement;

        // Extract processId
        var processId = startObj.GetProperty("processId").GetString();

        if (string.IsNullOrEmpty(processId))
        {
            logger.LogError("No processId in start-job queue message!");
            return;
        }

        // Fetch up to 50 weather stations
        var stations = await _weather.GetStationsAsync(50);

        // Queue client for image jobs
        var qClient = new QueueClient(_queueConn, _imageQueueName);
        await qClient.CreateIfNotExistsAsync();

        int counter = 0;

        // Enqueue one job per weather station
        foreach (var st in stations)
        {
            var job = new
            {
                processId,
                stationId = st.stationId,
                stationName = st.stationName,
                lat = st.lat,
                lon = st.lon
            };

            var jobJson = JsonSerializer.Serialize(job);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(jobJson));

            await qClient.SendMessageAsync(base64);
            counter++;
        }

        // Initial status.json
        var status = new
        {
            processId,
            total = counter,
            completed = 0,
            startedAt = DateTime.UtcNow
        };

        await _storage.UploadTextAsync($"images/{processId}/status.json", JsonSerializer.Serialize(status));

        logger.LogInformation($"✅ Enqueued {counter} image jobs for process {processId}");
    }
}
