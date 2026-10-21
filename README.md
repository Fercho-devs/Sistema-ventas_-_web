# 🛍️ Sistema de Ventas Web – QuesitoStore

Aplicación web de ventas para una tienda de ropa y calzado urbano de Cajamarca. Permite a los clientes ver el catálogo, armar un carrito y hacer pedidos, mientras el administrador gestiona productos y pedidos.

## ¿Qué puede hacer?

**Cliente**
- Registrarse e iniciar sesión.
- Ver el catálogo de productos disponibles (activos y con stock).
- Agregar productos al carrito, cambiar cantidades o quitarlos.
- Confirmar el pedido eligiendo el tipo de entrega.
- Ver el estado de sus pedidos en "Mis pedidos".

**Administrador**
- Crear, editar, desactivar y eliminar productos (con imagen).
- Ver todos los pedidos, aprobarlos, marcarlos como pagados o cancelarlos.

**Tipos de entrega**
- Recojo en tienda
- Envío a domicilio (dentro de Cajamarca)
- Envío a otra ciudad (con empresa de transporte)

## Tecnologías

- **C# / ASP.NET Core MVC** (.NET 10)
- **Entity Framework Core 10** (Code First con migraciones)
- **SQL Server**
- **Bootstrap**, jQuery y Razor Views
- Autenticación con cookies y contraseñas con hash (`PasswordHasher`)

## Base de datos

Tablas principales: `Roles`, `Usuarios`, `Categorias`, `Productos`, `TiposEntrega`, `Pedidos`, `DetallesPedido`, `Entregas`, `EstadosEntrega`, `Carritos` y `CarritoDetalles`.

Al crear la base de datos se cargan automáticamente:
- **Roles:** Administrador, Empleado, Repartidor y Cliente.
- **Categorías:** Gorras, Zapatillas, Polos, Poleras y Pantalones.
- **Tipos de entrega:** los 3 mencionados arriba.
- **Un usuario administrador inicial.**


### Usuario administrador de prueba

| Correo | Contraseña |
|---|---|
| admin@gmail.com | admin123 |

> ⚠️ Cambia esta contraseña antes de usar el sistema en un entorno real.

## Estado del proyecto

En desarrollo. Ya funcionan el registro/login, el catálogo, el carrito, los pedidos y la gestión de productos. Pendiente por completar:
- Paneles para los roles **Empleado** y **Repartidor**.
- Asignación de repartidores y seguimiento del estado de las entregas.

## Autor

Proyecto desarrollado por **[Tu nombre]**.
