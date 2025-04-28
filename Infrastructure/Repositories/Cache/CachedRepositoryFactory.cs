using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Cache;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Infrastructure.Repositories.Cache
{
    public class CachedRepositoryFactory
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Dictionary<Type, int> _entityCacheTimes;

        public CachedRepositoryFactory(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILoggerFactory loggerFactory)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _loggerFactory = loggerFactory;

            // Definimos tiempos de caché diferenciados para las distintas entidades paramétricas
            _entityCacheTimes = new Dictionary<Type, int>
            {
                { typeof(StateProposal), 60 },        // 1 hora
                { typeof(ResearchLine), 60 },         // 1 hora
                { typeof(ResearchSubLine), 60 },      // 1 hora
                { typeof(StateInscription), 60 },     // 1 hora
                { typeof(Modality), 60 },             // 1 hora
                { typeof(AcademicPeriod), 60 },       // 1 hora
                { typeof(AcademicProgram), 60 },      // 1 hora
                { typeof(ResearchGroup), 60 },        // 1 hora
                { typeof(Faculty), 60 },              // 1 hora
                { typeof(IdentificationType), 240 },  // 4 horas
                { typeof(Role), 120 },                // 2 horas
                { typeof(Permission), 120 },          // 2 horas
                { typeof(ThematicArea), 60 }          // 1 hora
            };
        }

        /// <summary>
        /// Obtiene un repositorio con caché para una entidad
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <typeparam name="TId">Tipo de ID de la entidad</typeparam>
        /// <returns>Un repositorio decorado con caché</returns>
        public IRepository<T, TId> GetCachedRepository<T, TId>() 
            where T : BaseEntity<TId> 
            where TId : struct
        {
            var repository = _unitOfWork.GetRepository<T, TId>();
            
            // Si la entidad es paramétrica, aplicamos caché
            if (_entityCacheTimes.TryGetValue(typeof(T), out int cacheMinutes))
            {
                var logger = _loggerFactory.CreateLogger<CachedRepository<T, TId>>();
                return new CachedRepository<T, TId>(repository, _cacheService, logger, cacheMinutes);
            }
            
            // Para otras entidades, devolvemos el repositorio original sin caché
            return repository;
        }

        /// <summary>
        /// Determina si una entidad es paramétrica y debería usar caché
        /// </summary>
        public bool IsParametricEntity<T>() where T : class
        {
            return _entityCacheTimes.ContainsKey(typeof(T));
        }
    }
}