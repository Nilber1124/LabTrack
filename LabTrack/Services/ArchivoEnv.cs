namespace LabTrack.Services
{
    // Carga el archivo .env de la raíz del repositorio como variables de entorno.
    // En un despliegue no existe la solución, así que solo se usan las variables reales.
    public static class ArchivoEnv
    {
        private const string NombreArchivo = ".env";
        private const string MarcaRaiz = "LabTrack.slnx";

        public static void Cargar()
        {
            var raiz = BuscarRaiz(Directory.GetCurrentDirectory());
            var archivo = raiz is null ? null : Path.Combine(raiz, NombreArchivo);
            if (archivo is null || !File.Exists(archivo))
            {
                return;
            }

            foreach (var (clave, valor) in Interpretar(File.ReadAllLines(archivo)))
            {
                // Una variable definida de verdad en el sistema manda sobre el archivo.
                if (Environment.GetEnvironmentVariable(clave) is null)
                {
                    Environment.SetEnvironmentVariable(clave, valor);
                }
            }
        }

        public static IReadOnlyDictionary<string, string> Interpretar(IEnumerable<string> lineas)
        {
            var valores = new Dictionary<string, string>();

            foreach (var linea in lineas.Select(l => l.Trim()))
            {
                var separador = linea.IndexOf('=');
                if (linea.Length == 0 || linea.StartsWith('#') || separador <= 0)
                {
                    continue;
                }

                var valor = QuitarComillas(linea[(separador + 1)..].Trim());
                if (valor.Length > 0)
                {
                    valores[linea[..separador].Trim()] = valor;
                }
            }

            return valores;
        }

        private static string QuitarComillas(string valor)
            => valor.Length >= 2 && (valor[0], valor[^1]) is ('"', '"') or ('\'', '\'') ? valor[1..^1] : valor;

        private static string? BuscarRaiz(string desde)
        {
            for (var carpeta = new DirectoryInfo(desde); carpeta is not null; carpeta = carpeta.Parent)
            {
                if (File.Exists(Path.Combine(carpeta.FullName, MarcaRaiz)))
                {
                    return carpeta.FullName;
                }
            }

            return null;
        }
    }
}
