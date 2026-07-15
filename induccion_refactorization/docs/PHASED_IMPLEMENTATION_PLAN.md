# **Phased Implementation Plan**
## **Induction Platform Migration: Solo Developer Roadmap**

---

## **Master Checklist: 7 Core Phases**

This plan is designed for **sequential execution** by a solo developer. Each phase builds upon the previous one, ensuring you never feel overwhelmed.

---

### **Phase 1: Foundation Setup (Days 1-2)**
**Goal:** Create the ASP.NET MVC 5 project structure, configure database connection, and establish the Pastel Green/White custom theme.

**Tasks:**
- [ ] Create new ASP.NET MVC 5 project in Visual Studio
- [ ] Install required NuGet packages (Entity Framework, BCrypt, Bootstrap)
- [ ] Configure connection string to CaptacionDB
- [ ] Create folder structure (Controllers, Models, ViewModels, Filters, Services, Helpers)
- [ ] Implement custom CSS theme (Pastel Green primary, White background)
- [ ] Create base `_Layout.cshtml` with themed Bootstrap navbar
- [ ] Create `_LoginPartial.cshtml` placeholder
- [ ] Test application runs successfully

**Deliverables:**
- ✅ Running ASP.NET MVC 5 application
- ✅ Custom pastel green theme applied globally
- ✅ Database connection verified

---

### **Phase 2: Database-First Entity Framework (Days 3-4)**
**Goal:** Generate all EF models from CaptacionDB using Database-First approach.

**Tasks:**
- [ ] Execute SQL scripts from `analisis_captaciondb.md` in SSMS (create `Ind_*` tables)
- [ ] Verify all Foreign Key constraints in database
- [ ] Add ADO.NET Entity Data Model to project
- [ ] Select all core tables: `Usuarios`, `Roles`, `Aspirantes`, `Carreras`, `Periodos`
- [ ] Select all Induction tables: `Ind_Materias`, `Ind_Unidades`, `Ind_Materiales`, `Ind_ProgresoAspirantes`
- [ ] Verify navigation properties are correctly generated
- [ ] Create partial classes for custom business logic if needed
- [ ] Test DbContext connection with a simple LINQ query

**Deliverables:**
- ✅ All EF models generated and functional
- ✅ DbContext configured with proper connection string
- ✅ Navigation properties working correctly

---

### **Phase 3: Authentication & Authorization (Days 5-6)**
**Goal:** Implement secure login system with Forms Authentication and role-based access.

**Tasks:**
- [ ] Create `LoginViewModel` and `EditarEmailViewModel` in ViewModels folder
- [ ] Implement `AccountController` with Login/Logout actions
- [ ] Configure Forms Authentication in `Web.config`
- [ ] Install BCrypt.Net-Next for password hashing
- [ ] Create `RoleAuthorizeAttribute` custom filter in Filters folder
- [ ] Implement session management for user data
- [ ] Create `Login.cshtml` view with themed form
- [ ] Test login flow for all 4 roles (Admin, Aspirante, Coordinador, Captador)
- [ ] Implement role-based redirection logic
- [ ] Update `_LoginPartial.cshtml` with session data display

**Deliverables:**
- ✅ Secure login system operational
- ✅ Role-based authorization working
- ✅ Session management implemented

---

### **Phase 4: Student (Aspirante) Module (Days 7-9)**
**Goal:** Build complete student workspace with course viewing and assignment submission.

**Tasks:**
- [ ] Create all ViewModels: `MateriaViewModel`, `MateriaDetalleViewModel`, `UnidadViewModel`, `MaterialViewModel`
- [ ] Implement `AspiranteController` with all actions:
  - [ ] `Index()` - List assigned courses
  - [ ] `ContenidoMateria(int id)` - Display course content with units
  - [ ] `EnviarTarea()` - Handle assignment file uploads
  - [ ] `EditarEmail()` - Fix Bug #1 (dummy email update)
- [ ] Create `Views/Aspirante/Index.cshtml` (course list with Bootstrap cards)
- [ ] Create `Views/Aspirante/ContenidoMateria.cshtml` (course details with units/materials)
- [ ] Create `Views/Aspirante/EditarEmail.cshtml` (email update form)
- [ ] Implement `FileUploadHelper` in Helpers folder
- [ ] Create `App_Data/Uploads` folder for file storage
- [ ] Test complete student workflow end-to-end
- [ ] Verify dummy email detection and redirect

**Deliverables:**
- ✅ Student can view assigned courses
- ✅ Student can access course materials (PDFs, videos, links)
- ✅ Student can submit assignment files
- ✅ Bug #1 (double email check) is fixed

---

### **Phase 5: Coordinator Module (Days 10-12)**
**Goal:** Build coordinator dashboard with student roster and grading functionality.

