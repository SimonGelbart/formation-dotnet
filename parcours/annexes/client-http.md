# Annexe — Client HTTP, configuration et logs

**Comprendre sur un exemple complet.** Cette annexe est indépendante du fil rouge. Elle appelle le catalogue mémoire déjà lancé sur 5080. Le catalogue SQLite et les commandes n'en dépendent pas.

## Créer une seconde application

Dans ton dossier de travail, hors des applications de référence :

```bash
dotnet new web -n Catalogue.Proxy --framework net10.0
cd Catalogue.Proxy
```

Le projet `web` fournit les références ASP.NET Core nécessaires. Ajoute les fichiers suivants. Ils utilisent tous le namespace `CatalogueProxy` sauf Program.cs, qui l'importe.

## `appsettings.json`

```json
{
  "Catalog": { "BaseUrl": "http://localhost:5080" }
}
```

## `Program.cs` — contenu complet

```csharp
using CatalogueProxy;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    var baseUrl = builder.Configuration["Catalog:BaseUrl"]
        ?? throw new InvalidOperationException("Adresse du catalogue absente.");
    client.BaseAddress = new Uri(baseUrl);
});
var app = builder.Build();
app.MapControllers();
app.Run();
```

## `CatalogClient.cs` — contenu complet

```csharp
using System.Net.Http.Json;

namespace CatalogueProxy;

public record ProductResponse(Guid Id, string Name, decimal Price);

public sealed class CatalogClient
{
    private readonly HttpClient _http;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(HttpClient http, ILogger<CatalogClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<ProductResponse>> GetAllAsync(CancellationToken ct)
    {
        var products = await _http.GetFromJsonAsync<List<ProductResponse>>("/products", ct)
            ?? new List<ProductResponse>();
        _logger.LogInformation("Catalogue reçu : {Count} produits", products.Count);
        return products;
    }
}
```

## `CatalogController.cs` — contenu complet

```csharp
using Microsoft.AspNetCore.Mvc;

namespace CatalogueProxy;

[ApiController]
[Route("catalog-copy")]
public sealed class CatalogController : ControllerBase
{
    private readonly CatalogClient _catalog;
    public CatalogController(CatalogClient catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            return Ok(await _catalog.GetAllAsync(ct));
        }
        catch (HttpRequestException)
        {
            return Problem(statusCode: 502, title: "Catalogue distant indisponible");
        }
    }
}
```

## Observer

Laisse l'API mémoire sur 5080 et lance ce proxy sur un autre port :

```bash
dotnet run --urls http://localhost:5081
```

GET `http://localhost:5081/catalog-copy` retourne les produits et écrit un log avec leur nombre. Arrête l'API mémoire puis recommence : le client observe 502.

`AddHttpClient` utilise la factory pour gérer les clients et leurs connexions. Le client reçoit sa configuration depuis l'extérieur. `{Count}` reste une propriété structurée du log : ce n'est pas une simple concaténation de texte.

Les retries, timeouts personnalisés et politiques de résilience sont hors de cet exemple. Ne le transforme pas en système complet avant d'avoir compris le trajet d'un appel.
