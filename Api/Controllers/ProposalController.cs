using Api.Responses;
using Application.Shared.DTOs;
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

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener las propuestas con detalles: {ex.Message}" }
                });
            }
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
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            try
            {
                var query = new GetProposalsByUserQuery(id, status);
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

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(
            int id, 
            [FromQuery] PaginatedRequest request)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener las propuestas asignadas al docente: {ex.Message}" }
                });
            }
        }

        [NonAction]
        public override Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> GetById(int id)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }
    }
}