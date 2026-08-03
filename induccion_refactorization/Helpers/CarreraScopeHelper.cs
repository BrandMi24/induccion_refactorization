using System.Collections.Generic;
using System.Linq;
using induccion_refactorization.Models;

namespace induccion_refactorization.Helpers
{
    /// <summary>
    /// Centraliza el filtrado por carrera para Coordinador y Maestro: ambos roles
    /// solo deben ver/gestionar contenido y aspirantes de la(s) carrera(s) a las
    /// que están asignados (Ind_UsuarioCarreras). Administrador no tiene restricción.
    /// </summary>
    public static class CarreraScopeHelper
    {
        public const int RolAdmin = 1;
        public const int RolCoordinador = 3;
        public const int RolMaestro = 5;

        public static bool IsScopedRole(int rolId)
        {
            return rolId == RolCoordinador || rolId == RolMaestro;
        }

        public static List<int> GetUserCarreraIds(CaptacionDbContext db, int usuarioId)
        {
            return db.Usuarios
                .Where(u => u.UsuarioID == usuarioId)
                .SelectMany(u => u.Carreras.Select(c => c.CarreraID))
                .ToList();
        }

        public static IQueryable<Ind_Materia> ScopeMaterias(IQueryable<Ind_Materia> query, int rolId, List<int> carreraIds)
        {
            if (!IsScopedRole(rolId))
            {
                return query;
            }

            return query.Where(m => m.TodasLasCarreras || m.Carreras.Any(c => carreraIds.Contains(c.CarreraID)));
        }

        public static bool MateriaEnScope(Ind_Materia materia, int rolId, List<int> carreraIds)
        {
            if (materia == null)
            {
                return false;
            }

            if (!IsScopedRole(rolId))
            {
                return true;
            }

            return materia.TodasLasCarreras || materia.Carreras.Any(c => carreraIds.Contains(c.CarreraID));
        }
    }
}
