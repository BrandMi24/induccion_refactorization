using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Carreras")]
    public partial class Carrera
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Carrera()
        {
            Ind_Materias = new HashSet<Ind_Materia>();
        }

        [Key]
        public int CarreraID { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(20)]
        public string Nomenclatura { get; set; }

        public int TipoCarreraID { get; set; }

        // Navigation Properties
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_Materia> Ind_Materias { get; set; }
    }
}
