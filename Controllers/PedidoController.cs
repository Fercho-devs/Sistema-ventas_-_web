using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;

namespace SistemaWeb_Ventas.Controllers
{
    public class PedidoController : Controller
    {
        private readonly AppDbContext _db;
        public PedidoController(AppDbContext db) => _db = db;

        private string? GetRol() => HttpContext.Session.GetString("UsuarioRol");
        private int GetUserId() => int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");

        public async Task<IActionResult> Catalogo()
        {
            if (GetRol() != "Cliente") return RedirectToAction("Index", "Login");
            var productos = await _db.Productos.Include(p => p.Categoria)
                .Where(p => p.Activo && p.Stock > 0).ToListAsync();
            return View(productos);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarCarrito(int idProducto, int cantidad, string? tallaSeleccionada)
        {
            if (GetRol() != "Cliente") return Json(new { ok = false, mensaje = "Sesión inválida." });

            var producto = await _db.Productos.FindAsync(idProducto);
            if (producto == null) return Json(new { ok = false, mensaje = "Producto no encontrado." });

            // Validar talla
            if (string.IsNullOrWhiteSpace(tallaSeleccionada))
            {
                return Json(new { ok = false, mensaje = "Debes seleccionar una talla." });
            }

            var idUsuario = GetUserId();
            var carrito = await _db.Carritos.FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);

            if (carrito == null)
            {
                carrito = new Carrito { IdUsuario = idUsuario };
                _db.Carritos.Add(carrito);
                await _db.SaveChangesAsync();
            }

            // Buscar si ya existe el producto con la misma talla
            var detalle = await _db.CarritoDetalles
                .FirstOrDefaultAsync(d => d.IdCarrito == carrito.IdCarrito
                                       && d.IdProducto == idProducto
                                       && d.TallaSeleccionada == tallaSeleccionada);

            int cantidadActual = detalle?.Cantidad ?? 0;

            if (cantidad < 1) return Json(new { ok = false, mensaje = "La cantidad debe ser al menos 1." });

            if (cantidadActual + cantidad > producto.Stock)
            {
                int disponible = producto.Stock - cantidadActual;
                return Json(new
                {
                    ok = false,
                    mensaje = disponible <= 0
                        ? "Ya tienes en el carrito todo el stock disponible de este producto."
                        : $"Solo puedes agregar {disponible} unidad(es) más. Stock disponible: {producto.Stock}."
                });
            }

            if (detalle == null)
            {
                _db.CarritoDetalles.Add(new CarritoDetalle
                {
                    IdCarrito = carrito.IdCarrito,
                    IdProducto = idProducto,
                    Cantidad = cantidad,
                    TallaSeleccionada = tallaSeleccionada  // ✅ Guardar talla
                });
            }
            else
            {
                detalle.Cantidad += cantidad;
            }

            await _db.SaveChangesAsync();

            int restante = producto.Stock - (cantidadActual + cantidad);
            return Json(new { ok = true, mensaje = $"Se agregó correctamente al carrito. Talla: {tallaSeleccionada}", restante });
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarCantidadCarrito(int idDetalle, int cantidad)
        {
            if (GetRol() != "Cliente") return RedirectToAction("Index", "Login");
            var idUsuario = GetUserId();
            var detalle = await _db.CarritoDetalles.Include(d => d.Carrito).Include(d => d.Producto)
                .FirstOrDefaultAsync(d => d.IdCarritoDetalle == idDetalle && d.Carrito!.IdUsuario == idUsuario);
            if (detalle != null)
            {
                if (cantidad <= 0)
                {
                    _db.CarritoDetalles.Remove(detalle);
                }
                else if (cantidad > detalle.Producto!.Stock)
                {
                    TempData["ErrorPedido"] = $"Solo hay {detalle.Producto.Stock} unidad(es) en stock de {detalle.Producto.Nombre}.";
                }
                else
                {
                    detalle.Cantidad = cantidad;
                }
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Carrito));
        }

        [HttpPost]
        public async Task<IActionResult> EliminarDelCarrito(int idDetalle)
        {
            if (GetRol() != "Cliente") return RedirectToAction("Index", "Login");
            var idUsuario = GetUserId();
            var detalle = await _db.CarritoDetalles.Include(d => d.Carrito)
                .FirstOrDefaultAsync(d => d.IdCarritoDetalle == idDetalle && d.Carrito!.IdUsuario == idUsuario);
            if (detalle != null)
            {
                _db.CarritoDetalles.Remove(detalle);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Carrito));
        }

