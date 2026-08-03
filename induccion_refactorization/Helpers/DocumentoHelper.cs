using System.IO;
using System.Linq;
using System.Security.Cryptography;
using induccion_refactorization.Models;

namespace induccion_refactorization.Helpers
{
    public static class DocumentoHelper
    {
        public const string TipoEntregableInduccion = "Entregable de Inducción";
        public const string EstadoPendiente = "Pendiente";

        // Reutilizan el vocabulario ya existente en dbo.EstadosDocumentos (compartido
        // con el resto del sistema de captación) en vez de crear estados nuevos y
        // redundantes: "Revisado y aprobado" de un entregable de inducción es lo mismo
        // que "Aprobado" para cualquier otro documento del sistema.
        public const string EstadoRevisado = "Aprobado";
        public const string EstadoRechazado = "Rechazado";

        public static string ComputeSha256Hash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return System.BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public static TipoDocumento GetOrCreateTipoDocumento(CaptacionDbContext db, string nombre)
        {
            var tipo = db.TiposDocumentos.FirstOrDefault(t => t.Nombre == nombre);
            if (tipo == null)
            {
                tipo = new TipoDocumento { Nombre = nombre };
                db.TiposDocumentos.Add(tipo);
                db.SaveChanges();
            }
            return tipo;
        }

        public static EstadoDocumento GetOrCreateEstadoDocumento(CaptacionDbContext db, string nombre)
        {
            var estado = db.EstadosDocumentos.FirstOrDefault(e => e.Nombre == nombre);
            if (estado == null)
            {
                estado = new EstadoDocumento { Nombre = nombre };
                db.EstadosDocumentos.Add(estado);
                db.SaveChanges();
            }
            return estado;
        }
    }
}
