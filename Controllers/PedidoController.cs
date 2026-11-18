using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;

namespace SistemaWeb_Ventas.Controllers
{
    [Authorize]
    public class PedidoController : Controller
    {
        private readonly AppDbContext _db;
        public PedidoController(AppDbContext db) => _db = db;

        private string? GetRol() => HttpContext.Session.GetString("UsuarioRol");
        private int GetUserId() => int.TryParse(HttpContext.Session.GetString("UsuarioId"), out var id) ? id : 0;

        private void RegistrarAuditoria(string accion, string entidad, int? idEntidad, string detalle)
        {
            var idUsuario = int.TryParse(HttpContext.Session.GetString("UsuarioId"), out var id) ? id : (int?)null;

            _db.RegistrosAuditoria.Add(new RegistroAuditoria
            {
                IdUsuario = idUsuario,
                NombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Usuario",
                RolUsuario = HttpContext.Session.GetString("UsuarioRol") ?? "",
                Accion = accion,
                Entidad = entidad,
                IdEntidad = idEntidad,
                Detalle = detalle,
                FechaHora = GetHoraPeru()
            });
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Catalogo()
        {
            if (GetRol() != "Cliente") return RedirectToAction("Index", "Login");
            var productos = await _db.Productos.Include(p => p.Categoria)
                .Where(p => p.Activo && p.Stock > 0).ToListAsync();
            return View(productos);
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [Authorize(Roles = "Cliente")]
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

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
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

            if (carrito == null || !carrito.Detalles.Any())
                return RedirectToAction(nameof(Carrito));

            await using var transaccion = await _db.Database.BeginTransactionAsync();

            try
            {
                foreach (var detalle in carrito.Detalles)
                {
                    if (detalle.Producto == null || !detalle.Producto.Activo)
                        throw new InvalidOperationException("Uno de los productos ya no está disponible.");

                    if (detalle.Cantidad <= 0 || detalle.Cantidad > detalle.Producto.Stock)
                        throw new InvalidOperationException($"Stock insuficiente para {detalle.Producto.Nombre}.");
                }

                var pedido = new Pedido
                {
                    IdCliente = idUsuario,
                    IdTipoEntrega = idTipoEntrega,
                    Total = carrito.Detalles.Sum(d => d.Producto!.Precio * d.Cantidad)
                };

                _db.Pedidos.Add(pedido);
                await _db.SaveChangesAsync();

                foreach (var detalle in carrito.Detalles)
                {
                    _db.DetallesPedido.Add(new DetallePedido
                    {
                        IdPedido = pedido.IdPedido,
                        IdProducto = detalle.IdProducto,
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = detalle.Producto!.Precio,
                        TallaSeleccionada = detalle.TallaSeleccionada
                    });

                    detalle.Producto!.Stock -= detalle.Cantidad;
                }

                var entrega = new Entrega { IdPedido = pedido.IdPedido };

                if (idTipoEntrega == 1)
                {
                    entrega.DireccionDestino = "Recojo en tienda - Cajamarca";
                }
                else if (idTipoEntrega == 2)
                {
                    entrega.DireccionDestino = direccionDestino;
                    entrega.CiudadDestino = "Cajamarca";
                }
                else if (idTipoEntrega == 3)
                {
                    entrega.CiudadDestino = ciudadDestino;
                    entrega.DireccionDestino = jironDestino;
                }

                _db.Entregas.Add(entrega);
                _db.CarritoDetalles.RemoveRange(carrito.Detalles);
                await _db.SaveChangesAsync();
                await transaccion.CommitAsync();
            }
            catch (InvalidOperationException ex)
            {
                await transaccion.RollbackAsync();
                TempData["ErrorPedido"] = ex.Message;
                return RedirectToAction(nameof(Carrito));
            }

            return RedirectToAction(nameof(MisPedidos));
        }

        [Authorize(Roles = "Cliente")]
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

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index(int pagina = 1)
        {
            if (GetRol() != "Administrador") return RedirectToAction("LogIn", "Login");

            const int tamanoPagina = 20;
            pagina = Math.Max(pagina, 1);

            var consulta = _db.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.TipoEntrega);

            var total = await consulta.CountAsync();
            var pedidos = await consulta
                .OrderByDescending(p => p.FechaPedido)
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
            return View(pedidos);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            if (GetRol() != "Administrador") return RedirectToAction("Index", "Login");
            var p = await _db.Pedidos.FindAsync(id);
            if (p != null && p.EstadoPedido == "Pendiente")
            {
                p.EstadoPedido = "Aprobado";
                RegistrarAuditoria("Aprobó pedido", "Pedido", p.IdPedido, "Pedido aprobado por administrador.");
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desaprobar(int id)
        {
            var pedido = await _db.Pedidos.FindAsync(id);
            if (pedido != null && pedido.EstadoPedido == "Aprobado")
            {
                pedido.EstadoPedido = "Pendiente";
                RegistrarAuditoria("Desaprobó pedido", "Pedido", pedido.IdPedido, "Pedido devuelto al estado pendiente por el administrador.");
                await _db.SaveChangesAsync();
            }

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

        [Authorize(Roles = "Empleado")]
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

            ViewBag.Repartidores = await _db.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol != null && u.Rol.NombreRol == "Repartidor")
                .OrderBy(u => u.NombreUsuario)
                .ToListAsync();

            return View(pedidos);
        }

        [Authorize(Roles = "Empleado")]
        public async Task<IActionResult> EmpleadoHistorial()
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var pedidos = await _db.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.TipoEntrega)
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Include(p => p.Entrega).ThenInclude(e => e!.EstadosEntrega)
                .Where(p => p.EstadoPedido == "Finalizado")
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            var pedidosAgrupados = pedidos
                .GroupBy(p => p.Cliente?.NombreUsuario ?? "Cliente desconocido")
                .OrderByDescending(g => g.First().FechaPedido)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.PedidosAgrupados = pedidosAgrupados;

