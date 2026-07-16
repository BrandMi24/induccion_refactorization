using System.Collections.Generic;

namespace induccion_refactorization.ViewModels
{
    public class ReportesViewModel
    {
        // Usuarios
        public int TotalUsuarios { get; set; }
        public int TotalAdministradores { get; set; }
        public int TotalDirectores { get; set; }
        public int TotalCoordinadores { get; set; }
        public int TotalAspirantes { get; set; }

        // Contenido
        public int TotalMaterias { get; set; }
        public int TotalUnidades { get; set; }
        public int TotalMateriales { get; set; }
        public int TotalEntregables { get; set; }

        // Entregables (Ind_Submision)
        public int EntregasPendientes { get; set; }
        public int EntregasRevisadas { get; set; }
        public int EntregasRechazadas { get; set; }
        public decimal? PromedioEntregables { get; set; }

        // Unidades (Ind_ProgresoAspirante)
        public int UnidadesAsignadas { get; set; }
        public int UnidadesEntregadas { get; set; }
        public int UnidadesCalificadas { get; set; }
        public decimal? PromedioUnidades { get; set; }

        public List<MateriaProgresoResumen> MateriasConProgreso { get; set; } = new List<MateriaProgresoResumen>();
    }
}
