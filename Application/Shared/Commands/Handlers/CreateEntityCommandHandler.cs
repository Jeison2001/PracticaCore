using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Dispatcher;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using Application.Common.Services.Jobs;

namespace Application.Shared.Commands.Handlers
{
    public class CreateEntityCommandHandler<T, TId, TDto> : IRequestHandler<CreateEntityCommand<T, TId, TDto>, TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer? _jobEnqueuer;
        private readonly ILogger<CreateEntityCommandHandler<T, TId, TDto>> _logger;

        public CreateEntityCommandHandler(
            IRepository<T, TId> repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<CreateEntityCommandHandler<T, TId, TDto>> logger,
            IJobEnqueuer? jobEnqueuer = null)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
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
                            throw new InvalidOperationException($"Ya existe un registro con el c�digo '{codeValue}'.");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Error al verificar la unicidad del c�digo.", ex);
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
                    // Intentar identificar violaciones de llaves for�neas de forma gen�rica
                    // Esto es menos robusto que usar las propiedades espec�ficas del proveedor de BD
                    if (innerExceptionMessage.Contains("violates foreign key constraint") ||
                        innerExceptionMessage.Contains("FOREIGN KEY constraint") ||
                        innerExceptionMessage.Contains("referential integrity constraint")) // A�adir otras variaciones comunes
                    {
                        // Intentar extraer el nombre de la restricci�n si est� disponible en el mensaje
                        var constraintName = ExtractConstraintName(innerExceptionMessage);
                        if (!string.IsNullOrEmpty(constraintName) && constraintName.Equals("fkteachingassignmentinscriptionmodality", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("Error de llave for�nea: El valor de 'idInscriptionModality' no es v�lido o no existe.", ex);
                        }
                        else if (!string.IsNullOrEmpty(constraintName))
                        {
                             throw new InvalidOperationException($"Error de llave for�nea: La restricci�n '{constraintName}' ha sido violada. Verifique los datos relacionados.", ex);
                        }
                        throw new InvalidOperationException("Error de llave for�nea: Uno de los valores proporcionados no existe en las tablas relacionadas.", ex);
                    }

                    // Mantener la l�gica existente para otras violaciones comunes
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
            // Ejemplo de patrones para extraer el nombre de la restricci�n.
            // Estos patrones pueden necesitar ajustes dependiendo del formato exacto del mensaje de error de tu proveedor de base de datos.
            var patterns = new[]
            {
                @"constraint ""([^""]+)""", // Para PostgreSQL: constraint "constraint_name"
                @"constraint '([^']+)'",    // Para SQL Server: constraint 'constraint_name'
                // A�ade m�s patrones aqu� si es necesario para otros proveedores de BD o formatos de mensaje.
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
            // Solo procesar Proposals - InscriptionModality manejado por handler espec�fico
            if (typeof(T) == typeof(Proposal) && _jobEnqueuer != null)
            {
                // ? MEJORADO: Fire-and-forget seguro usando Hangfire
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleEntityCreationAsync<T, TId>(entity.Id));
            }

            // ? ELIMINADO COMPLETAMENTE: InscriptionModality ya no se notifica aqu�
            // InscriptionModality solo se crea via CreateInscriptionWithStudentsHandler
            // que usa InscriptionCreationService para notificaciones
        }
    }
}
