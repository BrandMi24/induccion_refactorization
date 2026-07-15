using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_Unidades")]
    public partial class Ind_Unidad
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Ind_Unidad()
        {
            Ind_Materiales = new HashSet<Ind_Material>();
            Ind_ProgresoAspirantes = new HashSet<Ind_ProgresoAspirante>();
            Ind_Entregables = new HashSet<Ind_Entregable>();
        }

        [Key]
        public int UnidadID { get; set; }

        public int MateriaID { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        public int Orden { get; set; }

        // Navigation Properties
        [ForeignKey("MateriaID")]
        public virtual Ind_Materia Ind_Materia { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_Material> Ind_Materiales { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_ProgresoAspirante> Ind_ProgresoAspirantes { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_Entregable> Ind_Entregables { get; set; }
    }
}
