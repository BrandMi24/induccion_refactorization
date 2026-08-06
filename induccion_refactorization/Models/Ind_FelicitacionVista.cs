using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    // Registra que un aspirante ya vio (y descartó permanentemente) la pantalla
    // de "¡Felicidades!" de una materia que completó, para no volver a mostrarla
    // cada vez que visita el contenido de esa materia.
    [Table("Ind_FelicitacionesVistas")]
    public partial class Ind_FelicitacionVista
    {
        public int AspiranteID { get; set; }

        public int MateriaID { get; set; }

        public DateTime FechaVista { get; set; }

        [ForeignKey("AspiranteID")]
        public virtual Usuario AspiranteUsuario { get; set; }

        [ForeignKey("MateriaID")]
        public virtual Ind_Materia Ind_Materia { get; set; }
    }
}
