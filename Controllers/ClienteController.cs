using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;

namespace SistemaWeb_Ventas.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ClienteController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public ClienteController(AppDbContext db, IPasswordHasher<Usuario> passwordHasher)
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
                .Include(u => u.Pedidos)
                .Where(u => u.Rol != null && u.Rol.NombreRol == "Cliente");

            var total = await consulta.CountAsync();
            var clientes = await consulta
                .OrderBy(u => u.NombreUsuario)
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
            return View(clientes);
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var cliente = await _db.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Pedidos).ThenInclude(p => p.TipoEntrega)
                .Include(u => u.Pedidos).ThenInclude(p => p.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(u => u.IdUsuario == id
                    && u.Rol != null
                    && u.Rol.NombreRol == "Cliente");

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(int id, string nuevaContrasena)
        {
            if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Length < 6)
            {
                TempData["ErrorCliente"] = "La contraseña debe tener al menos 6 caracteres.";
                return RedirectToAction(nameof(Index));
            }

            var cliente = await _db.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.IdUsuario == id
                    && u.Rol != null
                    && u.Rol.NombreRol == "Cliente");

            if (cliente == null)
                return NotFound();

            cliente.Contrasena = _passwordHasher.HashPassword(cliente, nuevaContrasena);
            await _db.SaveChangesAsync();

            TempData["MensajeExito"] = "Contraseña restablecida correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
