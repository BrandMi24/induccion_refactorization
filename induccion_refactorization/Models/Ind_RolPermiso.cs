using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_RolPermisos")]
    public partial class Ind_RolPermiso
    {
        public int RolID { get; set; }

        public int PermisoID { get; set; }

        public bool PuedeLeer { get; set; }

        public bool PuedeCrear { get; set; }

        public bool PuedeEditar { get; set; }

        public bool PuedeEliminar { get; set; }

        // Navigation Properties
        [ForeignKey("RolID")]
        public virtual Role Role { get; set; }

        [ForeignKey("PermisoID")]
        public virtual Ind_Permiso Ind_Permiso { get; set; }
    }
}
