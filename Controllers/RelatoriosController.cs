using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVendasDoces.Data;
using SistemaVendasDoces.Models;
using SistemaVendasDoces.Models.ViewModels;

namespace SistemaVendasDoces.Controllers
{
    public class RelatoriosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RelatoriosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Relatorios
        public IActionResult Index()
        {
            return View();
        }

        // GET: Relatorios/ProducaoPedidos
        public async Task<IActionResult> ProducaoPedidos()
        {
            // Buscar pedidos pendentes e em produção
            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .Where(p => p.Status == StatusPedido.Pendente || p.Status == StatusPedido.EmProducao)
                .OrderBy(p => p.DataPedido)
                .ToListAsync();

            // Agrupar todos os itens por produto para resumo de produção
            var todosItens = pedidos
                .SelectMany(p => p.Itens)
                .GroupBy(i => i.ProdutoId)
                .Select(g => new ItemProducao
                {
                    NomeProduto = g.First().Produto?.Nome ?? "Produto desconhecido",
                    QuantidadeNecessaria = g.Sum(i => i.Quantidade),
                    EstoqueAtual = g.First().Produto?.QuantidadeEstoque ?? 0
                })
                .OrderByDescending(i => i.QuantidadeNecessaria)
                .ToList();

            // Montar lista de pedidos com itens
            var pedidosProducao = pedidos.Select(p => new PedidoProducao
            {
                PedidoId = p.Id,
                ClienteNome = p.Cliente?.Nome ?? "Cliente desconhecido",
                DataPedido = p.DataPedido,
                DataEntrega = p.DataEntrega,
                Status = p.Status.ToString(),
                ValorTotal = p.ValorTotal,
                Observacoes = p.Observacoes,
                Itens = p.Itens.Select(i => new ItemPedidoProducao
                {
                    NomeProduto = i.Produto?.Nome ?? "Produto desconhecido",
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario,
                    Subtotal = i.Subtotal
                }).ToList()
            }).ToList();

            var viewModel = new RelatorioProducaoViewModel
            {
                ResumoProducao = todosItens,
                Pedidos = pedidosProducao,
                TotalPedidos = pedidos.Count,
                TotalUnidades = todosItens.Sum(i => i.QuantidadeNecessaria)
            };

            return View(viewModel);
        }

        // GET: Relatorios/SaboresMaisVendidos
        public async Task<IActionResult> SaboresMaisVendidos(DateTime? dataInicio, DateTime? dataFim)
        {
            dataInicio ??= DateTime.Now.AddMonths(-1);
            dataFim ??= DateTime.Now;

            var sabores = await _context.ItensPedido
                .Include(i => i.Produto)
                .Include(i => i.Pedido)
                .Where(i => i.Pedido!.Status == StatusPedido.Entregue &&
                           i.Pedido.DataEntrega >= dataInicio && 
                           i.Pedido.DataEntrega <= dataFim)
                .GroupBy(i => i.ProdutoId)
                .Select(g => new
                {
                    Produto = g.First().Produto,
                    QuantidadeVendida = g.Sum(i => i.Quantidade),
                    ReceitaGerada = g.Sum(i => i.Subtotal)
                })
                .OrderByDescending(s => s.QuantidadeVendida)
                .ToListAsync();

            ViewBag.DataInicio = dataInicio;
            ViewBag.DataFim = dataFim;
            return View(sabores);
        }

        // GET: Relatorios/ClientesFrequentes
        public async Task<IActionResult> ClientesFrequentes(DateTime? dataInicio, DateTime? dataFim)
        {
            dataInicio ??= DateTime.Now.AddMonths(-1);
            dataFim ??= DateTime.Now;

            var clientes = await _context.Pedidos
                .Include(p => p.Cliente)
                .Where(p => p.Status == StatusPedido.Entregue &&
                           p.DataEntrega >= dataInicio && 
                           p.DataEntrega <= dataFim)
                .GroupBy(p => p.ClienteId)
                .Select(g => new
                {
                    Cliente = g.First().Cliente,
                    TotalGasto = g.Sum(p => p.ValorTotal),
                    QuantidadePedidos = g.Count(),
                    UltimoPedido = g.Max(p => p.DataPedido)
                })
                .OrderByDescending(c => c.TotalGasto)
                .ToListAsync();

            ViewBag.DataInicio = dataInicio;
            ViewBag.DataFim = dataFim;
            return View(clientes);
        }
    }
}

