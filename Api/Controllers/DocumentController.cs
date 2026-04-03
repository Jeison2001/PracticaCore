using Application.Shared.Commands.Documents;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Documents;
using Application.Shared.Queries.Documents;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Interfaces.Services.Storage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;
        private readonly IFileStorageService _fileStorageService;
        
        public DocumentController(IMediator mediator, IConfiguration configuration, IFileStorageService fileStorageService)
        {
            _mediator = mediator;
            _configuration = configuration;
            _fileStorageService = fileStorageService;
        }

        /// <summary>
        /// Obtiene un documento por su ID.
        /// </summary>
        /// <param name="id">Id del documento.</param>
        /// <returns>Documento.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetDocumentByIdQuery(id));
            if (result == null)
            {
                return NotFound(new Responses.ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Errors = new List<string> { "Documento no encontrado" },
                    Messages = new List<string>()
                });
            }
            return Ok(new Responses.ApiResponse<DocumentDto>
            {
                Success = true,
                Data = result,
                Errors = new List<string>(),
                Messages = new List<string>()
            });
        }

        /// <summary>
        /// Descarga o previsualiza el archivo asociado a un documento.
        /// </summary>
        /// <param name="id">Id del documento.</param>
        /// <returns>Archivo como blob.</returns>
        [HttpGet("File/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var result = await _mediator.Send(new GetDocumentByIdQuery(id));
            if (result == null)
            {
                return NotFound(new Responses.ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Errors = new List<string> { "Documento no encontrado" },
                    Messages = new List<string>()
                });
            }
            var fileStream = await _fileStorageService.GetFileAsync(result.StoredFileName, HttpContext.RequestAborted);
            var mimeType = string.IsNullOrEmpty(result.MimeType) ? "application/octet-stream" : result.MimeType;
            return File(fileStream, mimeType, result.OriginalFileName);
        }

        /// <summary>
        /// Obtiene una lista paginada y filtrada de documentos asociados a una modalidad de inscripción.
        /// Permite filtrar por idStageModality e idDocumentClass, además de parámetros de paginación y ordenamiento.
        /// </summary>
        /// <param name="id">Id de la modalidad de inscripción.</param>
        /// <param name="request">Parámetros de paginación, filtrado y ordenamiento.</param>
        /// <param name="idStageModality">Id de la modalidad de etapa (opcional).</param>
        /// <param name="idDocumentClass">Id de la clase de documento (opcional).</param>
        /// <returns>Lista paginada de documentos.</returns>
        [HttpGet("ByInscriptionModality/{id}")]
        public async Task<IActionResult> GetByInscriptionModality(
            int id, 
            [FromQuery] PaginatedRequest request,
            [FromQuery] int? idStageModality = null,
            [FromQuery] int? idDocumentClass = null)
        {
            var query = new GetDocumentsByInscriptionModalityQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                request.Filters,
                idStageModality,
                idDocumentClass
            );
            
            var result = await _mediator.Send(query);
            return Ok(new Responses.ApiResponse<PaginatedResult<DocumentDto>>
            {
                Success = true,
                Data = result,
                Errors = new List<string>(),
                Messages = new List<string>()
            });
        }

        /// <summary>
        /// Sube un nuevo documento con archivo.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] DocumentUploadDto dto)
        {
            var command = new CreateDocumentWithFileCommand(dto, User.GetCurrentUserInfo());
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(DownloadFile), new { id = result.Id }, new Responses.ApiResponse<DocumentDto>
            {
                Success = true,
                Data = result,
                Errors = new List<string>(),
                Messages = new List<string>()
            });
        }

        /// <summary>
        /// Actualiza los metadatos de un documento y opcionalmente reemplaza el archivo.
        /// </summary>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] DocumentUpdateDto dto)
        {
            var command = new UpdateDocumentWithFileCommand(id, dto, User.GetCurrentUserInfo());
            var result = await _mediator.Send(command);
            return Ok(new Responses.ApiResponse<DocumentDto>
            {
                Success = true,
                Data = result,
                Errors = new List<string>(),
                Messages = new List<string>()
            });
        }

        /// <summary>
        /// Cambia el estado StatusRegister de un documento.
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
        {
            var command = new UpdateDocumentStatusCommand(id, dto.StatusRegister, User.GetCurrentUserInfo(), dto.OperationRegister);
            var result = await _mediator.Send(command);
            
            if (!result)
            {
                return NotFound(new Responses.ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Errors = new List<string> { "Documento no encontrado" },
                    Messages = new List<string>()
                });
            }

            return Ok(new Responses.ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Errors = new List<string>(),
                Messages = new List<string> { "Estado actualizado correctamente" }
            });
        }
    }
}