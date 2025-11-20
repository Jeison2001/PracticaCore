using Application.Shared.DTOs.ThematicArea;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration;

namespace Tests.Integration.ThematicAreas
{
    public class ThematicAreaControllerTests : GenericControllerIntegrationTests<Domain.Entities.ThematicArea, ThematicAreaDto>
    {
        public ThematicAreaControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/ThematicArea";

        protected override void SeedAdditionalData(Domain.Entities.ThematicArea entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var line = context.Set<ResearchLine>().FirstOrDefault(x => x.Code == "L_Test");
                if (line == null)
                {
                    line = new ResearchLine 
                    { 
                        Code = "L_Test", 
                        Name = "Line 1", 
                        StatusRegister = true 
                    };
                    context.Set<ResearchLine>().Add(line);
                    context.SaveChanges();
                }

                var subLine = context.Set<ResearchSubLine>().FirstOrDefault(x => x.Code == "SL_Test");
                if (subLine == null)
                {
                    subLine = new ResearchSubLine 
                    { 
                        IdResearchLine = line.Id,
                        Code = "SL_Test", 
                        Name = "SubLine 1", 
                        StatusRegister = true 
                    };
                    context.Set<ResearchSubLine>().Add(subLine);
                    context.SaveChanges();
                }

                entity.IdResearchSubLine = subLine.Id;
            }
        }

        protected override ThematicAreaDto CreateValidDto()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var line = context.Set<ResearchLine>().FirstOrDefault(x => x.Code == "L_Dto");
                if (line == null)
                {
                    line = new ResearchLine 
                    { 
                        Code = "L_Dto", 
                        Name = "Line Dto", 
                        StatusRegister = true 
                    };
                    context.Set<ResearchLine>().Add(line);
                    context.SaveChanges();
                }

                var subLine = context.Set<ResearchSubLine>().FirstOrDefault(x => x.Code == "SL_Dto");
                if (subLine == null)
                {
                    subLine = new ResearchSubLine 
                    { 
                        IdResearchLine = line.Id,
                        Code = "SL_Dto", 
                        Name = "SubLine Dto", 
                        StatusRegister = true 
                    };
                    context.Set<ResearchSubLine>().Add(subLine);
                    context.SaveChanges();
                }

                return new ThematicAreaDto
                {
                    Code = $"TA_{Guid.NewGuid().ToString().Substring(0, 5)}",
                    Name = "Thematic Area Dto",
                    Description = "Test Description",
                    IdResearchSubLine = subLine.Id,
                    StatusRegister = true
                };
            }
        }

        protected override Domain.Entities.ThematicArea CreateValidEntity()
        {
            return new Domain.Entities.ThematicArea
            {
                Code = $"TA_{Guid.NewGuid().ToString().Substring(0, 5)}",
                Name = "Thematic Area Entity",
                Description = "Test Description",
                StatusRegister = true
            };
        }
    }
}
