using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVendasDoces.Models
{
    public class ItemPedido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PedidoId { get; set; }

        [Required]
        public int ProdutoId { get; set; }

        [Required]
        public int Quantidade { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecoUnitario { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        // Relacionamentos
        [ForeignKey("PedidoId")]
        public virtual Pedido? Pedido { get; set; }

        [ForeignKey("ProdutoId")]
        public virtual Produto? Produto { get; set; }
    }
}
