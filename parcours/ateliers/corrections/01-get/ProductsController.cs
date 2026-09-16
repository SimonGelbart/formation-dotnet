using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("products")]
public sealed class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new[] { new Product("Clavier", 30m) });
    }
}
