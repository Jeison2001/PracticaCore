using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicPeriods;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class AcademicPeriodController : GenericController<AcademicPeriod, int, AcademicPeriodDto>
    {
        public AcademicPeriodController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACP)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] AcademicPeriodDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACP)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<AcademicPeriodDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACP)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] AcademicPeriodDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACP)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}