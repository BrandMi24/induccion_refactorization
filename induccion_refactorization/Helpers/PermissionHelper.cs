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
