using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;

namespace SistemaWeb_Ventas.Controllers  // ✅ NAMESPACE CORREGIDO
{
    public class ProductoController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductoController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private IActionResult? VerificarAdmin()
        {
            if (HttpContext.Session.GetString("UsuarioRol") != "Administrador")
                return RedirectToAction("Index", "Login");
            return null;
        }

        public async Task<IActionResult> Index()
        {
            var check = VerificarAdmin();
            if (check != null) return check;
            return View(await _db.Productos.Include(p => p.Categoria).ToListAsync());
        }

        public async Task<IActionResult> Crear()
        {
            var check = VerificarAdmin();
            if (check != null) return check;
            ViewBag.Categorias = new SelectList(
                await _db.Categorias.ToListAsync(),
                "IdCategoria", "NombreCategoria");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Producto p, IFormFile? imagen)
        {
            try
            {
                // Limpiar propiedades de navegación
                ModelState.Remove("ImagenUrl");
                ModelState.Remove("Categoria");
                ModelState.Remove("DetallesPedido");
                ModelState.Remove("CarritoDetalles");

                // Validar imagen
                if (imagen == null || imagen.Length == 0)
                {
                    ModelState.AddModelError("", "Debes subir una imagen de referencia del producto.");
                    ViewBag.Categorias = new SelectList(
                        await _db.Categorias.ToListAsync(),
                        "IdCategoria", "NombreCategoria", p.IdCategoria);
                    return View(p);
                }

                // Validar modelo
                if (!ModelState.IsValid)
                {
                    ViewBag.Categorias = new SelectList(
                        await _db.Categorias.ToListAsync(),
                        "IdCategoria", "NombreCategoria", p.IdCategoria);
                    return View(p);
                }

                // Validar talla
                if (string.IsNullOrWhiteSpace(p.Talla))
                {
                    ModelState.AddModelError("Talla", "Selecciona al menos una talla.");
                    ViewBag.Categorias = new SelectList(
                        await _db.Categorias.ToListAsync(),
                        "IdCategoria", "NombreCategoria", p.IdCategoria);
                    return View(p);
                }

                // ✅ Producto activo por defecto
                p.Activo = true;

                // Guardar imagen
                p.ImagenUrl = await GuardarImagen(imagen);

                // Guardar en base de datos
                _db.Productos.Add(p);
                await _db.SaveChangesAsync();

                TempData["MensajeExito"] = "Producto creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log detallado en consola
                System.Diagnostics.Debug.WriteLine($"❌ ERROR CRÍTICO: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");

                ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                ViewBag.Categorias = new SelectList(
                    await _db.Categorias.ToListAsync(),
                    "IdCategoria", "NombreCategoria", p.IdCategoria);
                return View(p);
            }
        }

        public async Task<IActionResult> Editar(int id)
        {
            var check = VerificarAdmin();
            if (check != null) return check;

            var p = await _db.Productos.FindAsync(id);
            if (p == null) return NotFound();

            ViewBag.Categorias = new SelectList(
                await _db.Categorias.ToListAsync(),
                "IdCategoria", "NombreCategoria", p.IdCategoria);
            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Producto p, IFormFile? imagen)
        {
            try
            {
                ModelState.Remove("ImagenUrl");
                ModelState.Remove("Categoria");
                ModelState.Remove("DetallesPedido");
                ModelState.Remove("CarritoDetalles");

                if (!ModelState.IsValid)
                {
                    ViewBag.Categorias = new SelectList(
                        await _db.Categorias.ToListAsync(),
                        "IdCategoria", "NombreCategoria", p.IdCategoria);
                    return View(p);
                }

                var existente = await _db.Productos.FindAsync(p.IdProducto);
                if (existente == null) return NotFound();

                existente.Nombre = p.Nombre;
                existente.Descripcion = p.Descripcion;
                existente.Talla = p.Talla;
                existente.Color = p.Color;
                existente.Precio = p.Precio;
                existente.Stock = p.Stock;
                existente.IdCategoria = p.IdCategoria;
                existente.Activo = p.Activo;

                if (imagen != null && imagen.Length > 0)
                    existente.ImagenUrl = await GuardarImagen(imagen);

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($" ERROR Editar: {ex.Message}");
                ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                ViewBag.Categorias = new SelectList(
                    await _db.Categorias.ToListAsync(),
                    "IdCategoria", "NombreCategoria", p.IdCategoria);
                return View(p);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var check = VerificarAdmin();
                if (check != null) return check;

                var p = await _db.Productos.FindAsync(id);
                if (p != null)
                {
                    if (!string.IsNullOrEmpty(p.ImagenUrl))
                    {
                        var ruta = Path.Combine(_env.WebRootPath, p.ImagenUrl.TrimStart('/'));
                        if (System.IO.File.Exists(ruta))
                            System.IO.File.Delete(ruta);
                    }
                    _db.Productos.Remove(p);
                    await _db.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR Eliminar: {ex.Message}");
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                var p = await _db.Productos.FindAsync(id);
                if (p != null)
                {
                    p.Activo = !p.Activo;
                    await _db.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($" ERROR Desactivar: {ex.Message}");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<string> GuardarImagen(IFormFile imagen)
        {
            try
            {
                var carpeta = Path.Combine(_env.WebRootPath, "uploads", "productos");
                Directory.CreateDirectory(carpeta);

                var nombre = $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
                var ruta = Path.Combine(carpeta, nombre);

                using var stream = new FileStream(ruta, FileMode.Create);
                await imagen.CopyToAsync(stream);

                return $"/uploads/productos/{nombre}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR GuardarImagen: {ex.Message}");
                throw;
            }
        }
    }
}