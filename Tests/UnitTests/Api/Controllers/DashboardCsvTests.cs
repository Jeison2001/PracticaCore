using System.Text;
using Api.Controllers;
using Api.Responses;
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
        public async Task ExportCsv_Neutralizes_Spreadsheet_Formula_Prefixes_And_Includes_Enriched_Columns()
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
                    CreatedAt: "2026-01-01 10:30",
                    ApprovalDate: "2026-01-05",
                    CurrentPhase: "Anteproyecto",
                    PhaseState: "En evaluación",
                    ModalityDetail: "Detalle normal",
                    ModalityCode: "PROYECTO_GRADO",
                    LifecycleCode: "IN_REVIEW",
                    LifecycleStatus: "En revisión",
                    StudentIdentifications: "1065123456",
                    StudentEmails: "juan@unicesar.edu.co",
                    Teachers: "=Dr. Evil (Director)",
                    Title: "+Sistema Inteligente",
                    ResearchLine: "Ingeniería de Software",
                    ResearchSubLine: "Arquitectura",
                    LastActivityAt: "2026-02-01 15:00",
                    DaysElapsed: 31)
            };

            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(m => m.Send(It.IsAny<GetDashboardExportQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows);

            var controller = new DashboardController(mediatorMock.Object);
            var actionResult = await controller.ExportCsv(new DashboardFilter());

            var fileResult = actionResult.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("text/csv");

            var csvText = Encoding.UTF8.GetString(fileResult.FileContents).TrimStart('\uFEFF');

            // Encabezados originales + enriquecidos
            csvText.Should().StartWith("Id,Modalidad,Estado,Periodo,Programa,Facultad,Estudiantes,FechaCreacion,FechaAprobacion,FaseActual,EstadoFase,DetalleModalidad,EstadoCicloVida,Identificaciones,Correos,Docentes,Titulo,LineaInvestigacion,SublineaInvestigacion,UltimaActividad,DiasTranscurridos");

            // Neutralización de fórmulas en columnas originales y nuevas
            csvText.Should().Contain(",'=CMD|' /C calc'!A0,");
            csvText.Should().Contain(",\"'+SUM(1,2)\",");
            csvText.Should().Contain(",'-2+3,");
            csvText.Should().Contain(",'@AVERAGE(A1:A2),");
            csvText.Should().Contain(",\"'\t=HYPERLINK");
            csvText.Should().Contain("\"Juan, \"\"El\"\" Estudiante\"");
            csvText.Should().Contain("Detalle normal");
            csvText.Should().Contain(",En revisión,1065123456,juan@unicesar.edu.co,'=Dr. Evil (Director),'+Sistema Inteligente,Ingeniería de Software,Arquitectura,2026-02-01 15:00,31");
        }

        [Fact]
        public async Task ExportExcel_Returns_Xlsx_FileResult()
        {
            var fakeBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(m => m.Send(It.IsAny<GetDashboardExcelQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeBytes);

            var controller = new DashboardController(mediatorMock.Object);
            var result = await controller.ExportExcel(new DashboardFilter());

            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileDownloadName.Should().EndWith(".xlsx");
            fileResult.FileContents.Should().Equal(fakeBytes);
        }

        [Fact]
        public async Task GetAlerts_And_GetRecords_Return_ApiResponse_Payload()
        {
            var expectedAlerts = new DashboardOperationalAlerts { StaleThresholdDays = 30, TotalStaleCount = 3 };
            var expectedPage = new DashboardRecordsPage(new List<DashboardExportRow>(), 12, 1, 20);

            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(m => m.Send(It.IsAny<GetDashboardAlertsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedAlerts);
            mediatorMock
                .Setup(m => m.Send(It.IsAny<GetDashboardRecordsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPage);

            var controller = new DashboardController(mediatorMock.Object);

            var alertsResult = await controller.GetAlerts(new DashboardFilter());
            var alertsOk = alertsResult.Should().BeOfType<OkObjectResult>().Subject;
            var alertsResponse = alertsOk.Value.Should().BeOfType<ApiResponse<DashboardOperationalAlerts>>().Subject;
            alertsResponse.Success.Should().BeTrue();
            alertsResponse.Data!.TotalStaleCount.Should().Be(3);

            var recordsResult = await controller.GetRecords(new DashboardFilter());
            var recordsOk = recordsResult.Should().BeOfType<OkObjectResult>().Subject;
            var recordsResponse = recordsOk.Value.Should().BeOfType<ApiResponse<DashboardRecordsPage>>().Subject;
            recordsResponse.Success.Should().BeTrue();
            recordsResponse.Data!.TotalCount.Should().Be(12);
        }

        [Fact]
        public async Task ExportCsv_FormatsIso8601UtcStrings_ToColombiaLocalTimeText()
        {
            var rows = new List<DashboardExportRow>
            {
                new(
                    InscriptionId: 77,
                    ModalityName: "Proyecto de Grado",
                    StateName: "Aprobado",
                    AcademicPeriod: "2026-1",
                    Program: "Ingeniería de Sistemas",
                    Faculty: "Ingenierías y Tecnológicas",
                    Students: "Valeria Mendoza",
                    CreatedAt: "2026-03-16T04:30:00Z", // 2026-03-15 23:30 COT
                    ApprovalDate: "2026-03-16T04:59:59Z", // 2026-03-15 COT
                    CurrentPhase: "Propuesta",
                    PhaseState: "Pertinente",
                    ModalityDetail: "Detalle",
                    LastActivityAt: "2026-04-01T20:15:00Z", // 2026-04-01 15:15 COT
                    DaysElapsed: 16)
            };

            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(m => m.Send(It.IsAny<GetDashboardExportQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows);

            var controller = new DashboardController(mediatorMock.Object);
            var actionResult = await controller.ExportCsv(new DashboardFilter());
            var fileResult = actionResult.Should().BeOfType<FileContentResult>().Subject;
            var csvText = Encoding.UTF8.GetString(fileResult.FileContents).TrimStart('\uFEFF');

            csvText.Should().Contain(",2026-03-15 23:30,2026-03-15,");
            csvText.Should().Contain(",2026-04-01 15:15,16");
        }
    }
}
