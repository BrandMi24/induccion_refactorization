using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Usuarios")]
    public partial class Usuario
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Usuario()
        {
            Aspirantes = new HashSet<Aspirante>();
            Ind_ProgresoAspirantes = new HashSet<Ind_ProgresoAspirante>();
        }

        [Key]
        public int UsuarioID { get; set; }

        public int RolID { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(80)]
        public string ApellidoPaterno { get; set; }

        [StringLength(80)]
        public string ApellidoMaterno { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreUsuario { get; set; }

        [Required]
        [StringLength(200)]
        public string CorreoElectronico { get; set; }

        [StringLength(10)]
        public string Telefono { get; set; }

        [Required]
        [StringLength(255)]
        public string Contrasena { get; set; }

        public bool Activo { get; set; }

        public DateTime? FechaRegistro { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        public string FotoPerfil { get; set; }

        // Computed property for full name
        [NotMapped]
        public string NombreCompleto
        {
            get
            {
                return $"{Nombre} {ApellidoPaterno} {ApellidoMaterno}".Trim();
            }
        }

        // Navigation Properties
        [ForeignKey("RolID")]
        public virtual Role Role { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Aspirante> Aspirantes { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_ProgresoAspirante> Ind_ProgresoAspirantes { get; set; }
    }
}
