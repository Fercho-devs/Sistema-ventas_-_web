using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWeb_Ventas.Models
{
    public class CarritoDetalle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCarritoDetalle { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [StringLength(50)]
        public string? TallaSeleccionada { get; set; }

        // FK Carrito
        public int IdCarrito { get; set; }
        [ForeignKey("IdCarrito")]
        public Carrito? Carrito { get; set; }

        // FK Producto
        public int IdProducto { get; set; }
        [ForeignKey("IdProducto")]
        public Producto? Producto { get; set; }
    }
}
