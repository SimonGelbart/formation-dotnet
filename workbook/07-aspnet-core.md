# 7 — Construire une API ASP.NET Core

## Objectifs

Ce chapitre est volontairement construit comme un **parcours continu** : on part d'un endpoint simple puis on enrichit progressivement la même API.

À la fin, tu dois savoir :

- suivre le trajet d'une requête HTTP ;
- comprendre `Program.cs`, routing et middleware ;
- créer des endpoints avec Controllers ;
- distinguer DTO HTTP, domaine et persistence ;
- comprendre model binding et `[ApiController]` ;
- valider une entrée ;
- choisir des codes HTTP cohérents ;
- utiliser `ProblemDetails` pour les erreurs ;
- utiliser DI et observer les lifetimes dans une vraie requête ;
- utiliser configuration, Options et logging ;
- utiliser `HttpClient` via `IHttpClientFactory` ;
- comprendre CORS ;
- exposer le contrat avec OpenAPI.

---

# Étape 1 — Faire circuler une première requête

Garde cette représentation mentale :

```text
Client
  ↓
HTTP request
  ↓
Middleware
  ↓
Routing
  ↓
Controller / Endpoint
  ↓
Application service
  ↓
Repository
  ↓
Database
```

Puis la réponse remonte vers le client.

Toutes les applications n'ont pas exactement ces couches. Ce schéma sert à **situer les responsabilités**, pas à imposer une architecture universelle.

---

## 1. `Program.cs`

Version minimale avec Controllers :

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
```

Deux phases importantes :

```text
builder.Services...
→ préparer les services / le graphe de dépendances

app.Use... / app.Map...
→ construire le pipeline HTTP
```

---

## 2. Premier Controller

```csharp
[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        return Ok(new { id });
    }
}
```

Teste :

```text
GET /orders/{guid}
```

Le segment `:guid` est une contrainte de route.

### Minimal APIs

Tu rencontreras aussi :

```csharp
app.MapGet("/orders/{id:guid}", (Guid id) => Results.Ok(new { id }));
```

Le workbook utilise surtout les Controllers pour rendre les responsabilités visibles, mais les deux styles reposent sur les mêmes fondations ASP.NET Core.

---

# Étape 2 — Comprendre le pipeline middleware

Un middleware peut agir avant et après la suite du pipeline :

```text
Request
  ↓
Middleware A
  ↓
Middleware B
  ↓
Endpoint
  ↑
Middleware B
  ↑
Middleware A
  ↑
Response
```

Exercice :

```csharp
app.Use(async (context, next) =>
{
    var started = Stopwatch.StartNew();

    await next();

    Console.WriteLine(
        $"{context.Request.Path}: {started.ElapsedMilliseconds} ms");
});
```

Identifie la partie exécutée avant l'endpoint puis celle exécutée après.

L'ordre des middlewares peut être important.

---

# Étape 3 — Injecter le service au lieu de le construire

Enregistrement :

```csharp
builder.Services.AddScoped<OrderService>();
```

Utilisation :

```csharp
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }
}
```

Le Controller n'a pas besoin de connaître la construction du service.

### Observer enfin les lifetimes du chapitre 3

Créer :

```csharp
public sealed class InstanceId
{
    public Guid Id { get; } = Guid.NewGuid();
}
```

Teste successivement :

```csharp
AddTransient<InstanceId>()
AddScoped<InstanceId>()
AddSingleton<InstanceId>()
```

Expose temporairement deux résolutions pendant une même requête et compare les GUID, puis répète sur une nouvelle requête.

À ce stade, `Scoped = même instance dans une requête` devient un comportement observé plutôt qu'une définition abstraite.

---

# Étape 4 — DTOs et model binding

Ne retourne pas directement le modèle métier par réflexe.

```csharp
public record CreateOrderRequest(Guid CustomerId);

public record OrderResponse(
    Guid Id,
    decimal Total,
    OrderStatus Status);
