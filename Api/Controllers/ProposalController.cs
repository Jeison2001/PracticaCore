using Application.Shared.DTOs.Proposal;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class ProposalController : GenericController<Proposal, int, ProposalDto>
    {
        public ProposalController(IMediator mediator) : base(mediator)
        {
        }
    }
}