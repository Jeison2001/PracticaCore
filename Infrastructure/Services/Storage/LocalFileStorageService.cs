using Domain.Configuration;
using Domain.Interfaces.Common;
using Domain.Interfaces.Services.Storage;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Storage
{
    public class LocalFileStorageService : IFileStorageService, ISingletonService
    {
        private readonly string _basePath;
        
        public LocalFileStorageService(IOptions<LocalStorageOptions> options)
        {
            _basePath = options.Value.BasePath ?? "Uploads";
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
        {
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var filePath = Path.Combine(_basePath, uniqueFileName);
            Directory.CreateDirectory(_basePath);
            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(file, cancellationToken);
            }
            return uniqueFileName;
        }

        public async Task<Stream> GetFileAsync(string fileName, CancellationToken cancellationToken)
        {
            var filePath = Path.Combine(_basePath, fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Archivo no encontrado: {fileName}");
                
            // Retorna un FileStream directo (Asíncrono) para que ASP.NET Core lo envíe
            return await Task.FromResult(new FileStream(
                filePath, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.Read, 
                4096, 
                FileOptions.Asynchronous));
        }

        public Task DeleteFileAsync(string fileName, CancellationToken cancellationToken)
        {
            var filePath = Path.Combine(_basePath, fileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
            return Task.CompletedTask;
        }
    }
}