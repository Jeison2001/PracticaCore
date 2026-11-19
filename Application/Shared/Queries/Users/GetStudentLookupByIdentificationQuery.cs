using Application.Shared.DTOs.Users;
using MediatR;

namespace Application.Shared.Queries.Users
{
    public record GetStudentLookupByIdentificationQuery(string Identification) : IRequest<StudentLookupDto?>;
}
