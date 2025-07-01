using Application.Shared.Commands.AcademicPractice;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands.AcademicPractice.Handlers
{
    public class UpdateAcademicPracticeInstitutionCommandHandler : IRequestHandler<UpdateAcademicPracticeInstitutionCommand, bool>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAcademicPracticeInstitutionCommandHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IUnitOfWork unitOfWork)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateAcademicPracticeInstitutionCommand request, CancellationToken cancellationToken)
        {
            var academicPractice = await _academicPracticeRepository.GetByIdAsync(request.AcademicPracticeId);
            
            if (academicPractice == null)
                return false;

            // Update institution information
            if (!string.IsNullOrEmpty(request.InstitutionName))
                academicPractice.InstitutionName = request.InstitutionName;
                
            if (!string.IsNullOrEmpty(request.InstitutionContact))
                academicPractice.InstitutionContact = request.InstitutionContact;
                
            if (request.PracticeStartDate.HasValue)
                academicPractice.PracticeStartDate = request.PracticeStartDate;
                
            if (request.PracticeEndDate.HasValue)
                academicPractice.PracticeEndDate = request.PracticeEndDate;
                
            if (request.PracticeHours.HasValue)
                academicPractice.PracticeHours = request.PracticeHours;
                
            if (request.IsEmprendimiento.HasValue)
                academicPractice.IsEmprendimiento = request.IsEmprendimiento.Value;
                
            if (!string.IsNullOrEmpty(request.Observations))
                academicPractice.Observations = request.Observations;

            academicPractice.UpdatedAt = DateTime.UtcNow;
            academicPractice.IdUserUpdatedAt = request.UserId;

            await _academicPracticeRepository.UpdateAsync(academicPractice);
            await _unitOfWork.CommitAsync(cancellationToken);

            return true;
        }
    }
}
