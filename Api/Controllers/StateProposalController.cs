using Application.Shared.DTOs.StateProposal;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class StateProposalController : GenericController<StateProposal, int, StateProposalDto>
    {
        public StateProposalController(IMediator mediator) : base(mediator)
        {
        }
    }
}