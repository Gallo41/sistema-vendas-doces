using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVendasDoces.Models
{
    public class Venda
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public DateTime DataVenda { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorTotal { get; set; }

        [StringLength(20)]
        public string FormaPagamento { get; set; } = "Dinheiro";

        [StringLength(500)]
        public string? Observacoes { get; set; }

        // Relacionamentos
        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        public virtual ICollection<ItemVenda>? Itens { get; set; }
    }
}
