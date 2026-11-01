using ClassicERPMainV2Core.Models;
using Core_APIOracle.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ClassicERPMainV2Core.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DatabaseContext _context;
        public HomeController(ILogger<HomeController> logger, DatabaseContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var result = await _context.Products.ToListAsync();
            return Ok(result);
        }

        [HttpGet("GetModule")]
        public async Task<IActionResult> GetModule()
        {
            var result = await _context.Modules.ToListAsync();
            return Ok(result);
        }

        [HttpGet("GetProductGroup")]
        public async Task<IActionResult> GetProductGroup()
        {
            var result = _context.Products.GroupBy(x => x.ProductId).SelectMany(x => x);
            var asas = _context.Products.GroupBy(x => x.ProductId).Select(x => x);
            return Ok(asas);
        }

        [HttpPost]
        public async Task<IActionResult> Post(ProductsInfo productsInfo)
        {
            var result = await _context.Products.AddAsync(productsInfo);
            await _context.SaveChangesAsync();
            return Ok(result.Entity);
        }

        public IActionResult Index()
        {
            return View();
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
}
