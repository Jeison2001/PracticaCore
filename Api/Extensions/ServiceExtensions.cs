using Application.Common.Behaviors;
using Application.Shared.Commands;
using Application.Shared.DTOs;
using Application.Shared.Mappings;
using Application.Shared.Queries;
using Application.Validations.BaseValidators;
using Application.Validations.SpecificValidators.Example;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Api.Extensions
{
    public static class ServiceExtensions
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("Default")));
            //options.UseSqlServer(config.GetConnectionString("Default")));

            services.AddScoped(typeof(IRepository<,>), typeof(BaseRepository<,>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        public static void AddApplicationLayer(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining(typeof(GetEntityByIdQueryHandler<,,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            RegisterGenericHandlers(services);
            services.AddAutoMapper(typeof(GenericProfile));

            // Registrar primero los validadores específicos del ensamblado
            services.AddValidatorsFromAssembly(typeof(CreateExampleCommandValidator).Assembly);

            // Luego registrar los validadores genéricos solo donde no existan específicos
            RegisterGenericValidators(services);
        }

        private static void RegisterGenericValidators(IServiceCollection services)
        {
            var entityTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.BaseType?.IsGenericType == true &&
                            t.BaseType.GetGenericTypeDefinition() == typeof(BaseEntity<>))
                .ToList();

            var dtoTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.BaseType?.IsGenericType == true &&
                            t.BaseType.GetGenericTypeDefinition() == typeof(BaseDto<>))
                .ToList();

            foreach (var entityType in entityTypes)
            {
                var idType = entityType.BaseType!.GetGenericArguments()[0];
                var dtoName = entityType.Name + "Dto";
                var dtoType = dtoTypes.FirstOrDefault(dto =>
                    dto.Name == dtoName &&
                    dto.BaseType!.GetGenericArguments()[0] == idType);

                if (dtoType == null) continue;

                // Registrar validadores genéricos solo si no existe un validador específico
                var createCommandType = typeof(CreateEntityCommand<,,>).MakeGenericType(entityType, idType, dtoType);
                RegisterGenericValidatorIfNeeded(services, createCommandType, typeof(BaseCreateCommandValidator<,,>), entityType, idType, dtoType);

                var updateCommandType = typeof(UpdateEntityCommand<,,>).MakeGenericType(entityType, idType, dtoType);
                RegisterGenericValidatorIfNeeded(services, updateCommandType, typeof(BaseUpdateCommandValidator<,,>), entityType, idType, dtoType);

                var deleteCommandType = typeof(DeleteEntityCommand<,>).MakeGenericType(entityType, idType);
                RegisterGenericValidatorIfNeeded(services, deleteCommandType, typeof(BaseDeleteCommandValidator<,>), entityType, idType);
                RegisterQueryByIdValidatorIfNeeded(services, entityType, idType, dtoType);
            }
        }

        private static void RegisterGenericValidatorIfNeeded(IServiceCollection services, Type commandType,
            Type validatorGenericType, Type entityType, Type idType, Type? dtoType = null)
        {
            // Construir el tipo del validador genérico
            Type[] validatorTypeArgs = dtoType != null
                ? [entityType, dtoType, idType]
                : [entityType, idType];

            Type validatorType = validatorGenericType.MakeGenericType(validatorTypeArgs);

            // Verificar si ya existe un validador para este comando
            var validatorInterface = typeof(IValidator<>).MakeGenericType(commandType);
            bool validatorExists = services.Any(sd => sd.ServiceType == validatorInterface);

            if (!validatorExists)
            {
                services.AddTransient(validatorInterface, validatorType);
            }
        }

        private static void RegisterQueryByIdValidatorIfNeeded(IServiceCollection services, Type entityType, Type idType, Type dtoType)
        {
            var queryType = typeof(GetEntityByIdQuery<,,>).MakeGenericType(entityType, idType, dtoType);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(queryType);

            bool validatorExists = services.Any(sd => sd.ServiceType == validatorInterface);

            if (!validatorExists)
            {
                var validatorType = typeof(BaseQueryByIdValidator<,,,>).MakeGenericType(queryType, entityType, idType, dtoType);
                services.AddTransient(validatorInterface, validatorType);
            }
        }
        private static void RegisterGenericHandlers(IServiceCollection services)
        {
            var entityTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.BaseType?.IsGenericType == true &&
                            t.BaseType.GetGenericTypeDefinition() == typeof(BaseEntity<>))
                .ToList();

            var dtoTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.BaseType?.IsGenericType == true &&
                            t.BaseType.GetGenericTypeDefinition() == typeof(BaseDto<>))
                .ToList();

            foreach (var entityType in entityTypes)
            {
                var idType = entityType.BaseType!.GetGenericArguments()[0];
                var dtoName = entityType.Name + "Dto";
                var dtoType = dtoTypes.FirstOrDefault(dto =>
                    dto.Name == dtoName &&
                    dto.BaseType!.GetGenericArguments()[0] == idType);

                if (dtoType == null) continue;

                // Registrar Query Handlers
                RegisterQueryHandler(services, typeof(GetEntityByIdQuery<,,>), typeof(GetEntityByIdQueryHandler<,,>), entityType, idType, dtoType);
                RegisterQueryHandler(services, typeof(GetAllEntitiesQuery<,,>), typeof(GetAllEntitiesQueryHandler<,,>), entityType, idType, dtoType);

                // Registrar Command Handlers
                RegisterCommandHandler(services, typeof(CreateEntityCommand<,,>), typeof(CreateEntityCommandHandler<,,>), entityType, idType, dtoType);
                RegisterCommandHandler(services, typeof(UpdateEntityCommand<,,>), typeof(UpdateEntityCommandHandler<,,>), entityType, idType, dtoType);
                RegisterCommandHandler(services, typeof(DeleteEntityCommand<,>), typeof(DeleteEntityCommandHandler<,>), entityType, idType, dtoType);
            }
        }

        private static void RegisterQueryHandler(IServiceCollection services, Type queryType, Type handlerType, Type entityType, Type idType, Type dtoType)
        {
            var genericQuery = queryType.MakeGenericType(entityType, idType, dtoType);
            var genericHandler = handlerType.MakeGenericType(entityType, idType, dtoType);
            var responseType = queryType == typeof(GetAllEntitiesQuery<,,>)
                ? typeof(Domain.Common.PaginatedResult<>).MakeGenericType(dtoType)
                : dtoType;

            var handlerInterface = typeof(IRequestHandler<,>).MakeGenericType(genericQuery, responseType);
            services.AddTransient(handlerInterface, genericHandler);
        }

        private static void RegisterCommandHandler(IServiceCollection services, Type commandType, Type handlerType, Type entityType, Type idType, Type dtoType)
        {
            Type[] genericArgs;
            if (commandType == typeof(DeleteEntityCommand<,>))
            {
                genericArgs = [entityType, idType];
            }
            else
            {
                genericArgs = [entityType, idType, dtoType];
            }

            var genericCommand = commandType.MakeGenericType(genericArgs);
            var genericHandler = handlerType.MakeGenericType(genericArgs);

            Type responseType = commandType == typeof(DeleteEntityCommand<,>)
                ? typeof(bool)
                : dtoType;

            var handlerInterface = typeof(IRequestHandler<,>).MakeGenericType(genericCommand, responseType);
            services.AddTransient(handlerInterface, genericHandler);
        }
    }
}
