using System.IO;
using System.Web;

namespace induccion_refactorization.Helpers
{
    public static class FileUploadValidator
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".xlsx", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public static bool IsValid(HttpPostedFileBase file, out string errorMessage)
        {
            if (file == null || file.ContentLength == 0)
            {
                errorMessage = "Debe seleccionar un archivo.";
                return false;
            }

            if (file.ContentLength > MaxFileSizeBytes)
            {
                errorMessage = "El archivo excede el tamaño máximo permitido de 10 MB.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || System.Array.IndexOf(AllowedExtensions, extension) < 0)
            {
                errorMessage = "Tipo de archivo no permitido. Formatos aceptados: PDF, DOCX, XLSX, JPG, PNG.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
