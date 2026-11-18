using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;

namespace SistemaWeb_Ventas.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AuditoriaController : Controller
    {
        private readonly AppDbContext _db;

        public AuditoriaController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int pagina = 1)
        {
            const int tamanoPagina = 20;
            pagina = Math.Max(pagina, 1);

            var consulta = _db.RegistrosAuditoria.Include(r => r.Usuario);
            var total = await consulta.CountAsync();

            var registros = await consulta
                .OrderByDescending(r => r.FechaHora)
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
            return View(registros);
        }
    }
}
