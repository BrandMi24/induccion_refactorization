using System;
using System.Collections.Generic;

namespace induccion_refactorization.ViewModels
{
    public class UnidadCambioDto
    {
        public int? UnidadID { get; set; }
        public string Nombre { get; set; }
        public bool Eliminado { get; set; }
        public List<MaterialCambioDto> Materiales { get; set; } = new List<MaterialCambioDto>();
        public List<EntregableCambioDto> Entregables { get; set; } = new List<EntregableCambioDto>();
    }

    public class MaterialCambioDto
    {
        public int? MaterialID { get; set; }
        public string Nombre { get; set; }
        public string TipoRecurso { get; set; }
        public string RutaURL { get; set; }
        public bool Eliminado { get; set; }
    }

    public class EntregableCambioDto
    {
        public int? EntregableID { get; set; }
        public string Titulo { get; set; }
        public string Instrucciones { get; set; }
        public DateTime? FechaLimite { get; set; }
        public bool Eliminado { get; set; }
    }
}