        public async Task<IActionResult> Carrito()
        {
            if (GetRol() != "Cliente") return RedirectToAction("Index", "Login");
            var idUsuario = GetUserId();
            var carrito = await _db.Carritos
                .Include(c => c.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);
            ViewBag.TiposEntrega = new SelectList(await _db.TiposEntrega.ToListAsync(), "IdTipoEntrega", "Nombre");
            return View(carrito);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarPedido(int idTipoEntrega, string? direccionDestino, string? ciudadDestino, string? jironDestino)
        {
            if (GetRol() != "Cliente") return RedirectToAction("Index", "Login");

            if (idTipoEntrega <= 0)
            {
                TempData["ErrorPedido"] = "Por favor seleccione el tipo de entrega.";
                return RedirectToAction(nameof(Carrito));
            }

            // Validaciones según tipo de entrega
            if (idTipoEntrega == 2 && string.IsNullOrWhiteSpace(direccionDestino))
            {
                TempData["ErrorPedido"] = "Por favor ingrese la dirección de entrega.";
                return RedirectToAction(nameof(Carrito));
            }

            if (idTipoEntrega == 3)
            {
                if (string.IsNullOrWhiteSpace(ciudadDestino))
                {
                    TempData["ErrorPedido"] = "Por favor ingrese la ciudad de destino.";
                    return RedirectToAction(nameof(Carrito));
                }
                if (string.IsNullOrWhiteSpace(jironDestino))
                {
                    TempData["ErrorPedido"] = "Por favor ingrese el jirón o dirección.";
                    return RedirectToAction(nameof(Carrito));
                }
            }

            var idUsuario = GetUserId();
            var carrito = await _db.Carritos
                .Include(c => c.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);

            if (carrito == null || !carrito.Detalles.Any()) return RedirectToAction(nameof(Carrito));

            var pedido = new Pedido
            {
                IdCliente = idUsuario,
                IdTipoEntrega = idTipoEntrega,
                Total = carrito.Detalles.Sum(d => d.Producto!.Precio * d.Cantidad)
            };
            _db.Pedidos.Add(pedido);
            await _db.SaveChangesAsync();

            foreach (var d in carrito.Detalles)
            {
                _db.DetallesPedido.Add(new DetallePedido
                {
                    IdPedido = pedido.IdPedido,
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.Producto!.Precio,
                    TallaSeleccionada = d.TallaSeleccionada
                });
                d.Producto.Stock -= d.Cantidad;
            }

            // Crear la entrega con los datos según el tipo
            var entrega = new Entrega
            {
                IdPedido = pedido.IdPedido
            };

            if (idTipoEntrega == 1)
            {
                // Recojo en tienda - sin dirección
                entrega.DireccionDestino = "Recojo en tienda - Cajamarca";
            }
            else if (idTipoEntrega == 2)
            {
                // Envío a domicilio
                entrega.DireccionDestino = direccionDestino;
                entrega.CiudadDestino = "Cajamarca";
            }
            else if (idTipoEntrega == 3)
            {
                // Envío a otra ciudad
                entrega.CiudadDestino = ciudadDestino;
                entrega.DireccionDestino = jironDestino;
            }

            _db.Entregas.Add(entrega);

            // Limpiar carrito
            _db.CarritoDetalles.RemoveRange(carrito.Detalles);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(MisPedidos));
        }

        public async Task<IActionResult> MisPedidos()
        {
            if (GetRol() != "Cliente") return RedirectToAction("Index", "Login");
            var pedidos = await _db.Pedidos
                .Include(p => p.TipoEntrega)
                .Include(p => p.Entrega).ThenInclude(e => e!.EstadosEntrega)
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Where(p => p.IdCliente == GetUserId())
                .OrderByDescending(p => p.FechaPedido).ToListAsync();
            return View(pedidos);
        }

