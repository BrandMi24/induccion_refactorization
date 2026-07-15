using System.Data.Entity;

namespace induccion_refactorization.Models
{
    public partial class CaptacionDbContext : DbContext
    {
        public CaptacionDbContext()
            : base("name=CaptacionDbContext")
        {
            // Disable lazy loading for better performance and explicit control
            Configuration.LazyLoadingEnabled = false;
            
            // Enable proxy creation for navigation properties
            Configuration.ProxyCreationEnabled = true;
        }

        // Core Tables
        public virtual DbSet<Usuario> Usuarios { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Aspirante> Aspirantes { get; set; }
        public virtual DbSet<Carrera> Carreras { get; set; }
        public virtual DbSet<Periodo> Periodos { get; set; }

        // Induction Module Tables
        public virtual DbSet<Ind_Materia> Ind_Materias { get; set; }
        public virtual DbSet<Ind_Unidad> Ind_Unidades { get; set; }
        public virtual DbSet<Ind_Material> Ind_Materiales { get; set; }
        public virtual DbSet<Ind_ProgresoAspirante> Ind_ProgresoAspirante { get; set; }
        public virtual DbSet<Ind_Entregable> Ind_Entregables { get; set; }
        public virtual DbSet<Ind_Submision> Ind_Submisiones { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure decimal precision for Calificacion
            modelBuilder.Entity<Ind_ProgresoAspirante>()
                .Property(e => e.Calificacion)
                .HasPrecision(5, 2);

            // Configure decimal precision for PromedioGeneral
            modelBuilder.Entity<Aspirante>()
                .Property(e => e.PromedioGeneral)
                .HasPrecision(3, 1);

            // Configure decimal precision for Ind_Entregable.PonderacionMax
            modelBuilder.Entity<Ind_Entregable>()
                .Property(e => e.PonderacionMax)
                .HasPrecision(5, 2);

            // Configure decimal precision for Ind_Submision.Calificacion
            modelBuilder.Entity<Ind_Submision>()
                .Property(e => e.Calificacion)
                .HasPrecision(5, 2);

            // Configure default values
            modelBuilder.Entity<Ind_Materia>()
                .Property(e => e.Activo)
                .IsRequired();

            modelBuilder.Entity<Ind_Unidad>()
                .Property(e => e.Orden)
                .IsRequired();

            modelBuilder.Entity<Ind_ProgresoAspirante>()
                .Property(e => e.Estado)
                .IsRequired()
                .HasMaxLength(50);

            // Configure cascade delete behavior
            modelBuilder.Entity<Ind_Unidad>()
                .HasRequired(u => u.Ind_Materia)
                .WithMany(m => m.Ind_Unidades)
                .HasForeignKey(u => u.MateriaID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_Material>()
                .HasRequired(m => m.Ind_Unidad)
                .WithMany(u => u.Ind_Materiales)
                .HasForeignKey(m => m.UnidadID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_ProgresoAspirante>()
                .HasRequired(p => p.Aspirante)
                .WithMany(a => a.Ind_ProgresoAspirantes)
                .HasForeignKey(p => p.AspiranteID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_ProgresoAspirante>()
                .HasRequired(p => p.Ind_Unidad)
                .WithMany(u => u.Ind_ProgresoAspirantes)
                .HasForeignKey(p => p.UnidadID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_Entregable>()
                .HasRequired(e => e.Ind_Unidad)
                .WithMany(u => u.Ind_Entregables)
                .HasForeignKey(e => e.UnidadID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_Submision>()
                .HasRequired(s => s.Aspirante)
                .WithMany(a => a.Ind_Submisiones)
                .HasForeignKey(s => s.AspiranteID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_Submision>()
                .HasRequired(s => s.Ind_Entregable)
                .WithMany(e => e.Ind_Submisiones)
                .HasForeignKey(s => s.EntregableID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_Submision>()
                .HasOptional(s => s.UsuarioRevisor)
                .WithMany()
                .HasForeignKey(s => s.UsuarioRevisorID)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