            return View();
        }

        [Authorize(Roles = "Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpleadoAprobar(int id)
        {
            var pedido = await _db.Pedidos.FindAsync(id);
            if (pedido == null || pedido.EstadoPedido == "Finalizado" || pedido.EstadoPedido == "Cancelado")
                return RedirectToAction(nameof(EmpleadoPedidos));

            pedido.EstadoPedido = "Aprobado";
            RegistrarAuditoria("Aprobó pedido", "Pedido", pedido.IdPedido, "Pedido aprobado por empleado.");
            await _db.SaveChangesAsync();
            TempData["MensajeExito"] = "Pedido aprobado correctamente.";
            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [Authorize(Roles = "Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpleadoDesaprobar(int id)
        {
            var pedido = await _db.Pedidos.FindAsync(id);
            if (pedido == null || pedido.EstadoPedido != "Aprobado")
                return RedirectToAction(nameof(EmpleadoPedidos));

            pedido.EstadoPedido = "Pendiente";
            RegistrarAuditoria("Desaprobó pedido", "Pedido", pedido.IdPedido, "Pedido devuelto al estado pendiente por el empleado.");
            await _db.SaveChangesAsync();
            TempData["MensajeExito"] = "Pedido devuelto a pendiente.";
            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [Authorize(Roles = "Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpleadoMarcarPagado(int id)
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var p = await _db.Pedidos
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Include(p => p.Cliente)
                .Include(p => p.Entrega)
                .FirstOrDefaultAsync(x => x.IdPedido == id);

            if (p != null && p.EstadoPedido == "Aprobado")
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

                RegistrarAuditoria("Registró pago", "Pedido", p.IdPedido, "Pago confirmado por empleado.");
                await _db.SaveChangesAsync();
                TempData["MensajeExito"] = "Pago registrado correctamente.";
            }
            else
            {
                TempData["ErrorPedido"] = "Solo puedes registrar el pago de pedidos aprobados.";
            }

            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [Authorize(Roles = "Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpleadoPagoContraEntrega(int id)
        {
            var pedido = await _db.Pedidos
                .Include(p => p.TipoEntrega)
                .FirstOrDefaultAsync(p => p.IdPedido == id);

            if (pedido == null || pedido.EstadoPedido != "Aprobado"
                || pedido.TipoEntrega?.Nombre == "Recojo en tienda")
            {
                TempData["ErrorPedido"] = "Solo se puede configurar pago contra entrega en pedidos aprobados con envío.";
                return RedirectToAction(nameof(EmpleadoPedidos));
            }

            pedido.EstadoPago = "Por cobrar";
            RegistrarAuditoria("Configuró pago contra entrega", "Pedido", pedido.IdPedido, "El pago se cobrará al recibir el pedido.");
            await _db.SaveChangesAsync();
            TempData["MensajeExito"] = "Pago contra entrega configurado.";
            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [Authorize(Roles = "Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpleadoMarcarEnviado(int id)
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var p = await _db.Pedidos
                .Include(p => p.Entrega)
                .Include(p => p.Cliente)
                .Include(p => p.TipoEntrega)
                .FirstOrDefaultAsync(x => x.IdPedido == id);

            if (p != null && p.EstadoPedido == "Aprobado" && p.EstadoPago == "Pagado")
            {
                if (p.TipoEntrega?.Nombre != "Recojo en tienda" && p.Entrega?.IdRepartidor == null)
                {
                    TempData["ErrorPedido"] = "Debes asignar un repartidor antes de marcar el pedido como enviado.";
                    return RedirectToAction(nameof(EmpleadoPedidos));
                }

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

                RegistrarAuditoria("Marcó envío", "Pedido", p.IdPedido, "Pedido marcado como enviado por empleado.");
                await _db.SaveChangesAsync();
                TempData["MensajeExito"] = "Pedido marcado como enviado.";
            }
            else
            {
                TempData["ErrorPedido"] = "Solo puedes enviar pedidos aprobados y pagados con repartidor asignado.";
            }

            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [Authorize(Roles = "Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpleadoMarcarEntregado(int id)
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var p = await _db.Pedidos
                .Include(p => p.Entrega)
                .Include(p => p.Cliente)
                .Include(p => p.TipoEntrega)
                .FirstOrDefaultAsync(x => x.IdPedido == id);

            if (p != null && p.EstadoPedido == "Aprobado" && p.EstadoPago == "Pagado"
                && p.TipoEntrega?.Nombre == "Recojo en tienda")
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

                RegistrarAuditoria("Marcó entrega", "Pedido", p.IdPedido, "Pedido marcado como entregado por empleado.");
                await _db.SaveChangesAsync();
                TempData["MensajeExito"] = "Pedido marcado como entregado.";
            }

            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [Authorize(Roles = "Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpleadoAsignarRepartidor(int id, int idRepartidor)
        {
            if (GetRol() != "Empleado")
                return RedirectToAction("Index", "Login");

            var pedido = await _db.Pedidos
                .Include(p => p.Entrega)
                .Include(p => p.TipoEntrega)
                .FirstOrDefaultAsync(p => p.IdPedido == id);

            var repartidor = await _db.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.IdUsuario == idRepartidor
                    && u.Rol != null
                    && u.Rol.NombreRol == "Repartidor");

            if (pedido?.Entrega == null || repartidor == null
                || pedido.EstadoPedido != "Aprobado"
                || pedido.TipoEntrega?.Nombre == "Recojo en tienda")
                return RedirectToAction(nameof(EmpleadoPedidos));

            pedido.Entrega.IdRepartidor = repartidor.IdUsuario;
            _db.EstadosEntrega.Add(new EstadoEntrega
            {
                IdEntrega = pedido.Entrega.IdEntrega,
                Estado = "Repartidor asignado",
                Observacion = $"Entrega asignada a {repartidor.NombreUsuario}.",
                FechaHora = GetHoraPeru()
            });

            RegistrarAuditoria("Asignó repartidor", "Pedido", pedido.IdPedido, $"Pedido asignado al repartidor {repartidor.NombreUsuario}.");
            await _db.SaveChangesAsync();
            TempData["MensajeExito"] = "Repartidor asignado correctamente.";
            return RedirectToAction(nameof(EmpleadoPedidos));
        }

        [Authorize(Roles = "Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmpleadoCancelar(int id)
        {
            if (GetRol() != "Empleado") return RedirectToAction("Index", "Login");

            var p = await _db.Pedidos
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .Include(p => p.Cliente)
                .Include(p => p.Entrega)
                .FirstOrDefaultAsync(x => x.IdPedido == id);

            if (p != null && p.EstadoPedido == "Aprobado")
            {
                await using var transaccion = await _db.Database.BeginTransactionAsync();

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

                RegistrarAuditoria("Canceló pedido", "Pedido", p.IdPedido, "Pedido cancelado por empleado y stock devuelto.");
                await _db.SaveChangesAsync();
                await transaccion.CommitAsync();
                TempData["MensajeExito"] = "Pedido cancelado y stock devuelto.";
            }
            else
            {
                TempData["ErrorPedido"] = "Solo puedes cancelar pedidos aprobados.";
            }

            return RedirectToAction(nameof(EmpleadoPedidos));
        }
    }
}