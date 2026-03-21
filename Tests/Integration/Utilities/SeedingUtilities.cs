using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;

namespace Tests.Integration.Utilities
{
    public static class SeedingUtilities
    {
        public static void SeedCatalogs(AppDbContext context)
        {
            // Seed Modalities
            context.Set<Modality>().AddRange(
                new Modality { Id = 1, Code = ModalityCodes.ProyectoGrado, Name = "Proyecto de Grado", RequiresApproval = true, StatusRegister = true, OperationRegister = "Seed" },
                new Modality { Id = 2, Code = ModalityCodes.PracticaAcademica, Name = "Práctica Académica", RequiresApproval = true, StatusRegister = true, OperationRegister = "Seed" },
                new Modality { Id = 3, Code = ModalityCodes.CoTerminal, Name = "CoTerminal", RequiresApproval = false, StatusRegister = true, OperationRegister = "Seed" },
                new Modality { Id = 4, Code = ModalityCodes.SeminarioAct, Name = "Seminario", RequiresApproval = true, StatusRegister = true, OperationRegister = "Seed" },
                new Modality { Id = 5, Code = ModalityCodes.PublicacionArticulo, Name = "Artículo", RequiresApproval = true, StatusRegister = true, OperationRegister = "Seed" },
                new Modality { Id = 6, Code = ModalityCodes.GradoPromedio, Name = "Grado por Promedio", RequiresApproval = false, StatusRegister = true, OperationRegister = "Seed" },
                new Modality { Id = 7, Code = ModalityCodes.SaberPro, Name = "Saber Pro", RequiresApproval = false, StatusRegister = true, OperationRegister = "Seed" }
            );

            // Seed StateInscriptions
            context.Set<StateInscription>().AddRange(
                new StateInscription { Id = 1, Code = StateInscriptionCodes.Pendiente, Name = "Pendiente", StatusRegister = true, OperationRegister = "Seed" },
                new StateInscription { Id = 2, Code = StateInscriptionCodes.Aprobado, Name = "Aprobado", StatusRegister = true, OperationRegister = "Seed" },
                new StateInscription { Id = 3, Code = StateInscriptionCodes.NoAplica, Name = "No Aplica", StatusRegister = true, OperationRegister = "Seed" }
            );

            // Seed StageModalities
            context.Set<StageModality>().AddRange(
                new StageModality { Id = 1, Code = StageModalityCodes.PaFaseInscripcion, IdModality = 2, StageOrder = 1, StatusRegister = true, OperationRegister = "Seed" },
                new StageModality { Id = 2, Code = StageModalityCodes.PaFaseDesarrollo, IdModality = 2, StageOrder = 2, StatusRegister = true, OperationRegister = "Seed" },
                new StageModality { Id = 3, Code = StageModalityCodes.PaFaseEvaluacion, IdModality = 2, StageOrder = 3, StatusRegister = true, OperationRegister = "Seed" },

                new StageModality { Id = 4, Code = StageModalityCodes.PgFasePropuesta, IdModality = 1, StageOrder = 1, StatusRegister = true, OperationRegister = "Seed" },
                new StageModality { Id = 5, Code = StageModalityCodes.PgFaseAnteproyecto, IdModality = 1, StageOrder = 2, StatusRegister = true, OperationRegister = "Seed" },
                new StageModality { Id = 6, Code = StageModalityCodes.PgFaseProyectoInforme, IdModality = 1, StageOrder = 3, StatusRegister = true, OperationRegister = "Seed" },

                // StageModality for CoTerminal (Order 1)
                new StageModality { Id = 7, Code = "CT_FASE_1", IdModality = 3, StageOrder = 1, StatusRegister = true, OperationRegister = "Seed" }
            );

            // Seed StateStages
            context.Set<StateStage>().AddRange(
                // Academic Practice
                new StateStage { Id = 1, Code = StateStageCodes.PaInscripcionAprobada, IdStageModality = 1, IsInitialState = false, IsFinalStateForStage = true, StatusRegister = true, OperationRegister = "Seed" },
                new StateStage { Id = 2, Code = StateStageCodes.PaDesarrolloAprobada, IdStageModality = 2, IsInitialState = false, IsFinalStateForStage = true, StatusRegister = true, OperationRegister = "Seed" },
                new StateStage { Id = 10, Code = StateStageCodes.PaInscripcionPendDoc, IdStageModality = 1, IsInitialState = true, StatusRegister = true, OperationRegister = "Seed" },
                
                // Proyecto de Grado - Propuesta
                new StateStage { Id = 3, Code = StateStageCodes.PropPertinente, IdStageModality = 4, IsInitialState = false, IsFinalStateForStage = true, StatusRegister = true, OperationRegister = "Seed" },
                
                // Proyecto de Grado - Anteproyecto
                new StateStage { Id = 4, Code = StateStageCodes.ApPendienteDocumento, IdStageModality = 5, IsInitialState = true, StatusRegister = true, OperationRegister = "Seed" },
                new StateStage { Id = 5, Code = StateStageCodes.ApRadicadoPendAsigEval, IdStageModality = 5, IsInitialState = false, StatusRegister = true, OperationRegister = "Seed" },
                new StateStage { Id = 6, Code = StateStageCodes.ApAprobado, IdStageModality = 5, IsInitialState = false, IsFinalStateForStage = true, StatusRegister = true, OperationRegister = "Seed" },

                // Proyecto de Grado - Proyecto Final
                new StateStage { Id = 7, Code = StateStageCodes.PfinfPendienteInforme, IdStageModality = 6, IsInitialState = true, StatusRegister = true, OperationRegister = "Seed" },
                new StateStage { Id = 8, Code = StateStageCodes.PfinfRadicadoEnEvaluacion, IdStageModality = 6, IsInitialState = false, StatusRegister = true, OperationRegister = "Seed" },

                // CoTerminal Initial State
                new StateStage { Id = 9, Code = "CT_INICIAL", IdStageModality = 7, IsInitialState = true, StatusRegister = true, OperationRegister = "Seed" }
            );

            // Seed DocumentTypes
            context.Set<DocumentType>().AddRange(
                new DocumentType { Id = 1, Code = DocumentTypeCodes.AnteproyectoEntregable, Name = "Entregable Anteproyecto", StatusRegister = true, OperationRegister = "Seed" },
                new DocumentType { Id = 2, Code = DocumentTypeCodes.ProyectoFinalEntregable, Name = "Entregable Proyecto", StatusRegister = true, OperationRegister = "Seed" }
            );

            // ── Minor Modalities ────────────────────────────────────────────────────
            // StageModalities for minor modalities (Order 1)
            context.Set<StageModality>().AddRange(
                new StageModality { Id = 10, Code = "SA_FASE_1", IdModality = 4,  StageOrder = 1, StatusRegister = true, OperationRegister = "Seed", Name = "Seminario Fase 1" },
                new StageModality { Id = 11, Code = "PA_FASE_1", IdModality = 5,  StageOrder = 1, StatusRegister = true, OperationRegister = "Seed", Name = "Artículo Fase 1" },
                new StageModality { Id = 12, Code = "PA_FASE_2", IdModality = 5,  StageOrder = 2, StatusRegister = true, OperationRegister = "Seed", Name = "Artículo Fase 2" },
                new StageModality { Id = 13, Code = "GP_FASE_1", IdModality = 6,  StageOrder = 1, StatusRegister = true, OperationRegister = "Seed", Name = "Grado Promedio" },
                new StageModality { Id = 14, Code = "SP_FASE_1", IdModality = 7,  StageOrder = 1, StatusRegister = true, OperationRegister = "Seed", Name = "Saber Pro" }
            );

            // Initial StateStages for minor modality phases
            context.Set<StateStage>().AddRange(
                // Seminario Fase 1
                new StateStage { Id = 20, Code = "SA_INICIAL", IdStageModality = 10, IsInitialState = true, StatusRegister = true, OperationRegister = "Seed", Name = "Seminario Inicial" },
                // Artículo Fase 1 (Final de Fase, no Final Overall → dispara avance a Fase 2)
                new StateStage { Id = 21, Code = "PC_FASEINICIAL", IdStageModality = 11, IsInitialState = true, StatusRegister = true, OperationRegister = "Seed", Name = "Artículo Fase 1 Inicial" },
                new StateStage { Id = 22, Code = "PC_FASE1_APROBADO", IdStageModality = 11, IsInitialState = false, IsFinalStateForStage = true, IsFinalStateForModalityOverall = false, StatusRegister = true, OperationRegister = "Seed", Name = "Artículo Fase 1 Aprobado" },
                // Artículo Fase 2
                new StateStage { Id = 23, Code = "PC_FASE2_INICIAL", IdStageModality = 12, IsInitialState = true, StatusRegister = true, OperationRegister = "Seed", Name = "Artículo Fase 2 Inicial" },
                // Grado Promedio Fase 1
                new StateStage { Id = 24, Code = "GP_INICIAL", IdStageModality = 13, IsInitialState = true, StatusRegister = true, OperationRegister = "Seed", Name = "Grado Promedio Inicial" },
                // Saber Pro Fase 1
                new StateStage { Id = 25, Code = "SP_INICIAL", IdStageModality = 14, IsInitialState = true, StatusRegister = true, OperationRegister = "Seed", Name = "Saber Pro Inicial" }
            );

            // Commit all seeded configuration data
            context.SaveChanges();
        }


        public static void SeedPermissions(AppDbContext context, params string[] permissionCodes)
        {
            foreach (var code in permissionCodes)
            {
                if (!context.Set<Permission>().Any(p => p.Code == code))
                {
                    context.Set<Permission>().Add(new Permission { Code = code, Description = code, StatusRegister = true, OperationRegister = "Seed" });
                }
            }
            context.SaveChanges();
        }
    }
}
