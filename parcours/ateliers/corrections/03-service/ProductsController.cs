using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ProductService _service;
    public ProductsController(ProductService service) => _service = service;

    [HttpGet]
    public IActionResult GetAll() => Ok(_service.GetAll());

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var product = _service.GetById(id);
        if (product is null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public IActionResult Create(ProductRequest request)
    {
        var product = _service.Create(request.Name, request.Price);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
