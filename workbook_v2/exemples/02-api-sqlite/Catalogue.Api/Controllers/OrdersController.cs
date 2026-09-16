using Microsoft.AspNetCore.Mvc;

namespace Catalogue;

[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _service;
    public OrdersController(OrderService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CancellationToken ct)
    {
        var order = await _service.CreateAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, OrderResponse.From(order));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _service.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        return Ok(OrderResponse.From(order));
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, AddItemRequest request, CancellationToken ct)
    {
        if (!await _service.AddItemAsync(id, request.ProductId, request.Quantity, ct))
            return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        if (!await _service.ConfirmAsync(id, ct)) return NotFound();
        return NoContent();
    }
}
