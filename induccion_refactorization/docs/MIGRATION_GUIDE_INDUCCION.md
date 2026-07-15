# **Complete Migration & Refactoring Guide**
## **Induction Platform: Legacy Stack → ASP.NET MVC 5 (.NET Framework 4.7.2)**

---

## **Table of Contents**
1. [Executive Summary](#executive-summary)
2. [Codebase Structural Blueprint](#1-codebase-structural-blueprint-induccion_migration)
3. [Database-First Entity Framework Setup](#2-database-first-entity-framework-setup)
4. [API Endpoint-to-Controller Remapping](#3-api-endpoint-to-controller-remapping)
5. [UI/UX Transition: Vue/Quasar → Razor](#4-uiux-transition-vuequasar--razor)
6. [Legacy Bug Fixes](#5-legacy-bug-fixes)
7. [Authentication & Authorization](#6-authentication--authorization)
8. [Deployment Checklist](#7-deployment-checklist)

---

## **Executive Summary**

### **Migration Scope**
**FROM:**
- **Frontend:** Quasar Framework (Vue.js 3) @ `C:\dev\induccion`
- **Backend:** Strapi 5.x (Node.js) @ `C:\dev\induccion-api`
- **Database:** SQL Server `CaptacionDB` (already structured)

**TO:**
- **Unified Platform:** ASP.NET MVC 5 (.NET Framework 4.7.2) with C# and Razor Views
- **Location:** `C:\dev\induccion_migration`
- **Architecture:** Server-side rendering, Entity Framework 6.x (Database-First), Bootstrap 4 UI

### **Key Objectives**
1. ✅ Eliminate the dual-codebase architecture (Quasar + Strapi)
2. ✅ Leverage existing `CaptacionDB` schema with new `Ind_*` tables
3. ✅ Maintain role-based security (1-Admin, 2-Aspirante, 3-Coordinador, 4-Captador)
4. ✅ Fix critical legacy bugs (double email check, missing student roster)
5. ✅ Preserve all existing functionality with improved performance

---

## **1. Codebase Structural Blueprint (`induccion_migration`)**

### **1.1 Complete ASP.NET MVC 5 Folder Structure**

```
C:\dev\induccion_migration\
│
├── App_Start\
│   ├── BundleConfig.cs              # CSS/JS bundling & minification
│   ├── FilterConfig.cs              # Global filters (authorization, error handling)
│   ├── RouteConfig.cs               # URL routing patterns
│   └── Startup.Auth.cs              # OWIN authentication middleware
│
├── Content\                          # Static CSS files
│   ├── bootstrap.min.css
│   ├── bootstrap-theme.min.css
│   ├── Site.css                     # Custom global styles
│   └── themes\
│       └── induccion-theme.css      # Custom theme matching Quasar colors
│
├── Controllers\
│   ├── AccountController.cs         # Login, Logout, Session management
│   ├── AdminController.cs           # Role 1 - Full system administration
│   ├── AspiranteController.cs       # Role 2 - Student workspace
│   ├── CoordinadorController.cs     # Role 3 - Coordinator dashboard
│   ├── CaptadorController.cs        # Role 4 - Recruitment management
│   └── HomeController.cs            # Landing page
│
├── Models\                           # Entity Framework DB-First Models
│   ├── CaptacionDbContext.cs        # EF DbContext (auto-generated + partial extensions)
│   ├── Usuario.cs                   # Maps to dbo.Usuarios
│   ├── Role.cs                      # Maps to dbo.Roles
│   ├── Aspirante.cs                 # Maps to dbo.Aspirantes
│   ├── Carrera.cs                   # Maps to dbo.Carreras
│   ├── Periodo.cs                   # Maps to dbo.Periodos
│   ├── Ind_Materia.cs               # Maps to dbo.Ind_Materias
│   ├── Ind_Unidad.cs                # Maps to dbo.Ind_Unidades
│   ├── Ind_Material.cs              # Maps to dbo.Ind_Materiales
│   └── Ind_ProgresoAspirante.cs     # Maps to dbo.Ind_ProgresoAspirantes
│
├── ViewModels\                       # DTOs for complex views
│   ├── LoginViewModel.cs
│   ├── AspiranteWorkspaceViewModel.cs
│   ├── CoordinadorDashboardViewModel.cs
│   ├── MateriaDetalleViewModel.cs
│   └── TareaSubmissionViewModel.cs
│
├── Views\
│   ├── Shared\
│   │   ├── _Layout.cshtml           # Master layout with Bootstrap navbar
│   │   ├── _LoginPartial.cshtml     # User session info widget
│   │   └── Error.cshtml
│   ├── Account\
│   │   └── Login.cshtml
│   ├── Aspirante\
│   │   ├── Index.cshtml             # Course list (replaces ListaMateria.vue)
│   │   ├── ContenidoMateria.cshtml  # Course content view
│   │   └── EditarEmail.cshtml       # Email update form (bug fix)
│   ├── Coordinador\
│   │   ├── Index.cshtml             # Student roster (FIX: missing list bug)
│   │   ├── ListaMateria.cshtml
│   │   ├── TareaAlumno.cshtml       # Grade assignment view
│   │   └── CargarAlumnos.cshtml     # Bulk student upload
│   ├── Captador\
│   │   └── Index.cshtml
│   └── Admin\
│       ├── Index.cshtml
│       ├── CrearMateria.cshtml
│       └── GestionUsuarios.cshtml
│
├── Scripts\                          # Client-side JavaScript
│   ├── jquery-3.6.0.min.js
│   ├── bootstrap.min.js
│   ├── aspnet-ajax-validation.js
│   └── induccion-custom.js          # Custom AJAX handlers
│
├── Helpers\                          # Utility classes
│   ├── FileUploadHelper.cs          # Handle assignment submissions
│   ├── SecurityHelper.cs            # Password hashing (BCrypt/PBKDF2)
│   └── RoleAuthorizationHelper.cs   # Custom role validation
│
├── Services\                         # Business logic layer
│   ├── AspiranteService.cs
│   ├── MateriaService.cs
│   └── CalificacionService.cs
│
├── Filters\                          # Custom MVC filters
│   └── RoleAuthorizeAttribute.cs    # Custom [RoleAuthorize(1,3)] attribute
│
├── Web.config                        # Connection strings, authentication mode
└── Global.asax.cs                   # Application startup configuration
```

---

## **2. Database-First Entity Framework Setup**

### **2.1 Generate EF Models from CaptacionDB**

**Step 1:** Install Entity Framework 6.x via NuGet Package Manager Console:
```powershell
Install-Package EntityFramework -Version 6.4.4
```

**Step 2:** Add ADO.NET Entity Data Model (Database-First approach):
1. Right-click `Models` folder → Add → New Item → ADO.NET Entity Data Model
2. Choose "EF Designer from database"
3. Configure connection string to `CaptacionDB`:
```xml
<connectionStrings>
  <add name="CaptacionDbContext" 
       connectionString="Data Source=YOUR_SERVER;Initial Catalog=CaptacionDB;Integrated Security=True;MultipleActiveResultSets=True" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

**Step 3:** Select tables to include in model:
- Core Tables: `Usuarios`, `Roles`, `Aspirantes`, `Carreras`, `Periodos`
- Induction Tables: `Ind_Materias`, `Ind_Unidades`, `Ind_Materiales`, `Ind_ProgresoAspirantes`

### **2.2 Strapi Content-Type → C# Model Mapping**

#### **Legacy Strapi Schema vs. New EF Model**

| **Strapi Collection** | **SQL Server Table** | **C# Model Class** | **Key Changes** |
|----------------------|---------------------|-------------------|----------------|
| `usuarios` (Strapi) | `dbo.Ind_ProgresoAspirantes` + `dbo.Aspirantes` | `Ind_ProgresoAspirante.cs` | Merged user profile into existing `Aspirantes` table |
| `materias` (Strapi) | `dbo.Ind_Materias` | `Ind_Materia.cs` | Changed JSON `unidades` field → relational `Ind_Unidades` table |
| `tareas` (Strapi) | `dbo.Ind_ProgresoAspirantes` | `Ind_ProgresoAspirante.cs` | JSON `entregados` → structured columns (`FechaEnvio`, `Estado`, `Calificacion`) |

#### **Example: `Ind_Materia.cs` Model**

```csharp
namespace InduccionMigration.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Ind_Materias")]
    public partial class Ind_Materia
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Ind_Materia()
        {
            Ind_Unidades = new HashSet<Ind_Unidad>();
        }

        [Key]
        public int MateriaID { get; set; }

        public int CarreraID { get; set; }

        public int PeriodoID { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public bool Activo { get; set; }

        // Navigation Properties
        [ForeignKey("CarreraID")]
        public virtual Carrera Carrera { get; set; }

        [ForeignKey("PeriodoID")]
        public virtual Periodo Periodo { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Ind_Unidad> Ind_Unidades { get; set; }
    }
}
```

#### **Example: `Ind_ProgresoAspirante.cs` Model**

```csharp
namespace InduccionMigration.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Ind_ProgresoAspirantes")]
    public partial class Ind_ProgresoAspirante
    {
        [Key]
        public int ProgresoID { get; set; }

        public int AspiranteID { get; set; }

        public int UnidadID { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } // "Asignado", "Entregado", "Calificado"

        [Column(TypeName = "decimal(5, 2)")]
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
```

#### **DbContext Configuration**

```csharp
namespace InduccionMigration.Models
{
    using System.Data.Entity;

    public partial class CaptacionDbContext : DbContext
    {
        public CaptacionDbContext()
            : base("name=CaptacionDbContext")
        {
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
        public virtual DbSet<Ind_ProgresoAspirante> Ind_ProgresoAspirantes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure decimal precision
            modelBuilder.Entity<Ind_ProgresoAspirante>()
                .Property(e => e.Calificacion)
                .HasPrecision(5, 2);
        }
    }
}
```

---

## **3. API Endpoint-to-Controller Remapping**

### **3.1 Legacy Strapi Endpoints Analysis**

Based on your Strapi API structure, here are the core endpoints being used:

| **Legacy Endpoint** | **HTTP Method** | **Purpose** | **New MVC Route** |
|---------------------|----------------|------------|------------------|
| `/api/auth/local` | POST | Login with credentials | `/Account/Login` |
| `/api/users/me?populate=role` | GET | Get current user profile | Session-based (no API call) |
| `/api/usuarios?populate=carrera&filters[user][documentId][$eq]=...` | GET | Get student details | `/Aspirante/Index` (server-side) |
| `/api/materias?populate=coordinacion` | GET | List all subjects | `/Aspirante/Index` or `/Coordinador/ListaMaterias` |
| `/api/materias/:id` | GET | Get subject details | `/Aspirante/ContenidoMateria/{id}` |
| `/api/tareas?filters[user][id][$eq]=...` | GET | Get student assignments | `/Aspirante/ContenidoMateria/{id}` |
| `/api/tareas/:id` | PUT | Update assignment (submit/grade) | `/Coordinador/CalificarTarea/{id}` |
| `/api/usuarios?pagination[page]=1&pagination[pageSize]=25` | GET | Coordinator student list | `/Coordinador/Index` |

### **3.2 AccountController - Unified Login**

**File:** `Controllers/AccountController.cs`

```csharp
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using InduccionMigration.Models;
using InduccionMigration.ViewModels;

namespace InduccionMigration.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /Account/Login
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Query CaptacionDB Usuarios table
            var user = db.Usuarios
                .Include("Role")
                .FirstOrDefault(u => u.Email == model.Email && u.Activo);

            if (user == null)
            {
                ModelState.AddModelError("", "Email no registrado o usuario inactivo.");
                return View(model);
            }

            // Validate password (assuming BCrypt hashing)
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Contraseña incorrecta.");
                return View(model);
            }

            // Create Forms Authentication ticket
            var authTicket = new FormsAuthenticationTicket(
                version: 1,
                name: user.Email,
                issueDate: DateTime.Now,
                expiration: DateTime.Now.AddHours(8),
                isPersistent: model.RememberMe,
                userData: $"{user.UsuarioID}|{user.RolID}|{user.NombreCompleto}"
            );

            string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
            var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL
            };
            Response.Cookies.Add(authCookie);

            // Store additional session data
            Session["UsuarioID"] = user.UsuarioID;
            Session["RolID"] = user.RolID;
            Session["NombreCompleto"] = user.NombreCompleto;
            Session["Email"] = user.Email;

            // Role-specific data loading
            if (user.RolID == 2) // Aspirante
            {
                var aspirante = db.Aspirantes
                    .Include("Carrera")
                    .FirstOrDefault(a => a.UsuarioID == user.UsuarioID);

                if (aspirante != null)
                {
                    Session["AspiranteID"] = aspirante.AspiranteID;
                    Session["CarreraNombre"] = aspirante.Carrera?.Nombre;
                    Session["Especialidad"] = aspirante.Especialidad;

                    // FIX BUG #1: Check for dummy email and redirect
                    if (user.Email.Contains("@example.com"))
                    {
                        return RedirectToAction("EditarEmail", "Aspirante");
                    }
                }

                return RedirectToAction("Index", "Aspirante");
            }
            else if (user.RolID == 3) // Coordinador
            {
                // Load coordinator-specific data if needed
                return RedirectToAction("Index", "Coordinador");
            }
            else if (user.RolID == 4) // Captador
            {
                return RedirectToAction("Index", "Captador");
            }
            else if (user.RolID == 1) // Admin
            {
                return RedirectToAction("Index", "Admin");
            }

            // Fallback to home
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
```

**ViewModel:** `ViewModels/LoginViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace InduccionMigration.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
    }
}
```

### **3.3 AspiranteController - Student Workspace**

**File:** `Controllers/AspiranteController.cs`

```csharp
using System;
using System.Linq;
using System.Web.Mvc;
using InduccionMigration.Models;
using InduccionMigration.ViewModels;
using InduccionMigration.Filters;

namespace InduccionMigration.Controllers
{
    [RoleAuthorize(2)] // Only Aspirante role (RolID = 2)
    public class AspiranteController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /Aspirante/Index (replaces ListaMateria.vue)
        public ActionResult Index()
        {
            int usuarioID = (int)Session["UsuarioID"];

            // Get aspirante profile
            var aspirante = db.Aspirantes
                .Include("Carrera")
                .FirstOrDefault(a => a.UsuarioID == usuarioID);

            if (aspirante == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get assigned courses using SQL JOIN (from analisis_captaciondb.md)
            var materias = db.Ind_Materias
                .Where(m => m.CarreraID == aspirante.CarreraID && m.Activo)
                .Select(m => new MateriaViewModel
                {
                    MateriaID = m.MateriaID,
                    Nombre = m.Nombre,
                    Descripcion = m.Descripcion,
                    CarreraNombre = m.Carrera.Nombre,
                    PeriodoNombre = m.Periodo.Nombre
                })
                .ToList();

            ViewBag.NombreCompleto = aspirante.Usuario.NombreCompleto;
            ViewBag.CarreraNombre = aspirante.Carrera?.Nombre;

            return View(materias);
        }

        // GET: /Aspirante/ContenidoMateria/{id}
        public ActionResult ContenidoMateria(int id)
        {
            int usuarioID = (int)Session["UsuarioID"];
            int aspiranteID = (int)Session["AspiranteID"];

            // Verify student has access to this course
            var aspirante = db.Aspirantes.Find(aspiranteID);
            var materia = db.Ind_Materias
                .Include("Ind_Unidades.Ind_Materiales")
                .FirstOrDefault(m => m.MateriaID == id && m.CarreraID == aspirante.CarreraID);

            if (materia == null)
            {
                return HttpNotFound("Materia no encontrada o no asignada.");
            }

            // Get student progress for each unit
            var progreso = db.Ind_ProgresoAspirantes
                .Where(p => p.AspiranteID == aspiranteID && 
                           p.Ind_Unidad.MateriaID == id)
                .ToList();

            var viewModel = new MateriaDetalleViewModel
            {
                MateriaID = materia.MateriaID,
                Nombre = materia.Nombre,
                Descripcion = materia.Descripcion,
                Unidades = materia.Ind_Unidades.Select(u => new UnidadViewModel
                {
                    UnidadID = u.UnidadID,
                    Nombre = u.Nombre,
                    Orden = u.Orden,
                    Materiales = u.Ind_Materiales.Select(mat => new MaterialViewModel
                    {
                        MaterialID = mat.MaterialID,
                        Nombre = mat.Nombre,
                        TipoRecurso = mat.TipoRecurso,
                        RutaURL = mat.RutaURL
                    }).ToList(),
                    Progreso = progreso.FirstOrDefault(p => p.UnidadID == u.UnidadID)
                }).OrderBy(u => u.Orden).ToList()
            };

            return View(viewModel);
        }

        // POST: /Aspirante/EnviarTarea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnviarTarea(int unidadID, HttpPostedFileBase archivoEntrega)
        {
            int aspiranteID = (int)Session["AspiranteID"];

            var progreso = db.Ind_ProgresoAspirantes
                .FirstOrDefault(p => p.AspiranteID == aspiranteID && p.UnidadID == unidadID);

            if (progreso == null)
            {
                // Create new progress record if not exists
                progreso = new Ind_ProgresoAspirante
                {
                    AspiranteID = aspiranteID,
                    UnidadID = unidadID,
                    Estado = "Asignado",
                    FechaAsignacion = DateTime.Now
                };
                db.Ind_ProgresoAspirantes.Add(progreso);
            }

            // Update to "Entregado" status
            progreso.Estado = "Entregado";
            progreso.FechaEnvio = DateTime.Now;

            // Handle file upload (save to server)
            if (archivoEntrega != null && archivoEntrega.ContentLength > 0)
            {
                string fileName = $"{aspiranteID}_{unidadID}_{DateTime.Now:yyyyMMddHHmmss}_{archivoEntrega.FileName}";
                string path = Server.MapPath($"~/App_Data/Uploads/{fileName}");
                archivoEntrega.SaveAs(path);
                
                // Store file path in database (you may need to add a column)
                // progreso.ArchivoEntrega = fileName;
            }

            db.SaveChanges();

            TempData["Success"] = "Tarea enviada exitosamente.";
            return RedirectToAction("ContenidoMateria", new { id = progreso.Ind_Unidad.MateriaID });
        }

        // GET: /Aspirante/EditarEmail (BUG FIX #1)
        public ActionResult EditarEmail()
        {
            int usuarioID = (int)Session["UsuarioID"];
            var usuario = db.Usuarios.Find(usuarioID);

            if (usuario == null || !usuario.Email.Contains("@example.com"))
            {
                return RedirectToAction("Index");
            }

            return View(new EditarEmailViewModel { EmailActual = usuario.Email });
        }

        // POST: /Aspirante/EditarEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarEmail(EditarEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int usuarioID = (int)Session["UsuarioID"];
            var usuario = db.Usuarios.Find(usuarioID);

            if (usuario == null)
            {
                return HttpNotFound();
            }

            // Verify email doesn't already exist
            if (db.Usuarios.Any(u => u.Email == model.NuevoEmail && u.UsuarioID != usuarioID))
            {
                ModelState.AddModelError("NuevoEmail", "Este correo ya está registrado.");
                return View(model);
            }

            usuario.Email = model.NuevoEmail;
            db.SaveChanges();

            Session["Email"] = model.NuevoEmail;
            TempData["Success"] = "Correo actualizado exitosamente.";

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
```

### **3.4 CoordinadorController - Student Roster & Grading**

**File:** `Controllers/CoordinadorController.cs`

```csharp
using System;
using System.Linq;
using System.Web.Mvc;
using InduccionMigration.Models;
using InduccionMigration.ViewModels;
using InduccionMigration.Filters;
using System.Data.Entity;

namespace InduccionMigration.Controllers
{
    [RoleAuthorize(3)] // Only Coordinador role (RolID = 3)
    public class CoordinadorController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /Coordinador/Index (BUG FIX #2: Missing Student List)
        public ActionResult Index(int page = 1, int pageSize = 25)
        {
            // Get total count for pagination
            int totalStudents = db.Aspirantes.Count(a => a.Usuario.Activo);

            // Fetch paginated student list with proper JOIN
            var alumnos = db.Aspirantes
                .Include("Usuario")
                .Include("Carrera")
                .Where(a => a.Usuario.Activo)
                .OrderBy(a => a.Usuario.NombreCompleto)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AspiranteListViewModel
                {
                    AspiranteID = a.AspiranteID,
                    NombreCompleto = a.Usuario.NombreCompleto,
                    Email = a.Usuario.Email,
                    CarreraNombre = a.Carrera.Nombre,
                    Especialidad = a.Especialidad,
                    Username = a.Usuario.Email // Using email as username for routing
                })
                .ToList();

            var viewModel = new CoordinadorDashboardViewModel
            {
                Alumnos = alumnos,
                TotalAlumnos = totalStudents,
                PaginaActual = page,
                ElementosPorPagina = pageSize,
                TotalPaginas = (int)Math.Ceiling((double)totalStudents / pageSize)
            };

            ViewBag.NombreCompleto = Session["NombreCompleto"];

            return View(viewModel);
        }

        // GET: /Coordinador/TareaAlumno/{username}
        public ActionResult TareaAlumno(string username)
        {
            // Find student by email (username)
            var aspirante = db.Aspirantes
                .Include("Usuario")
                .Include("Carrera")
                .FirstOrDefault(a => a.Usuario.Email == username);

            if (aspirante == null)
            {
                return HttpNotFound("Aspirante no encontrado.");
            }

            // Get all progress records for this student
            var progreso = db.Ind_ProgresoAspirantes
                .Include("Ind_Unidad.Ind_Materia")
                .Include("UsuarioCalificador")
                .Where(p => p.AspiranteID == aspirante.AspiranteID)
                .OrderBy(p => p.Ind_Unidad.Ind_Materia.Nombre)
                .ThenBy(p => p.Ind_Unidad.Orden)
                .ToList();

            var viewModel = new TareaAlumnoViewModel
            {
                AspiranteID = aspirante.AspiranteID,
                NombreCompleto = aspirante.Usuario.NombreCompleto,
                Email = aspirante.Usuario.Email,
                CarreraNombre = aspirante.Carrera.Nombre,
                Progreso = progreso.Select(p => new ProgresoViewModel
                {
                    ProgresoID = p.ProgresoID,
                    MateriaNombre = p.Ind_Unidad.Ind_Materia.Nombre,
                    UnidadNombre = p.Ind_Unidad.Nombre,
                    Estado = p.Estado,
                    Calificacion = p.Calificacion,
                    FechaAsignacion = p.FechaAsignacion,
                    FechaEnvio = p.FechaEnvio,
                    CalificadorNombre = p.UsuarioCalificador?.NombreCompleto,
                    Comentarios = p.ComentariosEvaluador
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: /Coordinador/CalificarTarea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CalificarTarea(int progresoID, decimal calificacion, string comentarios)
        {
            int usuarioID = (int)Session["UsuarioID"];

            var progreso = db.Ind_ProgresoAspirantes.Find(progresoID);

            if (progreso == null)
            {
                return HttpNotFound();
            }

            if (progreso.Estado != "Entregado")
            {
                TempData["Error"] = "Solo se pueden calificar tareas entregadas.";
                return RedirectToAction("TareaAlumno", new { username = progreso.Aspirante.Usuario.Email });
            }

            progreso.Estado = "Calificado";
            progreso.Calificacion = calificacion;
            progreso.ComentariosEvaluador = comentarios;
            progreso.UsuarioCalificadorID = usuarioID;

            db.SaveChanges();

            TempData["Success"] = "Tarea calificada exitosamente.";
            return RedirectToAction("TareaAlumno", new { username = progreso.Aspirante.Usuario.Email });
        }

        // GET: /Coordinador/ListaMaterias
        public ActionResult ListaMaterias()
        {
            var materias = db.Ind_Materias
                .Include("Carrera")
                .Include("Periodo")
                .Where(m => m.Activo)
                .Select(m => new MateriaViewModel
                {
                    MateriaID = m.MateriaID,
                    Nombre = m.Nombre,
                    Descripcion = m.Descripcion,
                    CarreraNombre = m.Carrera.Nombre,
                    PeriodoNombre = m.Periodo.Nombre
                })
                .ToList();

            return View(materias);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
```

### **3.5 Custom Authorization Filter**

**File:** `Filters/RoleAuthorizeAttribute.cs`

```csharp
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace InduccionMigration.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly int[] _allowedRoles;

        public RoleAuthorizeAttribute(params int[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            if (httpContext.Session["RolID"] == null)
            {
                return false;
            }

            int userRoleID = (int)httpContext.Session["RolID"];

            return _allowedRoles.Contains(userRoleID);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
            }
            else
            {
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/Unauthorized.cshtml"
                };
            }
        }
    }
}
```

---

## **4. UI/UX Transition: Vue/Quasar → Razor**

### **4.1 Layout Structure Conversion**

**Legacy Quasar Layout:** `induccion/src/layouts/MainLayout.vue`
**New Razor Layout:** `Views/Shared/_Layout.cshtml`

```html
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@ViewBag.Title - Sistema de Inducción</title>
    @Styles.Render("~/Content/css")
    @Scripts.Render("~/bundles/modernizr")
</head>
<body>
    <nav class="navbar navbar-expand-lg navbar-dark bg-primary">
        <div class="container">
            <a class="navbar-brand" href="@Url.Action("Index", "Home")">
                <i class="fas fa-graduation-cap"></i> Sistema de Inducción
            </a>
            <button class="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarNav">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbarNav">
                @Html.Partial("_LoginPartial")
            </div>
        </div>
    </nav>

    <div class="container body-content">
        @if (TempData["Success"] != null)
        {
            <div class="alert alert-success alert-dismissible fade show mt-3" role="alert">
                <strong>Éxito!</strong> @TempData["Success"]
                <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
        }

        @if (TempData["Error"] != null)
        {
            <div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
                <strong>Error!</strong> @TempData["Error"]
                <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
        }

        @RenderBody()
        <hr />
        <footer>
            <p>&copy; @DateTime.Now.Year - Universidad Tecnológica</p>
        </footer>
    </div>

    @Scripts.Render("~/bundles/jquery")
    @Scripts.Render("~/bundles/bootstrap")
    @RenderSection("scripts", required: false)
</body>
</html>
```

**Partial View:** `Views/Shared/_LoginPartial.cshtml`

```html
@if (Request.IsAuthenticated)
{
    <ul class="navbar-nav ml-auto">
        <li class="nav-item dropdown">
            <a class="nav-link dropdown-toggle" href="#" id="userDropdown" role="button" data-toggle="dropdown">
                <i class="fas fa-user-circle"></i> @Session["NombreCompleto"]
            </a>
            <div class="dropdown-menu dropdown-menu-right" aria-labelledby="userDropdown">
                <div class="dropdown-item-text">
                    <small class="text-muted">@Session["Email"]</small>
                </div>
                <div class="dropdown-divider"></div>
                @{
                    int rolID = (int)Session["RolID"];
                    string dashboardUrl = rolID == 2 ? Url.Action("Index", "Aspirante") :
                                         rolID == 3 ? Url.Action("Index", "Coordinador") :
                                         rolID == 4 ? Url.Action("Index", "Captador") :
                                         Url.Action("Index", "Admin");
                }
                <a class="dropdown-item" href="@dashboardUrl">
                    <i class="fas fa-tachometer-alt"></i> Mi Panel
                </a>
                <div class="dropdown-divider"></div>
                @using (Html.BeginForm("Logout", "Account", FormMethod.Post, new { id = "logoutForm" }))
                {
                    @Html.AntiForgeryToken()
                    <a href="javascript:document.getElementById('logoutForm').submit()" class="dropdown-item text-danger">
                        <i class="fas fa-sign-out-alt"></i> Cerrar Sesión
                    </a>
                }
            </div>
        </li>
    </ul>
}
else
{
    <ul class="navbar-nav ml-auto">
        <li class="nav-item">
            <a class="nav-link" href="@Url.Action("Login", "Account")">
                <i class="fas fa-sign-in-alt"></i> Iniciar Sesión
            </a>
        </li>
    </ul>
}
```

### **4.2 Student Workspace View**

**Legacy:** `induccion/src/pages/alumno/ListaMateria.vue`  
**New:** `Views/Aspirante/Index.cshtml`

```html
@model List<InduccionMigration.ViewModels.MateriaViewModel>

@{
    ViewBag.Title = "Mis Materias";
}

<div class="row mt-4">
    <div class="col-md-12">
        <div class="card">
            <div class="card-header bg-primary text-white">
                <h4 class="mb-0">
                    <i class="fas fa-book"></i> Lista de Materias
                </h4>
                <small>@ViewBag.NombreCompleto - @ViewBag.CarreraNombre</small>
            </div>
            <div class="card-body">
                @if (Model.Count > 0)
                {
                    <div class="table-responsive">
                        <table class="table table-striped table-hover">
                            <thead class="thead-dark">
                                <tr>
                                    <th>ID</th>
                                    <th>Materia</th>
                                    <th>Descripción</th>
                                    <th>Periodo</th>
                                    <th class="text-center">Acciones</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var materia in Model)
                                {
                                    <tr>
                                        <td>@materia.MateriaID</td>
                                        <td><strong>@materia.Nombre</strong></td>
                                        <td>@(materia.Descripcion?.Substring(0, Math.Min(materia.Descripcion.Length, 50)) ?? "Sin descripción")...</td>
                                        <td>@materia.PeriodoNombre</td>
                                        <td class="text-center">
                                            <a href="@Url.Action("ContenidoMateria", new { id = materia.MateriaID })" 
                                               class="btn btn-sm btn-info" 
                                               title="Ver Contenido">
                                                <i class="fas fa-folder-open"></i> Abrir
                                            </a>
                                        </td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                }
                else
                {
                    <div class="alert alert-info" role="alert">
                        <i class="fas fa-info-circle"></i> No tienes materias asignadas actualmente.
                    </div>
                }
            </div>
        </div>
    </div>
</div>
```

### **4.3 Course Content View (with Units and Materials)**

**File:** `Views/Aspirante/ContenidoMateria.cshtml`

```html
@model InduccionMigration.ViewModels.MateriaDetalleViewModel

@{
    ViewBag.Title = Model.Nombre;
}

<div class="row mt-4">
    <div class="col-md-12">
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb">
                <li class="breadcrumb-item">
                    <a href="@Url.Action("Index")">Mis Materias</a>
                </li>
                <li class="breadcrumb-item active" aria-current="page">@Model.Nombre</li>
            </ol>
        </nav>

        <div class="card">
            <div class="card-header bg-primary text-white">
                <h3 class="mb-0">@Model.Nombre</h3>
            </div>
            <div class="card-body">
                <div class="mb-3">
                    @Html.Raw(Model.Descripcion)
                </div>

                <hr />

                <h4>Unidades</h4>

                @foreach (var unidad in Model.Unidades)
                {
                    <div class="card mb-3">
                        <div class="card-header" style="background-color: #f8f9fa;">
                            <h5 class="mb-0">
                                <i class="fas fa-layer-group"></i> Unidad @unidad.Orden: @unidad.Nombre
                            </h5>
                        </div>
                        <div class="card-body">
                            @if (unidad.Materiales.Count > 0)
                            {
                                <h6>Materiales:</h6>
                                <ul class="list-group mb-3">
                                    @foreach (var material in unidad.Materiales)
                                    {
                                        <li class="list-group-item">
                                            @if (material.TipoRecurso == "PDF")
                                            {
                                                <i class="fas fa-file-pdf text-danger"></i>
                                            }
                                            else if (material.TipoRecurso == "Video")
                                            {
                                                <i class="fas fa-video text-primary"></i>
                                            }
                                            else
                                            {
                                                <i class="fas fa-link text-success"></i>
                                            }
                                            <a href="@material.RutaURL" target="_blank">@material.Nombre</a>
                                        </li>
                                    }
                                </ul>
                            }

                            @if (unidad.Progreso != null)
                            {
                                <div class="alert alert-@(unidad.Progreso.Estado == "Calificado" ? "success" : 
                                                         unidad.Progreso.Estado == "Entregado" ? "info" : "warning")" 
                                     role="alert">
                                    <strong>Estado:</strong> @unidad.Progreso.Estado
                                    @if (unidad.Progreso.Calificacion.HasValue)
                                    {
                                        <br />
                                        <strong>Calificación:</strong> @unidad.Progreso.Calificacion.Value.ToString("0.00")
                                    }
                                    @if (!string.IsNullOrEmpty(unidad.Progreso.ComentariosEvaluador))
                                    {
                                        <br />
                                        <strong>Comentarios:</strong> @unidad.Progreso.ComentariosEvaluador
                                    }
                                </div>

                                @if (unidad.Progreso.Estado == "Asignado")
                                {
                                    using (Html.BeginForm("EnviarTarea", "Aspirante", FormMethod.Post, new { enctype = "multipart/form-data" }))
                                    {
                                        @Html.AntiForgeryToken()
                                        @Html.Hidden("unidadID", unidad.UnidadID)
                                        
                                        <div class="form-group">
                                            <label for="archivoEntrega">Subir Entrega:</label>
                                            <input type="file" class="form-control-file" id="archivoEntrega" name="archivoEntrega" required />
                                        </div>
                                        <button type="submit" class="btn btn-primary">
                                            <i class="fas fa-upload"></i> Enviar Tarea
                                        </button>
                                    }
                                }
                            }
                            else
                            {
                                <div class="alert alert-secondary" role="alert">
                                    Esta unidad aún no ha sido asignada.
                                </div>
                            }
                        </div>
                    </div>
                }
            </div>
        </div>
    </div>
</div>
```

### **4.4 Coordinator Dashboard (Student List)**

**File:** `Views/Coordinador/Index.cshtml`

```html
@model InduccionMigration.ViewModels.CoordinadorDashboardViewModel

@{
    ViewBag.Title = "Lista de Alumnos";
}

<div class="row mt-4">
    <div class="col-md-12">
        <div class="card">
            <div class="card-header bg-success text-white">
                <div class="row">
                    <div class="col-md-6">
                        <h4 class="mb-0">
                            <i class="fas fa-users"></i> Lista de Alumnos
                        </h4>
                        <small>@ViewBag.NombreCompleto</small>
                    </div>
                    <div class="col-md-6 text-right">
                        <a href="@Url.Action("CargarAlumnos")" class="btn btn-light btn-sm">
                            <i class="fas fa-plus"></i> Cargar Alumnos
                        </a>
                        <a href="@Url.Action("ListaMaterias")" class="btn btn-light btn-sm">
                            <i class="fas fa-book"></i> Materias
                        </a>
                    </div>
                </div>
            </div>
            <div class="card-body">
                @if (Model.Alumnos.Count > 0)
                {
                    <div class="table-responsive">
                        <table class="table table-striped table-hover">
                            <thead class="thead-dark">
                                <tr>
                                    <th>ID</th>
                                    <th>Nombre Completo</th>
                                    <th>Correo</th>
                                    <th>Carrera</th>
                                    <th>Especialidad</th>
                                    <th class="text-center">Acciones</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var alumno in Model.Alumnos)
                                {
                                    <tr>
                                        <td>@alumno.AspiranteID</td>
                                        <td><strong>@alumno.NombreCompleto</strong></td>
                                        <td>@alumno.Email</td>
                                        <td>@alumno.CarreraNombre</td>
                                        <td>@alumno.Especialidad</td>
                                        <td class="text-center">
                                            <a href="@Url.Action("TareaAlumno", new { username = alumno.Username })" 
                                               class="btn btn-sm btn-info" 
                                               title="Ver Tareas">
                                                <i class="fas fa-folder-open"></i> Tareas
                                            </a>
                                        </td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>

                    <!-- Pagination -->
                    <div class="row mt-3">
                        <div class="col-md-4">
                            <p>
                                Mostrando 
                                <strong>@((Model.PaginaActual - 1) * Model.ElementosPorPagina + 1)</strong>
                                - 
                                <strong>@Math.Min(Model.PaginaActual * Model.ElementosPorPagina, Model.TotalAlumnos)</strong>
                                de 
                                <strong>@Model.TotalAlumnos</strong>
                            </p>
                        </div>
                        <div class="col-md-8">
                            <nav>
                                <ul class="pagination justify-content-end">
                                    @if (Model.PaginaActual > 1)
                                    {
                                        <li class="page-item">
                                            <a class="page-link" href="@Url.Action("Index", new { page = 1, pageSize = Model.ElementosPorPagina })">
                                                Primera
                                            </a>
                                        </li>
                                        <li class="page-item">
                                            <a class="page-link" href="@Url.Action("Index", new { page = Model.PaginaActual - 1, pageSize = Model.ElementosPorPagina })">
                                                Anterior
                                            </a>
                                        </li>
                                    }

                                    @for (int i = Math.Max(1, Model.PaginaActual - 2); i <= Math.Min(Model.TotalPaginas, Model.PaginaActual + 2); i++)
                                    {
                                        <li class="page-item @(i == Model.PaginaActual ? "active" : "")">
                                            <a class="page-link" href="@Url.Action("Index", new { page = i, pageSize = Model.ElementosPorPagina })">
                                                @i
                                            </a>
                                        </li>
                                    }

                                    @if (Model.PaginaActual < Model.TotalPaginas)
                                    {
                                        <li class="page-item">
                                            <a class="page-link" href="@Url.Action("Index", new { page = Model.PaginaActual + 1, pageSize = Model.ElementosPorPagina })">
                                                Siguiente
                                            </a>
                                        </li>
                                        <li class="page-item">
                                            <a class="page-link" href="@Url.Action("Index", new { page = Model.TotalPaginas, pageSize = Model.ElementosPorPagina })">
                                                Última
                                            </a>
                                        </li>
                                    }
                                </ul>
                            </nav>
                        </div>
                    </div>
                }
                else
                {
                    <div class="alert alert-info" role="alert">
                        <i class="fas fa-info-circle"></i> No hay alumnos registrados actualmente.
                    </div>
                }
            </div>
        </div>
    </div>
</div>
```

---

## **5. Legacy Bug Fixes**

### **5.1 Bug #1: Double Email Check (Dummy @example.com Email)**

**Root Cause:** In the legacy Strapi system, students were created with dummy emails like `alumno123@example.com` because their actual emails weren't collected during initial registration.

**Solution Implemented:**

1. **Detection in Login Controller** (`AccountController.cs`, lines 60-65):
```csharp
// After successful login for Aspirante role
if (user.Email.Contains("@example.com"))
{
    return RedirectToAction("EditarEmail", "Aspirante");
}
```

2. **Email Update Form** (`Views/Aspirante/EditarEmail.cshtml`):
```html
@model InduccionMigration.ViewModels.EditarEmailViewModel

@{
    ViewBag.Title = "Actualizar Correo Electrónico";
}

<div class="row mt-4">
    <div class="col-md-6 offset-md-3">
        <div class="card">
            <div class="card-header bg-warning text-dark">
                <h4 class="mb-0">
                    <i class="fas fa-envelope"></i> Actualizar Correo Electrónico
                </h4>
            </div>
            <div class="card-body">
                <div class="alert alert-warning" role="alert">
                    <strong>Acción Requerida:</strong> Tu correo actual es temporal (@example.com). 
                    Por favor, actualízalo con tu correo institucional o personal válido.
                </div>

                @using (Html.BeginForm("EditarEmail", "Aspirante", FormMethod.Post, new { @class = "mt-3" }))
                {
                    @Html.AntiForgeryToken()
                    @Html.ValidationSummary(true, "", new { @class = "text-danger" })

                    <div class="form-group">
                        <label>Correo Actual:</label>
                        <input type="text" class="form-control" value="@Model.EmailActual" disabled />
                    </div>

                    <div class="form-group">
                        @Html.LabelFor(m => m.NuevoEmail, "Nuevo Correo Electrónico:", new { @class = "control-label" })
                        @Html.TextBoxFor(m => m.NuevoEmail, new { @class = "form-control", placeholder = "ejemplo@universidad.edu.mx" })
                        @Html.ValidationMessageFor(m => m.NuevoEmail, "", new { @class = "text-danger" })
                    </div>

                    <button type="submit" class="btn btn-primary btn-block">
                        <i class="fas fa-save"></i> Actualizar Correo
                    </button>
                }
            </div>
        </div>
    </div>
</div>
```

3. **ViewModel** (`ViewModels/EditarEmailViewModel.cs`):
```csharp
using System.ComponentModel.DataAnnotations;

namespace InduccionMigration.ViewModels
{
    public class EditarEmailViewModel
    {
        public string EmailActual { get; set; }

        [Required(ErrorMessage = "El nuevo correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Nuevo Correo Electrónico")]
        public string NuevoEmail { get; set; }
    }
}
```

**Result:** Students with dummy emails are automatically redirected to update their email before accessing course materials, preventing authentication issues.

---

### **5.2 Bug #2: Missing Student List in Coordinator Dashboard**

**Root Cause:** The legacy Strapi endpoint `/api/usuarios?pagination[page]=1&pagination[pageSize]=25` was not properly populating the student list due to:
1. Missing JOIN between `usuarios` collection and `users-permissions.user`
2. No filtering for active students
3. Inconsistent pagination state management in Vue

**Solution Implemented:**

1. **Proper Entity Framework Query** (`CoordinadorController.cs`, lines 18-40):
```csharp
// Get total count for pagination
int totalStudents = db.Aspirantes.Count(a => a.Usuario.Activo);

// Fetch paginated student list with proper JOIN
var alumnos = db.Aspirantes
    .Include("Usuario")          // JOIN with Usuarios table
    .Include("Carrera")          // JOIN with Carreras table
    .Where(a => a.Usuario.Activo)  // Filter only active users
    .OrderBy(a => a.Usuario.NombreCompleto)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(a => new AspiranteListViewModel
    {
        AspiranteID = a.AspiranteID,
        NombreCompleto = a.Usuario.NombreCompleto,
        Email = a.Usuario.Email,
        CarreraNombre = a.Carrera.Nombre,
        Especialidad = a.Especialidad,
        Username = a.Usuario.Email
    })
    .ToList();
```

2. **Verified SQL JOIN** (as per `analisis_captaciondb.md`):
```sql
SELECT 
    a.AspiranteID,
    u.NombreCompleto,
    u.Email,
    c.Nombre AS CarreraNombre,
    a.Especialidad
FROM dbo.Aspirantes a
INNER JOIN dbo.Usuarios u ON a.UsuarioID = u.UsuarioID
INNER JOIN dbo.Carreras c ON a.CarreraID = c.CarreraID
WHERE u.Activo = 1 AND u.RolID = 2
ORDER BY u.NombreCompleto;
```

3. **Bootstrap Pagination** (server-side, no JavaScript state):
- See `Views/Coordinador/Index.cshtml` lines 70-110 for pagination implementation
- Query string parameters (`?page=2&pageSize=25`) maintain state
- Total pages calculated: `Math.Ceiling((double)totalStudents / pageSize)`

**Result:** Coordinator dashboard now displays complete, paginated student roster with proper navigation.

---

## **6. Authentication & Authorization**

### **6.1 Web.config Authentication Setup**

```xml
<system.web>
    <compilation debug="true" targetFramework="4.7.2" />
    <httpRuntime targetFramework="4.7.2" />
    
    <authentication mode="Forms">
        <forms loginUrl="~/Account/Login" 
               timeout="480" 
               slidingExpiration="true" 
               cookieless="UseCookies" />
    </authentication>
    
    <authorization>
        <deny users="?" />  <!-- Deny anonymous users globally -->
    </authorization>
    
    <sessionState mode="InProc" timeout="480" />
</system.web>
```

### **6.2 Role-Based Access Summary**

| **Role** | **RolID** | **Controller** | **Access Level** |
|----------|----------|----------------|------------------|
| Administrador | 1 | `AdminController` | Full CRUD on `Ind_Materias`, `Ind_Unidades`, user management |
| Aspirante | 2 | `AspiranteController` | View assigned courses, submit assignments, update email |
| Coordinador | 3 | `CoordinadorController` | View all students, grade assignments, manage courses |
| Captador | 4 | `CaptadorController` | View student progress reports, export data |

### **6.3 Password Security**

**Install BCrypt.Net-Next NuGet Package:**
```powershell
Install-Package BCrypt.Net-Next
```

**Hashing passwords during user creation:**
```csharp
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
```

**Verification in login:**
```csharp
bool isValid = BCrypt.Net.BCrypt.Verify(plainTextPassword, storedHashedPassword);
```

---

## **7. Deployment Checklist**

### **7.1 Pre-Deployment Tasks**

- [ ] **Database Migration:**
  - Execute SQL scripts from `analisis_captaciondb.md` to create `Ind_*` tables
  - Verify Foreign Key constraints are in place
  - Run test queries to ensure data integrity

- [ ] **Connection String Update:**
  - Configure production SQL Server connection in `Web.config`
  - Enable SSL for database connections if required

- [ ] **Migrate Legacy Data:**
  - Export Strapi MongoDB/SQLite data
  - Transform JSON `unidades` field into relational `Ind_Unidades` records
  - Transform `tareas.entregados` JSON into `Ind_ProgresoAspirantes` rows
  - Migrate user authentication from Strapi JWT to ASP.NET Forms Authentication (rehash passwords with BCrypt)

- [ ] **File Uploads Migration:**
  - Copy existing assignment files from Strapi `uploads/` folder to ASP.NET `~/App_Data/Uploads/`
  - Update file paths in database if necessary

- [ ] **Testing:**
  - Test all 4 role workflows (Admin, Aspirante, Coordinador, Captador)
  - Verify bug fixes (email update, student roster pagination)
  - Load test with 500+ students to validate performance

### **7.2 IIS Deployment Configuration**

**Application Pool Settings:**
- .NET CLR Version: v4.0
- Managed Pipeline Mode: Integrated
- Identity: ApplicationPoolIdentity (or custom service account with DB access)

**MIME Types for File Downloads:**
```xml
<system.webServer>
    <staticContent>
        <mimeMap fileExtension=".pdf" mimeType="application/pdf" />
        <mimeMap fileExtension=".mp4" mimeType="video/mp4" />
    </staticContent>
</system.webServer>
```

### **7.3 Post-Deployment Validation**

- [ ] Verify all controllers are accessible through proper role authorization
- [ ] Test file upload/download functionality
- [ ] Confirm email updates are persisted correctly
- [ ] Validate pagination with large student datasets
- [ ] Check HTTPS enforcement and SSL certificate

---

## **Appendix A: Required ViewModels**

### **MateriaViewModel.cs**
```csharp
namespace InduccionMigration.ViewModels
{
    public class MateriaViewModel
    {
        public int MateriaID { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string CarreraNombre { get; set; }
        public string PeriodoNombre { get; set; }
    }
}
```

### **MateriaDetalleViewModel.cs**
```csharp
using System.Collections.Generic;

namespace InduccionMigration.ViewModels
{
    public class MateriaDetalleViewModel
    {
        public int MateriaID { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public List<UnidadViewModel> Unidades { get; set; }
    }

    public class UnidadViewModel
    {
        public int UnidadID { get; set; }
        public string Nombre { get; set; }
        public int Orden { get; set; }
        public List<MaterialViewModel> Materiales { get; set; }
        public Ind_ProgresoAspirante Progreso { get; set; }
    }

    public class MaterialViewModel
    {
        public int MaterialID { get; set; }
        public string Nombre { get; set; }
        public string TipoRecurso { get; set; }
        public string RutaURL { get; set; }
    }
}
```

### **CoordinadorDashboardViewModel.cs**
```csharp
using System.Collections.Generic;

namespace InduccionMigration.ViewModels
{
    public class CoordinadorDashboardViewModel
    {
        public List<AspiranteListViewModel> Alumnos { get; set; }
        public int TotalAlumnos { get; set; }
        public int PaginaActual { get; set; }
        public int ElementosPorPagina { get; set; }
        public int TotalPaginas { get; set; }
    }

    public class AspiranteListViewModel
    {
        public int AspiranteID { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string CarreraNombre { get; set; }
        public string Especialidad { get; set; }
        public string Username { get; set; }
    }
}
```

### **TareaAlumnoViewModel.cs**
```csharp
using System;
using System.Collections.Generic;

namespace InduccionMigration.ViewModels
{
    public class TareaAlumnoViewModel
    {
        public int AspiranteID { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string CarreraNombre { get; set; }
        public List<ProgresoViewModel> Progreso { get; set; }
    }

    public class ProgresoViewModel
    {
        public int ProgresoID { get; set; }
        public string MateriaNombre { get; set; }
        public string UnidadNombre { get; set; }
        public string Estado { get; set; }
        public decimal? Calificacion { get; set; }
        public DateTime FechaAsignacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public string CalificadorNombre { get; set; }
        public string Comentarios { get; set; }
    }
}
```

---

## **Appendix B: NuGet Packages Required**

Execute in Package Manager Console:

```powershell
Install-Package EntityFramework -Version 6.4.4
Install-Package BCrypt.Net-Next -Version 4.0.3
Install-Package Microsoft.AspNet.Mvc -Version 5.2.9
Install-Package Microsoft.AspNet.Web.Optimization -Version 1.1.3
Install-Package Microsoft.AspNet.Razor -Version 3.2.9
Install-Package Bootstrap -Version 4.6.2
Install-Package jQuery -Version 3.6.0
```

---

## **Appendix C: SQL Migration Script for Legacy Data**

**Transform Strapi JSON `unidades` into relational structure:**

```sql
-- Assuming you have exported Strapi data to a temp table: #StrapiMaterias
-- This script transforms JSON "unidades" into normalized rows

DECLARE @MateriaID INT, @UnidadesJSON NVARCHAR(MAX), @UnidadNombre NVARCHAR(255), @Orden INT;

DECLARE materia_cursor CURSOR FOR
SELECT MateriaID, UnidadesJSON FROM #StrapiMaterias;

OPEN materia_cursor;
FETCH NEXT FROM materia_cursor INTO @MateriaID, @UnidadesJSON;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Parse JSON array and insert into Ind_Unidades
    -- (You would use OPENJSON in SQL Server 2016+)
    
    INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden)
    SELECT 
        @MateriaID,
        JSON_VALUE(value, '$.nombre'),
        JSON_VALUE(value, '$.orden')
    FROM OPENJSON(@UnidadesJSON);
    
    FETCH NEXT FROM materia_cursor INTO @MateriaID, @UnidadesJSON;
END;

CLOSE materia_cursor;
DEALLOCATE materia_cursor;
```

**Transform Strapi `tareas.entregados` JSON into `Ind_ProgresoAspirantes`:**

```sql
-- Similar approach: parse JSON and insert into relational structure
INSERT INTO dbo.Ind_ProgresoAspirantes (AspiranteID, UnidadID, Estado, FechaEnvio)
SELECT 
    a.AspiranteID,
    u.UnidadID,
    JSON_VALUE(entregado.value, '$.estado'),
    TRY_CAST(JSON_VALUE(entregado.value, '$.fechaEnvio') AS DATETIME)
FROM #StrapiTareas t
CROSS APPLY OPENJSON(t.EntregadosJSON) AS entregado
INNER JOIN dbo.Aspirantes a ON a.UsuarioID = t.UserID
INNER JOIN dbo.Ind_Unidades u ON u.UnidadID = JSON_VALUE(entregado.value, '$.unidadId');
```

---

## **Conclusion**

This migration guide provides a complete roadmap for transforming your legacy Quasar/Strapi Induction platform into a unified ASP.NET MVC 5 application. The new architecture:

✅ **Eliminates dual-codebase complexity** (Quasar + Strapi → Single C# MVC app)  
✅ **Leverages existing CaptacionDB** with proper relational integrity  
✅ **Fixes critical legacy bugs** (email validation, student roster)  
✅ **Implements robust role-based security** using ASP.NET Forms Authentication  
✅ **Provides server-side rendering** for improved performance and SEO  
✅ **Uses Entity Framework Database-First** for type-safe data access  

**Next Steps:**
1. Create the project structure in `C:\dev\induccion_migration`
2. Run Entity Framework Database-First wizard
3. Implement controllers following the code samples above
4. Create Razor views using Bootstrap
5. Test with sample data before production migration
6. Execute SQL migration scripts to transfer legacy Strapi data

For any specific implementation questions or custom requirements, reference the exact file paths and code snippets provided throughout this guide.
