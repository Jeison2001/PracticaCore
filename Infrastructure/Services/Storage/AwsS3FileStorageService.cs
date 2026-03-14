using Domain.Interfaces.Common;
using Domain.Interfaces.Services.Storage;

namespace Infrastructure.Services.Storage
{
    public class AwsS3FileStorageService : IFileStorageService, ISingletonService
    {
        public Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public Task<Stream> GetFileAsync(string fileName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public Task DeleteFileAsync(string fileName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}