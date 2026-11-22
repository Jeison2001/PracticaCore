using System.Reflection;
using Api.Authorization;
using Api.Controllers;
using Api.Controllers.Utilities;
using Domain.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Tests.UnitTests.Api.Authorization
{
    public class PermissionScopeArchitectureTests
    {
        private static readonly string[] ExpectedObservationHistoryEntities =
        [
            "InscriptionModality",
            "Proposal",
            "PreliminaryProject",
            "ProjectFinal",
            "ProjectPresentation",
            "AcademicPractice",
            "CoTerminal",
            "Seminar",
            "ScientificArticle",
            "AcademicAverage",
            "SaberPro"
        ];

        private static readonly string[] GuardedFilesWithoutInscriptionManagementPermissions =
        [
            Path.Combine("Application", "Common", "Services", "Documents", "DocumentAccessGuard.cs"),
            Path.Combine("Application", "Common", "Services", "MinorModalityAuthorizationHelper.cs"),
            Path.Combine("Application", "Shared", "Commands", "Proposals", "Handlers", "CreateProposalCommandHandler.cs"),
            Path.Combine("Application", "Shared", "Commands", "PreliminaryProjects", "Handlers", "PatchPreliminaryProjectCommandHandler.cs"),
            Path.Combine("Application", "Shared", "Commands", "ProjectFinals", "Handlers", "PatchProjectFinalCommandHandler.cs"),
            Path.Combine("Application", "Shared", "Commands", "ProjectPresentations", "Handlers", "PatchProjectPresentationCommandHandler.cs"),
            Path.Combine("Application", "Shared", "Commands", "AcademicPractices", "Handlers", "UpdateAcademicPracticeInstitutionCommandHandler.cs"),
            Path.Combine("Application", "Shared", "Commands", "AcademicPractices", "Handlers", "UpdateAcademicPracticePhaseCommandHandler.cs"),
            Path.Combine("Api", "Controllers", "PreliminaryProjectController.cs"),
            Path.Combine("Api", "Controllers", "ProjectFinalController.cs"),
            Path.Combine("Api", "Controllers", "ProjectPresentationController.cs"),
            Path.Combine("Api", "Controllers", "AcademicPracticeController.cs"),
            Path.Combine("Api", "Controllers", "ProposalController.cs")
        ];

        private static string ResolveRepositoryRoot()
        {
            var candidates = new[]
            {
                AppContext.BaseDirectory,
                Path.GetDirectoryName(typeof(PermissionScopeArchitectureTests).Assembly.Location),
                Directory.GetCurrentDirectory()
            };

            foreach (var start in candidates)
            {
                if (string.IsNullOrWhiteSpace(start))
                    continue;

                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "PracticaCore.sln")))
                        return dir.FullName;

                    dir = dir.Parent;
                }
            }

            throw new DirectoryNotFoundException(
                "No se pudo localizar la raíz del repositorio PracticaCore (PracticaCore.sln).");
        }

        private static string ReadRepoFile(string repoRoot, params string[] segments)
        {
            var fullPath = Path.Combine(new[] { repoRoot }.Concat(segments).ToArray());
            File.Exists(fullPath).Should().BeTrue($"el archivo esperado '{fullPath}' debe existir");
            return File.ReadAllText(fullPath);
        }

        private static IEnumerable<string> EnumerateCSharpFiles(string directory)
        {
            return Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Where(path =>
                {
                    var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    return !normalized.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !normalized.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
                });
        }

        [Fact]
        public void ApplicationAndCrossLayerGuards_DoNotReference_N3CFSU()
        {
            var repoRoot = ResolveRepositoryRoot();
            var applicationDir = Path.Combine(repoRoot, "Application");

            foreach (var file in EnumerateCSharpFiles(applicationDir))
            {
                var content = File.ReadAllText(file);
                content.Should().NotContain(
                    PermissionCodes.Configuraciones.N3CFSU,
                    $"'{file}' no debe usar {PermissionCodes.Configuraciones.N3CFSU} en Application/");
            }

            var controllerExtensions = ReadRepoFile(repoRoot, "Api", "Authorization", "ControllerReadAuthorizationExtensions.cs");
            controllerExtensions.Should().NotContain(PermissionCodes.Configuraciones.N3CFSU);

            var academicPracticeController = ReadRepoFile(repoRoot, "Api", "Controllers", "AcademicPracticeController.cs");
            academicPracticeController.Should().NotContain(PermissionCodes.Configuraciones.N3CFSU);
        }

        [Fact]
        public void DocumentAccessGuardAndTeachingAssignmentHandler_DoNotReference_DocumentCatalogPermissions()
        {
            var repoRoot = ResolveRepositoryRoot();

            var documentGuard = ReadRepoFile(repoRoot, "Application", "Common", "Services", "Documents", "DocumentAccessGuard.cs");
            documentGuard.Should().NotContain(PermissionCodes.Configuraciones.N2CFDO);
            documentGuard.Should().NotContain(PermissionCodes.Configuraciones.N3CFDOT);

            var teachingHandler = ReadRepoFile(
                repoRoot,
                "Application",
                "Shared",
                "Queries",
                "TeachingAssignments",
                "Handlers",
                "GetTeachingAssignmentsByProposalIdHandler.cs");
            teachingHandler.Should().NotContain(PermissionCodes.Configuraciones.N2CFDO);
            teachingHandler.Should().NotContain(PermissionCodes.Configuraciones.N3CFDOT);
        }

        [Fact]
        public void PhaseHandlersAndControllers_DoNotReference_InscriptionManagementPermissions()
        {
            var repoRoot = ResolveRepositoryRoot();

            foreach (var relativePath in GuardedFilesWithoutInscriptionManagementPermissions)
            {
                var fullPath = Path.Combine(repoRoot, relativePath);
                File.Exists(fullPath).Should().BeTrue($"el archivo '{relativePath}' debe existir");

                var content = File.ReadAllText(fullPath);
                content.Should().NotContain(
                    PermissionCodes.ModalidadesGrado.N3MGPR,
                    $"'{relativePath}' no debe referenciar {PermissionCodes.ModalidadesGrado.N3MGPR}");
                content.Should().NotContain(
                    PermissionCodes.ModalidadesGrado.N3MGPG,
                    $"'{relativePath}' no debe referenciar {PermissionCodes.ModalidadesGrado.N3MGPG}");
            }
        }

        [Fact]
        public void ModalityPermissionCatalog_EnforcesObservationHistoryEntityMatrixAndScopeIsolation()
        {
            var historyMap = ModalityPermissionCatalog.ObservationHistoryManagementByEntityType;

            historyMap.Keys.Should().HaveCount(11);
            historyMap.Keys.Should().BeEquivalentTo(ExpectedObservationHistoryEntities);

            foreach (var (entityType, permissions) in historyMap)
            {
                permissions.Should().NotBeEmpty($"'{entityType}' debe tener al menos un permiso de gestión");
                permissions.Should().NotContain(PermissionCodes.Configuraciones.N3CFSU);
                permissions.Should().NotContain(PermissionCodes.Configuraciones.N2CFDO);
                permissions.Should().NotContain(PermissionCodes.Configuraciones.N3CFDOT);

                if (!string.Equals(entityType, "InscriptionModality", StringComparison.OrdinalIgnoreCase))
                {
                    permissions.Should().NotContain(PermissionCodes.ModalidadesGrado.N3MGPR);
                    permissions.Should().NotContain(PermissionCodes.ModalidadesGrado.N3MGPG);
                }
            }

            var forbiddenEverywhere = new[]
            {
                PermissionCodes.Configuraciones.N3CFSU,
                PermissionCodes.Configuraciones.N2CFDO,
                PermissionCodes.Configuraciones.N3CFDOT
            };

            foreach (var field in typeof(ModalityPermissionCatalog).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var value = field.GetValue(null);
                value.Should().NotBeNull($"'{field.Name}' no debe ser null");

                if (value is IEnumerable<string> flatPermissions)
                {
                    var perms = flatPermissions.ToList();
                    perms.Should().NotContain(forbiddenEverywhere, $"'{field.Name}' no debe incluir permisos globales de seguridad/catálogo");

                    if (field.Name != nameof(ModalityPermissionCatalog.InscriptionModalityManagement))
                    {
                        perms.Should().NotContain(PermissionCodes.ModalidadesGrado.N3MGPR);
                        perms.Should().NotContain(PermissionCodes.ModalidadesGrado.N3MGPG);
                    }
                }
                else if (value is IReadOnlyDictionary<string, IReadOnlySet<string>> mappedPermissions)
                {
                    foreach (var (key, perms) in mappedPermissions)
                    {
                        perms.Should().NotContain(forbiddenEverywhere, $"'{field.Name}[{key}]' no debe incluir permisos globales de seguridad/catálogo");

                        if (field.Name != nameof(ModalityPermissionCatalog.ObservationHistoryManagementByEntityType)
                            || !string.Equals(key, "InscriptionModality", StringComparison.OrdinalIgnoreCase))
                        {
                            perms.Should().NotContain(PermissionCodes.ModalidadesGrado.N3MGPR);
                            perms.Should().NotContain(PermissionCodes.ModalidadesGrado.N3MGPG);
                        }
                    }
                }
            }
        }

        [Fact]
        public void PhaseHandlers_DoNotMixPermissionsFromOtherPhases()
        {
            var repoRoot = ResolveRepositoryRoot();

            var patchPreliminary = ReadRepoFile(
                repoRoot,
                "Application",
                "Shared",
                "Commands",
                "PreliminaryProjects",
                "Handlers",
                "PatchPreliminaryProjectCommandHandler.cs");
            patchPreliminary.Should().NotContain(PermissionCodes.ProyectoGrado.N4PGPPG);

            var patchProjectFinal = ReadRepoFile(
                repoRoot,
                "Application",
                "Shared",
                "Commands",
                "ProjectFinals",
                "Handlers",
                "PatchProjectFinalCommandHandler.cs");
            patchProjectFinal.Should().NotContain(PermissionCodes.ProyectoGrado.N4PGPAA);
            patchProjectFinal.Should().NotContain(PermissionCodes.ProyectoGrado.N4PGPPG);

            var teachingHandler = ReadRepoFile(
                repoRoot,
                "Application",
                "Shared",
                "Queries",
                "TeachingAssignments",
                "Handlers",
                "GetTeachingAssignmentsByProposalIdHandler.cs");
            teachingHandler.Should().NotContain("CO_TERMINAL");
        }

        [Fact]
        public void CrossCuttingControllers_DocumentAndEvaluation_UseAuthorizePlusContextualGuardsInsteadOfStaticRequirePermission()
        {
            // DocumentController y EvaluationController son transversales a todas las modalidades,
            // fases y actores (estudiantes propietarios, docentes confirmados y comités de etapa).
            // Un [RequirePermission] estático en el controlador no puede distinguir en compilación
            // el EntityType, StageModality, DocumentClass o membresía sobre el recurso concreto,
            // por lo que ambos exigen [Authorize] en la clase y delegan la autorización contextual
            // en tiempo de ejecución a IDocumentAccessGuard / GetObservationHistoryByEntityHandler /
            // UpdateEvaluationObservationCommandHandler.
            foreach (var controllerType in new[] { typeof(DocumentController), typeof(EvaluationController) })
            {
                controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                    .Should().NotBeEmpty($"{controllerType.Name} debe requerir [Authorize]");

                controllerType.GetCustomAttributes<RequirePermissionAttribute>(inherit: true)
                    .Should().BeEmpty($"{controllerType.Name} no debe usar [RequirePermission] estático en clase");

                foreach (var method in controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    method.GetCustomAttributes<RequirePermissionAttribute>(inherit: true)
                        .Should().BeEmpty($"{controllerType.Name}.{method.Name} usa guard/handler contextual en lugar de [RequirePermission] estático");
                }
            }

            var repoRoot = ResolveRepositoryRoot();
            var updateObsHandler = ReadRepoFile(
                repoRoot,
                "Application",
                "Features",
                "Evaluations",
                "Commands",
                "Handlers",
                "UpdateEvaluationObservationCommandHandler.cs");
            updateObsHandler.Should().Contain(nameof(ModalityPermissionCatalog));
            updateObsHandler.Should().NotContain(PermissionCodes.Configuraciones.N3CFSU);
            updateObsHandler.Should().NotContain(PermissionCodes.Configuraciones.N2CFDO);
            updateObsHandler.Should().NotContain(PermissionCodes.Configuraciones.N3CFDOT);
            updateObsHandler.Should().NotContain(PermissionCodes.ModalidadesGrado.N3MGPR);
            updateObsHandler.Should().NotContain(PermissionCodes.ModalidadesGrado.N3MGPG);
        }

        [Fact]
        public void AllHttpEndpoints_MustHaveRequirePermissionAllowAnonymousOrDocumentedAuthorizeException()
        {
            var apiAssembly = typeof(AuthController).Assembly;

            var catalogControllerTypes = new[]
            {
                typeof(AcademicPeriodController),
                typeof(DocumentClassController),
                typeof(DocumentTypeController),
                typeof(EvaluationTypeController),
                typeof(FacultyController),
                typeof(ModalityController),
                typeof(ResearchGroupController),
                typeof(ResearchLineController),
                typeof(ResearchSubLineController),
                typeof(StageModalityController),
                typeof(StateInscriptionController),
                typeof(StateStageController),
                typeof(StudyPlanController),
                typeof(ThematicAreaController)
            };

            foreach (var catalogType in catalogControllerTypes)
            {
                foreach (var writeActionName in new[] { "Create", "CreateMultiple", "Update", "UpdateStatus" })
                {
                    var writeMethod = catalogType.GetMethod(writeActionName, BindingFlags.Public | BindingFlags.Instance);
                    writeMethod.Should().NotBeNull($"{catalogType.Name} debe definir '{writeActionName}'");
                    writeMethod!.GetCustomAttributes<NonActionAttribute>(inherit: true).Should().BeEmpty(
                        $"{catalogType.Name}.{writeActionName} no debe quedar oculto inesperadamente");
                    writeMethod.GetCustomAttributes<RequirePermissionAttribute>(inherit: true).Should().NotBeEmpty(
                        $"{catalogType.Name}.{writeActionName} debe exigir [RequirePermission]");
                }
            }

            var expectedExceptions = new HashSet<string>(StringComparer.Ordinal)
            {
                $"{nameof(AuthController)}.{nameof(AuthController.LogoutAll)}",
                $"{nameof(DocumentController)}.{nameof(DocumentController.GetById)}",
                $"{nameof(DocumentController)}.{nameof(DocumentController.DownloadFile)}",
                $"{nameof(DocumentController)}.{nameof(DocumentController.GetByInscriptionModality)}",
                $"{nameof(DocumentController)}.{nameof(DocumentController.Upload)}",
                $"{nameof(DocumentController)}.{nameof(DocumentController.Update)}",
                $"{nameof(DocumentController)}.{nameof(DocumentController.UpdateStatus)}",
                $"{nameof(EvaluationController)}.{nameof(EvaluationController.GetObservationHistory)}",
                $"{nameof(EvaluationController)}.{nameof(EvaluationController.UpdateObservation)}",
                $"{nameof(ServicesController)}.{nameof(ServicesController.GenerateId)}",
                $"{nameof(RoleController)}.{nameof(RoleController.GetAll)}",
                $"{nameof(RoleController)}.{nameof(RoleController.GetById)}",
                $"{nameof(UserRoleController)}.{nameof(UserRoleController.GetUserRolesByUserId)}",
                $"{nameof(UserPermissionController)}.{nameof(UserPermissionController.GetUserPermissionsByUserId)}",
                $"{nameof(TeachingAssignmentController)}.{nameof(TeachingAssignmentController.GetByIdInscription)}",
                $"{nameof(StudyPlanController)}.{nameof(StudyPlanController.GetByAcademicProgram)}"
            };

            foreach (var catalogType in catalogControllerTypes)
            {
                expectedExceptions.Add($"{catalogType.Name}.GetAll");
                expectedExceptions.Add($"{catalogType.Name}.GetById");
            }

            var controllerTypes = apiAssembly.GetTypes()
                .Where(t => t.IsPublic && !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(ControllerBase).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToList();

            controllerTypes.Should().NotBeEmpty();

            var foundWithoutPermissionOrAnonymous = new List<string>();

            foreach (var controllerType in controllerTypes)
            {
                var classHasRequirePermission = controllerType.GetCustomAttributes<RequirePermissionAttribute>(inherit: true).Any();
                var classHasAllowAnonymous = controllerType.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
                var classHasAuthorize = controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any();

                var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .Where(m => !m.IsSpecialName
                                && m.DeclaringType != typeof(object)
                                && m.DeclaringType != typeof(ControllerBase)
                                && !m.GetCustomAttributes<NonActionAttribute>(inherit: true).Any()
                                && m.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any());

                foreach (var method in methods)
                {
                    var hasRequirePermission = classHasRequirePermission
                        || method.GetCustomAttributes<RequirePermissionAttribute>(inherit: true).Any();
                    var hasAllowAnonymous = classHasAllowAnonymous
                        || method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
                    var hasAuthorize = classHasAuthorize
                        || method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any();

                    if (!hasRequirePermission && !hasAllowAnonymous)
                    {
                        var endpointKey = $"{controllerType.Name}.{method.Name}";
                        hasAuthorize.Should().BeTrue(
                            $"el endpoint '{endpointKey}' sin [RequirePermission] ni [AllowAnonymous] debe exigir [Authorize]");
                        foundWithoutPermissionOrAnonymous.Add(endpointKey);
                    }
                }
            }

            foundWithoutPermissionOrAnonymous.Should().BeEquivalentTo(expectedExceptions);
        }
    }
}