```

Pourquoi séparer DTO et domaine ?

```text
contrat HTTP
≠ forcément
modèle métier
≠ forcément
modèle de persistence
```

Ces trois représentations peuvent évoluer pour des raisons différentes.

### Model binding

```csharp
[HttpGet("{id:guid}")]
public async Task<ActionResult<OrderResponse>> GetById(
    Guid id,
    CancellationToken cancellationToken)
{
    ...
}
```

ASP.NET Core peut obtenir des valeurs depuis :

- route ;
- query string ;
- headers ;
- body JSON ;
- services DI dans certains contextes ;
- token d'annulation de la requête.

Tu peux préciser la source :

```csharp
public IActionResult Search([FromQuery] string? text)
```

```csharp
public IActionResult Create([FromBody] CreateOrderRequest request)
```

---

## 5. `CancellationToken` côté HTTP

Le token reçu par une action correspond à l'annulation de la requête HTTP.

```csharp
public async Task<IActionResult> Get(
    Guid id,
    CancellationToken cancellationToken)
```

Il est lié à la même idée que :

```csharp
HttpContext.RequestAborted
```

Propage-le :

```text
Controller
  ↓
Service
  ↓
Repository
  ↓
EF Core / HttpClient
```

---

# Étape 5 — Ajouter une création correcte

```csharp
[HttpPost]
public async Task<ActionResult<OrderResponse>> Create(
    CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    var order = await _orderService.CreateAsync(
        request.CustomerId,
        cancellationToken);

    var response = ToResponse(order);

    return CreatedAtAction(
        nameof(GetById),
        new { id = order.Id },
        response);
}
```

`CreatedAtAction` permet de produire un `201 Created` et une localisation permettant de relire la ressource.

Codes utiles :

```text
200 OK
201 Created
204 No Content
400 Bad Request
404 Not Found
409 Conflict
500 Internal Server Error
```

Le code HTTP fait partie du contrat de l'API.

---

# Étape 6 — Ajouter de la validation d'entrée

```csharp
public class AddOrderItemRequest
{
    public Guid ProductId { get; init; }

    [Range(1, 100)]
    public int Quantity { get; init; }
}
```

Avec `[ApiController]`, une entrée invalide déclenche normalement une réponse `400` avant l'exécution normale de l'action.

### Validation d'entrée ≠ invariant métier

```text
Quantity = 0 dans le JSON
→ validation du contrat HTTP

Confirmer une commande vide
→ règle métier
```

N'essaie pas de déplacer toutes les règles métier dans des DataAnnotations.

### Exercice

Envoie une quantité `0` avant d'écrire un `if` manuel dans le Controller. Observe la réponse du framework.

---

# Étape 7 — Gérer les erreurs globalement

Évite de répéter :

```csharp
try
{
    ...
}
catch (Exception)
{
    return StatusCode(500);
}
```

Configuration simple :

```csharp
builder.Services.AddProblemDetails();
```

puis :

```csharp
app.UseExceptionHandler();
```

Pense les cas séparément :

```text
entrée invalide
→ 400

ressource absente
→ 404

conflit avec l'état courant
→ 409 selon le contrat choisi

exception inattendue
→ gestion transversale / 500
```

`ProblemDetails` donne une structure standardisée aux erreurs HTTP.

---

# Étape 8 — Configuration et Options

`appsettings.json` :

```json
{
  "ExternalApi": {
    "BaseUrl": "https://example.test"
  }
}
```

Type d'options :

```csharp
public sealed class ExternalApiOptions
{
    public const string SectionName = "ExternalApi";

