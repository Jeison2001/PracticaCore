using Domain.Configuration;
using Domain.Interfaces.Registration;
using Domain.Interfaces.Storage;
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
            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory, cancellationToken);
            }
            memory.Position = 0;
            return memory;
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