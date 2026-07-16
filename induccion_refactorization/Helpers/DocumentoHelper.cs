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
