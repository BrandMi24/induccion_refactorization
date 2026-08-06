using System.Collections.Generic;
using System.Linq;
using induccion_refactorization.Models;

namespace induccion_refactorization.ViewModels
{
    // Resumen por materia usado en el "semáforo" de Mis Aspirantes: verde
    // cuando todas las unidades de esa materia ya están Revisadas, amarillo
    // cuando hay avance parcial, rojo cuando el aspirante todavía no tiene
    // nada revisado en ella.
    public class MateriaSemaforoViewModel
    {
        public int MateriaID { get; set; }
        public string Nombre { get; set; }
        public int TotalUnidades { get; set; }
        public int UnidadesRevisadas { get; set; }

        public string Abreviatura => Nombre != null && Nombre.Length > 3 ? Nombre.Substring(0, 3) + "." : Nombre + ".";

        public string Semaforo =>
            TotalUnidades > 0 && UnidadesRevisadas == TotalUnidades ? "completo"
            : UnidadesRevisadas > 0 ? "parcial"
            : "nuevo";
    }

    // Una fila de "Mis Aspirantes": nombre+folio, semáforo por materia,
    // cuántas entregas le faltan por revisar, y su progreso general —
    // reemplaza a la antigua tabla separada de "Revisar Entregas".
    public class AspiranteResumenViewModel
    {
        // Ya no existe una entidad Aspirante aparte — este es directamente el
        // Usuario (RolID=4) del aspirante.
        public Usuario Aspirante { get; set; }
        public List<MateriaSemaforoViewModel> Materias { get; set; } = new List<MateriaSemaforoViewModel>();
        public int EntregasPendientes { get; set; }
        public int TotalUnidadesAsignadas { get; set; }
        public int UnidadesCompletadas { get; set; }

        public int TotalMaterias => Materias.Count;
        public int MateriasCompletas => Materias.Count(m => m.Semaforo == "completo");

        public int PorcentajeProgreso =>
            TotalUnidadesAsignadas > 0 ? (int)((UnidadesCompletadas * 100.0) / TotalUnidadesAsignadas) : 0;
    }
}
