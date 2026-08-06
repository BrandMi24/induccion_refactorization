using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("TiposCarreras")]
    public partial class TipoCarrera
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public TipoCarrera()
        {
            Carreras = new HashSet<Carrera>();
        }

        [Key]
        public int TipoCarreraID { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; }

        // Navigation Properties
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Carrera> Carreras { get; set; }
    }
}
