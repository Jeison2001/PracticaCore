using Domain.Entities;
using AutoMapper;
using Moq;
using Application.Shared.Mappings;
using Domain.Interfaces.Repositories;

namespace Tests.Shared
{
    public abstract class BaseTest
    {
        protected readonly IMapper _mapper;

        public BaseTest()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<GenericProfile>());
            _mapper = config.CreateMapper();
        }

        protected static Mock<IRepository<T, TId>> GetMockRepository<T, TId>()
            where T : BaseEntity<TId>
            where TId : struct
            => new();
    }
}
