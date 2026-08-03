using induccion_refactorization.Models;

namespace induccion_refactorization.ViewModels
{
    public class MaestroResumenViewModel
    {
        public Usuario Usuario { get; set; }
        public int TotalMateriasCompartidas { get; set; }
        public int UnidadesCalificadasPorEl { get; set; }
        public int EntregasCalificadasPorEl { get; set; }
    }
}
