using Domain.Entities;
using AutoMapper;
using Moq;
using Application.Shared.Mappings;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Shared
{
    public abstract class BaseTest
    {
        protected readonly IMapper _mapper;

        public BaseTest()
        {
            var services = new ServiceCollection();
            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(GenericProfile).Assembly));
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();
        }

        protected static Mock<IRepository<T, TId>> GetMockRepository<T, TId>()
            where T : BaseEntity<TId>
            where TId : struct
            => new();
    }
}
