using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("products")]
public sealed class ProductsController : ControllerBase
{
    private readonly List<Product> _products;
    public ProductsController(List<Product> products) => _products = products;

    [HttpGet]
    public IActionResult GetAll() => Ok(_products);

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product is null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public IActionResult Create(ProductRequest request)
    {
        var product = new Product(request.Name, request.Price);
        _products.Add(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
