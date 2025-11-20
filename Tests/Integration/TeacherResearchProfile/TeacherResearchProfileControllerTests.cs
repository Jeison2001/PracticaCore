using Application.Shared.DTOs.TeacherResearchProfile;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration;

namespace Tests.Integration.TeacherResearchProfiles
{
    public class TeacherResearchProfileControllerTests : GenericControllerIntegrationTests<Domain.Entities.TeacherResearchProfile, TeacherResearchProfileDto>
    {
        public TeacherResearchProfileControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        // Skip GetById test because TeacherResearchProfileController has [NonAction] on GetById
        public override Task GetById_ReturnsOkAndEntity()
        {
            // This endpoint is disabled in the controller, so we skip this test
            return Task.CompletedTask;
        }

        protected override string BaseUrl => "/api/TeacherResearchProfile";

        protected override void SeedAdditionalData(Domain.Entities.TeacherResearchProfile entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var idType = context.Set<IdentificationType>().FirstOrDefault(x => x.Code == "CC");
                if (idType == null)
                {
                    idType = new IdentificationType { Code = "CC", Name = "Cedula", Description = "Cedula", StatusRegister = true };
                    context.Set<IdentificationType>().Add(idType);
                }

                var faculty = context.Set<Faculty>().FirstOrDefault(x => x.Code == "ING");
                if (faculty == null)
                {
                    faculty = new Faculty { Name = "Ingenieria", Code = "ING", StatusRegister = true };
                    context.Set<Faculty>().Add(faculty);
                }
                context.SaveChanges();

                var program = context.Set<AcademicProgram>().FirstOrDefault(x => x.Code == "SIS");
                if (program == null)
                {
                    program = new AcademicProgram { Name = "Sistemas", Code = "SIS", IdFaculty = faculty.Id, StatusRegister = true };
                    context.Set<AcademicProgram>().Add(program);
                    context.SaveChanges();
                }

                var user = new User 
                { 
                    FirstName = "Test Teacher", 
                    LastName = "Test", 
                    Email = $"teacher_{Guid.NewGuid()}@test.com", 
                    Identification = Guid.NewGuid().ToString().Substring(0, 10),
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true 
                };
                
                var line = new ResearchLine 
                { 
                    Code = $"L_{Guid.NewGuid().ToString().Substring(0, 5)}", 
                    Name = "Line 1", 
                    StatusRegister = true 
                };

                context.Set<User>().Add(user);
                context.Set<ResearchLine>().Add(line);
                context.SaveChanges();

                entity.IdUser = user.Id;
                entity.IdResearchLine = line.Id;
            }
        }

        protected override TeacherResearchProfileDto CreateValidDto()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var idType = context.Set<IdentificationType>().FirstOrDefault(x => x.Code == "CC_Dto");
                if (idType == null)
                {
                    idType = new IdentificationType { Code = "CC_Dto", Name = "Cedula Dto", Description = "Cedula", StatusRegister = true };
                    context.Set<IdentificationType>().Add(idType);
                }

                var faculty = context.Set<Faculty>().FirstOrDefault(x => x.Code == "ING_D");
                if (faculty == null)
                {
                    faculty = new Faculty { Name = "Ingenieria Dto", Code = "ING_D", StatusRegister = true };
                    context.Set<Faculty>().Add(faculty);
                }
                context.SaveChanges();

                var program = context.Set<AcademicProgram>().FirstOrDefault(x => x.Code == "SIS_D");
                if (program == null)
                {
                    program = new AcademicProgram { Name = "Sistemas Dto", Code = "SIS_D", IdFaculty = faculty.Id, StatusRegister = true };
                    context.Set<AcademicProgram>().Add(program);
                    context.SaveChanges();
                }

                var user = new User 
                { 
                    FirstName = "Test Teacher Dto", 
                    LastName = "Test", 
                    Email = $"teacher_dto_{Guid.NewGuid()}@test.com", 
                    Identification = Guid.NewGuid().ToString().Substring(0, 10),
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true 
                };
                
                var line = new ResearchLine 
                { 
                    Code = $"L_Dto_{Guid.NewGuid().ToString().Substring(0, 5)}", 
                    Name = "Line Dto", 
                    StatusRegister = true 
                };

                context.Set<User>().Add(user);
                context.Set<ResearchLine>().Add(line);
                context.SaveChanges();

                return new TeacherResearchProfileDto
                {
                    IdUser = user.Id,
                    IdResearchLine = line.Id,
                    ProfileDescription = "Test Profile",
                    StatusRegister = true
                };
            }
        }

        protected override Domain.Entities.TeacherResearchProfile CreateValidEntity()
        {
            return new Domain.Entities.TeacherResearchProfile
            {
                ProfileDescription = "Test Profile",
                StatusRegister = true
            };
        }
    }
}
