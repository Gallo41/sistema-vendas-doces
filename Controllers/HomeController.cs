using Microsoft.AspNetCore.Mvc;
using SistemaVendasDoces.Data;
using System.Diagnostics;

namespace SistemaVendasDoces.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var dashboard = new Models.DashboardViewModel
            {
                TotalProdutos = _context.Produtos.Count(p => p.Ativo),
                TotalClientes = _context.Clientes.Count(c => c.Ativo),
                VendasMes = _context.Vendas
                    .Where(v => v.DataVenda.Month == DateTime.Now.Month && v.DataVenda.Year == DateTime.Now.Year)
                    .Sum(v => (decimal?)v.ValorTotal) ?? 0,
                ProdutosEmEstoque = _context.Produtos.Where(p => p.Ativo).Sum(p => p.QuantidadeEstoque)
            };
            
            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
