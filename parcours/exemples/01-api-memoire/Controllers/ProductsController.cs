using Microsoft.AspNetCore.Mvc;

namespace Catalogue;

[ApiController]
[Route("products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ProductService _service;
    public ProductsController(ProductService service) => _service = service;

    [HttpGet]
    public IActionResult GetAll()
        => Ok(_service.GetAll().Select(ProductResponse.From));

    [HttpGet("{id:guid}")]
    public ActionResult<ProductResponse> GetById(Guid id)
    {
        var product = _service.GetById(id);
        if (product is null) return NotFound();
        return Ok(ProductResponse.From(product));
    }

    [HttpPost]
    public ActionResult<ProductResponse> Create(ProductRequest request)
    {
        var product = _service.Create(request.Name, request.Price);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ProductResponse.From(product));
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, ProductRequest request)
    {
        if (!_service.Update(id, request.Name, request.Price)) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        if (!_service.Delete(id)) return NotFound();
        return NoContent();
    }
}
