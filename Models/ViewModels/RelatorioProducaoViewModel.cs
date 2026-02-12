namespace SistemaVendasDoces.Models.ViewModels
{
    public class RelatorioProducaoViewModel
    {
        // Resumo geral: total de cada produto a produzir
        public List<ItemProducao> ResumoProducao { get; set; } = new();

        // Lista de pedidos pendentes/em produção com seus itens
        public List<PedidoProducao> Pedidos { get; set; } = new();

        // Totais
        public int TotalPedidos { get; set; }
        public int TotalUnidades { get; set; }
    }

    public class ItemProducao
    {
        public string NomeProduto { get; set; } = string.Empty;
        public int QuantidadeNecessaria { get; set; }
        public int EstoqueAtual { get; set; }
        public int Diferenca => EstoqueAtual - QuantidadeNecessaria;
        public bool EstoqueSuficiente => Diferenca >= 0;
    }

    public class PedidoProducao
    {
        public int PedidoId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public DateTime DataPedido { get; set; }
        public DateTime? DataEntrega { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public string? Observacoes { get; set; }
        public List<ItemPedidoProducao> Itens { get; set; } = new();
    }

    public class ItemPedidoProducao
    {
        public string NomeProduto { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
