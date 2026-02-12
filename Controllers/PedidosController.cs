using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaVendasDoces.Data;
using SistemaVendasDoces.Models;

namespace SistemaVendasDoces.Controllers
{
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Pedidos
        public async Task<IActionResult> Index(string? status)
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusPedido>(status, out var statusEnum))
            {
                pedidos = pedidos.Where(p => p.Status == statusEnum);
            }

            return View(await pedidos.OrderByDescending(p => p.DataPedido).ToListAsync());
        }

        // GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pedido== null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        // GET: Pedidos/Create
        public IActionResult Create()
        {
            ViewData["Clientes"] = new SelectList(_context.Clientes.Where(c => c.Ativo), "Id", "Nome");
            ViewData["Produtos"] = _context.Produtos.Where(p => p.Ativo).ToList();
            return View();
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pedido pedido, int[] produtoIds, int[] quantidades)
        {
            if (produtoIds == null || produtoIds.Length == 0)
            {
                ModelState.AddModelError("", "Selecione pelo menos um produto");
                ViewData["Clientes"] = new SelectList(_context.Clientes.Where(c => c.Ativo), "Id", "Nome", pedido.ClienteId);
                ViewData["Produtos"] = _context.Produtos.Where(p => p.Ativo).ToList();
                return View(pedido);
            }

            pedido.DataPedido = DateTime.Now;
            pedido.Status = StatusPedido.Pendente;
            pedido.StatusPagamento = StatusPagamento.Pendente;
            pedido.ValorPago = 0;

            var itens = new List<ItemPedido>();
            decimal valorTotal = 0;

            for (int i = 0; i < produtoIds.Length; i++)
            {
                var produto = await _context.Produtos.FindAsync(produtoIds[i]);
                if (produto != null && quantidades[i] > 0)
                {
                    decimal subtotal;

                    // Promoção Pão de Mel: 2 por R$15, 3 por R$23
                    if (produto.Nome.Contains("Pão de Mel", StringComparison.OrdinalIgnoreCase) && quantidades[i] >= 2)
                    {
                        subtotal = CalcularPromoPaoDeMel(quantidades[i], produto.PrecoVenda);
                    }
                    else
                    {
                        subtotal = produto.PrecoVenda * quantidades[i];
                    }

                    itens.Add(new ItemPedido
                    {
                        ProdutoId = produtoIds[i],
                        Quantidade = quantidades[i],
                        PrecoUnitario = produto.PrecoVenda,
                        Subtotal = subtotal
                    });
                    valorTotal += subtotal;
                }
            }

            pedido.ValorTotal = valorTotal;
            pedido.Itens = itens;

            _context.Add(pedido);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            ViewData["StatusList"] = new SelectList(Enum.GetValues(typeof(StatusPedido)));
            ViewData["StatusPagamentoList"] = new SelectList(Enum.GetValues(typeof(StatusPagamento)));
            return View(pedido);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pedido pedido, decimal? novoPagamento)
        {
            if (id != pedido.Id)
            {
                return NotFound();
            }

            try
            {
                var pedidoExistente = await _context.Pedidos
                    .Include(p => p.Itens)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pedidoExistente == null)
                {
                    return NotFound();
                }

                var statusAnterior = pedidoExistente.Status;

                pedidoExistente.Status = pedido.Status;
                pedidoExistente.Observacoes = pedido.Observacoes;
                pedidoExistente.FormaPagamento = pedido.FormaPagamento;

                // Registrar novo pagamento
                if (novoPagamento.HasValue && novoPagamento.Value > 0)
                {
                    pedidoExistente.ValorPago += novoPagamento.Value;
                    
                    // Atualizar status de pagamento
                    if (pedidoExistente.ValorPago >= pedidoExistente.ValorTotal)
                    {
                        pedidoExistente.StatusPagamento = StatusPagamento.Pago;
                    }
                    else if (pedidoExistente.ValorPago > 0)
                    {
                        pedidoExistente.StatusPagamento = StatusPagamento.Parcial;
                    }
                }

                // Se marcar como entregue (e antes NÃO era entregue), criar venda e atualizar estoque
                if (pedido.Status == StatusPedido.Entregue && statusAnterior != StatusPedido.Entregue)
                {
                    pedidoExistente.DataEntrega = DateTime.Now;
                    
                    // Criar venda automática
                    var venda = new Venda
                    {
                        ClienteId = pedidoExistente.ClienteId,
                        DataVenda = DateTime.Now,
                        ValorTotal = pedidoExistente.ValorTotal,
                        FormaPagamento = pedidoExistente.FormaPagamento ?? "Não informado",
                        Observacoes = $"Pedido #{pedidoExistente.Id}"
                    };

                    var itensVenda = pedidoExistente.Itens?.Select(item => new ItemVenda
                    {
                        Venda = venda,
                        ProdutoId = item.ProdutoId,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = item.PrecoUnitario,
                        Subtotal = item.Subtotal
                    }).ToList();

                    venda.Itens = itensVenda;
                    _context.Vendas.Add(venda);

                    // Atualizar estoque
                    foreach (var item in pedidoExistente.Itens ?? Enumerable.Empty<ItemPedido>())
                    {
                        var produto = await _context.Produtos.FindAsync(item.ProdutoId);
                        if (produto != null)
                        {
                            produto.QuantidadeEstoque -= item.Quantidade;
                        }
                    }

                    TempData["Sucesso"] = $"✅ Pedido #{pedidoExistente.Id} entregue! Venda registrada automaticamente.";
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PedidoExists(pedido.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Details), new { id = pedido.Id });
        }

        // POST: Pedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                pedido.Status = StatusPedido.Cancelado;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
        }

        private decimal CalcularPromoPaoDeMel(int quantidade, decimal precoUnitario)
        {
            if (quantidade <= 0) return 0;
            if (quantidade == 1) return precoUnitario;

            // Opção 1: pacotes de 3 (R$23) + pacotes de 2 (R$15) + unidades soltas
            int restante1 = quantidade;
            decimal total1 = 0;
            int pacotes3 = restante1 / 3;
            restante1 -= pacotes3 * 3;
            total1 += pacotes3 * 23m;
            int pacotes2a = restante1 / 2;
            restante1 -= pacotes2a * 2;
            total1 += pacotes2a * 15m;
            total1 += restante1 * precoUnitario;

            // Opção 2: só pacotes de 2 (R$15) + unidades soltas
            int restante2 = quantidade;
            decimal total2 = 0;
            int pacotes2b = restante2 / 2;
            restante2 -= pacotes2b * 2;
            total2 += pacotes2b * 15m;
            total2 += restante2 * precoUnitario;

            return Math.Min(total1, total2);
        }
    }
}
