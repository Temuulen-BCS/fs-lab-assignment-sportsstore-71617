using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;

namespace SportsStore.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly IStoreRepository repository;

        public ProductController(IStoreRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = repository.Products
                .Select(p => new ProductDto
                {
                    ProductID = p.ProductID ?? 0,
                    Name = p.Name ?? "",
                    Price = p.Price,
                    Category = p.Category ?? ""
                })
                .ToList();

            return Ok(products);
        }
    }

    public class ProductDto
    {
        public long ProductID { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
    }
}