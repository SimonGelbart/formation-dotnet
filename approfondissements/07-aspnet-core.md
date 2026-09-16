# 7 — Approfondir ASP.NET Core

> **Prérequis conseillé :** [parcours principal — 05 API HTTP](../parcours/05-api-http.md).
>
> **Niveau :** À approfondir pour le pipeline et les erreurs · Nuance pour les lifetimes et CORS · Référence pour Options, HttpClientFactory et OpenAPI.

Le parcours principal montre comment construire et appeler une API. Ce chapitre répond plutôt à la question :

> **qu'est-ce qui se passe réellement entre la requête HTTP et la réponse ?**

Il n'est pas nécessaire de reconstruire une seconde API pour le lire.

## 1. Carte mentale d'une requête

```text
Client
  ↓
HTTP request
  ↓
Middleware
  ↓
Routing
  ↓
Model binding / validation
  ↓
Controller / Endpoint
  ↓
Service applicatif
  ↓
Infrastructure éventuelle
  ↓
HTTP response
```

Ce schéma situe des responsabilités. Il n'impose pas une architecture universelle.

## 2. `Program.cs` contient deux constructions différentes

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();

app.Run();
```

Lis-le en deux parties :

```text
builder.Services...
→ préparer le graphe de services

app.Use... / app.Map...
→ construire le pipeline HTTP
```

Confondre les deux conduit souvent à mal comprendre DI et middleware.

## 3. Middleware : avant **et** après l'endpoint

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

Exemple d'observation :

```csharp
app.Use(async (context, next) =>
{
    var started = Stopwatch.StartNew();

    await next();

    Console.WriteLine(
        $"{context.Request.Path}: {started.ElapsedMilliseconds} ms");
});
```

L'ordre des middlewares compte : certains ont besoin qu'un middleware précédent ait déjà enrichi le contexte.

## 4. Routing, binding et validation sont différents

Route :

```csharp
[HttpGet("{id:guid}")]
public IActionResult GetById(Guid id)
```

Le `:guid` participe au choix de la route.

Model binding : ASP.NET Core construit les paramètres depuis la route, la query string, les headers ou le corps JSON.

```csharp
public IActionResult Search([FromQuery] string? text)
```

Validation : avec `[ApiController]`, un DTO invalide peut produire un `400` avant l'exécution normale de l'action.

```csharp
public sealed class AddOrderItemRequest
{
    public Guid ProductId { get; init; }

    [Range(1, 100)]
    public int Quantity { get; init; }
}
```

### Nuance importante

```text
Quantity = 0 dans le JSON
→ validation du contrat HTTP

confirmer une commande vide
→ invariant métier
```

Les DataAnnotations ne remplacent pas le domaine.

## 5. DTO HTTP ≠ domaine ≠ persistence

```text
CreateOrderRequest
        ↓
   cas d'usage
        ↓
      Order
        ↓
     EF Core
```

Ces représentations peuvent évoluer pour des raisons différentes.

Évite de retourner directement une entité EF ou un objet métier uniquement parce que sa forme ressemble aujourd'hui au JSON attendu.

## 6. Codes HTTP : décrire le résultat du contrat

Repères fréquents :

```text
200 OK
201 Created
204 No Content
400 Bad Request
404 Not Found
409 Conflict
500 Internal Server Error
```

Pour une création :

```csharp
return CreatedAtAction(
    nameof(GetById),
    new { id = order.Id },
    response);
```

`201` et `Location` rendent le nouveau resource relisible par le client.

## 7. Erreurs inattendues et erreurs métier

Évite de répéter dans chaque Controller :

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

Configuration transversale :

```csharp
builder.Services.AddProblemDetails();
app.UseExceptionHandler();
```

Mais `ProblemDetails` ne décide pas à lui seul qu'une règle métier correspond à `409`.

Il faut toujours distinguer :

```text
entrée invalide        → 400
ressource absente      → 404
conflit métier         → 409 si le contrat le décide
exception inattendue   → 500 / gestion globale
```

## 8. DI et lifetimes dans une vraie requête

```text
Transient
→ nouvelle instance à chaque résolution

