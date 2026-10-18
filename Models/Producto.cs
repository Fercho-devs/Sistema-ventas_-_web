using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWeb_Ventas.Models
{
    public class Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;


        [StringLength(300)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "Selecciona al menos una talla")]
        [StringLength(150)]
        public string? Talla { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Precision(10,2)]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }

        [StringLength(300)]
        public string? ImagenUrl { get; set; }

        public bool Activo { get; set; } = true;

        public int IdCategoria { get; set; }
        [ForeignKey("IdCategoria")]
        public Categoria? Categoria { get; set; }

        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
        public ICollection<CarritoDetalle> CarritoDetalles { get; set; } = new List<CarritoDetalle>();
    }
}
