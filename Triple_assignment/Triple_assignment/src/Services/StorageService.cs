using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class StorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "images";

    public StorageService()
    {
        string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")!;
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    // Uploads a stream to Blob Storage with specified content type, overwriting if it exists.
    public async Task UploadStreamAsync(string blobPath, Stream stream, string contentType = "application/octet-stream")
    {
        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobPath);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };
        // UploadAsync will overwrite by default if you call UploadAsync(stream, options)
        await blob.UploadAsync(stream, options);
    }

    // Uploads text to Blob Storage as UTF-8, overwriting if it exists.
    public async Task UploadTextAsync(string blobPath, string text)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobPath);
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
        };
        await blob.UploadAsync(ms, options);
    }

    // Downloads text content from blob
    public async Task<string> DownloadTextAsync(string blobPath)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blob = container.GetBlobClient(blobPath);
        if (!await blob.ExistsAsync())
            return "{}";
        var download = await blob.DownloadContentAsync();
        // Safer decode:
        return Encoding.UTF8.GetString(download.Value.Content.ToArray());
    }

    // Gets all blob URLs under a specific job folder.
    public async Task<IEnumerable<string>> GetImagesForJobAsync(string processId)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        if (!await container.ExistsAsync())
            return Array.Empty<string>();
        var urls = new List<string>();
        await foreach (var blob in container.GetBlobsAsync(prefix: $"{processId}/"))
        {
            var uri = container.GetBlobClient(blob.Name).Uri.ToString();
            urls.Add(uri);
        }
        return urls;
    }
}
