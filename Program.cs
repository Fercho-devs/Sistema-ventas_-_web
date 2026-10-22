
using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;

var builder = WebApplication.CreateBuilder(args);

// Registrar MVC
builder.Services.AddControllersWithViews();

// Configurar Entity Framework Core con SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CadenaSQL"));
});

// Configurar la caché en memoria para las sesiones
builder.Services.AddDistributedMemoryCache();

// Configurar las sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Registrar el servicio para el hash de contraseñas
builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>>();

// Configurar la autenticación mediante cookies
builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/AccesoDenegado";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();

// Configurar el manejo de errores en producción
if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Esto muestra errores detallados
}
else
{ 
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTPS y archivos estáticos
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Habilitar la autenticación mediante cookies
app.UseAuthentication();

// Evitar la caché de páginas para usuarios autenticados
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers["Cache-Control"] =
            "no-cache, no-store, must-revalidate";

        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }

    await next();
});

// Habilitar la autorización
app.UseAuthorization();

// Habilitar las sesiones
app.UseSession();

// Configurar las rutas
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
