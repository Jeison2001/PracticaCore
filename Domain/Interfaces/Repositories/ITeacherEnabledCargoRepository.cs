using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface ITeacherEnabledCargoRepository : IScopedService
    {
        Task<List<TeacherEnabledCargo>> GetByUserIdAsync(int userId, CancellationToken ct = default);
        Task<List<TeacherEnabledCargo>> GetByCargoIdAsync(int cargoId, CancellationToken ct = default);
        Task BulkAssignAsync(int userId, IEnumerable<int> typeTeachingAssignmentIds, int? operationUserId, CancellationToken ct = default);
    }
}
