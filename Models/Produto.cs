using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVendasDoces.Models
{
    public class Produto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do produto é obrigatório")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descricao { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecoVenda { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PrecoCusto { get; set; }

        [Required]
        public int QuantidadeEstoque { get; set; }

        public int? EstoqueMinimo { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime DataCadastro { get; set; } = DateTime.Now;

        // Relacionamento com Vendas
        public virtual ICollection<ItemVenda>? ItensVenda { get; set; }
    }
}
