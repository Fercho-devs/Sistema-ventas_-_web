using System;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar Entity Framework Core con SQL Server
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaSQL")));

// Configurar el sistema de Sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // La sesión expira a los 30 minutos de inactividad
    options.Cookie.HttpOnly = true;                 // Protege la cookie contra ataques XSS
    options.Cookie.IsEssential = true;              // Hace que la cookie se cree incluso si el usuario no acepta cookies de rastreo
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // En producción, mostrar una página de error genérica en lugar de detalles técnicos
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Forzar HTTPS en producción
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Habilita el uso de archivos estáticos (CSS, JS, imágenes en wwwroot)

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
