using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using induccion_refactorization.Filters;
using induccion_refactorization.Models;
using induccion_refactorization.ViewModels;

namespace induccion_refactorization.Controllers
{
    [RoleAuthorize(2)] // Only Director (RolID = 2)
    public class DirectorController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /Director/Index
        public ActionResult Index()
        {
            var model = new DirectorDashboardViewModel
            {
                NombreCompleto = Session["NombreCompleto"] as string,
                Email = Session["Email"] as string,
                TotalAspirantes = db.Aspirantes.Count(),
                TotalMaterias = db.Ind_Materias.Count(m => m.Activo),
                TotalEntregables = db.Ind_Entregables.Count(e => e.Activo)
            };

            var submisiones = db.Ind_Submisiones.ToList();
            model.PendientesCount = submisiones.Count(s => s.Estado == "Pendiente");
            model.RevisadasCount = submisiones.Count(s => s.Estado == "Revisado");
            model.RechazadasCount = submisiones.Count(s => s.Estado == "Rechazado");

            var calificadas = submisiones.Where(s => s.Calificacion.HasValue).ToList();
            model.PromedioGeneral = calificadas.Any() ? calificadas.Average(s => s.Calificacion.Value) : (decimal?)null;

            // Progress by materia: % of assigned units marked "Calificado"
            model.MateriasConProgreso = db.Ind_Materias
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_ProgresoAspirantes))
                .Where(m => m.Activo)
                .ToList()
                .Select(m =>
                {
                    var progresos = m.Ind_Unidades.SelectMany(u => u.Ind_ProgresoAspirantes).ToList();
                    return new MateriaProgresoResumen
                    {
                        Nombre = m.Nombre,
                        Total = progresos.Count,
                        Completados = progresos.Count(p => p.Estado == "Calificado")
                    };
                })
                .ToList();

            return View(model);
        }
    }
}
