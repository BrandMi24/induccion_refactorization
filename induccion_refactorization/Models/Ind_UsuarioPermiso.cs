using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_UsuarioPermisos")]
    public partial class Ind_UsuarioPermiso
    {
        public int UsuarioID { get; set; }

        public int PermisoID { get; set; }

        // NULL = hereda del rol; 1/0 = permite/deniega sin importar el rol.
        public bool? PuedeLeer { get; set; }

        public bool? PuedeCrear { get; set; }

        public bool? PuedeEditar { get; set; }

        public bool? PuedeEliminar { get; set; }

        // Navigation Properties
        [ForeignKey("UsuarioID")]
        public virtual Usuario Usuario { get; set; }

        [ForeignKey("PermisoID")]
        public virtual Ind_Permiso Ind_Permiso { get; set; }
    }
}
