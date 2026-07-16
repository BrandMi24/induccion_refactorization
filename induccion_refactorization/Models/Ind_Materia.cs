using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_Materias")]
    public partial class Ind_Materia
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Ind_Materia()
        {
            Ind_Unidades = new HashSet<Ind_Unidad>();
            Carreras = new HashSet<Carrera>();
        }

        [Key]
        public int MateriaID { get; set; }

        public int PeriodoID { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public bool Activo { get; set; }

        // When true, the materia is visible to every Carrera regardless of the Carreras collection below
        public bool TodasLasCarreras { get; set; }

        // Navigation Properties
        [ForeignKey("PeriodoID")]
        public virtual Periodo Periodo { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_Unidad> Ind_Unidades { get; set; }

        // Carreras this materia is explicitly assigned to (ignored when TodasLasCarreras is true)
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Carrera> Carreras { get; set; }
    }
}
