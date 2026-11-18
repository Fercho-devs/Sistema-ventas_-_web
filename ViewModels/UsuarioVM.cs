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
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "El DNI debe tener 8 dígitos")]
        public string DNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^9[0-9]{8}$", ErrorMessage = "Ingresa un teléfono peruano válido de 9 dígitos")]
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

        public int IdRol { get; set; }

        public List<Rol> Roles { get; set; } = new();
    }
}
