using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace induccion_refactorization.Helpers
{
    public class FilaUsuarioImportado
    {
        public int NumeroFila { get; set; }
        public string Nombre { get; set; }
        public string ApellidoPaterno { get; set; }
        public string ApellidoMaterno { get; set; }
        public string CorreoElectronico { get; set; }
        public List<string> NombresCarreras { get; set; } = new List<string>();
    }

    public static class ExcelImportHelper
    {
        private static readonly string[] Encabezados = { "Nombre", "ApellidoPaterno", "ApellidoMaterno", "CorreoElectronico", "Carrera 1", "Carrera 2", "Carrera 3" };
        private const int PrimeraColumnaCarrera = 5;
        private const int UltimaColumnaCarrera = 7;
        private const int ColumnaCorreo = 4;

        // El usuario ya no llena "Nombre de Usuario" a mano — el sistema lo deriva
        // del correo (el correo ya es lo que usan para iniciar sesión). Las
        // carreras se seleccionan de un desplegable con las carreras a cargo de
        // quien importa, en vez de escribir el nombre a mano.
        public static byte[] GenerarPlantilla(string tituloHoja, List<string> carrerasDisponibles)
        {
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add(tituloHoja);
                for (int i = 0; i < Encabezados.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = Encabezados[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                }

                ws.Cells[2, 1].Value = "Juan";
                ws.Cells[2, 2].Value = "Pérez";
                ws.Cells[2, 3].Value = "García";
                ws.Cells[2, 4].Value = "juan.perez@ejemplo.com";
                if (carrerasDisponibles.Count > 0)
                {
                    ws.Cells[2, PrimeraColumnaCarrera].Value = carrerasDisponibles[0];
                }

                if (carrerasDisponibles.Count > 0)
                {
                    // Las carreras válidas viven en una hoja oculta y el desplegable de
                    // cada columna "Carrera N" apunta a ese rango — así el usuario solo
                    // puede elegir una carrera real (de las que tiene a su cargo), nunca
                    // escribirla a mano.
                    var wsRef = package.Workbook.Worksheets.Add("_Carreras");
                    for (int i = 0; i < carrerasDisponibles.Count; i++)
                    {
                        wsRef.Cells[i + 1, 1].Value = carrerasDisponibles[i];
                    }
                    wsRef.Hidden = eWorkSheetHidden.VeryHidden;

                    var rango = $"'_Carreras'!$A$1:$A${carrerasDisponibles.Count}";
                    for (int col = PrimeraColumnaCarrera; col <= UltimaColumnaCarrera; col++)
                    {
                        var letra = ((char)('A' + col - 1)).ToString();
                        var validacion = ws.DataValidations.AddListValidation($"{letra}2:{letra}200");
                        validacion.Formula.ExcelFormula = rango;
                        validacion.ShowErrorMessage = true;
                        validacion.ErrorTitle = "Carrera no válida";
                        validacion.Error = "Selecciona una carrera de la lista.";
                    }
                }

                for (int i = 1; i <= Encabezados.Length; i++)
                {
                    ws.Column(i).AutoFit();
                }

                return package.GetAsByteArray();
            }
        }

        // Lee las filas crudas del archivo (sin validar contra la base de datos,
        // eso lo hace el controlador que sí tiene acceso al DbContext y al scope
        // de carreras del usuario que está importando).
        public static List<FilaUsuarioImportado> LeerFilas(Stream archivo)
        {
            var filas = new List<FilaUsuarioImportado>();

            using (var package = new ExcelPackage(archivo))
            {
                var ws = package.Workbook.Worksheets.FirstOrDefault(w => w.Name != "_Carreras")
                    ?? package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null || ws.Dimension == null)
                {
                    return filas;
                }

                for (int row = 2; row <= ws.Dimension.End.Row; row++)
                {
                    var nombre = (ws.Cells[row, 1].Text ?? "").Trim();
                    var apellidoPaterno = (ws.Cells[row, 2].Text ?? "").Trim();
                    var apellidoMaterno = (ws.Cells[row, 3].Text ?? "").Trim();
                    var correo = (ws.Cells[row, ColumnaCorreo].Text ?? "").Trim();

                    var carreras = new List<string>();
                    for (int col = PrimeraColumnaCarrera; col <= UltimaColumnaCarrera; col++)
                    {
                        var valor = (ws.Cells[row, col].Text ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(valor))
                        {
                            carreras.Add(valor);
                        }
                    }

                    // Fila completamente vacía (huecos al final del archivo): se ignora.
                    if (string.IsNullOrWhiteSpace(nombre) && string.IsNullOrWhiteSpace(apellidoPaterno) &&
                        string.IsNullOrWhiteSpace(correo) && !carreras.Any())
                    {
                        continue;
                    }

                    filas.Add(new FilaUsuarioImportado
                    {
                        NumeroFila = row,
                        Nombre = nombre,
                        ApellidoPaterno = apellidoPaterno,
                        ApellidoMaterno = apellidoMaterno,
                        CorreoElectronico = correo,
                        NombresCarreras = carreras
                    });
                }
            }

            return filas;
        }

        public static string GenerarContrasenaTemporal()
        {
            const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new System.Random();
            var chars = new char[10];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = caracteres[random.Next(caracteres.Length)];
            }
            return new string(chars);
        }

        // Genera el Excel de credenciales que se ofrece descargar justo después de
        // una carga masiva, para que quien importó pueda entregarle a cada persona
        // cómo entrar por primera vez (su contraseña se vuelve hash en automático
        // la primera vez que inicien sesión; después la cambian desde su Perfil).
        public static byte[] GenerarExcelCredenciales(string tituloHoja, List<CredencialGenerada> registros)
        {
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add(tituloHoja);
                var encabezados = new[] { "Nombre Completo", "Usuario", "Correo Electrónico", "Contraseña Temporal" };
                for (int i = 0; i < encabezados.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = encabezados[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                }

                for (int i = 0; i < registros.Count; i++)
                {
                    var r = registros[i];
                    ws.Cells[i + 2, 1].Value = r.NombreCompleto;
                    ws.Cells[i + 2, 2].Value = r.Usuario;
                    ws.Cells[i + 2, 3].Value = r.Correo;
                    ws.Cells[i + 2, 4].Value = r.ContrasenaTemporal;
                }

                for (int i = 1; i <= encabezados.Length; i++)
                {
                    ws.Column(i).AutoFit();
                }

                return package.GetAsByteArray();
            }
        }
    }

    public class CredencialGenerada
    {
        public string NombreCompleto { get; set; }
        public string Usuario { get; set; }
        public string Correo { get; set; }
        public string ContrasenaTemporal { get; set; }
    }
}