Scoped
→ même instance dans le scope
→ généralement une requête HTTP

Singleton
→ même instance pendant la vie de l'application
```

Expérience utile :

```csharp
public sealed class InstanceId
{
    public Guid Id { get; } = Guid.NewGuid();
}
```

Enregistre successivement ce type en transient, scoped et singleton, puis compare les GUID entre deux résolutions dans une requête et entre deux requêtes.

### Piège

Un singleton qui contient un état mutable partagé doit être conçu pour la concurrence. Le lifetime ne rend pas son contenu thread-safe.

## 9. `CancellationToken` : propager, pas seulement recevoir

Une action peut recevoir directement le token de la requête :

```csharp
public async Task<IActionResult> Get(
    Guid id,
    CancellationToken cancellationToken)
```

Le trajet attendu est :

```text
Controller
  ↓
Service
  ↓
EF Core / HttpClient
```

Recevoir un token et l'abandonner ensuite n'apporte presque rien.

## 10. Configuration et Options

Configuration :

```json
{
  "ExternalApi": {
    "BaseUrl": "https://example.test"
  }
}
```

Type :

```csharp
public sealed class ExternalApiOptions
{
    public const string SectionName = "ExternalApi";

    [Required]
    public string? BaseUrl { get; init; }
}
```

Validation au démarrage :

```csharp
builder.Services
    .AddOptions<ExternalApiOptions>()
    .Bind(builder.Configuration.GetSection(
        ExternalApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Nuance

```text
required en C#
→ contrat d'initialisation du code C#

validation Options
→ validation de la configuration réellement bindée au runtime
```

Ne versionne pas de vrais secrets. Utilise les mécanismes adaptés à l'environnement : variables d'environnement, user secrets en développement, gestionnaire de secrets en production.

## 11. Logging structuré

```csharp
_logger.LogInformation(
    "Creating order for customer {CustomerId}",
    customerId);
```

est préférable à :

```csharp
Console.WriteLine("Creating order " + customerId);
```

car `CustomerId` reste une propriété structurée exploitable par le système de logs.

Évite de logger des mots de passe, tokens ou secrets, et évite de journaliser la même exception à chaque couche.

## 12. `HttpClientFactory`

Dans une application longue durée, centralise la configuration des clients HTTP :

```csharp
public sealed class CatalogClient
{
    private readonly HttpClient _httpClient;

    public CatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
```

```csharp
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https://example.test");
});
```

`IHttpClientFactory` aide notamment à gérer la configuration et les handlers/connexions sans disperser la construction des clients.

## 13. CORS : une politique de navigateur, pas une autorisation métier

```text
Front : http://localhost:5173
API   : https://localhost:7001
```

Origines différentes : le navigateur applique la Same-Origin Policy.

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

app.UseCors("frontend");
```

`curl` ou un autre backend ne sont pas protégés par CORS de la même façon. CORS ne remplace donc jamais l'authentification ou l'autorisation.

## 14. OpenAPI rend le contrat inspectable

```csharp
builder.Services.AddOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

Le document permet d'inspecter routes, paramètres, schémas et réponses. Une interface interactive peut être ajoutée séparément si nécessaire.

## Quand revenir dans ce chapitre ?

Consulte-le quand tu te demandes :

- pourquoi un Controller n'est pas appelé ;
- d'où vient une valeur bindée ;
- pourquoi une validation retourne déjà `400` ;
- où gérer une erreur transversalement ;
- pourquoi deux services ont ou non la même instance ;
- comment propager l'annulation ;
- où mettre configuration, logging ou appel HTTP externe ;
- pourquoi un front est bloqué par CORS alors que `curl` fonctionne.

Pour construire l'API pas à pas, retourne au [parcours principal](../parcours/README.md).
