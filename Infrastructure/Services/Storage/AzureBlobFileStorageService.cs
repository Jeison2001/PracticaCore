using Domain.Interfaces;
using Domain.Interfaces.Registration;

namespace Infrastructure.Services.Storage
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Microsoft.Extensions.Configuration;

    public class AzureBlobFileStorageService : IFileStorageService, ISingletonService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly string _baseUrl;

        public AzureBlobFileStorageService(IConfiguration configuration)
        {
            var azureConfig = configuration.GetSection("FileStorage:AzureBlob");
            var connectionString = azureConfig["ConnectionString"];
            var containerName = azureConfig["ContainerName"];
            _baseUrl = azureConfig["BaseUrl"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(containerName))
                throw new InvalidOperationException("Azure Blob Storage configuration is missing or invalid.");
            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, overwrite: true, cancellationToken);
            return !string.IsNullOrEmpty(_baseUrl) ? $"{_baseUrl}/{fileName}" : blobClient.Uri.ToString();
        }

        public async Task<Stream> GetFileAsync(string fileName, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            var response = await blobClient.DownloadAsync(cancellationToken);
            return response.Value.Content;
        }

        public async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, cancellationToken);
        }
    }
}