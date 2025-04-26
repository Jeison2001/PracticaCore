using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
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

        public CreateEntityCommandHandler(IRepository<T, TId> repository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<TDto> Handle(CreateEntityCommand<T, TId, TDto> request, CancellationToken ct)
        {
            var entity = _mapper.Map<T>(request.Dto);
            
            // Verificar si la entidad tiene una propiedad Code
            PropertyInfo? codeProperty = typeof(T).GetProperty("Code");
            if (codeProperty != null)
            {
                // Obtener el valor de Code
                string? codeValue = codeProperty.GetValue(entity) as string;
                
                if (!string.IsNullOrEmpty(codeValue))
                {
                    try
                    {
                        // Verificar si ya existe una entidad con el mismo código
                        var existingEntity = await _repository.GetFirstOrDefaultAsync(
                            e => EF.Property<string>(e, "Code") == codeValue, 
                            ct);
                            
                        if (existingEntity != null)
                        {
                            throw new InvalidOperationException($"Ya existe un registro con el código '{codeValue}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is InvalidOperationException))
                    {
                        // Si es un error diferente al que acabamos de lanzar, rethrow
                        throw;
                    }
                }
            }
            
            try
            {
                await _repository.AddAsync(entity);
                await _unitOfWork.CommitAsync(ct);
                return _mapper.Map<TDto>(entity);
            }
            catch (DbUpdateException ex)
            {
                // Capturar y transformar excepciones específicas de la base de datos
                if (ex.InnerException?.Message.Contains("duplicate") == true ||
                    ex.InnerException?.Message.Contains("unique") == true ||
                    ex.InnerException?.Message.Contains("violation of primary key") == true)
                {
                    throw new InvalidOperationException("El registro ya existe en la base de datos.", ex);
                }
                
                throw;
            }
        }
    }
}
