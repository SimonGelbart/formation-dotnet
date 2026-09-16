using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ProductService _service;
    public ProductsController(ProductService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await _service.GetByIdAsync(id, ct);
        if (product is null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct)
    {
        var product = await _service.CreateAsync(request.Name, request.Price, ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
