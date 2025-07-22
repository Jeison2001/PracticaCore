using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Application.Shared.Commands
{
    public class CreateEntityCommandHandler<T, TId, TDto> : IRequestHandler<CreateEntityCommand<T, TId, TDto>, TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProposalNotificationService? _proposalNotificationService;

        public CreateEntityCommandHandler(
            IRepository<T, TId> repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IProposalNotificationService? proposalNotificationService = null)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _proposalNotificationService = proposalNotificationService;
        }

        public async Task<TDto> Handle(CreateEntityCommand<T, TId, TDto> request, CancellationToken ct)
        {
            T entity;
            try
            {
                entity = _mapper.Map<T>(request.Dto);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al mapear el DTO a la entidad.", ex);
            }
            // Verificar si la entidad tiene una propiedad Code
            PropertyInfo? codeProperty = typeof(T).GetProperty("Code");
            if (codeProperty != null)
            {
                string? codeValue = codeProperty.GetValue(entity) as string;
                if (!string.IsNullOrEmpty(codeValue))
                {
                    try
                    {
                        var existingEntity = await _repository.GetFirstOrDefaultAsync(
                            e => EF.Property<string>(e, "Code") == codeValue,
                            ct);
                        if (existingEntity != null)
                        {
                            throw new InvalidOperationException($"Ya existe un registro con el código '{codeValue}'.");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Error al verificar la unicidad del código.", ex);
                    }
                }
            }
            try
            {
                await _repository.AddAsync(entity);
                await _unitOfWork.CommitAsync(ct);
                
                // Procesar notificaciones en background
                ProcessNotificationsAsync(entity);
                
                return _mapper.Map<TDto>(entity);
            }
            catch (DbUpdateException ex)
            {
                var innerExceptionMessage = ex.InnerException?.Message;

                if (innerExceptionMessage != null)
                {
                    // Intentar identificar violaciones de llaves foráneas de forma genérica
                    // Esto es menos robusto que usar las propiedades específicas del proveedor de BD
                    if (innerExceptionMessage.Contains("violates foreign key constraint") ||
                        innerExceptionMessage.Contains("FOREIGN KEY constraint") ||
                        innerExceptionMessage.Contains("referential integrity constraint")) // Añadir otras variaciones comunes
                    {
                        // Intentar extraer el nombre de la restricción si está disponible en el mensaje
                        var constraintName = ExtractConstraintName(innerExceptionMessage);
                        if (!string.IsNullOrEmpty(constraintName) && constraintName.Equals("fkteachingassignmentinscriptionmodality", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("Error de llave foránea: El valor de 'idInscriptionModality' no es válido o no existe.", ex);
                        }
                        else if (!string.IsNullOrEmpty(constraintName))
                        {
                             throw new InvalidOperationException($"Error de llave foránea: La restricción '{constraintName}' ha sido violada. Verifique los datos relacionados.", ex);
                        }
                        throw new InvalidOperationException("Error de llave foránea: Uno de los valores proporcionados no existe en las tablas relacionadas.", ex);
                    }

                    // Mantener la lógica existente para otras violaciones comunes
                    if (innerExceptionMessage.Contains("duplicate") == true ||
                        innerExceptionMessage.Contains("unique") == true ||
                        innerExceptionMessage.Contains("violation of primary key") == true)
                    {
                        throw new InvalidOperationException("El registro ya existe en la base de datos.", ex);
                    }
                }
                
                throw new InvalidOperationException("Error al guardar en la base de datos.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error inesperado al crear la entidad.", ex);
            }
        }
private string ExtractConstraintName(string errorMessage)
        {
            // Ejemplo de patrones para extraer el nombre de la restricción.
            // Estos patrones pueden necesitar ajustes dependiendo del formato exacto del mensaje de error de tu proveedor de base de datos.
            var patterns = new[]
            {
                @"constraint ""([^""]+)""", // Para PostgreSQL: constraint "constraint_name"
                @"constraint '([^']+)'",    // Para SQL Server: constraint 'constraint_name'
                // Añade más patrones aquí si es necesario para otros proveedores de BD o formatos de mensaje.
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(errorMessage, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }
            return string.Empty;
        }

        private void ProcessNotificationsAsync(T entity)
        {
            // Notificación para Proposal en background
            if (_proposalNotificationService != null && typeof(T) == typeof(Proposal))
            {
                var proposalEntity = entity as Proposal;
                if (proposalEntity != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _proposalNotificationService.ProcessProposalEventAsync(proposalEntity, (Domain.Enums.StateStageEnum)proposalEntity.IdStateStage);
                        }
                        catch (Exception ex)
                        {
                            // Note: No logger available in CreateEntityCommandHandler, consider adding one if needed
                            Console.WriteLine($"Error procesando notificación para Proposal ID: {proposalEntity.Id} - {ex.Message}");
                        }
                    });
                }
            }
        }
    }
}
