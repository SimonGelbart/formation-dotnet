using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddScoped<ProductService>();
var app = builder.Build();
app.MapControllers();
app.Run();
