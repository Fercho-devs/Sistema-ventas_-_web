using System.ComponentModel.DataAnnotations;

namespace SistemaWeb_Ventas.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; } = string.Empty;
    }
}
