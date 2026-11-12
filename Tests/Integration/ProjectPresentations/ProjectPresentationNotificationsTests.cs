using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.ProjectPresentations
{
    public class ProjectPresentationNotificationsTests : IntegrationTestBase
    {
        public ProjectPresentationNotificationsTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task BuildProjectPresentationEventDataAsync_FormatsDatesInColombiaTimeZone()
        {
            // Arrange
            int presentationId;
            using (var seedScope = _factory.Services.CreateScope())
            {
                var context = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
                SeedingUtilities.SeedCatalogs(context);

                var modality = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado);
                if (!context.Set<AcademicPeriod>().Any())
                {
                    context.Set<AcademicPeriod>().Add(new AcademicPeriod
                    {
                        Id = 1,
                        Code = "2026-1",
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(6),
                        StatusRegister = true,
                        OperationRegister = "Seed"
                    });
                    context.SaveChanges();
                }

                var period = context.Set<AcademicPeriod>().First();
                var approvedState = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado);
                var sustStage = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseSustentacion);
                var sustProgramada = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustProgramada);
                var propInitial = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PgPropuestaInicial);

                var rLine = context.Set<ResearchLine>().FirstOrDefault()
                    ?? context.Set<ResearchLine>().Add(new ResearchLine { Name = "Line TZ", Description = "Desc" }).Entity;
                context.SaveChanges();
                var subLine = context.Set<ResearchSubLine>().FirstOrDefault(s => s.IdResearchLine == rLine.Id)
                    ?? context.Set<ResearchSubLine>().Add(new ResearchSubLine { Name = "SubLine TZ", Description = "Desc", IdResearchLine = rLine.Id }).Entity;
                context.SaveChanges();

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdAcademicPeriod = period.Id,
                    IdStateInscription = approvedState.Id,
                    IdStageModality = sustStage.Id,
                    StatusRegister = true,
                    OperationRegister = "Seed"
                };
                context.Set<InscriptionModality>().Add(inscription);
                context.SaveChanges();

                context.Set<Proposal>().Add(new Proposal
                {
                    Id = inscription.Id,
                    Title = "Proyecto Zona Horaria Colombia",
                    GeneralObjective = "Obj",
                    SpecificObjectives = new List<string> { "Obj 1" },
                    IdResearchLine = rLine.Id,
                    IdResearchSubLine = subLine.Id,
                    IdStateStage = propInitial.Id,
                    StatusRegister = true
                });

                context.Set<ProjectPresentation>().Add(new ProjectPresentation
                {
                    Id = inscription.Id,
                    IdInscriptionModality = inscription.Id,
                    IdStateStage = sustProgramada.Id,
                    AttemptNumber = 1,
                    // 20:00 UTC -> 15:00 UTC-5 (Colombia)
                    ScheduledDateTime = new DateTimeOffset(2026, 11, 27, 20, 0, 0, TimeSpan.Zero),
                    // 01:30 UTC 28/11 -> 20:30 27/11 UTC-5 (Colombia)
                    ActualDateTime = new DateTimeOffset(2026, 11, 28, 1, 30, 0, TimeSpan.Zero),
                    PresentationModality = "VIRTUAL",
                    LocationOrLink = "https://meet.google.com/tz-test",
                    StatusRegister = true
                });
                context.SaveChanges();
                presentationId = inscription.Id;
            }

            // Act
            using var actScope = _factory.Services.CreateScope();
            var builder = actScope.ServiceProvider.GetRequiredService<IProjectPresentationEventDataBuilder>();
            var eventData = await builder.BuildProjectPresentationEventDataAsync(presentationId, "SUSTENTACION_SCHEDULED");

            // Assert
            eventData["ScheduledDateTime"].Should().Be("27/11/2026 15:00");
            eventData["ActualDateTime"].Should().Be("27/11/2026 20:30");
        }

        [Fact]
        public async Task ResolveRecipientsAsync_WhenRuleIsDirectorAssigned_ResolvesDirectorEmail()
        {
            // Arrange
            int inscriptionId;
            using (var seedScope = _factory.Services.CreateScope())
            {
                var context = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
                SeedingUtilities.SeedCatalogs(context);

                var directorType = context.Set<TypeTeachingAssignment>().FirstOrDefault(t => t.Code == "DIRECTOR")
                    ?? context.Set<TypeTeachingAssignment>().Add(new TypeTeachingAssignment
                    {
                        Code = "DIRECTOR",
                        Name = "Director",
                        StatusRegister = true,
                        OperationRegister = "Seed"
                    }).Entity;

                var directorUser = new User
                {
                    Email = "director.sust@unicesar.edu.co",
                    FirstName = "Ana",
                    LastName = "Directora",
                    Identification = "55443322",
                    StatusRegister = true,
                    OperationRegister = "Seed"
                };
                context.Set<User>().Add(directorUser);

                var modality = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado);
                if (!context.Set<AcademicPeriod>().Any())
                {
                    context.Set<AcademicPeriod>().Add(new AcademicPeriod
                    {
                        Id = 1,
                        Code = "2026-1",
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(6),
                        StatusRegister = true,
                        OperationRegister = "Seed"
                    });
                    context.SaveChanges();
                }

                var period = context.Set<AcademicPeriod>().First();
                var approvedState = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado);
                var sustStage = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseSustentacion);

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdAcademicPeriod = period.Id,
                    IdStateInscription = approvedState.Id,
                    IdStageModality = sustStage.Id,
                    StatusRegister = true,
                    OperationRegister = "Seed"
                };
                context.Set<InscriptionModality>().Add(inscription);
                context.SaveChanges();

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = inscription.Id,
                    IdTeacher = directorUser.Id,
                    IdTypeTeachingAssignment = directorType.Id,
                    StatusRegister = true,
                    OperationRegister = "Seed"
                });
                context.SaveChanges();
                inscriptionId = inscription.Id;
            }

            var rules = new List<EmailRecipientRule>
            {
                new()
                {
                    RecipientType = "CC",
                    RuleType = "BY_ENTITY_RELATION",
                    RuleValue = "DIRECTOR_ASSIGNED",
                    Priority = 1,
                    StatusRegister = true
                }
            };
            var eventData = new Dictionary<string, object>
            {
                ["InscriptionModalityId"] = inscriptionId
            };

            // Act
            using var actScope = _factory.Services.CreateScope();
            var resolver = actScope.ServiceProvider.GetRequiredService<IEmailRecipientResolverService>();
            var recipients = await resolver.ResolveRecipientsAsync(rules, eventData);

            // Assert
            recipients.Cc.Should().Contain("director.sust@unicesar.edu.co");
        }
    }
}
