using System.ComponentModel.DataAnnotations;
using SistemaWeb_Ventas.Models;

namespace SistemaWeb_Ventas.ViewModels
{
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [StringLength(15)]
        public string DNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El celular es obligatorio")]
        [StringLength(15)]
        public string Celular { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Direccion { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la contraseña")]
        [DataType(DataType.Password)]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarContrasena { get; set; } = string.Empty;

        // Se preseleccionará "Cliente" por defecto en la vista
        public int IdRol { get; set; } = 4;

        // Lista para llenar el dropdown (aunque solo mostraremos Cliente en el registro público)
        public List<Rol> Roles { get; set; } = new();
    }
}