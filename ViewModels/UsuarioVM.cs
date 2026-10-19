using SistemaWeb_Ventas.Models;
using System.ComponentModel.DataAnnotations;

namespace SistemaWeb_Ventas.ViewModels
{
    public class UsuarioVM
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [StringLength(15)]
        public string DNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(15)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Direccion { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato inválido")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la contraseña")]
        [DataType(DataType.Password)]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona un rol")]
        public int IdRol { get; set; }

        public List<Rol> Roles { get; set; } = new();
    }
}