**Tasks:**
- [ ] Create ViewModels: `CoordinadorDashboardViewModel`, `AspiranteListViewModel`, `TareaAlumnoViewModel`, `ProgresoViewModel`
- [ ] Implement `CoordinadorController` with all actions:
  - [ ] `Index(int page, int pageSize)` - Student list with pagination (Fix Bug #2)
  - [ ] `TareaAlumno(string username)` - View student progress
  - [ ] `CalificarTarea()` - Grade submitted assignments
  - [ ] `ListaMaterias()` - View all courses
  - [ ] `CargarAlumnos()` - Bulk student upload (placeholder)
- [ ] Create `Views/Coordinador/Index.cshtml` (student roster with pagination)
- [ ] Create `Views/Coordinador/TareaAlumno.cshtml` (student progress view)
- [ ] Create `Views/Coordinador/ListaMaterias.cshtml` (course list)
- [ ] Implement server-side pagination logic
- [ ] Test pagination with sample data (create 50+ test students if needed)
- [ ] Test grading workflow (assign, submit, grade)
- [ ] Verify Bug #2 (missing student list) is resolved

**Deliverables:**
- ✅ Coordinator can view complete student roster
- ✅ Pagination working correctly
- ✅ Coordinator can view student progress per course
- ✅ Grading system functional
- ✅ Bug #2 (missing student roster) is fixed

---

### **Phase 6: Admin & Captador Modules (Days 13-15)**
**Goal:** Complete the remaining role modules for full system functionality.

**Tasks:**
- [ ] Implement `AdminController` with actions:
  - [ ] `Index()` - Admin dashboard
  - [ ] `CrearMateria()` - Create new courses
  - [ ] `GestionUsuarios()` - User management
  - [ ] CRUD operations for `Ind_Materias` and `Ind_Unidades`
- [ ] Implement `CaptadorController` with actions:
  - [ ] `Index()` - Recruitment dashboard
  - [ ] Student progress reports
  - [ ] Export functionality (optional: Excel/CSV)
- [ ] Create all admin views (Index, CrearMateria, GestionUsuarios)
- [ ] Create Captador dashboard view
- [ ] Implement `MateriaService` in Services folder for business logic
- [ ] Test all CRUD operations for courses and units
- [ ] Verify role isolation (Admin cannot access Aspirante views, etc.)

**Deliverables:**
- ✅ Admin module fully functional
- ✅ Captador module operational
- ✅ All 4 roles have complete functionality

---

### **Phase 7: Testing, Optimization & Deployment (Days 16-18)**
**Goal:** Comprehensive testing, performance optimization, and production deployment.

**Tasks:**
- [ ] **Data Migration:**
  - [ ] Export legacy Strapi data
  - [ ] Transform JSON `unidades` into `Ind_Unidades` using SQL scripts (Appendix C)
  - [ ] Transform `tareas.entregados` into `Ind_ProgresoAspirantes`
  - [ ] Migrate user passwords (rehash with BCrypt)
  - [ ] Copy file uploads from Strapi to ASP.NET `App_Data/Uploads`
- [ ] **Comprehensive Testing:**
  - [ ] Test all 4 role workflows with real data
  - [ ] Verify bug fixes (email update, student roster)
  - [ ] Test file upload/download functionality
  - [ ] Test pagination with 500+ student records
  - [ ] Cross-browser testing (Chrome, Firefox, Edge)
  - [ ] Mobile responsiveness testing
- [ ] **Performance Optimization:**
  - [ ] Enable output caching for static views
  - [ ] Optimize LINQ queries (use `.AsNoTracking()` for read-only)
  - [ ] Minify CSS/JS bundles
  - [ ] Add database indexes on frequently queried columns
- [ ] **IIS Deployment:**
  - [ ] Configure production connection string
  - [ ] Set up IIS Application Pool (.NET 4.0, Integrated pipeline)
  - [ ] Configure MIME types for file downloads
  - [ ] Enable HTTPS and install SSL certificate
  - [ ] Test production deployment
- [ ] **Documentation:**
  - [ ] Update README with deployment instructions
  - [ ] Document any custom configurations
  - [ ] Create user manual for administrators

**Deliverables:**
- ✅ All legacy data migrated successfully
- ✅ System tested and validated
- ✅ Production deployment successful
- ✅ Documentation complete

---

## **Time Estimates**

| Phase | Duration | Cumulative |
|-------|----------|------------|
| Phase 1: Foundation | 2 days | 2 days |
| Phase 2: Entity Framework | 2 days | 4 days |
| Phase 3: Authentication | 2 days | 6 days |
| Phase 4: Student Module | 3 days | 9 days |
| Phase 5: Coordinator Module | 3 days | 12 days |
| Phase 6: Admin/Captador | 3 days | 15 days |
| Phase 7: Testing & Deployment | 3 days | **18 days** |

**Total Estimated Timeline:** 18 working days (~3.5 weeks)

---

## **Daily Work Rhythm (Recommended)**

To avoid burnout as a solo developer, follow this daily structure:

1. **Morning (3 hours):** Focus on complex logic (controllers, business logic, EF queries)
2. **Afternoon (3 hours):** UI development (Razor views, CSS refinements)
3. **End of Day (1 hour):** Testing and documentation

**Weekly Checkpoints:**
- End of Week 1: Phases 1-3 complete ✅
- End of Week 2: Phases 4-5 complete ✅
- End of Week 3: Phases 6-7 complete ✅

---

## **Critical Success Factors**

1. **Don't Skip Phases:** Each phase depends on the previous one
2. **Test Incrementally:** Don't wait until Phase 7 to start testing
3. **Commit to Git Daily:** Version control is your safety net
4. **Use the Migration Guide:** Reference `MIGRATION_GUIDE_INDUCCION.md` for exact code samples
5. **Ask for Help:** If stuck, refer back to the guide or ask specific questions

---

## **Next Step: Execute Phase 1**

You're ready to start! See `PHASE_1_EXECUTION.md` for exact terminal commands, Visual Studio steps, and the complete Pastel Green theme CSS code.
