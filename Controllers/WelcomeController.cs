using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;

namespace SistemaWeb_Ventas.Controllers
{
    [Authorize]
    public class WelcomeController : Controller
    {
        private readonly AppDbContext _db;

        public WelcomeController(AppDbContext db)
        {
            _db = db;
        }

        private IActionResult? VerificarSesion(string rolRequerido)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (string.IsNullOrEmpty(rol) || rol != rolRequerido)
                return RedirectToAction("LogIn", "Login");
            return null;
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Administrador()
        {
            var check = VerificarSesion("Administrador");
            if (check != null) return check;

            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.TotalPedidos = await _db.Pedidos.CountAsync();
            ViewBag.PedidosPendientes = await _db.Pedidos.CountAsync(p => p.EstadoPedido == "Pendiente");
            ViewBag.PedidosFinalizados = await _db.Pedidos.CountAsync(p => p.EstadoPedido == "Finalizado");
            ViewBag.PedidosCancelados = await _db.Pedidos.CountAsync(p => p.EstadoPedido == "Cancelado");
            ViewBag.TotalClientes = await _db.Usuarios.CountAsync(u => u.Rol != null && u.Rol.NombreRol == "Cliente");
            ViewBag.ProductosActivos = await _db.Productos.CountAsync(p => p.Activo);
            ViewBag.TotalVentas = await _db.Pedidos
                .Where(p => p.EstadoPago == "Pagado" && p.EstadoPedido != "Cancelado")
                .SumAsync(p => (decimal?)p.Total) ?? 0;

            ViewBag.PedidosRecientes = await _db.Pedidos
                .Include(p => p.Cliente)
                .OrderByDescending(p => p.FechaPedido)
                .Take(5)
                .ToListAsync();

            return View();
        }

        [Authorize(Roles = "Empleado")]
        public IActionResult Empleado()
        {
            var check = VerificarSesion("Empleado");
            if (check != null) return check;
            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            return View();
        }

        [Authorize(Roles = "Repartidor")]
        public IActionResult Repartidor()
        {
            var check = VerificarSesion("Repartidor");
            if (check != null) return check;
            return RedirectToAction("Index", "Repartidor");
        }
    }
}