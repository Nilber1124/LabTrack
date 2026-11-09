namespace LabTrack.ViewModels
{
    public class PaginacionViewModel
    {
        public int Pagina { get; set; } = 1;
        public int Total { get; set; }
        public int TamanoPagina { get; set; } = 1;
        public string Etiqueta { get; set; } = "registros";
        public Dictionary<string, string?> Parametros { get; set; } = new();

        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));
        public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;
        public int Hasta => Math.Min(Pagina * TamanoPagina, Total);

        public static int Normalizar(int pagina, int total, int tamanoPagina)
        {
            var ultima = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
            return Math.Clamp(pagina, 1, ultima);
        }
    }
}
