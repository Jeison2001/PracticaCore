using Application.Shared.DTOs.TypeTeachingAssignments;
using Domain.Entities;

namespace Tests.Integration.TypeTeachingAssignments
{
    public class TypeTeachingAssignmentControllerTests : GenericControllerIntegrationTests<TypeTeachingAssignment, TypeTeachingAssignmentDto>
    {
        public TypeTeachingAssignmentControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/TypeTeachingAssignment";

        protected override TypeTeachingAssignmentDto CreateValidDto()
        {
            return new TypeTeachingAssignmentDto
            {
                Code = $"TTA_{Guid.NewGuid().ToString().Substring(0, 5)}",
                Name = "Type Teaching Assignment Dto",
                Description = "Test Description",
                MaxAssignments = 5,
                StatusRegister = true
            };
        }

        protected override TypeTeachingAssignment CreateValidEntity()
        {
            return new TypeTeachingAssignment
            {
                Code = $"TTA_{Guid.NewGuid().ToString().Substring(0, 5)}",
                Name = "Type Teaching Assignment Entity",
                Description = "Test Description",
                MaxAssignments = 5,
                StatusRegister = true
            };
        }
    }
}