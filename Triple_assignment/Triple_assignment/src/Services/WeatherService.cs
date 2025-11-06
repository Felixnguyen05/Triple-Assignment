using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

public class WeatherService
{
    private readonly HttpClient _http = new HttpClient();

    public async Task<List<(string stationId, string stationName, double lat, double lon)>> GetStationsAsync(int max = 50)
    {
        var resp = await _http.GetStringAsync("https://data.buienradar.nl/2.0/feed/json");
        using var doc = JsonDocument.Parse(resp);

        var stations = new List<(string, string, double, double)>();

        if (doc.RootElement.TryGetProperty("actual", out var actual) &&
            actual.TryGetProperty("stationmeasurements", out var sm))
        {
            foreach (var el in sm.EnumerateArray().Take(max))
            {
                try
                {
                    var id = el.GetProperty("stationid").GetInt32().ToString();
                    var name = el.GetProperty("stationname").GetString() ?? "unknown";
                    var lat = el.GetProperty("lat").GetDouble();
                    var lon = el.GetProperty("lon").GetDouble();
                    stations.Add((id, name, lat, lon));
                }
                catch
                {
                    // skip any malformed station
                }
            }
        }

        return stations;
    }

    public async Task<string> GetWeatherForStationAsync(string stationId)
    {
        var resp = await _http.GetStringAsync("https://data.buienradar.nl/2.0/feed/json");
        using var doc = JsonDocument.Parse(resp);

        if (doc.RootElement.TryGetProperty("actual", out var actual) &&
            actual.TryGetProperty("stationmeasurements", out var sm))
        {
            foreach (var el in sm.EnumerateArray())
            {
                if (el.GetProperty("stationid").GetInt32().ToString() == stationId)
                {
                    var temp = el.TryGetProperty("temperature", out var t) && t.ValueKind == JsonValueKind.Number
                        ? t.GetDouble() : double.NaN;

                    var humidity = el.TryGetProperty("humidity", out var h) && h.ValueKind == JsonValueKind.Number
                        ? h.GetDouble() : double.NaN;

                    return $"Temp: {temp}°C, Humidity: {humidity}%";
                }
            }
        }

        return "No data";
    }
}
