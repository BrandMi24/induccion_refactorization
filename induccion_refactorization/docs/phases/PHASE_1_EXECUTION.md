# **Phase 1 Execution Guide**
## **Foundation Setup: Project Creation & Pastel Green Theme**

---

## **Step 1: Create ASP.NET MVC 5 Project in Visual Studio**

### **Option A: Using Visual Studio 2019/2022**

1. **Launch Visual Studio**
2. Click **"Create a new project"**
3. Search for **"ASP.NET Web Application (.NET Framework)"**
4. Click **Next**
5. Configure Project:
   - **Project name:** `InduccionMigration`
   - **Location:** `C:\dev\induccion_migration`
   - **Framework:** `.NET Framework 4.7.2`
   - Click **Create**
6. Select Template:
   - Choose **MVC**
   - Authentication: **No Authentication** (we'll implement custom auth)
   - Click **Create**

### **Option B: Using Command Line (if VS not available)**

Run in PowerShell from `C:\dev`:

```powershell
# Create project directory
New-Item -ItemType Directory -Path "C:\dev\induccion_migration" -Force
cd C:\dev\induccion_migration

# Note: Full project scaffolding requires Visual Studio
# If you must use CLI, use dotnet CLI with .NET Core instead
# For .NET Framework 4.7.2 MVC 5, Visual Studio is recommended
```

---

## **Step 2: Install NuGet Packages**

Open **Package Manager Console** in Visual Studio:  
`Tools` → `NuGet Package Manager` → `Package Manager Console`

Run these commands **one at a time**:

```powershell
# Entity Framework 6.x for Database-First
Install-Package EntityFramework -Version 6.4.4

# BCrypt for password hashing
Install-Package BCrypt.Net-Next -Version 4.0.3

# ASP.NET MVC (should already be installed, but verify version)
Install-Package Microsoft.AspNet.Mvc -Version 5.2.9

# Web Optimization (bundling & minification)
Install-Package Microsoft.AspNet.Web.Optimization -Version 1.1.3

# Bootstrap 4.6.2
Install-Package Bootstrap -Version 4.6.2

# jQuery 3.6.0
Install-Package jQuery -Version 3.6.0

# Font Awesome for icons
Install-Package FontAwesome -Version 6.4.0
```

**Wait for all packages to install successfully before proceeding.**

---

## **Step 3: Create Folder Structure**

Run in **Package Manager Console**:

```powershell
# Create ViewModels folder
New-Item -ItemType Directory -Path "ViewModels" -Force

# Create Filters folder
New-Item -ItemType Directory -Path "Filters" -Force

# Create Services folder
New-Item -ItemType Directory -Path "Services" -Force

# Create Helpers folder
New-Item -ItemType Directory -Path "Helpers" -Force

# Create App_Data/Uploads folder for file uploads
New-Item -ItemType Directory -Path "App_Data\Uploads" -Force
```

Your Solution Explorer should now show:
```
InduccionMigration
├── App_Data/
│   └── Uploads/
├── App_Start/
├── Content/
├── Controllers/
├── Filters/          ← NEW
├── Helpers/          ← NEW
├── Models/
├── Scripts/
├── Services/         ← NEW
├── ViewModels/       ← NEW
└── Views/
```

---

## **Step 4: Configure Database Connection String**

Open `Web.config` in the root of your project and add this connection string inside `<configuration>`:

```xml
<connectionStrings>
  <add name="CaptacionDbContext" 
       connectionString="Data Source=YOUR_SQL_SERVER_NAME;Initial Catalog=CaptacionDB;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

**⚠️ IMPORTANT:** Replace `YOUR_SQL_SERVER_NAME` with your actual SQL Server instance name.

**Common SQL Server Names:**
- Local SQL Server Express: `localhost\SQLEXPRESS` or `.\SQLEXPRESS`
- Local SQL Server: `localhost` or `.`
- Network SQL Server: `192.168.1.100` or `SERVERNAME\INSTANCENAME`

**Test Connection:** You can test this connection string later when we add Entity Framework models in Phase 2.

---

## **Step 5: Implement Custom Pastel Green Theme**

### **5.1 Create Custom CSS File**

1. Right-click `Content` folder → **Add** → **New Item**
2. Select **Style Sheet**
3. Name it **`InduccionTheme.css`**
4. Replace ALL content with the following:

```css
/* ========================================
   INDUCCION PLATFORM - CUSTOM THEME
   Pastel Green Primary + White Background
   Modern, Clean, Mobile-First Design
   ======================================== */

/* === ROOT VARIABLES === */
:root {
    /* Pastel Green Palette */
    --primary-color: #A8D5BA;           /* Soft Pastel Green */
    --primary-dark: #7FB89A;            /* Darker Green for hover */
    --primary-light: #C8E6D4;           /* Lighter Green for backgrounds */
    --primary-gradient: linear-gradient(135deg, #A8D5BA 0%, #7FB89A 100%);
    
    /* Accent Colors */
    --accent-mint: #B5EAD7;             /* Mint accent */
    --accent-sage: #C9CBA3;             /* Sage accent */
    
    /* Neutrals */
    --white: #FFFFFF;
    --off-white: #F9FAFB;
    --gray-50: #F8F9FA;
    --gray-100: #F1F3F5;
    --gray-200: #E9ECEF;
    --gray-300: #DEE2E6;
    --gray-400: #CED4DA;
    --gray-500: #ADB5BD;
    --gray-600: #6C757D;
    --gray-700: #495057;
    --gray-800: #343A40;
    --gray-900: #212529;
    
    /* Semantic Colors */
    --success: #8BC34A;
    --info: #4FC3F7;
    --warning: #FFC107;
    --danger: #EF5350;
    
    /* Typography */
    --font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    --font-size-base: 16px;
    
    /* Spacing */
    --border-radius: 12px;
    --border-radius-sm: 8px;
    --shadow-sm: 0 2px 4px rgba(0,0,0,0.05);
    --shadow-md: 0 4px 12px rgba(0,0,0,0.08);
    --shadow-lg: 0 8px 24px rgba(0,0,0,0.12);
}

/* === GLOBAL STYLES === */
* {
    box-sizing: border-box;
}

body {
    font-family: var(--font-family);
    font-size: var(--font-size-base);
    background-color: var(--white);
    color: var(--gray-800);
    line-height: 1.6;
    margin: 0;
    padding: 0;
}

/* === NAVBAR CUSTOMIZATION === */
.navbar {
    background: var(--primary-gradient) !important;
    box-shadow: var(--shadow-md);
    border: none;
    padding: 1rem 0;
    border-radius: 0;
}

.navbar-brand {
    color: var(--white) !important;
    font-weight: 700;
    font-size: 1.5rem;
    letter-spacing: 0.5px;
    transition: all 0.3s ease;
}

.navbar-brand:hover {
    color: var(--off-white) !important;
    transform: translateY(-2px);
}

.navbar-brand i {
    margin-right: 8px;
    font-size: 1.75rem;
    vertical-align: middle;
}

.navbar-nav .nav-link {
    color: var(--white) !important;
    font-weight: 500;
    padding: 0.5rem 1rem;
    transition: all 0.3s ease;
    border-radius: var(--border-radius-sm);
}

.navbar-nav .nav-link:hover {
    background-color: rgba(255, 255, 255, 0.15);
    transform: translateY(-2px);
}

.navbar-toggler {
    border-color: rgba(255, 255, 255, 0.3);
}

.navbar-toggler-icon {
    background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 30 30'%3e%3cpath stroke='rgba(255, 255, 255, 1)' stroke-linecap='round' stroke-miterlimit='10' stroke-width='2' d='M4 7h22M4 15h22M4 23h22'/%3e%3c/svg%3e");
}

/* Dropdown Menu */
.dropdown-menu {
    border: none;
    box-shadow: var(--shadow-lg);
    border-radius: var(--border-radius);
    overflow: hidden;
    margin-top: 0.5rem;
}

.dropdown-item {
    padding: 0.75rem 1.5rem;
    transition: all 0.2s ease;
    color: var(--gray-700);
}

.dropdown-item:hover {
    background-color: var(--primary-light);
    color: var(--gray-900);
}

.dropdown-item i {
    margin-right: 8px;
    width: 20px;
    text-align: center;
}

.dropdown-divider {
    margin: 0;
}

.dropdown-item-text {
    padding: 0.75rem 1.5rem;
}

/* === BUTTONS === */
.btn {
    border-radius: var(--border-radius);
    font-weight: 600;
    padding: 0.625rem 1.5rem;
    transition: all 0.3s ease;
    border: none;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    font-size: 0.875rem;
}

.btn-primary {
    background: var(--primary-gradient);
    color: var(--white);
    box-shadow: var(--shadow-sm);
}

.btn-primary:hover {
    background: linear-gradient(135deg, #7FB89A 0%, #6AA083 100%);
    box-shadow: var(--shadow-md);
    transform: translateY(-2px);
}

.btn-secondary {
    background-color: var(--gray-500);
    color: var(--white);
}

.btn-secondary:hover {
    background-color: var(--gray-600);
    transform: translateY(-2px);
}

.btn-success {
    background-color: var(--success);
    color: var(--white);
}

.btn-info {
    background-color: var(--info);
    color: var(--white);
}

.btn-warning {
    background-color: var(--warning);
    color: var(--gray-900);
}

.btn-danger {
    background-color: var(--danger);
    color: var(--white);
}

.btn-sm {
    padding: 0.375rem 1rem;
    font-size: 0.8rem;
}

.btn-lg {
    padding: 0.875rem 2rem;
    font-size: 1rem;
}

.btn-block {
    width: 100%;
}

/* === CARDS === */
.card {
    border: none;
    border-radius: var(--border-radius);
    box-shadow: var(--shadow-md);
    overflow: hidden;
    margin-bottom: 1.5rem;
    background-color: var(--white);
    transition: all 0.3s ease;
}

.card:hover {
    box-shadow: var(--shadow-lg);
    transform: translateY(-4px);
}

.card-header {
    background: var(--primary-gradient);
    color: var(--white);
    border-bottom: none;
    padding: 1.25rem 1.5rem;
    font-weight: 700;
    font-size: 1.125rem;
}

.card-header.bg-success {
    background: linear-gradient(135deg, #8BC34A 0%, #7CB342 100%) !important;
}

.card-header.bg-warning {
    background: linear-gradient(135deg, #FFC107 0%, #FFB300 100%) !important;
    color: var(--gray-900) !important;
}

.card-header.bg-danger {
    background: linear-gradient(135deg, #EF5350 0%, #E53935 100%) !important;
}

.card-body {
    padding: 1.5rem;
}

.card-footer {
    background-color: var(--gray-50);
    border-top: 1px solid var(--gray-200);
    padding: 1rem 1.5rem;
}

/* === ALERTS === */
.alert {
    border-radius: var(--border-radius);
    border: none;
    padding: 1rem 1.25rem;
    margin-bottom: 1.5rem;
    box-shadow: var(--shadow-sm);
}

.alert-success {
    background-color: #E8F5E9;
    color: #2E7D32;
    border-left: 4px solid var(--success);
}

.alert-info {
    background-color: #E1F5FE;
    color: #01579B;
    border-left: 4px solid var(--info);
}

.alert-warning {
    background-color: #FFF8E1;
    color: #F57F17;
    border-left: 4px solid var(--warning);
}

.alert-danger {
    background-color: #FFEBEE;
    color: #C62828;
    border-left: 4px solid var(--danger);
}

.alert i {
    margin-right: 8px;
}

/* === TABLES === */
.table {
    background-color: var(--white);
    border-radius: var(--border-radius);
    overflow: hidden;
}

.table thead {
    background-color: var(--gray-800);
    color: var(--white);
}

.table thead th {
    font-weight: 600;
    text-transform: uppercase;
    font-size: 0.875rem;
    letter-spacing: 0.5px;
    border: none;
    padding: 1rem;
}

.table tbody tr {
    transition: all 0.2s ease;
}

.table-hover tbody tr:hover {
    background-color: var(--primary-light);
    cursor: pointer;
}

.table tbody td {
    padding: 1rem;
    vertical-align: middle;
    border-top: 1px solid var(--gray-200);
}

.table-striped tbody tr:nth-of-type(odd) {
    background-color: var(--gray-50);
}

/* === FORMS === */
.form-control {
    border-radius: var(--border-radius-sm);
    border: 1px solid var(--gray-300);
    padding: 0.75rem 1rem;
    font-size: 1rem;
    transition: all 0.3s ease;
}

.form-control:focus {
    border-color: var(--primary-color);
    box-shadow: 0 0 0 0.2rem rgba(168, 213, 186, 0.25);
    outline: none;
}

.form-group {
    margin-bottom: 1.5rem;
}

.form-group label {
    font-weight: 600;
    color: var(--gray-700);
    margin-bottom: 0.5rem;
    display: block;
}

.form-control::placeholder {
    color: var(--gray-400);
}

/* === PAGINATION === */
.pagination {
    margin-top: 1.5rem;
}

.page-item .page-link {
    color: var(--primary-dark);
    border: 1px solid var(--gray-300);
    border-radius: var(--border-radius-sm);
    margin: 0 0.25rem;
    padding: 0.5rem 1rem;
    transition: all 0.3s ease;
}

.page-item.active .page-link {
    background: var(--primary-gradient);
    border-color: var(--primary-color);
    color: var(--white);
}

.page-item .page-link:hover {
    background-color: var(--primary-light);
    border-color: var(--primary-color);
    color: var(--gray-900);
}

.page-item.disabled .page-link {
    background-color: var(--gray-100);
    border-color: var(--gray-200);
    color: var(--gray-400);
}

/* === BREADCRUMB === */
.breadcrumb {
    background-color: var(--gray-50);
    border-radius: var(--border-radius-sm);
    padding: 0.75rem 1rem;
    margin-bottom: 1.5rem;
}

.breadcrumb-item a {
    color: var(--primary-dark);
    text-decoration: none;
    font-weight: 500;
}

.breadcrumb-item a:hover {
    color: var(--primary-color);
    text-decoration: underline;
}

.breadcrumb-item.active {
    color: var(--gray-600);
}

/* === LAYOUT === */
.container.body-content {
    padding-top: 2rem;
    padding-bottom: 2rem;
    min-height: calc(100vh - 200px);
}

footer {
    background-color: var(--gray-800);
    color: var(--gray-300);
    padding: 2rem 0;
    margin-top: 3rem;
    text-align: center;
}

footer p {
    margin: 0;
}

/* === UTILITY CLASSES === */
.text-primary {
    color: var(--primary-color) !important;
}

.bg-primary {
    background-color: var(--primary-color) !important;
}

.mt-4 {
    margin-top: 1.5rem !important;
}

.mb-3 {
    margin-bottom: 1rem !important;
}

.text-center {
    text-align: center !important;
}

/* === RESPONSIVE DESIGN === */
@media (max-width: 768px) {
    .navbar-brand {
        font-size: 1.25rem;
    }
    
    .card {
        margin-bottom: 1rem;
    }
    
    .card-body {
        padding: 1rem;
    }
    
    .table-responsive {
        overflow-x: auto;
        -webkit-overflow-scrolling: touch;
    }
    
    .btn {
        padding: 0.5rem 1rem;
        font-size: 0.875rem;
    }
}

@media (max-width: 576px) {
    .container.body-content {
        padding-left: 15px;
        padding-right: 15px;
    }
    
    .card-header {
        font-size: 1rem;
    }
    
    h1 { font-size: 1.75rem; }
    h2 { font-size: 1.5rem; }
    h3 { font-size: 1.25rem; }
}

/* === LOADING SPINNER === */
.spinner-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 200px;
}

.spinner-border {
    color: var(--primary-color);
}

/* === CUSTOM ICONS === */
i.fas, i.far, i.fab {
    margin-right: 0.5rem;
}

/* === ACCESSIBILITY === */
:focus {
    outline: 2px solid var(--primary-color);
    outline-offset: 2px;
}

button:focus,
a:focus {
    outline: 2px solid var(--primary-color);
    outline-offset: 2px;
}
```

### **5.2 Update BundleConfig.cs**

Open `App_Start/BundleConfig.cs` and modify the `RegisterBundles` method to include your custom theme:

Find the CSS bundle section and replace it with:

```csharp
bundles.Add(new StyleBundle("~/Content/css").Include(
    "~/Content/bootstrap.min.css",
    "~/Content/InduccionTheme.css",  // Add this line
    "~/Content/Site.css"));
```

### **5.3 Create Base _Layout.cshtml**

Navigate to `Views/Shared/_Layout.cshtml` and replace ALL content with:

```html
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <title>@ViewBag.Title - Sistema de Inducción</title>
    
    @Styles.Render("~/Content/css")
    @Scripts.Render("~/bundles/modernizr")
    
    <!-- Font Awesome -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
</head>
<body>
    <!-- Navigation Bar -->
    <nav class="navbar navbar-expand-lg navbar-dark">
        <div class="container">
            <a class="navbar-brand" href="@Url.Action("Index", "Home")">
                <i class="fas fa-graduation-cap"></i>
                Sistema de Inducción
            </a>
            
            <button class="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarNav" 
                    aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            
            <div class="collapse navbar-collapse" id="navbarNav">
                @Html.Partial("_LoginPartial")
            </div>
        </div>
    </nav>

    <!-- Main Content Area -->
    <div class="container body-content">
        <!-- Success Alert -->
        @if (TempData["Success"] != null)
        {
            <div class="alert alert-success alert-dismissible fade show mt-3" role="alert">
                <i class="fas fa-check-circle"></i>
                <strong>¡Éxito!</strong> @TempData["Success"]
                <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
        }

        <!-- Error Alert -->
        @if (TempData["Error"] != null)
        {
            <div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
                <i class="fas fa-exclamation-triangle"></i>
                <strong>Error:</strong> @TempData["Error"]
                <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
        }

        <!-- Info Alert -->
        @if (TempData["Info"] != null)
        {
            <div class="alert alert-info alert-dismissible fade show mt-3" role="alert">
                <i class="fas fa-info-circle"></i>
                @TempData["Info"]
                <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
        }

        <!-- Page Content -->
        @RenderBody()
        
        <hr style="margin-top: 3rem; border-color: var(--gray-200);" />
    </div>

    <!-- Footer -->
    <footer>
        <div class="container">
            <p>&copy; @DateTime.Now.Year - Universidad Tecnológica - Sistema de Inducción</p>
            <small class="text-muted">Desarrollado con ASP.NET MVC 5</small>
        </div>
    </footer>

    <!-- Scripts -->
    @Scripts.Render("~/bundles/jquery")
    @Scripts.Render("~/bundles/bootstrap")
    @RenderSection("scripts", required: false)
</body>
</html>
```

### **5.4 Create _LoginPartial.cshtml**

Create a new file: `Views/Shared/_LoginPartial.cshtml`

```html
@* This partial will be updated in Phase 3 with actual session data *@
@* For now, it's a placeholder *@

@if (Request.IsAuthenticated)
{
    <ul class="navbar-nav ml-auto">
        <li class="nav-item dropdown">
            <a class="nav-link dropdown-toggle" href="#" id="userDropdown" role="button" 
               data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                <i class="fas fa-user-circle"></i>
                Usuario Demo
            </a>
            <div class="dropdown-menu dropdown-menu-right" aria-labelledby="userDropdown">
                <div class="dropdown-item-text">
                    <small class="text-muted">usuario@ejemplo.com</small>
                </div>
                <div class="dropdown-divider"></div>
                <a class="dropdown-item" href="#">
                    <i class="fas fa-tachometer-alt"></i> Mi Panel
                </a>
                <div class="dropdown-divider"></div>
                <a href="#" class="dropdown-item text-danger">
                    <i class="fas fa-sign-out-alt"></i> Cerrar Sesión
                </a>
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

---

## **Step 6: Update HomeController (Test View)**

Open `Controllers/HomeController.cs` and ensure the `Index` action exists:

```csharp
using System.Web.Mvc;

namespace InduccionMigration.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Inicio";
            return View();
        }
    }
}
```

Now update `Views/Home/Index.cshtml` to test the theme:

```html
@{
    ViewBag.Title = "Inicio";
}

<div class="row mt-4">
    <div class="col-md-12 text-center">
        <h1 style="color: var(--primary-dark); margin-bottom: 2rem;">
            <i class="fas fa-rocket"></i> ¡Bienvenido al Sistema de Inducción!
        </h1>
        <p class="lead" style="color: var(--gray-600); margin-bottom: 3rem;">
            Plataforma unificada de gestión de cursos de inducción para aspirantes
        </p>
    </div>
</div>

<div class="row">
    <!-- Card 1: Students -->
    <div class="col-md-4">
        <div class="card">
            <div class="card-header">
                <i class="fas fa-user-graduate"></i> Para Aspirantes
            </div>
            <div class="card-body">
                <p>Accede a tus cursos, visualiza materiales y entrega tus tareas de inducción.</p>
                <a href="#" class="btn btn-primary btn-block">
                    <i class="fas fa-arrow-right"></i> Ir a Mis Cursos
                </a>
            </div>
        </div>
    </div>

    <!-- Card 2: Coordinators -->
    <div class="col-md-4">
        <div class="card">
            <div class="card-header bg-success">
                <i class="fas fa-users-cog"></i> Para Coordinadores
            </div>
            <div class="card-body">
                <p>Gestiona estudiantes, califica tareas y supervisa el progreso de inducción.</p>
                <a href="#" class="btn btn-success btn-block">
                    <i class="fas fa-arrow-right"></i> Panel de Coordinación
                </a>
            </div>
        </div>
    </div>

    <!-- Card 3: Admins -->
    <div class="col-md-4">
        <div class="card">
            <div class="card-header" style="background: linear-gradient(135deg, #6C757D 0%, #495057 100%);">
                <i class="fas fa-cog"></i> Para Administradores
            </div>
            <div class="card-body">
                <p>Administra materias, unidades, usuarios y configuraciones del sistema.</p>
                <a href="#" class="btn btn-secondary btn-block">
                    <i class="fas fa-arrow-right"></i> Administración
                </a>
            </div>
        </div>
    </div>
</div>

<!-- Features Section -->
<div class="row mt-5">
    <div class="col-md-12">
        <div class="card">
            <div class="card-header">
                <i class="fas fa-star"></i> Características del Sistema
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-6">
                        <ul class="list-group list-group-flush">
                            <li class="list-group-item">
                                <i class="fas fa-check text-success"></i> Gestión completa de cursos de inducción
                            </li>
                            <li class="list-group-item">
                                <i class="fas fa-check text-success"></i> Sistema de entrega de tareas y calificación
                            </li>
                            <li class="list-group-item">
                                <i class="fas fa-check text-success"></i> Dashboard personalizado por rol
                            </li>
                        </ul>
                    </div>
                    <div class="col-md-6">
                        <ul class="list-group list-group-flush">
                            <li class="list-group-item">
                                <i class="fas fa-check text-success"></i> Integración directa con CaptacionDB
                            </li>
                            <li class="list-group-item">
                                <i class="fas fa-check text-success"></i> Responsive design (mobile-first)
                            </li>
                            <li class="list-group-item">
                                <i class="fas fa-check text-success"></i> Arquitectura segura y escalable
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Alert Examples -->
<div class="row mt-4">
    <div class="col-md-12">
        <h3 style="color: var(--gray-700); margin-bottom: 1.5rem;">
            <i class="fas fa-palette"></i> Tema Pastel Green Aplicado
        </h3>
        
        <div class="alert alert-success">
            <i class="fas fa-check-circle"></i> 
            <strong>¡Tema aplicado correctamente!</strong> El sistema está usando el esquema de colores pastel verde con fondo blanco.
        </div>
        
        <div class="alert alert-info">
            <i class="fas fa-info-circle"></i> 
            Este es un mensaje informativo con el estilo del tema personalizado.
        </div>
        
        <div class="alert alert-warning">
            <i class="fas fa-exclamation-triangle"></i> 
            Este es un mensaje de advertencia con bordes redondeados modernos.
        </div>
    </div>
</div>
```

---

## **Step 7: Build and Run**

1. **Build the solution:**
   - Press `Ctrl + Shift + B` or go to `Build` → `Build Solution`
   - Verify there are **no compilation errors**

2. **Run the application:**
   - Press `F5` (Debug mode) or `Ctrl + F5` (without debugging)
   - Your default browser should open at `https://localhost:XXXXX/`

3. **Verify the theme:**
   - ✅ Navigation bar should be **pastel green gradient**
   - ✅ Background should be **white**
   - ✅ Cards should have **rounded corners** and **soft shadows**
   - ✅ Buttons should be **pastel green** with hover effects
   - ✅ The page should be **fully responsive** (test by resizing browser)

---

## **Step 8: Test Database Connection (Optional)**

To verify your connection string works, open **SQL Server Management Studio (SSMS)** and:

1. Connect using the same server name you put in `Web.config`
2. Right-click on `CaptacionDB` → **Properties**
3. Verify the database exists and is accessible

**If the database doesn't exist yet:**
- You'll create the `Ind_*` tables in **Phase 2**
- The core tables (`Usuarios`, `Aspirantes`, etc.) should already exist

---

## **Step 9: Commit to Git (Recommended)**

If you're using Git version control:

```powershell
cd C:\dev\induccion_migration

# Initialize git repository (if not already done)
git init

# Add gitignore for Visual Studio
# Download from: https://github.com/github/gitignore/blob/main/VisualStudio.gitignore
# Save as .gitignore in project root

# Commit Phase 1 work
git add .
git commit -m "Phase 1 Complete: Project setup with Pastel Green theme"
```

---

## **Phase 1 Completion Checklist**

Before moving to Phase 2, verify:

- [ ] ASP.NET MVC 5 project created successfully
- [ ] All NuGet packages installed (EntityFramework, BCrypt, Bootstrap, jQuery)
- [ ] Folder structure created (ViewModels, Filters, Services, Helpers)
- [ ] Database connection string configured in `Web.config`
- [ ] Custom `InduccionTheme.css` created and applied
- [ ] `_Layout.cshtml` updated with pastel green navbar
- [ ] `_LoginPartial.cshtml` placeholder created
- [ ] Home page displays correctly with themed cards
- [ ] Application builds without errors
- [ ] Application runs and displays pastel green theme
- [ ] Responsive design works on mobile viewport

---

## **🎉 Phase 1 Complete!**

Your foundation is now solid. The pastel green theme is applied globally, and you have a clean, modern base to build upon.

**Next Step:** Proceed to **Phase 2** to set up Entity Framework Database-First models.

**Estimated Time for Phase 1:** 2-3 hours (including Visual Studio setup and testing)

---

## **Troubleshooting**

### **Problem: NuGet packages fail to install**
**Solution:** 
- Clear NuGet cache: `Tools` → `Options` → `NuGet Package Manager` → `Clear All NuGet Cache(s)`
- Restart Visual Studio
- Try installing packages one by one

### **Problem: CSS theme not applying**
**Solution:**
- Verify `BundleConfig.cs` includes `InduccionTheme.css`
- Clear browser cache (`Ctrl + Shift + Delete`)
- Check browser DevTools (F12) → Network tab to ensure CSS files are loading

### **Problem: Bootstrap looks different**
**Solution:**
- Ensure Bootstrap 4.6.2 is installed (not Bootstrap 5.x which has different markup)
- Check that `InduccionTheme.css` comes AFTER `bootstrap.min.css` in the bundle

### **Problem: Icons not showing**
**Solution:**
- Verify Font Awesome CDN link in `_Layout.cshtml`
- Check internet connection (Font Awesome loads from CDN)
- Alternative: Install Font Awesome via NuGet instead

---

**Ready to proceed to Phase 2?** Let me know when you've completed Phase 1 and tested everything!
