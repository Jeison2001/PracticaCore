using Api.Controllers;
using Application.Shared.DTOs;
using Application.Shared.Queries;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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
        /// Obtiene todos los documentos asociados a una inscripción (IdInscriptionModality).
        /// </summary>
        /// <param name="id">Id de la modalidad de inscripción.</param>
        /// <param name="request">Parámetros de paginación, filtrado y ordenamiento.</param>
        /// <returns>Lista de documentos.</returns>
        [HttpGet("ByInscriptionModality/{id}")]
        public async Task<IActionResult> GetByInscriptionModality(int id, [FromQuery] PaginatedRequest request)
        {
            try
            {
                var query = new Application.Shared.Queries.GetDocumentsByInscriptionModalityQuery(
                    id,
                    request.PageNumber,
                    request.PageSize,
                    request.SortBy,
                    request.IsDescending,
                    request.Filters
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Responses.ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Errors = new List<string> { $"Error al obtener los documentos: {ex.Message}" },
                    Messages = new List<string>()
                });
            }
        }

        /// <summary>
        /// Sube un nuevo documento con archivo.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] DocumentUploadDto dto)
        {
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var fileExt = System.IO.Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExt))
            {
                return BadRequest(new Responses.ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Errors = new List<string> { $"Formato de archivo no permitido. Solo se aceptan: {string.Join(", ", allowedExtensions)}" },
                    Messages = new List<string>()
                });
            }
            try
            {
                var uniqueFileName = await _fileStorageService.SaveFileAsync(dto.File.OpenReadStream(), dto.File.FileName, HttpContext.RequestAborted);
                var command = new Application.Shared.Commands.CreateDocumentWithFileCommand(
                    dto,
                    uniqueFileName,
                    string.Empty, // StoragePath ya no se usa
                    dto.File.ContentType,
                    dto.File.Length
                );
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(DownloadFile), new { id = result.Id }, new Responses.ApiResponse<DocumentDto>
                {
                    Success = true,
                    Data = result,
                    Errors = new List<string>(),
                    Messages = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Responses.ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Errors = new List<string> { ex.Message },
                    Messages = new List<string>()
                });
            }
        }

        /// <summary>
        /// Actualiza los metadatos de un documento y opcionalmente reemplaza el archivo.
        /// </summary>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] DocumentUpdateDto dto)
        {
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            string? uniqueFileName = null;
            string? mimeType = null;
            long? fileSize = null;
            // Validar y guardar archivo solo si se envía uno nuevo
            if (dto.File != null)
            {
                var fileExt = System.IO.Path.GetExtension(dto.File.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExt))
                {
                    return BadRequest(new Responses.ApiResponse<object>
                    {
                        Success = false,
                        Data = null,
                        Errors = new List<string> { $"Formato de archivo no permitido. Solo se aceptan: {string.Join(", ", allowedExtensions)}" },
                        Messages = new List<string>()
                    });
                }
                uniqueFileName = await _fileStorageService.SaveFileAsync(dto.File.OpenReadStream(), dto.File.FileName, HttpContext.RequestAborted);
                mimeType = dto.File.ContentType;
                fileSize = dto.File.Length;
            }
            try
            {
                var command = new Application.Shared.Commands.UpdateDocumentWithFileCommand(
                    id,
                    dto,
                    uniqueFileName,
                    string.Empty, // StoragePath ya no se usa
                    mimeType,
                    fileSize
                );
                var result = await _mediator.Send(command);
                return Ok(new Responses.ApiResponse<DocumentDto>
                {
                    Success = true,
                    Data = result,
                    Errors = new List<string>(),
                    Messages = new List<string>()
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new Responses.ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Errors = new List<string> { ex.Message },
                    Messages = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Responses.ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Errors = new List<string> { ex.Message },
                    Messages = new List<string>()
                });
            }
        }

        /// <summary>
        /// Cambia el estado StatusRegister de un documento.
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
        {
            await Task.CompletedTask;
            // TODO: Implementar handler UpdateDocumentStatusCommand y su lógica
            return StatusCode(StatusCodes.Status501NotImplemented, new Responses.ApiResponse<object>
            {
                Success = false,
                Data = null,
                Errors = new List<string> { "No implementado: lógica de cambio de estado StatusRegister" },
                Messages = new List<string>()
            });
        }
    }
}