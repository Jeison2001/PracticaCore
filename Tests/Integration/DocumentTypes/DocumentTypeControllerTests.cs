using Application.Shared.DTOs.DocumentsTypes;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.DocumentTypes
{
    public class DocumentTypeControllerTests : GenericControllerIntegrationTests<DocumentType, DocumentTypeDto>
    {
        private int _documentClassId;

        public DocumentTypeControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/DocumentType";

        protected override void SeedAdditionalData(DocumentType entity)
        {
            _documentClassId = SeedDocumentClassAsync().GetAwaiter().GetResult();
            entity.IdDocumentClass = _documentClassId;
        }

        private async Task<int> SeedDocumentClassAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                var documentClass = new DocumentClass
                {
                    Code = "DC_" + Guid.NewGuid().ToString().Substring(0, 8),
                    Name = "Test Document Class",
                    Description = "Test Description",
                    CreatedAt = DateTime.UtcNow,
                    StatusRegister = true,
                    OperationRegister = "INSERT"
                };

                context.Set<DocumentClass>().Add(documentClass);
                await context.SaveChangesAsync();
                return documentClass.Id;
            }
        }

        protected override DocumentType CreateValidEntity()
        {
            return new DocumentType
            {
                IdDocumentClass = _documentClassId,
                Code = "DT_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Document Type",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow,
                StatusRegister = true,
                OperationRegister = "INSERT"
            };
        }

        protected override DocumentTypeDto CreateValidDto()
        {
            return new DocumentTypeDto
            {
                IdDocumentClass = _documentClassId,
                Code = "DT_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Document Type DTO",
                Description = "Test Description DTO",
                StatusRegister = true
            };
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            _documentClassId = await SeedDocumentClassAsync();
            await base.Create_ReturnsCreated();
        }
    }
}
