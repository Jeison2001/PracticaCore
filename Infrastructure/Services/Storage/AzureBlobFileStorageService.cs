using Domain.Configuration;
using Domain.Interfaces.Registration;

namespace Infrastructure.Services.Storage
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Domain.Interfaces.Storage;
    using Microsoft.Extensions.Options;

    public class AzureBlobFileStorageService : IFileStorageService, ISingletonService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly string _baseUrl;

        public AzureBlobFileStorageService(IOptions<AzureBlobOptions> options)
        {
            var opts = options.Value;
            var connectionString = opts.ConnectionString ?? throw new InvalidOperationException("ConnectionString no configurado");
            var containerName = opts.ContainerName ?? throw new InvalidOperationException("ContainerName no configurado");
            _baseUrl = opts.BaseUrl ?? string.Empty;
            
            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
        {
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var blobClient = _containerClient.GetBlobClient(uniqueFileName);
            await blobClient.UploadAsync(fileStream, overwrite: true, cancellationToken);
            return uniqueFileName;
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