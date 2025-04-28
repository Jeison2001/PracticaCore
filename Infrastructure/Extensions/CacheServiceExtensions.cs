using Domain.Interfaces;
using Domain.Interfaces.Cache;
using Infrastructure.Services.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Infrastructure.Extensions
{
    public static class CacheServiceExtensions
    {
        public static IServiceCollection AddCacheServices(this IServiceCollection services)
        {
            // Registramos el servicio de caché en memoria
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
            
            // Registramos CachedUnitOfWork decorando IUnitOfWork manualmente
            // Primero obtenemos el descriptor del servicio original
            var originalDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IUnitOfWork));
            
            if (originalDescriptor != null)
            {
                // Eliminamos el registro original
                services.Remove(originalDescriptor);
                
                // Añadimos un nuevo registro que decorará el servicio original
                if (originalDescriptor.ImplementationFactory != null)
                {
                    var factory = originalDescriptor.ImplementationFactory;
                    services.Add(new ServiceDescriptor(
                        typeof(IUnitOfWork),
                        sp => new CachedUnitOfWork(
                            (IUnitOfWork)factory(sp),
                            sp.GetRequiredService<ICacheService>(),
                            sp.GetRequiredService<ILoggerFactory>()),
                        originalDescriptor.Lifetime));
                }
                else if (originalDescriptor.ImplementationInstance != null)
                {
                    services.Add(new ServiceDescriptor(
                        typeof(IUnitOfWork),
                        sp => new CachedUnitOfWork(
                            (IUnitOfWork)originalDescriptor.ImplementationInstance,
                            sp.GetRequiredService<ICacheService>(),
                            sp.GetRequiredService<ILoggerFactory>()),
                        originalDescriptor.Lifetime));
                }
                else if (originalDescriptor.ImplementationType != null)
                {
                    services.Add(new ServiceDescriptor(
                        typeof(IUnitOfWork),
                        sp => new CachedUnitOfWork(
                            (IUnitOfWork)ActivatorUtilities.CreateInstance(sp, originalDescriptor.ImplementationType),
                            sp.GetRequiredService<ICacheService>(),
                            sp.GetRequiredService<ILoggerFactory>()),
                        originalDescriptor.Lifetime));
                }
                else
                {
                    throw new InvalidOperationException("No se pudo determinar cómo crear la instancia del servicio IUnitOfWork.");
                }
            }
            else
            {
                // Si IUnitOfWork no está registrado, lanzamos una excepción
                throw new InvalidOperationException("El servicio IUnitOfWork debe estar registrado antes de llamar a AddCacheServices.");
            }
            
            return services;
        }
    }
}