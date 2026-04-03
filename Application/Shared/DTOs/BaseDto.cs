using System.Text.Json.Serialization;

namespace Application.Shared.DTOs
{
    public record BaseDto<TId> where TId : struct
    {
        public TId Id { get; set; }

        /// <summary>ID interno del usuario que creó el registro — no se expone al cliente.</summary>
        [JsonIgnore]
        public int? IdUserCreatedAt { get; set; }

        /// <summary>Fecha de creación — el frontend la usa en columnas "Fecha Registro".</summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>ID interno del usuario que modificó el registro — no se expone al cliente.</summary>
        [JsonIgnore]
        public int? IdUserUpdatedAt { get; set; }

        /// <summary>Fecha de última modificación — puede usarse en el frontend.</summary>
        public DateTimeOffset? UpdatedAt { get; set; }

        /// <summary>Texto de auditoría interna ("UPDATE"/"INSERT") — no se expone al cliente.</summary>
        [JsonIgnore]
        public string OperationRegister { get; set; } = string.Empty;

        /// <summary>Estado activo/inactivo del registro — el frontend lo muestra como "Activo/Inactivo".</summary>
        public bool StatusRegister { get; set; } = true;
    }
}


