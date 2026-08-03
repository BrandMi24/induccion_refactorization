using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_Permisos")]
    public partial class Ind_Permiso
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Ind_Permiso()
        {
            Ind_RolPermisos = new HashSet<Ind_RolPermiso>();
            Ind_UsuarioPermisos = new HashSet<Ind_UsuarioPermiso>();
        }

        [Key]
        public int PermisoID { get; set; }

        [Required]
        [StringLength(50)]
        public string Clave { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }

        [StringLength(300)]
        public string Descripcion { get; set; }

        // Navigation Properties
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_RolPermiso> Ind_RolPermisos { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_UsuarioPermiso> Ind_UsuarioPermisos { get; set; }
    }
}
