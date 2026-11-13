using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Modalities;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class ModalityController : GenericController<Modality, int, ModalityDto>
    {
        public ModalityController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACM)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] ModalityDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACM)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<ModalityDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACM)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] ModalityDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACM)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}