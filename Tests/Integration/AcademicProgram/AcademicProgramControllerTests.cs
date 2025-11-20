using Application.Shared.DTOs.AcademicProgram;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.AcademicPrograms
{
    public class AcademicProgramControllerTests : GenericControllerIntegrationTests<Domain.Entities.AcademicProgram, AcademicProgramDto>
    {
        private int _facultyId;

        public AcademicProgramControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/AcademicProgram";

        protected override AcademicProgramDto CreateValidDto()
        {
            return new AcademicProgramDto
            {
                Name = "Software Engineering",
                Code = $"SE_{Guid.NewGuid().ToString().Substring(0, 8)}",
                IdFaculty = _facultyId,
                StatusRegister = true
            };
        }

        protected override Domain.Entities.AcademicProgram CreateValidEntity()
        {
            return new Domain.Entities.AcademicProgram
            {
                Name = "Software Engineering",
                Code = $"SE_{Guid.NewGuid().ToString().Substring(0, 8)}",
                IdFaculty = _facultyId,
                StatusRegister = true
            };
        }

        protected override void SeedAdditionalData(Domain.Entities.AcademicProgram entity)
        {
            _facultyId = SeedFacultyAsync().GetAwaiter().GetResult();
            entity.IdFaculty = _facultyId;
        }

        private async Task<int> SeedFacultyAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                var faculty = new Domain.Entities.Faculty
                {
                    Name = "Engineering " + Guid.NewGuid(),
                    Code = "ENG_" + Guid.NewGuid().ToString().Substring(0, 8),
                    StatusRegister = true
                };
                context.Set<Domain.Entities.Faculty>().Add(faculty);
                await context.SaveChangesAsync();
                return faculty.Id;
            }
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            _facultyId = await SeedFacultyAsync();
            await base.Create_ReturnsCreated();
        }
    }
}
