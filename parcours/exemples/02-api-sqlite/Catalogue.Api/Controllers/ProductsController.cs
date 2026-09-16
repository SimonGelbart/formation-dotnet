using Microsoft.AspNetCore.Mvc;

namespace Catalogue;

[ApiController]
[Route("products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ProductService _service;
    public ProductsController(ProductService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok((await _service.GetAllAsync(ct)).Select(ProductResponse.From));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken ct)
    {
        var product = await _service.GetByIdAsync(id, ct);
        if (product is null) return NotFound();
        return Ok(ProductResponse.From(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken ct)
    {
        var product = await _service.CreateAsync(request.Name, request.Price, ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ProductResponse.From(product));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken ct)
    {
        if (!await _service.UpdateAsync(id, request.Name, request.Price, ct)) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!await _service.DeleteAsync(id, ct)) return NotFound();
        return NoContent();
    }
}
