using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_Areas")]
    public partial class Ind_Area
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Ind_Area()
        {
            Usuarios = new HashSet<Usuario>();
            Carreras = new HashSet<Carrera>();
        }

        [Key]
        public int AreaID { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }

        public bool Activo { get; set; }

        // Navigation Properties

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Usuario> Usuarios { get; set; }

        // Las carreras que pertenecen a esta Área (relación invertida: antes el
        // Área dependía de una Carrera, ahora la Carrera pertenece a un Área).
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Carrera> Carreras { get; set; }
    }
}
