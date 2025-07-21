using Domain.Interfaces.Registration;
using Domain.Interfaces.Storage;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Storage
{
    public class GoogleCloudOptions
    {
        public string BucketName { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string? CredentialsPath { get; set; }
    }

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
                Console.WriteLine($"Variable GOOGLE_APPLICATION_CREDENTIALS: {envCreds ?? "No configurada"}");
                
                if (!string.IsNullOrEmpty(envCreds))
                {
                    Console.WriteLine($"Verificando archivo en: {envCreds}");
                    bool fileExists = File.Exists(envCreds);
                    Console.WriteLine($"¿Archivo existe según File.Exists?: {fileExists}");
                    
                    try
                    {
                        _client = StorageClient.Create();
                        Console.WriteLine("Usando credenciales de variable de entorno GOOGLE_APPLICATION_CREDENTIALS");
                        Console.WriteLine($"Google Cloud Storage inicializado correctamente: Bucket={_bucketName}, Project={projectId}");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al crear StorageClient con credenciales de variable de entorno: {ex.Message}");
                    }
                }
                
                if (!string.IsNullOrEmpty(credentialsPath))
                {
                    Console.WriteLine($"Intentando usar credenciales de appsettings: {credentialsPath}");
                    bool configFileExists = File.Exists(credentialsPath);
                    Console.WriteLine($"¿Archivo de credenciales en appsettings existe?: {configFileExists}");
                    
                    if (configFileExists)
                    {
                        // Si tenemos una ruta de credenciales específica, establecerla como variable de entorno
                        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
                        try
                        {
                            _client = StorageClient.Create();
                            Console.WriteLine($"Usando credenciales de archivo: {credentialsPath}");
                            Console.WriteLine($"Google Cloud Storage inicializado correctamente: Bucket={_bucketName}, Project={projectId}");
                            return;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al crear StorageClient con credenciales de archivo: {ex.Message}");
                        }
                    }
                }

                // Intento final - Tal vez las credenciales están disponibles de otra manera (por ejemplo, en GCP)
                try
                {
                    Console.WriteLine("Intentando inicializar StorageClient sin especificar credenciales explícitas...");
                    _client = StorageClient.Create();
                    Console.WriteLine($"Google Cloud Storage inicializado correctamente: Bucket={_bucketName}, Project={projectId}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en el intento final: {ex.Message}");
                }
                
                throw new InvalidOperationException("No se encontró la ruta de credenciales de Google Cloud Storage.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar Google Cloud Storage: {ex.Message}");
                throw;
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