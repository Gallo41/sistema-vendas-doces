namespace SistemaVendasDoces.Models
{
    public class DashboardViewModel
    {
        public int TotalProdutos { get; set; }
        public int TotalClientes { get; set; }
        public decimal VendasMes { get; set; }
        public int ProdutosEmEstoque { get; set; }
    }
}
