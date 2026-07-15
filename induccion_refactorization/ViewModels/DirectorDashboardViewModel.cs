using System.Collections.Generic;

namespace induccion_refactorization.ViewModels
{
    public class DirectorDashboardViewModel
    {
        public string NombreCompleto { get; set; }
        public string Email { get; set; }

        public int TotalAspirantes { get; set; }
        public int TotalMaterias { get; set; }
        public int TotalEntregables { get; set; }

        public int PendientesCount { get; set; }
        public int RevisadasCount { get; set; }
        public int RechazadasCount { get; set; }
        public decimal? PromedioGeneral { get; set; }

        public List<MateriaProgresoResumen> MateriasConProgreso { get; set; } = new List<MateriaProgresoResumen>();
    }

    public class MateriaProgresoResumen
    {
        public string Nombre { get; set; }
        public int Total { get; set; }
        public int Completados { get; set; }
        public int Porcentaje => Total > 0 ? (int)((Completados * 100.0) / Total) : 0;
    }
}
