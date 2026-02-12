using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVendasDoces.Data;
using SistemaVendasDoces.Models;

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
