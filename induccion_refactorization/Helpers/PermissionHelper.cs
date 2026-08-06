using System.Linq;
using induccion_refactorization.Models;

namespace induccion_refactorization.Helpers
{
    public enum Accion
    {
        Leer,
        Crear,
        Editar,
        Eliminar
    }

    /// <summary>
    /// Capa de permisos finos (Leer/Crear/Editar/Eliminar) por sección, encima de
    /// los [RoleAuthorize] existentes. Un usuario específico puede tener una
    /// excepción en Ind_UsuarioPermisos que permite o deniega una acción sin
    /// importar lo que diga su rol en Ind_RolPermisos; si no hay excepción para
    /// esa acción (columna NULL), se usa el valor del rol. Si no hay fila ni de
    /// rol ni de usuario, se deniega por defecto.
    /// </summary>
    public static class PermissionHelper
    {
        public static bool TieneAcceso(CaptacionDbContext db, int usuarioId, int rolId, string permisoClave, Accion accion)
        {
            var permiso = db.Ind_Permisos.FirstOrDefault(p => p.Clave == permisoClave);
            if (permiso == null)
            {
                return false;
            }

            var overrideUsuario = db.Ind_UsuarioPermisos
                .FirstOrDefault(up => up.UsuarioID == usuarioId && up.PermisoID == permiso.PermisoID);

            bool? valorOverride = GetValor(overrideUsuario, accion);
            if (valorOverride.HasValue)
            {
                return valorOverride.Value;
            }

            var permisoRol = db.Ind_RolPermisos
                .FirstOrDefault(rp => rp.RolID == rolId && rp.PermisoID == permiso.PermisoID);

            if (permisoRol == null)
            {
                return false;
            }

            switch (accion)
            {
                case Accion.Leer:
                    return permisoRol.PuedeLeer;
                case Accion.Crear:
                    return permisoRol.PuedeCrear;
                case Accion.Editar:
                    return permisoRol.PuedeEditar;
                case Accion.Eliminar:
                    return permisoRol.PuedeEliminar;
                default:
                    return false;
            }
        }

        // Llena ViewBag.PuedeCrear / PuedeEditar / PuedeEliminar para la sección
        // (clave) que renderiza la vista actual, así las vistas pueden ocultar
        // botones de acciones que el usuario no puede usar (en vez de solo dejar
        // que el [RequierePermiso] del lado del servidor lo rechace después de
        // que ya le mostramos el botón). Leer no se incluye porque, si la acción
        // GET que renderiza la vista ya está protegida con Accion.Leer, el usuario
        // ya demostró tener ese permiso con solo haber llegado a la página.
        public static void AsignarFlagsVista(dynamic viewBag, CaptacionDbContext db, int usuarioId, int rolId, string permisoClave)
        {
            viewBag.PuedeCrear = TieneAcceso(db, usuarioId, rolId, permisoClave, Accion.Crear);
            viewBag.PuedeEditar = TieneAcceso(db, usuarioId, rolId, permisoClave, Accion.Editar);
            viewBag.PuedeEliminar = TieneAcceso(db, usuarioId, rolId, permisoClave, Accion.Eliminar);
        }

        private static bool? GetValor(Ind_UsuarioPermiso overrideUsuario, Accion accion)
        {
            if (overrideUsuario == null)
            {
                return null;
            }

            switch (accion)
            {
                case Accion.Leer:
                    return overrideUsuario.PuedeLeer;
                case Accion.Crear:
                    return overrideUsuario.PuedeCrear;
                case Accion.Editar:
                    return overrideUsuario.PuedeEditar;
                case Accion.Eliminar:
                    return overrideUsuario.PuedeEliminar;
                default:
                    return null;
            }
        }
    }
}
