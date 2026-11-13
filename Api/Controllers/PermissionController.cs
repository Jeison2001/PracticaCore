using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Permissions;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class PermissionController : GenericController<Permission, int, PermissionDto>
    {
        public PermissionController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU, PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id) => await base.GetById(id);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU, PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpGet]
        public override async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request) => await base.GetAll(request);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] PermissionDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<PermissionDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] PermissionDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}