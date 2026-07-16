using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Documentos")]
    public partial class Documento
    {
        [Key]
        public int DocumentoID { get; set; }

        [Required]
        [StringLength(255)]
        public string NombreOriginal { get; set; }

        [Required]
        [StringLength(10)]
        public string ExtensionArchivo { get; set; }

        [Required]
        [StringLength(100)]
        public string TipoMIME { get; set; }

        public int TamanoArchivoBytes { get; set; }

        [Required]
        [StringLength(500)]
        public string RutaAlmacenamiento { get; set; }

        [Required]
        [StringLength(64)]
        public string HashArchivo { get; set; }

        public DateTime FechaSubida { get; set; }

        public DateTime? FechaRevision { get; set; }

        public int NumeroVersion { get; set; }

        public bool VersionActual { get; set; }

        public int AspiranteID { get; set; }

        public int TipoDocumentoID { get; set; }

        public int EstadoDocumentoID { get; set; }

        public int? UsuarioID { get; set; }

        // Navigation Properties
        [ForeignKey("AspiranteID")]
        public virtual Aspirante Aspirante { get; set; }

        [ForeignKey("TipoDocumentoID")]
        public virtual TipoDocumento TipoDocumento { get; set; }

        [ForeignKey("EstadoDocumentoID")]
        public virtual EstadoDocumento EstadoDocumento { get; set; }

        [ForeignKey("UsuarioID")]
        public virtual Usuario Usuario { get; set; }
    }
}
