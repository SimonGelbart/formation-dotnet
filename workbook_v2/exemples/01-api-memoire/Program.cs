using Catalogue;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IProductRepository, MemoryProductRepository>();
builder.Services.AddScoped<ProductService>();
var app = builder.Build();
app.UseExceptionHandler();
app.MapControllers();
app.Run();
