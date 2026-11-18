using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;
using SistemaWeb_Ventas.ViewModels;

namespace SistemaWeb_Ventas.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuarioController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public UsuarioController(AppDbContext db, IPasswordHasher<Usuario> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Index(int pagina = 1)
        {
            const int tamanoPagina = 10;
            pagina = Math.Max(pagina, 1);

            var consulta = _db.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol != null && u.Rol.NombreRol != "Cliente");

            var total = await consulta.CountAsync();
            var usuarios = await consulta
                .OrderBy(u => u.Rol!.NombreRol)
                .ThenBy(u => u.NombreUsuario)
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
            return View(usuarios);
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var vm = new UsuarioVM
            {
                Roles = await _db.Roles
                    .Where(r => r.NombreRol != "Cliente")
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(UsuarioVM model)
        {
            model.Roles = await _db.Roles
                .Where(r => r.NombreRol != "Cliente")
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(model);

            var rol = await _db.Roles
                .FirstOrDefaultAsync(r => r.IdRol == model.IdRol && r.NombreRol != "Cliente");

            if (rol == null)
            {
                ModelState.AddModelError("IdRol", "El rol seleccionado no es válido.");
                return View(model);
            }

            var correo = model.Correo.Trim().ToLower();
            if (await _db.Usuarios.AnyAsync(u => u.Correo.ToLower() == correo))
            {
                ModelState.AddModelError("Correo", "Ya existe una cuenta con este correo.");
                return View(model);
            }

            var usuario = new Usuario
            {
                NombreUsuario = model.NombreUsuario,
                DNI = model.DNI,
                Telefono = model.Telefono,
                Direccion = model.Direccion,
                Correo = model.Correo.Trim(),
                IdRol = rol.IdRol
            };

            usuario.Contrasena = _passwordHasher.HashPassword(usuario, model.Contrasena);
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            TempData["MensajeExito"] = "Cuenta creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            var vm = new UsuarioVM
            {
                IdRol = usuario.IdRol,
                NombreUsuario = usuario.NombreUsuario,
                DNI = usuario.DNI,
                Telefono = usuario.Telefono,
                Direccion = usuario.Direccion,
                Correo = usuario.Correo,
                Roles = await _db.Roles
                    .Where(r => r.NombreRol != "Cliente")
                    .ToListAsync()
            };

            ViewBag.IdUsuario = usuario.IdUsuario;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, UsuarioVM model)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            model.Roles = await _db.Roles
                .Where(r => r.NombreRol != "Cliente")
                .ToListAsync();

            ModelState.Remove(nameof(UsuarioVM.Contrasena));
            ModelState.Remove(nameof(UsuarioVM.ConfirmarContrasena));

            if (!ModelState.IsValid)
            {
                ViewBag.IdUsuario = id;
                return View(model);
            }

            var rol = await _db.Roles
                .FirstOrDefaultAsync(r => r.IdRol == model.IdRol && r.NombreRol != "Cliente");

            if (rol == null)
            {
                ModelState.AddModelError("IdRol", "El rol seleccionado no es válido.");
                ViewBag.IdUsuario = id;
                return View(model);
            }

            var correo = model.Correo.Trim().ToLower();
            if (await _db.Usuarios.AnyAsync(u => u.IdUsuario != id && u.Correo.ToLower() == correo))
            {
                ModelState.AddModelError("Correo", "Ya existe una cuenta con este correo.");
                ViewBag.IdUsuario = id;
                return View(model);
            }

            usuario.NombreUsuario = model.NombreUsuario;
            usuario.DNI = model.DNI;
            usuario.Telefono = model.Telefono;
            usuario.Direccion = model.Direccion;
            usuario.Correo = model.Correo.Trim();
            usuario.IdRol = rol.IdRol;

            await _db.SaveChangesAsync();
            TempData["MensajeExito"] = "Cuenta actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(int id, string nuevaContrasena)
        {
            if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Length < 6)
            {
                TempData["ErrorUsuario"] = "La contraseña debe tener al menos 6 caracteres.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            usuario.Contrasena = _passwordHasher.HashPassword(usuario, nuevaContrasena);
            await _db.SaveChangesAsync();

            TempData["MensajeExito"] = "Contraseña actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioActualId = int.TryParse(HttpContext.Session.GetString("UsuarioId"), out var valor)
                ? valor
                : 0;

            if (usuarioActualId == id)
            {
                TempData["ErrorUsuario"] = "No puedes eliminar tu propia cuenta.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _db.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
                return RedirectToAction(nameof(Index));

            var tieneRelacion = await _db.Pedidos.AnyAsync(p => p.IdCliente == id)
                || await _db.Entregas.AnyAsync(e => e.IdRepartidor == id)
                || await _db.Carritos.AnyAsync(c => c.IdUsuario == id);

            if (tieneRelacion)
            {
                TempData["ErrorUsuario"] = "No se puede eliminar una cuenta con pedidos o entregas asociadas.";
                return RedirectToAction(nameof(Index));
            }

            _db.Usuarios.Remove(usuario);
            await _db.SaveChangesAsync();

            TempData["MensajeExito"] = "Cuenta eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
