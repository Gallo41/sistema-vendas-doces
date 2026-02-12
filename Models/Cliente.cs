// Importa a caixa de ferramentas de validação (regras como "Obrigatório", "Tamanho Máximo")
using System.ComponentModel.DataAnnotations;

namespace SistemaVendasDoces.Models
{
    public class Cliente
    {
        // [Key] avisa ao banco que este campo é a CHAVE PRIMÁRIA (Primary Key).
        // No MySQL, ele vai virar um ID AUTO_INCREMENT (1, 2, 3...) automaticamente.
        [Key]
        public int Id { get; set; }

        // [Required] diz: "Não aceito nulo". Se tentar salvar sem nome, o sistema barra.
        // ErrorMessage é o texto que vai aparecer em vermelho na tela pra sua mãe.
        [Required(ErrorMessage = "O nome do cliente é obrigatório")]

        // [StringLength] limita o tamanho. No banco vira um VARCHAR(100).
        // Isso economiza espaço (não cria um texto infinito) e previne erros.
        [StringLength(100)]

        // "string.Empty" inicia a variável vazia para evitar erro de "nulo" no C# antes de preencher.
        public string Nome { get; set; } = string.Empty;

        // O "?" depois de string significa "NULLABLE" (Pode ser nulo).
        // Ou seja, o cliente pode não ter telefone, e o sistema aceita salvar assim.
        [StringLength(15)]
        public string? Telefone { get; set; }

        // [EmailAddress] é mágico: ele verifica sozinho se tem "@", se tem ponto...
        // Você não precisa criar lógica complexa pra validar email.
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Endereco { get; set; }

        // bool (Verdadeiro/Falso).
        // "= true" define um VALOR PADRÃO. Todo cliente novo já nasce "Ativo".
        // Útil pra não deletar cliente, apenas desativar (Soft Delete).
        public bool Ativo { get; set; } = true;

        // DateTime.Now pega a data e hora exata do servidor no momento que cria o objeto.
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        // --- RELACIONAMENTOS (A parte mais legal) ---

        // "virtual" permite que o Entity Framework faça "Lazy Loading" (carregamento preguiçoso).
        // "ICollection" é uma lista.
        // Tradução: "Um Cliente pode ter VÁRIAS (uma coleção de) Vendas".
        // Isso permite você fazer: var lista = cliente.Vendas;
        public virtual ICollection<Venda>? Vendas { get; set; }
    }
}