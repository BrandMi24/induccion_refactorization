using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_ProgresoAspirante")]
    public partial class Ind_ProgresoAspirante
    {
        [Key]
        public int ProgresoID { get; set; }

        public int AspiranteID { get; set; }

        public int UnidadID { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? Calificacion { get; set; }

        public DateTime FechaAsignacion { get; set; }

        public DateTime? FechaEnvio { get; set; }

        public int? UsuarioCalificadorID { get; set; }

        public string ComentariosEvaluador { get; set; }

        // Navigation Properties
        [ForeignKey("AspiranteID")]
        public virtual Aspirante Aspirante { get; set; }

        [ForeignKey("UnidadID")]
        public virtual Ind_Unidad Ind_Unidad { get; set; }

        [ForeignKey("UsuarioCalificadorID")]
        public virtual Usuario UsuarioCalificador { get; set; }
    }
}
