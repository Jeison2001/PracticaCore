using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.ResearchSubLines;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class ResearchSubLineController : GenericController<ResearchSubLine, int, ResearchSubLineDto>
    {
        public ResearchSubLineController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINS)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] ResearchSubLineDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINS)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<ResearchSubLineDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINS)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] ResearchSubLineDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINS)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}