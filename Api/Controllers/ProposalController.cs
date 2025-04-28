using Api.Responses;
using Application.Shared.DTOs.Proposal;
using Application.Shared.Queries.Proposal;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class ProposalController : GenericController<Proposal, int, ProposalDto>
    {
        private readonly IMediator _mediator;

        public ProposalController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            try
            {
                var query = new GetProposalWithDetailsQuery(id);
                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<ProposalWithDetailsResponseDto> { Success = true, Data = result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"No se encontró la propuesta con ID {id}" }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener la propuesta: {ex.Message}" }
                });
            }
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id)
        {
            try
            {
                var query = new GetProposalsByUserQuery(id);
                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<List<ProposalWithDetailsResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener las propuestas del usuario: {ex.Message}" }
                });
            }
        }
    }
}