        public async Task<IActionResult> Index()
        {
            if (GetRol() != "Administrador") return RedirectToAction("Index", "Login");
            var pedidos = await _db.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.TipoEntrega)
                .OrderByDescending(p => p.FechaPedido).ToListAsync();
            return View(pedidos);
        }

        [HttpPost]
        public async Task<IActionResult> Aprobar(int id)
        {
            if (GetRol() != "Administrador") return RedirectToAction("Index", "Login");
            var p = await _db.Pedidos.FindAsync(id);
            if (p != null) { p.EstadoPedido = "Aprobado"; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarcarPagado(int id)
        {
            if (GetRol() != "Administrador") return RedirectToAction("Index", "Login");
            var p = await _db.Pedidos.FindAsync(id);
            if (p != null) { p.EstadoPago = "Pagado"; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            if (GetRol() != "Administrador") return RedirectToAction("Index", "Login");
            var p = await _db.Pedidos.FindAsync(id);
            if (p != null) { p.EstadoPedido = "Cancelado"; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        // ==================== MÉTODOS PARA EMPLEADO ====================

        private DateTime GetHoraPeru()
        {
            try
            {
                var peruZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruZone);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        public async Task<IActionResult> EmpleadoPedidos()
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var pedidos = await _db.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.TipoEntrega)
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Include(p => p.Entrega).ThenInclude(e => e!.EstadosEntrega)
                .Where(p => p.EstadoPedido != "Finalizado" && p.EstadoPedido != "Cancelado")
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            return View(pedidos);
        }

        public async Task<IActionResult> EmpleadoHistorial()
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var pedidos = await _db.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.TipoEntrega)
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Include(p => p.Entrega).ThenInclude(e => e!.EstadosEntrega)
                .Where(p => p.EstadoPedido == "Finalizado" || p.EstadoPedido == "Cancelado")
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            var pedidosAgrupados = pedidos
                .GroupBy(p => p.Cliente?.NombreUsuario ?? "Cliente desconocido")
                .OrderByDescending(g => g.First().FechaPedido)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.PedidosAgrupados = pedidosAgrupados;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EmpleadoMarcarPagado(int id)
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var p = await _db.Pedidos
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Include(p => p.Cliente)
                .Include(p => p.Entrega)
                .FirstOrDefaultAsync(x => x.IdPedido == id);

            if (p != null)
            {
                p.EstadoPago = "Pagado";

                if (p.Entrega != null)
                {
                    _db.EstadosEntrega.Add(new EstadoEntrega
                    {
                        IdEntrega = p.Entrega.IdEntrega,
                        Estado = "Pago Registrado",
                        Observacion = $"Pago confirmado por empleado: {HttpContext.Session.GetString("UsuarioNombre")}. Cliente: {p.Cliente?.NombreUsuario}",
                        FechaHora = GetHoraPeru()
                    });
                }

                await _db.SaveChangesAsync();
                TempData["MensajeExito"] = "Pago registrado correctamente.";
            }

            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [HttpPost]
        public async Task<IActionResult> EmpleadoMarcarEnviado(int id)
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var p = await _db.Pedidos
                .Include(p => p.Entrega)
                .Include(p => p.Cliente)
                .FirstOrDefaultAsync(x => x.IdPedido == id);

            if (p != null)
            {
                p.EstadoPedido = "En despacho";

                if (p.Entrega != null)
                {
                    _db.EstadosEntrega.Add(new EstadoEntrega
                    {
                        IdEntrega = p.Entrega.IdEntrega,
                        Estado = "Enviado",
                        Observacion = $"Pedido enviado por empleado: {HttpContext.Session.GetString("UsuarioNombre")}. Cliente: {p.Cliente?.NombreUsuario}",
                        FechaHora = GetHoraPeru()
                    });
                }

                await _db.SaveChangesAsync();
                TempData["MensajeExito"] = "Pedido marcado como enviado.";
            }

            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [HttpPost]
        public async Task<IActionResult> EmpleadoMarcarEntregado(int id)
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var p = await _db.Pedidos
                .Include(p => p.Entrega)
                .Include(p => p.Cliente)
                .Include(p => p.TipoEntrega)
                .FirstOrDefaultAsync(x => x.IdPedido == id);

            if (p != null)
            {
                p.EstadoPedido = "Finalizado";

                if (p.Entrega != null)
                {
                    string tipoEntrega = p.TipoEntrega?.Nombre ?? "Desconocido";
                    string observacion = tipoEntrega == "Recojo en tienda"
                        ? $"Cliente recogió el pedido en tienda. Atendido por: {HttpContext.Session.GetString("UsuarioNombre")}. Cliente: {p.Cliente?.NombreUsuario}"
                        : $"Pedido entregado exitosamente. Atendido por: {HttpContext.Session.GetString("UsuarioNombre")}. Cliente: {p.Cliente?.NombreUsuario}";

                    _db.EstadosEntrega.Add(new EstadoEntrega
                    {
                        IdEntrega = p.Entrega.IdEntrega,
                        Estado = "Entregado",
                        Observacion = observacion,
                        FechaHora = GetHoraPeru()
                    });
                }

                await _db.SaveChangesAsync();
                TempData["MensajeExito"] = "Pedido marcado como entregado.";
            }

            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [HttpPost]
        public async Task<IActionResult> EmpleadoCancelar(int id)
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var p = await _db.Pedidos
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Include(p => p.Cliente)
                .Include(p => p.Entrega)
                .FirstOrDefaultAsync(x => x.IdPedido == id);

            if (p != null)
            {
                foreach (var detalle in p.Detalles)
                {
                    if (detalle.Producto != null)
                    {
                        detalle.Producto.Stock += detalle.Cantidad;
                    }
                }

                p.EstadoPedido = "Cancelado";

                if (p.Entrega != null)
                {
                    var productos = string.Join(", ", p.Detalles.Select(d => $"{d.Producto?.Nombre} (x{d.Cantidad})"));
                    _db.EstadosEntrega.Add(new EstadoEntrega
                    {
                        IdEntrega = p.Entrega.IdEntrega,
                        Estado = "Cancelado",
                        Observacion = $"Pedido cancelado por empleado: {HttpContext.Session.GetString("UsuarioNombre")}. " +
                                     $"Cliente: {p.Cliente?.NombreUsuario}. " +
                                     $"Productos: {productos}",
                        FechaHora = GetHoraPeru()
                    });
                }

                await _db.SaveChangesAsync();
                TempData["MensajeExito"] = "Pedido cancelado y stock devuelto.";
            }

            return RedirectToAction(nameof(EmpleadoPedidos));
        }
    }
}