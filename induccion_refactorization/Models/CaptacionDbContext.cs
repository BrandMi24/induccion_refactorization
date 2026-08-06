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
        public virtual DbSet<Carrera> Carreras { get; set; }
        public virtual DbSet<TipoCarrera> TiposCarreras { get; set; }
        public virtual DbSet<Ind_Area> Ind_Areas { get; set; }
        public virtual DbSet<Periodo> Periodos { get; set; }

        // Induction Module Tables
        public virtual DbSet<Ind_Materia> Ind_Materias { get; set; }
        public virtual DbSet<Ind_Unidad> Ind_Unidades { get; set; }
        public virtual DbSet<Ind_Material> Ind_Materiales { get; set; }
        public virtual DbSet<Ind_ProgresoAspirante> Ind_ProgresoAspirante { get; set; }
        public virtual DbSet<Ind_Entregable> Ind_Entregables { get; set; }
        public virtual DbSet<Ind_Submision> Ind_Submisiones { get; set; }
        public virtual DbSet<Ind_FelicitacionVista> Ind_FelicitacionesVistas { get; set; }

        // Permission System Tables
        public virtual DbSet<Ind_Permiso> Ind_Permisos { get; set; }
        public virtual DbSet<Ind_RolPermiso> Ind_RolPermisos { get; set; }
        public virtual DbSet<Ind_UsuarioPermiso> Ind_UsuarioPermisos { get; set; }

        // Document Management Tables
        public virtual DbSet<Documento> Documentos { get; set; }
        public virtual DbSet<TipoDocumento> TiposDocumentos { get; set; }
        public virtual DbSet<EstadoDocumento> EstadosDocumentos { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure default values
            modelBuilder.Entity<Ind_Materia>()
                .Property(e => e.Activo)
                .IsRequired();

            // Composite primary keys for the permission tables (no single [Key] property).
            modelBuilder.Entity<Ind_RolPermiso>()
                .HasKey(rp => new { rp.RolID, rp.PermisoID });

            modelBuilder.Entity<Ind_UsuarioPermiso>()
                .HasKey(up => new { up.UsuarioID, up.PermisoID });

            modelBuilder.Entity<Ind_RolPermiso>()
                .HasRequired(rp => rp.Role)
                .WithMany(r => r.Ind_RolPermisos)
                .HasForeignKey(rp => rp.RolID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_RolPermiso>()
                .HasRequired(rp => rp.Ind_Permiso)
                .WithMany(p => p.Ind_RolPermisos)
                .HasForeignKey(rp => rp.PermisoID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_UsuarioPermiso>()
                .HasRequired(up => up.Usuario)
                .WithMany()
                .HasForeignKey(up => up.UsuarioID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_UsuarioPermiso>()
                .HasRequired(up => up.Ind_Permiso)
                .WithMany(p => p.Ind_UsuarioPermisos)
                .HasForeignKey(up => up.PermisoID)
                .WillCascadeOnDelete(false);

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
                .HasRequired(p => p.AspiranteUsuario)
                .WithMany()
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
                .HasRequired(s => s.AspiranteUsuario)
                .WithMany()
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

            modelBuilder.Entity<Ind_Submision>()
                .HasOptional(s => s.Documento)
                .WithMany()
                .HasForeignKey(s => s.DocumentoID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Documento>()
                .HasRequired(d => d.AspiranteUsuario)
                .WithMany()
                .HasForeignKey(d => d.AspiranteID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Documento>()
                .HasRequired(d => d.TipoDocumento)
                .WithMany(t => t.Documentos)
                .HasForeignKey(d => d.TipoDocumentoID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Documento>()
                .HasRequired(d => d.EstadoDocumento)
                .WithMany(e => e.Documentos)
                .HasForeignKey(d => d.EstadoDocumentoID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Documento>()
                .HasOptional(d => d.Usuario)
                .WithMany()
                .HasForeignKey(d => d.UsuarioID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_FelicitacionVista>()
                .HasKey(f => new { f.AspiranteID, f.MateriaID });

            modelBuilder.Entity<Ind_FelicitacionVista>()
                .HasRequired(f => f.AspiranteUsuario)
                .WithMany()
                .HasForeignKey(f => f.AspiranteID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ind_FelicitacionVista>()
                .HasRequired(f => f.Ind_Materia)
                .WithMany()
                .HasForeignKey(f => f.MateriaID)
                .WillCascadeOnDelete(false);

            // Ind_Materia <-> Carrera many-to-many (a materia can target several careers, or all via TodasLasCarreras)
            modelBuilder.Entity<Ind_Materia>()
                .HasMany(m => m.Carreras)
                .WithMany(c => c.Ind_Materias)
                .Map(map =>
                {
                    map.ToTable("Ind_MateriaCarreras");
                    map.MapLeftKey("MateriaID");
                    map.MapRightKey("CarreraID");
                });

            // Usuario <-> Carrera many-to-many (a qué carrera(s) está asignado un Coordinador/Maestro/Aspirante)
            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Carreras)
                .WithMany(c => c.Usuarios)
                .Map(map =>
                {
                    map.ToTable("Ind_UsuarioCarreras");
                    map.MapLeftKey("UsuarioID");
                    map.MapRightKey("CarreraID");
                });

            base.OnModelCreating(modelBuilder);
        }
    }
}
