using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Models;

namespace SistemaWeb_Ventas.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<TipoEntrega> TiposEntrega { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<Entrega> Entregas { get; set; }
        public DbSet<EstadoEntrega> EstadosEntrega { get; set; }
        public DbSet<Carrito> Carritos { get; set; }
        public DbSet<CarritoDetalle> CarritoDetalles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //************************* RELACIONES ************************

            // Usuario → Rol (M:1)
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.IdRol);

            // Producto → Categoria (M:1)
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.IdCategoria);

            // Pedido → Cliente (M:1)
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany(u => u.Pedidos)
                .HasForeignKey(p => p.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            // Pedido → TipoEntrega (M:1)
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.TipoEntrega)
                .WithMany(t => t.Pedidos)
                .HasForeignKey(p => p.IdTipoEntrega);

            // DetallePedido → Pedido (M:1)
            modelBuilder.Entity<DetallePedido>()
                .HasOne(d => d.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.IdPedido);

            // DetallePedido → Producto (M:1)
            modelBuilder.Entity<DetallePedido>()
                .HasOne(d => d.Producto)
                .WithMany(p => p.DetallesPedido)
                .HasForeignKey(d => d.IdProducto);

            // Entrega → Pedido (1:1)
            modelBuilder.Entity<Entrega>()
                .HasOne(e => e.Pedido)
                .WithOne(p => p.Entrega)
                .HasForeignKey<Entrega>(e => e.IdPedido);

            // Entrega → Repartidor (M:1, opcional)
            modelBuilder.Entity<Entrega>()
                .HasOne(e => e.Repartidor)
                .WithMany(u => u.Entregas)
                .HasForeignKey(e => e.IdRepartidor)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // EstadoEntrega → Entrega (M:1)
            modelBuilder.Entity<EstadoEntrega>()
                .HasOne(s => s.Entrega)
                .WithMany(e => e.EstadosEntrega)
                .HasForeignKey(s => s.IdEntrega);

            // Carrito → Usuario (1:1)
            modelBuilder.Entity<Carrito>()
                .HasOne(c => c.Usuario)
                .WithOne(u => u.Carrito)
                .HasForeignKey<Carrito>(c => c.IdUsuario);

            // CarritoDetalle → Carrito (M:1)
            modelBuilder.Entity<CarritoDetalle>()
                .HasOne(cd => cd.Carrito)
                .WithMany(c => c.Detalles)
                .HasForeignKey(cd => cd.IdCarrito);

            // CarritoDetalle → Producto (M:1)
            modelBuilder.Entity<CarritoDetalle>()
                .HasOne(cd => cd.Producto)
                .WithMany(p => p.CarritoDetalles)
                .HasForeignKey(cd => cd.IdProducto);

            //************ SEED DATA ************

            // Roles
            modelBuilder.Entity<Rol>().HasData(
                new Rol { IdRol = 1, NombreRol = "Administrador" },
                new Rol { IdRol = 2, NombreRol = "Empleado" },
                new Rol { IdRol = 3, NombreRol = "Repartidor" },
                new Rol { IdRol = 4, NombreRol = "Cliente" }
            );

            // Admin inicial
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    IdUsuario = 1,
                    NombreUsuario = "Administrador",
                    DNI = "79461382",
                    Telefono = "987654321",
                    Direccion = "Cajamarca",
                    Correo = "admin@gmail.com",
                    Contrasena = "admin123",
                    IdRol = 1
                }
            );

            // Categorías
            modelBuilder.Entity<Categoria>().HasData(
                new Categoria { IdCategoria = 1, NombreCategoria = "Gorras", Descripcion = "Gorras urbanas y deportivas" },
                new Categoria { IdCategoria = 2, NombreCategoria = "Zapatillas", Descripcion = "Zapatillas casuales y deportivas" },
                new Categoria { IdCategoria = 3, NombreCategoria = "Polos", Descripcion = "Polos de algodón y lycra" },
                new Categoria { IdCategoria = 4, NombreCategoria = "Poleras", Descripcion = "Poleras con y sin capucha" },
                new Categoria { IdCategoria = 5, NombreCategoria = "Pantalones", Descripcion = "Jeans, joggers y cargo pants" }
            );

            // Tipos de entrega
            modelBuilder.Entity<TipoEntrega>().HasData(
                new TipoEntrega { IdTipoEntrega = 1, Nombre = "Recojo en tienda", Descripcion = "El cliente recoge en la tienda de Cajamarca" },
                new TipoEntrega { IdTipoEntrega = 2, Nombre = "Envío a domicilio", Descripcion = "Entrega en dirección del cliente dentro de Cajamarca" },
                new TipoEntrega { IdTipoEntrega = 3, Nombre = "Envío a otra ciudad", Descripcion = "Envío nacional mediante empresa de transporte" }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}