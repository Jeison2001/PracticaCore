using Application.Shared.DTOs.Users;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.Users.Handlers
{
    public class GetStudentLookupByIdentificationQueryHandler : IRequestHandler<GetStudentLookupByIdentificationQuery, StudentLookupDto?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetStudentLookupByIdentificationQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<StudentLookupDto?> Handle(GetStudentLookupByIdentificationQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Identification))
                return null;

            var trimmed = request.Identification.Trim();

            var user = await _unitOfWork.GetRepository<User, int>()
                .GetFirstOrDefaultAsync(u => u.Identification == trimmed && u.StatusRegister, cancellationToken);
            if (user == null)
                return null;

            var studentRole = await _unitOfWork.GetRepository<Role, int>()
                .GetFirstOrDefaultAsync(r => r.Code == "STUDENT" && r.StatusRegister, cancellationToken);
            if (studentRole == null)
                return null;

            var hasActiveStudentRole = await _unitOfWork.GetRepository<UserRole, int>()
                .AnyAsync(ur => ur.IdUser == user.Id && ur.IdRole == studentRole.Id && ur.StatusRegister, cancellationToken);
            if (!hasActiveStudentRole)
                return null;

            return new StudentLookupDto
            {
                IdIdentificationType = user.IdIdentificationType,
                Identification = user.Identification,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };
        }
    }
}
