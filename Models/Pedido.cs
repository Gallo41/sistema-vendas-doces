using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVendasDoces.Models
{
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public DateTime DataPedido { get; set; } = DateTime.Now;

        public DateTime? DataEntrega { get; set; }

        [Required]
        public StatusPedido Status { get; set; } = StatusPedido.Pendente;

        [Required]
        public StatusPagamento StatusPagamento { get; set; } = StatusPagamento.Pendente;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorTotal { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorPago { get; set; } = 0;

        [StringLength(50)]
        public string? FormaPagamento { get; set; }

        [StringLength(500)]
        public string? Observacoes { get; set; }

        // Relacionamentos
        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        public virtual ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();

        // Propriedade calculada
        [NotMapped]
        public decimal ValorPendente => ValorTotal - ValorPago;
    }

    public enum StatusPedido
    {
        [Display(Name = "Pendente")]
        Pendente,
        
        [Display(Name = "Em Produção")]
        EmProducao,
        
        [Display(Name = "Pronto")]
        Pronto,
        
        [Display(Name = "Entregue")]
        Entregue,
        
        [Display(Name = "Cancelado")]
        Cancelado
    }

    public enum StatusPagamento
    {
        [Display(Name = "Pendente")]
        Pendente,
        
        [Display(Name = "Parcial")]
        Parcial,
        
        [Display(Name = "Pago")]
        Pago
    }
}
