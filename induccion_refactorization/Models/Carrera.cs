using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace induccion_refactorization.Models
{
    [Table("Carreras")]
    public partial class Carrera
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Carrera()
        {
            Ind_Materias = new HashSet<Ind_Materia>();
            Usuarios = new HashSet<Usuario>();
        }

        [Key]
        public int CarreraID { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(20)]
        public string Nomenclatura { get; set; }

        public int TipoCarreraID { get; set; }

        public bool Activo { get; set; }

        // Nullable: carreras creadas antes de este cambio (o pendientes de que un
        // Admin les asigne un Área) pueden quedar en NULL temporalmente.
        public int? AreaID { get; set; }

        // Navigation Properties
        [ForeignKey("TipoCarreraID")]
        public virtual TipoCarrera TipoCarrera { get; set; }

        [ForeignKey("AreaID")]
        public virtual Ind_Area Area { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_Materia> Ind_Materias { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Usuario> Usuarios { get; set; }
    }
}
