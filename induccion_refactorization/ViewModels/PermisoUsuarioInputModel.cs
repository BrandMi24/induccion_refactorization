namespace induccion_refactorization.ViewModels
{
    // NULL = hereda del rol; true/false = permite/deniega sin importar el rol.
    public class PermisoUsuarioInputModel
    {
        public int PermisoID { get; set; }
        public bool? Leer { get; set; }
        public bool? Crear { get; set; }
        public bool? Editar { get; set; }
        public bool? Eliminar { get; set; }
    }
}
