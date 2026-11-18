using SistemaWeb_Ventas.Data;
using SistemaWeb_Ventas.Models;

namespace SistemaWeb_Ventas.Services
{
    public interface IAuditoriaService
    {
        void Registrar(int? idUsuario, string nombreUsuario, string rolUsuario, string accion,
            string entidad, int? idEntidad, string detalle);
    }

    public class AuditoriaService : IAuditoriaService
    {
        private readonly AppDbContext _db;

        public AuditoriaService(AppDbContext db)
        {
            _db = db;
        }

        public void Registrar(int? idUsuario, string nombreUsuario, string rolUsuario, string accion,
            string entidad, int? idEntidad, string detalle)
        {
            _db.RegistrosAuditoria.Add(new RegistroAuditoria
            {
                IdUsuario = idUsuario,
                NombreUsuario = nombreUsuario,
                RolUsuario = rolUsuario,
                Accion = accion,
                Entidad = entidad,
                IdEntidad = idEntidad,
                Detalle = detalle,
                FechaHora = DateTime.Now
            });
        }
    }
}
