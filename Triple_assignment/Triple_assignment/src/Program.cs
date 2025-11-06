using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults() // Enable Azure Functions isolated worker
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables(); // Load environment variables
    })
    .ConfigureServices((context, services) =>
    {
        // Register your services
        services.AddHttpClient<ImageService>(); // Registers HttpClient for ImageService
        services.AddSingleton<WeatherService>(); // No HttpClient needed
        services.AddSingleton<StorageService>(); // StorageService with BlobServiceClient internally
    })
    .Build();

host.Run();
