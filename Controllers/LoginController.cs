
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;
using SistemaWeb_Ventas.ViewModels;
using System.Security.Claims;

namespace SistemaWeb_Ventas.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class LoginController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public LoginController(
            AppDbContext db,
            IPasswordHasher<Usuario> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        // GET: Mostrar formulario de inicio de sesión
        [AllowAnonymous]
        [HttpGet]
        public IActionResult LogIn()
        {
            return View(new LoginVM());
        }

        // POST: Procesar inicio de sesión
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LogIn(LoginVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var correo = model.Correo.Trim().ToLower();

            var usuario = await _db.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u =>
                    u.Correo.ToLower() == correo);

            if (usuario == null || usuario.Rol == null)
            {
                ModelState.AddModelError(
                    "",
                    "Correo o contraseña incorrectos.");

                return View(model);
            }

            // Verificar la contraseña.
            // Admite temporalmente contraseñas antiguas en texto plano.
            var resultado = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.Contrasena,
                model.Contrasena);

            bool autenticado =
                resultado != PasswordVerificationResult.Failed;

            bool contrasenaAntigua =
                !autenticado &&
                usuario.Contrasena == model.Contrasena;

            if (!autenticado && !contrasenaAntigua)
            {
                ModelState.AddModelError(
                    "",
                    "Correo o contraseña incorrectos.");

                return View(model);
            }

            // Actualizar la contraseña antigua a un hash seguro.
            if (contrasenaAntigua ||
                resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.Contrasena =
                    _passwordHasher.HashPassword(
                        usuario,
                        model.Contrasena);

                await _db.SaveChangesAsync();
            }

            // Crear los Claims del usuario autenticado.
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.IdUsuario.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    usuario.NombreUsuario),

                new Claim(
                    ClaimTypes.Email,
                    usuario.Correo),

                new Claim(
                    ClaimTypes.Role,
                    usuario.Rol.NombreRol)
            };

            // Construir la identidad y el principal.
            var identidad = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identidad);

            // Crear la cookie de autenticación.
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // Mantener los datos de sesión que usa la aplicación.
            HttpContext.Session.SetString(
                "UsuarioId",
                usuario.IdUsuario.ToString());

            HttpContext.Session.SetString(
                "UsuarioNombre",
                usuario.NombreUsuario);

            HttpContext.Session.SetString(
                "UsuarioRol",
                usuario.Rol.NombreRol);

            HttpContext.Session.SetString(
                "UsuarioCorreo",
                usuario.Correo);

            // Redirigir según el rol.
            return usuario.Rol.NombreRol switch
            {
                "Administrador" => RedirectToAction("Administrador", "Welcome"),
                "Empleado" => RedirectToAction("Empleado", "Welcome"),
                "Repartidor" => RedirectToAction("Repartidor", "Welcome"),
                "Cliente" => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // GET: Mostrar formulario de registro
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Registro()
        {
            var vm = new UsuarioVM
            {
                Roles = await _db.Roles
                    .Where(r => r.NombreRol == "Cliente")
                    .ToListAsync()
            };

            return View(vm);
        }

        // POST: Registrar un nuevo cliente
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Registro(
            UsuarioVM model)
        {
            // Cargar los roles que se muestran en el formulario.
            model.Roles = await _db.Roles
                .Where(r => r.NombreRol == "Cliente")
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar que el rol Cliente exista en la base de datos.
            var rolCliente = await _db.Roles
                .FirstOrDefaultAsync(r => r.NombreRol == "Cliente");

            if (rolCliente == null)
            {
                ModelState.AddModelError(
                    "",
                    "No está disponible el registro de clientes.");

                return View(model);
            }

            // No confiar en el IdRol enviado por el navegador.
            // Asignar siempre el rol Cliente desde el servidor.
            var correo = model.Correo.Trim().ToLower();

            // Comprobar si el correo ya está registrado.
            if (await _db.Usuarios.AnyAsync(u =>
                u.Correo.ToLower() == correo))
            {
                ModelState.AddModelError(
                    "Correo",
                    "Ya existe una cuenta registrada con este correo.");

                return View(model);
            }

            // Crear el usuario sin guardar la contraseña en texto plano.
            var nuevoUsuario = new Usuario
            {
                NombreUsuario = model.NombreUsuario,
                DNI = model.DNI,
                Telefono = model.Telefono,
                Direccion = model.Direccion,
                Correo = model.Correo.Trim(),
                IdRol = rolCliente.IdRol
            };

            // Generar un hash seguro para la contraseña.
            nuevoUsuario.Contrasena =
                _passwordHasher.HashPassword(
                    nuevoUsuario,
                    model.Contrasena);

            _db.Usuarios.Add(nuevoUsuario);
            await _db.SaveChangesAsync();

            TempData["MensajeExito"] =
                "Cuenta creada exitosamente. Ahora puedes iniciar sesión.";

            return RedirectToAction(nameof(LogIn));
        }

        // POST: Cerrar sesión
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Clear();

            Response.Headers["Cache-Control"] =
                "no-cache, no-store, must-revalidate";

            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction(nameof(LogIn));
        }

        // GET: Página para usuarios sin permisos
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}
