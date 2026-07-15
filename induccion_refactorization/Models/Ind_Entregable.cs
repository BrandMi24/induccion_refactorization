using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_Entregables")]
    public partial class Ind_Entregable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Ind_Entregable()
        {
            Ind_Submisiones = new HashSet<Ind_Submision>();
        }

        [Key]
        public int EntregableID { get; set; }

        public int UnidadID { get; set; }

        [Required]
        [StringLength(255)]
        public string Titulo { get; set; }

        public string Instrucciones { get; set; }

        public DateTime? FechaLimite { get; set; }

        [Column(TypeName = "decimal")]
        public decimal PonderacionMax { get; set; }

        public bool Activo { get; set; }

        // Navigation Properties
        [ForeignKey("UnidadID")]
        public virtual Ind_Unidad Ind_Unidad { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_Submision> Ind_Submisiones { get; set; }
    }
}
