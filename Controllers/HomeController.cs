using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;
using System.Diagnostics;

namespace SistemaWeb_Ventas.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var rol = HttpContext.Session.GetString("UsuarioRol")
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                return rol switch
                {
                    "Administrador" => RedirectToAction("Administrador", "Welcome"),
                    "Empleado" => RedirectToAction("Empleado", "Welcome"),
                    "Repartidor" => RedirectToAction("Index", "Repartidor"),
                    _ => await VistaCliente()
                };
            }

            var usuarioIdString = HttpContext.Session.GetString("UsuarioId");

            if (!string.IsNullOrEmpty(usuarioIdString) && int.TryParse(usuarioIdString, out int idUsuario))
            {
                // Consultamos los 2 pedidos más recientes del cliente
                var pedidosRecientes = await _db.Pedidos
                    .Where(p => p.IdCliente == idUsuario)
                    .OrderByDescending(p => p.FechaPedido)
                    .Take(2)
                    .ToListAsync();

                // Los enviamos a la vista mediante ViewBag
                ViewBag.PedidosRecientes = pedidosRecientes;
            }

            return View();
        }

        private async Task<IActionResult> VistaCliente()
        {
            var usuarioIdString = HttpContext.Session.GetString("UsuarioId");

            if (!string.IsNullOrEmpty(usuarioIdString) && int.TryParse(usuarioIdString, out int idUsuario))
            {
                ViewBag.PedidosRecientes = await _db.Pedidos
                    .Where(p => p.IdCliente == idUsuario)
                    .OrderByDescending(p => p.FechaPedido)
                    .Take(2)
                    .ToListAsync();
            }

            return View("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}