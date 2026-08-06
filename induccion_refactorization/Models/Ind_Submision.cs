using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Ind_Submisiones")]
    public partial class Ind_Submision
    {
        [Key]
        public int SubmisionID { get; set; }

        public int AspiranteID { get; set; }

        public int EntregableID { get; set; }

        [Required]
        [StringLength(500)]
        public string RutaArchivo { get; set; }

        public int? DocumentoID { get; set; }

        public DateTime FechaEnvio { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; }

        public string ComentarioRevisor { get; set; }

        public int? UsuarioRevisorID { get; set; }

        public DateTime? FechaRevision { get; set; }

        // Navigation Properties
        // "AspiranteID" en realidad apunta directo a Usuarios.UsuarioID — ya no
        // existe una tabla Aspirantes aparte; quién es aspirante se determina
        // por Usuario.RolID.
        [ForeignKey("AspiranteID")]
        public virtual Usuario AspiranteUsuario { get; set; }

        [ForeignKey("EntregableID")]
        public virtual Ind_Entregable Ind_Entregable { get; set; }

        [ForeignKey("UsuarioRevisorID")]
        public virtual Usuario UsuarioRevisor { get; set; }

        [ForeignKey("DocumentoID")]
        public virtual Documento Documento { get; set; }
    }
}
