using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWeb_Ventas.Models
{
    public class RegistroAuditoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRegistroAuditoria { get; set; }

        public int? IdUsuario { get; set; }

        [Required, StringLength(100)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string RolUsuario { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Accion { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Entidad { get; set; } = string.Empty;

        public int? IdEntidad { get; set; }

        [StringLength(500)]
        public string? Detalle { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }
    }
}
