var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<ProductService>();
var app = builder.Build();
app.MapControllers();
app.Run();
