using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;

namespace SistemaWeb_Ventas.Controllers
{
    [Authorize(Roles = "Repartidor")]
    public class RepartidorController : Controller
    {
        private readonly AppDbContext _db;

        public RepartidorController(AppDbContext db)
        {
            _db = db;
        }

        private int GetUserId()
        {
            return int.TryParse(HttpContext.Session.GetString("UsuarioId"), out var id) ? id : 0;
        }

        private static DateTime GetHoraPeru()
        {
            try
            {
                var zona = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private void RegistrarAuditoria(string accion, int idEntrega, string detalle)
        {
            var idUsuario = int.TryParse(HttpContext.Session.GetString("UsuarioId"), out var id) ? id : (int?)null;

            _db.RegistrosAuditoria.Add(new RegistroAuditoria
            {
                IdUsuario = idUsuario,
                NombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Repartidor",
                RolUsuario = "Repartidor",
                Accion = accion,
                Entidad = "Entrega",
                IdEntidad = idEntrega,
                Detalle = detalle,
                FechaHora = GetHoraPeru()
            });
        }

        public async Task<IActionResult> Index()
        {
            var idRepartidor = GetUserId();

            var entregas = await _db.Entregas
                .Include(e => e.Pedido).ThenInclude(p => p!.Cliente)
                .Include(e => e.Pedido).ThenInclude(p => p!.TipoEntrega)
                .Include(e => e.Pedido).ThenInclude(p => p!.Detalles).ThenInclude(d => d.Producto)
                .Include(e => e.EstadosEntrega)
                .Where(e => e.IdRepartidor == idRepartidor
                    && e.Pedido != null
                    && e.Pedido.EstadoPedido != "Cancelado"
                    && e.Pedido.EstadoPedido != "Finalizado")
                .ToListAsync();

            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            return View(entregas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarEnCamino(int idEntrega)
        {
            var idRepartidor = GetUserId();
            var entrega = await _db.Entregas
                .Include(e => e.Pedido)
                .FirstOrDefaultAsync(e => e.IdEntrega == idEntrega && e.IdRepartidor == idRepartidor);

            if (entrega?.Pedido == null)
                return NotFound();

            if (entrega.Pedido.EstadoPedido != "Aprobado"
                || (entrega.Pedido.EstadoPago != "Pagado" && entrega.Pedido.EstadoPago != "Por cobrar"))
            {
                TempData["ErrorEntrega"] = "El pedido debe estar aprobado y tener un estado de pago válido.";
                return RedirectToAction(nameof(Index));
            }

            entrega.Pedido.EstadoPedido = "En despacho";
            _db.EstadosEntrega.Add(new EstadoEntrega
            {
                IdEntrega = entrega.IdEntrega,
                Estado = "En camino",
                Observacion = "Pedido recogido por el repartidor.",
                FechaHora = GetHoraPeru()
            });

            RegistrarAuditoria("Marcó entrega en camino", entrega.IdEntrega, "El repartidor inició el proceso de entrega.");
            await _db.SaveChangesAsync();
            TempData["MensajeExito"] = "La entrega fue marcada como en camino.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarEntregado(int idEntrega)
        {
            var idRepartidor = GetUserId();
            var entrega = await _db.Entregas
                .Include(e => e.Pedido)
                .FirstOrDefaultAsync(e => e.IdEntrega == idEntrega && e.IdRepartidor == idRepartidor);

            if (entrega?.Pedido == null)
                return NotFound();

            if (entrega.Pedido.EstadoPedido != "En despacho")
            {
                TempData["ErrorEntrega"] = "El pedido debe estar en despacho antes de completarlo.";
                return RedirectToAction(nameof(Index));
            }

            if (entrega.Pedido.EstadoPago == "Por cobrar")
                entrega.Pedido.EstadoPago = "Pagado";

            entrega.Pedido.EstadoPedido = "Finalizado";
            _db.EstadosEntrega.Add(new EstadoEntrega
            {
                IdEntrega = entrega.IdEntrega,
                Estado = "Entregado",
                Observacion = "Pedido entregado por el repartidor.",
                FechaHora = GetHoraPeru()
            });

            RegistrarAuditoria("Completó entrega", entrega.IdEntrega, "El repartidor confirmó la entrega del pedido.");
            await _db.SaveChangesAsync();
            TempData["MensajeExito"] = "La entrega fue completada.";
            return RedirectToAction(nameof(Index));
        }
    }
}
