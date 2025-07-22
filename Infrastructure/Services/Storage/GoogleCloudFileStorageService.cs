using Domain.Configuration;
using Domain.Interfaces.Registration;
using Domain.Interfaces.Storage;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Storage
{
    public class GoogleCloudFileStorageService : IFileStorageService, ISingletonService
    {
        private readonly StorageClient _client;
        private readonly string _bucketName;
        public GoogleCloudFileStorageService(IOptions<GoogleCloudOptions> options)
        {
            var opts = options.Value;
            _bucketName = opts.BucketName ?? throw new InvalidOperationException("BucketName no configurado");
            var projectId = opts.ProjectId ?? throw new InvalidOperationException("ProjectId no configurado");
            var credentialsPath = opts.CredentialsPath;

            try
            {
                // Si existe la variable de entorno GOOGLE_APPLICATION_CREDENTIALS, úsala
                var envCreds = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                
                if (!string.IsNullOrEmpty(envCreds) && File.Exists(envCreds))
                {
                    _client = StorageClient.Create();
                    return;
                }
                
                // Si tenemos credenciales en configuración, usarlas
                if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
                {
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
                    _client = StorageClient.Create();
                    return;
                }

                // Intento final - credenciales por defecto (ej: en GCP)
                _client = StorageClient.Create();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al inicializar Google Cloud Storage: {ex.Message}", ex);
            }
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
        {
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            await _client.UploadObjectAsync(_bucketName, uniqueFileName, null, fileStream, cancellationToken: cancellationToken);
            return uniqueFileName;
        }

        public async Task<Stream> GetFileAsync(string fileName, CancellationToken cancellationToken)
        {
            var memory = new MemoryStream();
            await _client.DownloadObjectAsync(_bucketName, fileName, memory, cancellationToken: cancellationToken);
            memory.Position = 0;
            return memory;
        }

        public async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken)
        {
            await _client.DeleteObjectAsync(_bucketName, fileName, cancellationToken: cancellationToken);
        }
    }
}