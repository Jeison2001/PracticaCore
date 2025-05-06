using Domain.Interfaces;
using Domain.Interfaces.Registration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class AzureBlobFileStorageService : IFileStorageService, ISingletonService
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