using Api.Controllers;
using Application.Shared.DTOs;
using Domain.Entities;
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
        public DocumentController(IMediator mediator, IConfiguration configuration)
        {
            _mediator = mediator;
            _configuration = configuration;
        }

        /// <summary>
        /// Descarga o previsualiza el archivo asociado a un documento.
        /// </summary>
        /// <param name="id">Id del documento.</param>
        /// <returns>Archivo como blob.</returns>
        [HttpGet("File/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            // Obtener el documento (puedes usar MediatR o el repositorio directamente)
            var result = await _mediator.Send(new Application.Shared.Queries.GetDocumentByIdQuery(id));
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

            var uploadsFolder = _configuration["FileStorage:LocalPath"] ?? "Uploads";
            var filePath = Path.Combine(uploadsFolder, result.StoredFileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new Responses.ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Errors = new List<string> { "Archivo no encontrado en el servidor" },
                    Messages = new List<string>()
                });
            }

            var mimeType = string.IsNullOrEmpty(result.MimeType) ? "application/octet-stream" : result.MimeType;
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, mimeType, result.OriginalFileName);
        }

        /// <summary>
        /// Obtiene todos los documentos asociados a una inscripción (IdInscriptionModality).
        /// </summary>
        /// <param name="id">Id de la modalidad de inscripción.</param>
        /// <returns>Lista de documentos.</returns>
        [HttpGet("ByInscriptionModality/{id}")]
        public async Task<IActionResult> GetByInscriptionModality(int id)
        {
            var result = await _mediator.Send(new Application.Shared.Queries.GetDocumentsByInscriptionModalityQuery(id));
            return Ok(new Responses.ApiResponse<List<DocumentDto>>
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
                var uploadsFolder = _configuration["FileStorage:LocalPath"] ?? "Uploads";
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExt}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }
                var command = new Application.Shared.Commands.CreateDocumentWithFileCommand(
                    dto,
                    uniqueFileName,
                    uploadsFolder,
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
            // TODO: Implementar handler UpdateDocumentWithFileCommand y su lógica
            return StatusCode(StatusCodes.Status501NotImplemented, new Responses.ApiResponse<object>
            {
                Success = false,
                Data = null,
                Errors = new List<string> { "No implementado: lógica de actualización de documento y archivo" },
                Messages = new List<string>()
            });
        }

        /// <summary>
        /// Cambia el estado StatusRegister de un documento.
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
        {
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