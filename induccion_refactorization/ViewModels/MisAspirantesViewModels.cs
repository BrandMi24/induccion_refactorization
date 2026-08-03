using System.Collections.Generic;
using induccion_refactorization.Models;

namespace induccion_refactorization.ViewModels
{
    public class AspiranteResumenViewModel
    {
        public Aspirante Aspirante { get; set; }
        public int TotalUnidadesAsignadas { get; set; }
        public int UnidadesCompletadas { get; set; }
        public decimal? PromedioCalificacion { get; set; }

        public int PorcentajeProgreso =>
            TotalUnidadesAsignadas > 0 ? (int)((UnidadesCompletadas * 100.0) / TotalUnidadesAsignadas) : 0;
    }
}
