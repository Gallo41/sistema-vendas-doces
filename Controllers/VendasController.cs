using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaVendasDoces.Data;
using SistemaVendasDoces.Models;

namespace SistemaVendasDoces.Controllers
{
    public class VendasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VendasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Vendas
        public async Task<IActionResult> Index()
        {
            var vendas = await _context.Vendas
                .Include(v => v.Cliente)
                .Include(v => v.Itens!)
                    .ThenInclude(i => i.Produto)
                .OrderByDescending(v => v.DataVenda)
                .ToListAsync();
            return View(vendas);
        }

        // GET: Vendas/Create
        public IActionResult Create()
        {
            ViewBag.Clientes = new SelectList(_context.Clientes.Where(c => c.Ativo), "Id", "Nome");
            ViewBag.Produtos = _context.Produtos.Where(p => p.Ativo && p.QuantidadeEstoque > 0).ToList();
            return View();
        }

        // POST: Vendas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int ClienteId,
            string FormaPagamento,
            string? Observacoes,
            int[] ProdutoIds,
            int[] Quantidades)
        {
            if (ProdutoIds == null || ProdutoIds.Length == 0)
            {
                TempData["Erro"] = "Adicione pelo menos um produto à venda!";
                ViewBag.Clientes = new SelectList(_context.Clientes.Where(c => c.Ativo), "Id", "Nome");
                ViewBag.Produtos = _context.Produtos.Where(p => p.Ativo && p.QuantidadeEstoque > 0).ToList();
                return View();
            }

            var venda = new Venda
            {
                ClienteId = ClienteId,
                FormaPagamento = FormaPagamento ?? "Dinheiro",
                Observacoes = Observacoes,
                DataVenda = DateTime.Now,
                Itens = new List<ItemVenda>()
            };

            decimal total = 0;

            for (int i = 0; i < ProdutoIds.Length; i++)
            {
                if (ProdutoIds[i] == 0 || Quantidades[i] <= 0) continue;

                var produto = await _context.Produtos.FindAsync(ProdutoIds[i]);
                if (produto == null) continue;

                // Verificar estoque
                if (produto.QuantidadeEstoque < Quantidades[i])
                {
                    TempData["Erro"] = $"Estoque insuficiente para {produto.Nome}! Disponível: {produto.QuantidadeEstoque}";
                    ViewBag.Clientes = new SelectList(_context.Clientes.Where(c => c.Ativo), "Id", "Nome");
                    ViewBag.Produtos = _context.Produtos.Where(p => p.Ativo && p.QuantidadeEstoque > 0).ToList();
                    return View();
                }

                var subtotal = produto.PrecoVenda * Quantidades[i];
                total += subtotal;

                venda.Itens.Add(new ItemVenda
                {
                    ProdutoId = ProdutoIds[i],
                    Quantidade = Quantidades[i],
                    PrecoUnitario = produto.PrecoVenda,
                    Subtotal = subtotal
                });

                // Descontar do estoque
                produto.QuantidadeEstoque -= Quantidades[i];
            }

            venda.ValorTotal = total;

            _context.Vendas.Add(venda);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"Venda #{venda.Id} registrada com sucesso! Total: R$ {total:N2}";
            return RedirectToAction(nameof(Index));
        }

        // GET: Vendas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var venda = await _context.Vendas
                .Include(v => v.Cliente)
                .Include(v => v.Itens!)
                    .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venda == null) return NotFound();

            return View(venda);
        }

        // POST: Vendas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venda = await _context.Vendas
                .Include(v => v.Itens!)
                    .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venda == null) return NotFound();

            // Devolver estoque
            if (venda.Itens != null)
            {
                foreach (var item in venda.Itens)
                {
                    if (item.Produto != null)
                    {
                        item.Produto.QuantidadeEstoque += item.Quantidade;
                    }
                }
            }

            _context.Vendas.Remove(venda);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Venda excluída e estoque devolvido com sucesso!";
            return RedirectToAction(nameof(Index));
        }
    }
}
