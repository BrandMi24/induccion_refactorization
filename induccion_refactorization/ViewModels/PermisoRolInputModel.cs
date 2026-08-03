namespace induccion_refactorization.ViewModels
{
    public class PermisoRolInputModel
    {
        public int PermisoID { get; set; }
        public bool Leer { get; set; }
        public bool Crear { get; set; }
        public bool Editar { get; set; }
        public bool Eliminar { get; set; }
    }
}
