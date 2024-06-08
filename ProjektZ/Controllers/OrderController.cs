using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjektZ.Data;
using ProjektZ.Models;

namespace ProjektZ.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public OrderController(ApplicationDbContext context, ILogger<OrderController> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _context = context;
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching all orders.");
            var orders = await _context.Orders.ToListAsync();
            return View(orders);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            _logger.LogInformation("Creating a new order.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state detected.");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogError("Field: {Field}, Error: {Error}", state.Key, error.ErrorMessage);
                    }
                }
            }

            try
            {
                order.OrderDate = DateTime.Now;
                order.Status = "Pending";
                _context.Add(order);
                _logger.LogInformation("Order added to context. Saving changes...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order saved successfully.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the order.");
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrders()
        {
            _logger.LogInformation("Processing all pending orders.");

            bool hasPendingOrders;
            do
            {
                var orders = await _context.Orders.Where(o => o.Status == "Pending").ToListAsync();
                hasPendingOrders = orders.Any();

                var tasks = orders.Select(order => Task.Run(() => ProcessOrder(order))).ToArray();
                await Task.WhenAll(tasks);
            } while (hasPendingOrders);

            return RedirectToAction(nameof(Index));
        }

        private async Task ProcessOrder(Order order)
        {
            _logger.LogInformation("Processing order: {OrderId}", order.Id);
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                order.Status = "Processed";
                context.Update(order);
                await context.SaveChangesAsync();
                _logger.LogInformation("Order processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order: {OrderId}", order.Id);
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerName,Product,Quantity,OrderDate,Status")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
