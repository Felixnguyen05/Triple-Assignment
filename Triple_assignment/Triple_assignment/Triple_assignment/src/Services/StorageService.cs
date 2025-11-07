using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class StorageService
{
    private readonly BlobContainerClient _containerClient;

    // Accepts either a full connection string or a SAS URL
    public StorageService(string blobContainerSasUrl)
    {
        _containerClient = new BlobContainerClient(new Uri(blobContainerSasUrl));
    }

    // Uploads a stream to Blob Storage with specified content type, overwriting if it exists.
    public async Task UploadStreamAsync(string blobPath, Stream stream, string contentType = "application/octet-stream")
    {
        var blob = _containerClient.GetBlobClient(blobPath);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };
        await blob.UploadAsync(stream, options);
    }

    // Uploads text to Blob Storage as UTF-8, overwriting if it exists.
    public async Task UploadTextAsync(string blobPath, string text)
    {
        var blob = _containerClient.GetBlobClient(blobPath);
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
        var blob = _containerClient.GetBlobClient(blobPath);
        if (!await blob.ExistsAsync())
            return "{}";
        var download = await blob.DownloadContentAsync();
        return Encoding.UTF8.GetString(download.Value.Content.ToArray());
    }

    // Gets all blob URLs under a specific job folder.
    public async Task<IEnumerable<string>> GetImagesForJobAsync(string processId)
    {
        var urls = new List<string>();
        await foreach (var blob in _containerClient.GetBlobsAsync(prefix: $"{processId}/"))
        {
            var uri = _containerClient.GetBlobClient(blob.Name).Uri.ToString();
            urls.Add(uri);
        }
        return urls;
    }
}