    [Required]
    public string? BaseUrl { get; init; }
}
```

Enregistrement avec validation :

```csharp
builder.Services
    .AddOptions<ExternalApiOptions>()
    .Bind(builder.Configuration.GetSection(
        ExternalApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Cela apporte une nuance importante :

```text
required en C#
→ contrat d'initialisation au moment où le code construit l'objet

validation Options
→ vérifier réellement la configuration bindée au runtime
```

Utilisation :

```csharp
public CatalogClient(IOptions<ExternalApiOptions> options)
{
    _options = options.Value;
}
```

### Secrets

Tu rencontreras :

```text
appsettings.json
appsettings.Development.json
variables d'environnement
user secrets en développement
```

Ne versionne pas de vrais mots de passe, tokens ou secrets.

---

# Étape 9 — Logging structuré

```csharp
_logger.LogInformation(
    "Creating order for customer {CustomerId}",
    customerId);
```

Préférer cela à :

```csharp
Console.WriteLine("Creating order " + customerId);
```

`CustomerId` reste une propriété structurée du log.

Ne logue pas arbitrairement :

- mots de passe ;
- tokens ;
- secrets ;
- données personnelles sensibles.

Évite aussi de journaliser la même exception à tous les étages.

---

# Étape 10 — Appeler une API externe

Parallèle front-end :

```text
fetch / Axios  ↔  HttpClient
```

Évite de disperser partout la construction de clients HTTP.

Client typé :

```csharp
public sealed class CatalogClient
{
    private readonly HttpClient _httpClient;

    public CatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _httpClient.GetAsync(
            $"/products/{id}",
            cancellationToken);
    }
}
```

Enregistrement :

```csharp
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https://example.test");
});
```

`IHttpClientFactory` aide à centraliser configuration et gestion des handlers/connexions.

---

# Étape 11 — Brancher un front et rencontrer CORS

```text
Front : http://localhost:5173
API   : https://localhost:7001
```

Ce sont deux origins différentes. Un navigateur applique alors la Same-Origin Policy et peut demander à l'API d'autoriser explicitement le front.

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

```csharp
app.UseCors("frontend");
```

### À ne pas retenir

> « CORS empêche n'importe quel client externe d'appeler l'API. »

`curl`, un serveur backend ou un script ne sont pas soumis à la politique du navigateur de la même façon. CORS n'est donc pas un mécanisme d'autorisation métier.

---

# Étape 12 — Rendre le contrat visible avec OpenAPI

Avec le support OpenAPI ASP.NET Core :

```csharp
builder.Services.AddOpenApi();
```

Puis en développement :

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

Le document est alors exposé par défaut sous une route du type :

```text
/openapi/v1.json
```

Utilise-le pour inspecter :

- routes ;
- verbes HTTP ;
- schémas JSON ;
- paramètres ;
- réponses documentées.

Une UI interactive peut être ajoutée séparément si le projet en a besoin ; le document OpenAPI lui-même reste le contrat généré.

---

## Application au projet fil rouge

À ce stade, expose au minimum :

```text
GET  /products
GET  /products/{id}
POST /orders
POST /orders/{id}/items
GET  /orders/{id}
GET  /orders
POST /orders/{id}/confirm
```

Progression recommandée :

```text
GET simple
  ↓
injection du service
  ↓
DTO + binding
  ↓
POST + CreatedAtAction
  ↓
validation
  ↓
ProblemDetails
  ↓
configuration / logging
  ↓
HttpClient
  ↓
front + CORS
  ↓
OpenAPI
```

### Checkpoint final

Tu dois pouvoir expliquer :

1. différence middleware / endpoint ;
2. rôle de `[ApiController]` ;
3. différence validation d'entrée / invariant métier ;
4. pourquoi `CreatedAtAction` est utile ;
5. comment `CancellationToken` voyage vers les I/O ;
6. rôle de `ProblemDetails` ;
7. différence entre `required` et validation runtime des Options ;
8. pourquoi le logging structuré est utile ;
9. ce que résout `IHttpClientFactory` ;
10. pourquoi CORS concerne particulièrement les navigateurs ;
11. comment OpenAPI rend le contrat inspectable.
