using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Faculties;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class FacultyController : GenericController<Faculty, int, FacultyDto>
    {
        public FacultyController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] FacultyDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<FacultyDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] FacultyDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}