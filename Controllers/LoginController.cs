using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;
using SistemaWeb_Ventas.ViewModels;

namespace SistemaWeb_Ventas.Controllers
{
    public class LoginController : Controller
    {
        private readonly AppDbContext _db;

        public LoginController(AppDbContext db)
        {
            _db = db;
        }

        // GET: Muestra el formulario de login
        [HttpGet]
        public IActionResult Index()
        {
            return View(new LoginViewModel());
        }

        // POST: Procesa el inicio de sesión
        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Buscar usuario por correo (insensible a mayúsculas/minúsculas)
            var usuario = await _db.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Correo.ToLower() == model.Correo.ToLower().Trim());

            // Validar existencia y contraseña
            if (usuario == null || usuario.Contrasena != model.Contrasena.Trim())
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                return View(model);
            }

            // Guardar datos en la Sesión
            HttpContext.Session.SetString("UsuarioId", usuario.IdUsuario.ToString());
            HttpContext.Session.SetString("UsuarioNombre", usuario.NombreCompleto);
            HttpContext.Session.SetString("UsuarioRol", usuario.Rol!.NombreRol);

            // Redirigir según el rol (puedes ajustar estas rutas luego)
            return usuario.Rol.NombreRol switch
            {
                "Administrador" => RedirectToAction("Index", "Home"), // O un panel admin específico
                "Empleado" => RedirectToAction("Index", "Home"),
                "Repartidor" => RedirectToAction("Index", "Home"),
                "Cliente" => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // GET: Muestra el formulario de registro
        [HttpGet]
        public async Task<IActionResult> Registro()
        {
            var vm = new RegistroViewModel
            {
                // Solo permitimos que se registren como "Cliente" desde el formulario público
                Roles = await _db.Roles.Where(r => r.NombreRol == "Cliente").ToListAsync()
            };
            return View(vm);
        }

        // POST: Procesa el nuevo registro
        [HttpPost]
        public async Task<IActionResult> Registro(RegistroViewModel model)
        {
            // Volver a cargar los roles por si el ModelState falla
            model.Roles = await _db.Roles.Where(r => r.NombreRol == "Cliente").ToListAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar si el correo ya existe
            if (await _db.Usuarios.AnyAsync(u => u.Correo.ToLower() == model.Correo.ToLower()))
            {
                ModelState.AddModelError("Correo", "Ya existe una cuenta registrada con este correo.");
                return View(model);
            }

            // Crear nuevo usuario
            var nuevoUsuario = new Usuario
            {
                NombreCompleto = model.NombreCompleto,
                DNI = model.DNI,
                Celular = model.Celular,
                Direccion = model.Direccion,
                Correo = model.Correo,
                Contrasena = model.Contrasena,
                IdRol = model.IdRol // Será 4 (Cliente)
            };

            _db.Usuarios.Add(nuevoUsuario);
            await _db.SaveChangesAsync();

            TempData["MensajeExito"] = "Cuenta creada exitosamente. Ahora puedes iniciar sesión.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cierra la sesión
        [HttpPost]
        public IActionResult Salir()
        {
            HttpContext.Session.Clear();

            // Evitar que el navegador guarde la página en caché tras salir
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Index", "Home");
        }
    }
}