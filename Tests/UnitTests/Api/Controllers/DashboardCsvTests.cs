using System.Text;
using Api.Controllers;
using Application.Shared.Queries.Dashboard;
using Domain.Common.Dashboard;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Tests.UnitTests.Api.Controllers
{
    public class DashboardCsvTests
    {
        [Fact]
        public async Task ExportCsv_Neutralizes_Spreadsheet_Formula_Prefixes()
        {
            var rows = new List<DashboardExportRow>
            {
                new(
                    InscriptionId: 42,
                    ModalityName: "=CMD|' /C calc'!A0",
                    StateName: "+SUM(1,2)",
                    AcademicPeriod: "-2+3",
                    Program: "@AVERAGE(A1:A2)",
                    Faculty: "\t=HYPERLINK(\"http://evil\")",
                    Students: "Juan, \"El\" Estudiante",
                    CreatedAt: "2026-01-01",
                    ApprovalDate: "",
                    CurrentPhase: "Propuesta",
                    PhaseState: "Radicada",
                    ModalityDetail: "Detalle normal")
            };

            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(m => m.Send(It.IsAny<GetDashboardExportQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows);

            var controller = new DashboardController(mediatorMock.Object);
            var actionResult = await controller.ExportCsv(new DashboardFilter());

            var fileResult = actionResult.Should().BeOfType<FileContentResult>().Subject;
            var csvText = Encoding.UTF8.GetString(fileResult.FileContents).TrimStart('\uFEFF');

            csvText.Should().Contain(",'=CMD|' /C calc'!A0,");
            csvText.Should().Contain(",\"'+SUM(1,2)\",");
            csvText.Should().Contain(",'-2+3,");
            csvText.Should().Contain(",'@AVERAGE(A1:A2),");
            csvText.Should().Contain(",\"'\t=HYPERLINK");
            csvText.Should().Contain("\"Juan, \"\"El\"\" Estudiante\"");
            csvText.Should().Contain("Detalle normal");
        }
    }
}
