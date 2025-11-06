using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

public class ImageService
{
    private readonly HttpClient _http;
    private readonly string _unsplashKey;

    public ImageService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _unsplashKey = Environment.GetEnvironmentVariable("UNSPLASH_ACCESS_KEY")
                        ?? throw new InvalidOperationException("UNSPLASH_ACCESS_KEY is not set in environment variables.");
    }

    public async Task<Image> FetchImageAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query must not be empty", nameof(query));

        // Try Unsplash first
        try
        {
            var url = $"https://api.unsplash.com/photos/random?query={Uri.EscapeDataString(query)}&orientation=landscape&client_id={_unsplashKey}";
            using var resp = await _http.GetAsync(url);

            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var imgUrl = doc.RootElement.GetProperty("urls").GetProperty("regular").GetString();
                if (!string.IsNullOrEmpty(imgUrl))
                {
                    var imgBytes = await _http.GetByteArrayAsync(imgUrl);
                    return Image.Load(imgBytes);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch Unsplash image for '{query}'. Error: {ex.Message}");
        }

        // Fallback: create an in-memory image
        var fallbackImage = new Image<Rgba32>(1024, 768);

        // Load system font
        var collection = new FontCollection();
        var family = collection.AddSystemFonts().Get("Arial"); // choose a font installed on your system
        var font = new Font(family, 48);

        fallbackImage.Mutate(ctx =>
        {
            ctx.Fill(Color.LightGray)
               .DrawText("Weather", font, Color.Black, new PointF(250, 350));
        });

        return fallbackImage;
    }
}
