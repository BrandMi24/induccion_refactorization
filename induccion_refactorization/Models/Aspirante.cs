using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Aspirantes")]
    public partial class Aspirante
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Aspirante()
        {
            Ind_ProgresoAspirantes = new HashSet<Ind_ProgresoAspirante>();
            Ind_Submisiones = new HashSet<Ind_Submision>();
        }

        [Key]
        public int AspiranteID { get; set; }

        [Required]
        [StringLength(10)]
        public string Folio { get; set; }

        [StringLength(10)]
        public string TelefonoAlterno { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaNacimiento { get; set; }

        [StringLength(18)]
        public string CURP { get; set; }

        [StringLength(11)]
        public string NSS { get; set; }

        [StringLength(200)]
        public string Direccion { get; set; }

        public int? AnoEgreso { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? PromedioGeneral { get; set; }

        public bool? CertificadoPreparatoria { get; set; }

        public int? PuntajeEXANI { get; set; }

        public int? PuntajePlacement { get; set; }

        public bool? PagoPreinscripcion { get; set; }

        public bool? PagoInscripcion { get; set; }

        public bool? PagoCENEVAL { get; set; }

        public bool? TieneBeca { get; set; }

        public int? PorcentajeBeca { get; set; }

        public bool? PlaticaIntroductoria { get; set; }

        [StringLength(10)]
        public string Matricula { get; set; }

        public bool? TieneDiscapacidad { get; set; }

        [StringLength(50)]
        public string TipoDiscapacidad { get; set; }

        public string Observaciones { get; set; }

        public int UsuarioID { get; set; }

        public int? GeneroID { get; set; }

        public int? EscuelaProcedenciaID { get; set; }

        public int PrimeraOpcionAreaID { get; set; }

        public int? SegundaOpcionAreaID { get; set; }

        public int? NivelInglesID { get; set; }

        // Navigation Properties
        [ForeignKey("UsuarioID")]
        public virtual Usuario Usuario { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_ProgresoAspirante> Ind_ProgresoAspirantes { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_Submision> Ind_Submisiones { get; set; }
    }
}
