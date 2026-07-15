using System.Collections.Generic;
using induccion_refactorization.Models;

namespace induccion_refactorization.ViewModels
{
    public class AspiranteDashboardViewModel
    {
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string Matricula { get; set; }
        public string Folio { get; set; }
        public List<MateriaProgresoViewModel> MateriasProgreso { get; set; } = new List<MateriaProgresoViewModel>();
    }

    public class MateriaProgresoViewModel
    {
        public Ind_Materia Materia { get; set; }
        public int TotalUnidades { get; set; }
        public int UnidadesCompletadas { get; set; }
        public decimal? PromedioCalificacion { get; set; }
        public List<Ind_ProgresoAspirante> ProgresoAspirantes { get; set; } = new List<Ind_ProgresoAspirante>();

        public int PorcentajeProgreso =>
            TotalUnidades > 0 ? (int)((UnidadesCompletadas * 100.0) / TotalUnidades) : 0;
    }
}
