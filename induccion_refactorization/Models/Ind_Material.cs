using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_Materiales")]
    public partial class Ind_Material
    {
        [Key]
        public int MaterialID { get; set; }

        public int UnidadID { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoRecurso { get; set; }

        [Required]
        public string RutaURL { get; set; }

        // Navigation Properties
        [ForeignKey("UnidadID")]
        public virtual Ind_Unidad Ind_Unidad { get; set; }
    }
}
