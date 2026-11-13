using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.DocumentTypes;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class DocumentTypeController : GenericController<DocumentType, int, DocumentTypeDto>
    {
        public DocumentTypeController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFDOT)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] DocumentTypeDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFDOT)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<DocumentTypeDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFDOT)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] DocumentTypeDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFDOT)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}