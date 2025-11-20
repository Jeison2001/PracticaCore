using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Proposals;
using Application.Shared.Queries.Proposals;
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

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllProposalsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var query = new GetProposalWithDetailsQuery(id);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<ProposalWithDetailsResponseDto> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetProposalsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<ProposalWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(
            int id, 
            [FromQuery] PaginatedRequest request)
        {
            var query = new GetProposalsByTeacherQuery(
                id, 
                request.PageNumber, 
                request.PageSize, 
                request.SortBy ?? "", 
                request.IsDescending, 
                request.Filters ?? new Dictionary<string, string>());
            
            var result = await _mediator.Send(query);
            
            return Ok(new ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>> { 
                Success = true, 
                Data = result,
                Messages = new List<string> { $"Se encontraron {result.TotalRecords} propuestas asignadas al docente." }
            });
        }
        
        [NonAction]
        public override Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }
    }
}