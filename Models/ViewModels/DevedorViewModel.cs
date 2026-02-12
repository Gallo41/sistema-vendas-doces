namespace SistemaVendasDoces.Models.ViewModels
{
    public class DevedorViewModel
    {
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }
        public decimal TotalDevendo { get; set; }
        public int QuantidadePedidos { get; set; }
        public List<Pedido> Pedidos { get; set; }
    }
}
