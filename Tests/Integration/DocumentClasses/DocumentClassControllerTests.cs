using Application.Shared.DTOs.DocumentsClasses;
using Domain.Entities;

namespace Tests.Integration.DocumentClasses
{
    public class DocumentClassControllerTests : GenericControllerIntegrationTests<DocumentClass, DocumentClassDto>
    {
        public DocumentClassControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/DocumentClass";

        protected override DocumentClass CreateValidEntity()
        {
            return new DocumentClass
            {
                Code = "DC_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Document Class",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow,
                StatusRegister = true,
                OperationRegister = "INSERT"
            };
        }

        protected override DocumentClassDto CreateValidDto()
        {
            return new DocumentClassDto
            {
                Code = "DC_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Document Class DTO",
                Description = "Test Description DTO",
                StatusRegister = true
            };
        }
    }
}