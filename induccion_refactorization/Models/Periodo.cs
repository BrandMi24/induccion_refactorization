using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Periodos")]
    public partial class Periodo
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Periodo()
        {
            Ind_Materias = new HashSet<Ind_Materia>();
        }

        [Key]
        public int PeriodoID { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaInicio { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaFin { get; set; }

        public bool Activo { get; set; }

        // Computed display label; the table has no dedicated name column
        [NotMapped]
        public string Nombre => $"{FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy}";

        // Navigation Properties
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_Materia> Ind_Materias { get; set; }
    }
}
