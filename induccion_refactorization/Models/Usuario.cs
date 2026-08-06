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
            Carreras = new HashSet<Carrera>();
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
        [StringLength(255, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string Contrasena { get; set; }

        public bool Activo { get; set; }

        public DateTime? FechaRegistro { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        public string FotoPerfil { get; set; }

        // Solo se usa para usuarios con rol Aspirante creados por la carga masiva:
        // el Área se autoasigna de la primera Área activa de su carrera.
        public int? Ind_AreaID { get; set; }

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

        [ForeignKey("Ind_AreaID")]
        public virtual Ind_Area Ind_Area { get; set; }

        // Carreras a las que está asignado este usuario (Coordinador, Maestro o Aspirante).
        // No aplica para Administrador.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Carrera> Carreras { get; set; }
    }
}
