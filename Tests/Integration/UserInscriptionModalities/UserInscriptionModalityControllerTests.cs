using Application.Shared.DTOs.UserInscriptionModalities;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.UserInscriptionModalities
{
    public class UserInscriptionModalityControllerTests : GenericControllerIntegrationTests<UserInscriptionModality, UserInscriptionModalityDto>
    {
        public UserInscriptionModalityControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/UserInscriptionModality";

        protected override void SeedAdditionalData(UserInscriptionModality entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                // Seed User dependencies
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

                // Seed User
                var user = new User 
                { 
                    FirstName = "Test Student", 
                    LastName = "Test", 
                    Email = $"student_{Guid.NewGuid()}@test.com", 
                    Identification = Guid.NewGuid().ToString().Substring(0, 10),
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true 
                };
                context.Set<User>().Add(user);

                // Seed InscriptionModality dependencies
                // Modality, State, Period should be unique per test run or reused?
                // Let's make them unique per test run to be safe, as they are not "Master Data" in the same sense as Faculty/Program.
                // But wait, Modality and State ARE master data.
                
                var modality = context.Set<Modality>().FirstOrDefault(x => x.Name == "Modality_Test");
                if (modality == null)
                {
                    modality = new Modality { Name = "Modality_Test", Description = "Test", StatusRegister = true };
                    context.Set<Modality>().Add(modality);
                }

                var state = context.Set<StateInscription>().FirstOrDefault(x => x.Name == "State_Test");
                if (state == null)
                {
                    state = new StateInscription { Name = "State_Test", Description = "Test", StatusRegister = true };
                    context.Set<StateInscription>().Add(state);
                }
                
                // Period should probably be unique or reused?
                var period = context.Set<AcademicPeriod>().FirstOrDefault(x => x.Code == "PER_Test");
                if (period == null)
                {
                    period = new AcademicPeriod { Code = "PER_Test", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true };
                    context.Set<AcademicPeriod>().Add(period);
                }
                context.SaveChanges();

                var inscriptionModality = new InscriptionModality 
                { 
                    IdModality = modality.Id,
                    IdStateInscription = state.Id,
                    IdAcademicPeriod = period.Id,
                    StatusRegister = true
                };
                context.Set<InscriptionModality>().Add(inscriptionModality);
                context.SaveChanges();

                entity.IdUser = user.Id;
                entity.IdInscriptionModality = inscriptionModality.Id;
            }
        }

        protected override UserInscriptionModalityDto CreateValidDto()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                // Seed User dependencies
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

                // Seed User
                var user = new User 
                { 
                    FirstName = "Test Student Dto", 
                    LastName = "Test", 
                    Email = $"student_dto_{Guid.NewGuid()}@test.com", 
                    Identification = Guid.NewGuid().ToString().Substring(0, 10),
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true 
                };
                context.Set<User>().Add(user);

                // Seed InscriptionModality dependencies
                var modality = context.Set<Modality>().FirstOrDefault(x => x.Name == "Modality_Dto");
                if (modality == null)
                {
                    modality = new Modality { Name = "Modality_Dto", Description = "Test", StatusRegister = true };
                    context.Set<Modality>().Add(modality);
                }

                var state = context.Set<StateInscription>().FirstOrDefault(x => x.Name == "State_Dto");
                if (state == null)
                {
                    state = new StateInscription { Name = "State_Dto", Description = "Test", StatusRegister = true };
                    context.Set<StateInscription>().Add(state);
                }

                var period = context.Set<AcademicPeriod>().FirstOrDefault(x => x.Code == "PER_Dto");
                if (period == null)
                {
                    period = new AcademicPeriod { Code = "PER_Dto", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true };
                    context.Set<AcademicPeriod>().Add(period);
                }
                context.SaveChanges();

                var inscriptionModality = new InscriptionModality 
                { 
                    IdModality = modality.Id,
                    IdStateInscription = state.Id,
                    IdAcademicPeriod = period.Id,
                    StatusRegister = true
                };
                context.Set<InscriptionModality>().Add(inscriptionModality);
                context.SaveChanges();

                return new UserInscriptionModalityDto
                {
                    IdUser = user.Id,
                    IdInscriptionModality = inscriptionModality.Id,
                    UserName = "Test Student Dto",
                    Identification = user.Identification,
                    Email = user.Email,
                    CurrentAcademicPeriod = "2024-1",
                    StatusRegister = true
                };
            }
        }

        protected override UserInscriptionModality CreateValidEntity()
        {
            return new UserInscriptionModality
            {
                StatusRegister = true
                // IDs set in SeedAdditionalData
            };
        }
    }
}
