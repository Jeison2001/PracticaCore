using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.TypeTeachingAssignments;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class TypeTeachingAssignmentController : GenericController<TypeTeachingAssignment, int, TypeTeachingAssignmentDto>
    {
        public TypeTeachingAssignmentController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSC, PermissionCodes.Configuraciones.N3CFSU)]
        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id) => await base.GetById(id);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSC, PermissionCodes.Configuraciones.N3CFSU)]
        [HttpGet]
        public override async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request) => await base.GetAll(request);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSC)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] TypeTeachingAssignmentDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSC)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<TypeTeachingAssignmentDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSC)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] TypeTeachingAssignmentDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSC)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}