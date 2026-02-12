using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVendasDoces.Data;
using SistemaVendasDoces.Models;

namespace SistemaVendasDoces.Controllers
{
    public class FinanceiroController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinanceiroController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Financeiro
        public async Task<IActionResult> Index()
        {
            var totalAReceber = await _context.Pedidos
                .Where(p => p.StatusPagamento != StatusPagamento.Pago && p.Status != StatusPedido.Cancelado)
                .SumAsync(p => p.ValorTotal - p.ValorPago);

            var recebidoMes = await _context.Pedidos
                .Where(p => p.DataPedido.Month == DateTime.Now.Month && 
                           p.DataPedido.Year == DateTime.Now.Year)
                .SumAsync(p => p.ValorPago);

            var pedidosPendentes = await _context.Pedidos
                .Where(p => p.StatusPagamento != StatusPagamento.Pago && p.Status != StatusPedido.Cancelado)
                .Include(p => p.Cliente)
                .OrderBy(p => p.DataPedido)
                .ToListAsync();

            ViewBag.TotalAReceber = totalAReceber;
            ViewBag.RecebidoMes = recebidoMes;
            ViewBag.PedidosPendentes = pedidosPendentes;

            return View();
        }

        // GET: Financeiro/Devedores
        public async Task<IActionResult> Devedores()
        {
            var devedores = await _context.Pedidos
                .Where(p => (p.ValorTotal - p.ValorPago) > 0 && p.Status != StatusPedido.Cancelado)
                .Include(p => p.Cliente)
                .GroupBy(p => p.ClienteId)
                .Select(g => new SistemaVendasDoces.Models.ViewModels.DevedorViewModel
                {
                    ClienteId = g.Key,
                    Cliente = g.First().Cliente,
                    TotalDevendo = g.Sum(p => p.ValorTotal - p.ValorPago),
                    QuantidadePedidos = g.Count(),
                    Pedidos = g.ToList()
                })
                .OrderByDescending(d => d.TotalDevendo)
                .ToListAsync();

            return View(devedores);
        }
    }
}
