using Microsoft.AspNetCore.Mvc;

namespace SistemaWeb_Ventas.Controllers
{
    public class WelcomeController : Controller
    {
        private IActionResult? VerificarSesion(string rolRequerido)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (string.IsNullOrEmpty(rol) || rol != rolRequerido)
                return RedirectToAction("LogIn", "Login"); 
            return null;
        }

        public IActionResult Administrador()
        {
            var check = VerificarSesion("Administrador");
            if (check != null) return check;
            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            return View();
        }

        public IActionResult Empleado()
        {
            var check = VerificarSesion("Empleado");
            if (check != null) return check;
            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            return View();
        }

        public IActionResult Repartidor()
        {
            var check = VerificarSesion("Repartidor");
            if (check != null) return check;
            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            return View();
        }
    }
}