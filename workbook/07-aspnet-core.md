# 7 — Construire une API ASP.NET Core

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- suivre le trajet d'une requête HTTP dans une application ASP.NET Core ;
- comprendre `Program.cs`, le routing et le pipeline middleware ;
- créer des endpoints avec Controllers ou Minimal APIs ;
- distinguer modèle métier et DTO HTTP ;
- comprendre le model binding et le rôle de `[ApiController]` ;
- mettre en place une validation d'entrée simple ;
- choisir des codes HTTP cohérents ;
- produire des erreurs HTTP structurées avec `ProblemDetails` ;
- utiliser le conteneur de DI .NET dans une API ;
- utiliser configuration, Options pattern et logging ;
- utiliser `HttpClient` via `IHttpClientFactory` ou un client typé ;
- comprendre pourquoi CORS existe ;
- exposer/documenter l'API avec OpenAPI pendant le développement.

---

## 1. Le trajet d'une requête

Avant de mémoriser des classes ASP.NET Core, garde cette représentation mentale :

```text
Client
  │
  ▼
HTTP request
  │
  ▼
Middleware
  │
  ▼
Routing
  │
  ▼
Controller / Endpoint
  │
  ▼
Service
  │
  ▼
Repository
  │
  ▼
Database
```

Puis la réponse remonte :

```text
Database
   ↑
Service
   ↑
Controller
   ↑
JSON / HTTP response
```

Toutes les applications n'ont pas exactement ces couches, mais ce schéma permet de situer les responsabilités.

### Question à garder en tête

À chaque nouvelle classe, demande-toi :

> est-ce une responsabilité HTTP, applicative, métier ou infrastructure ?

Cela évite de tout placer dans le Controller simplement parce qu'il est le premier code visible lors d'une requête.

---

## 2. `Program.cs`

Une application ASP.NET Core moderne démarre généralement dans `Program.cs`.

Version simplifiée :

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
```

Il faut distinguer deux grandes phases.

### Enregistrement des services

```csharp
builder.Services.AddControllers();
builder.Services.AddScoped<OrderService>();
```

On prépare les composants dont l'application aura besoin.

### Construction du pipeline HTTP

```csharp
app.UseExceptionHandler();
app.MapControllers();
```

On décrit comment les requêtes vont traverser l'application.

---

## 3. Middleware

Un middleware peut observer ou traiter une requête avant et/ou après le composant suivant.

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

Des fonctionnalités transversales utilisent ce principe :

- gestion d'erreur ;
- logging ;
- CORS ;
- authentification plus tard ;
- fichiers statiques ;
- redirections.

L'ordre des middlewares peut donc avoir de l'importance.

### Petit exercice

Imagine un middleware de chronométrage :

```csharp
app.Use(async (context, next) =>
{
    var started = Stopwatch.StartNew();

    await next();

    Console.WriteLine($"{context.Request.Path}: {started.ElapsedMilliseconds} ms");
});
```

Identifie ce qui s'exécute **avant** l'endpoint et ce qui s'exécute **après**.

---

## 4. Routing et endpoints

Avec un Controller :

```csharp
[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        ...
    }
}
```

La route :

```text
GET /orders/{id}
```

est reliée à cette méthode.

Le segment `:guid` est une contrainte de route : une valeur qui ne ressemble pas à un GUID ne correspond pas à cette route de la même manière qu'un GUID valide.

### Minimal API

Une alternative plus légère :

```csharp
app.MapGet("/orders/{id:guid}", (Guid id) =>
{
    ...
});
```

Le workbook privilégie les Controllers pour rendre les responsabilités visibles, mais il faut savoir que les deux approches existent et utilisent les mêmes grands mécanismes ASP.NET Core : routing, DI, binding, réponses HTTP.

---

## 5. DTOs : ne pas confondre HTTP, domaine et base

Supposons une requête de création :

```json
{
  "customerId": "2cc8f1b7-8c7b-4c08-9d16-96506df2d86d"
}
```

On peut représenter l'entrée avec :

```csharp
public record CreateOrderRequest(Guid CustomerId);
```

Puis la réponse avec un autre type :

```csharp
public record OrderResponse(
    Guid Id,
    decimal Total,
    OrderStatus Status);
```

### Pourquoi ne pas utiliser directement l'entité `Order` ?

Parce que le contrat HTTP, le modèle métier et le modèle de persistance peuvent évoluer pour des raisons différentes.

Exemples :

- certaines propriétés internes ne doivent pas être exposées ;
- le client peut envoyer une forme différente du modèle métier ;
- une colonne de DB n'a pas forcément de sens dans l'API ;
- le contrat HTTP doit parfois rester stable alors que l'interne change.

### Parallèle front-end

Un DTO HTTP joue un rôle proche d'un type décrivant précisément le contrat d'une API côté TypeScript. Il n'a pas besoin de reproduire toutes les propriétés du modèle serveur.

---

## 6. Model binding

ASP.NET Core peut convertir automatiquement des informations HTTP en paramètres C#.

```csharp
[HttpGet("{id:guid}")]
public async Task<ActionResult<OrderResponse>> GetById(
    Guid id,
    CancellationToken cancellationToken)
{
    ...
}
```

Le framework peut obtenir des valeurs depuis :

- la route ;
- la query string ;
- les headers ;
- le body JSON ;
- les services DI dans certains contextes ;
- le token d'annulation de la requête.

On peut rendre la source explicite lorsque cela aide :

```csharp
public IActionResult Search([FromQuery] string? text)
```

ou :

```csharp
public IActionResult Create([FromBody] CreateOrderRequest request)
```

Avec `[ApiController]`, plusieurs conventions de binding et de validation sont appliquées automatiquement.

---

## 7. `CancellationToken` et requête HTTP

Dans une action :

```csharp
public async Task<IActionResult> Get(
    Guid id,
    CancellationToken cancellationToken)
```

le token fourni par ASP.NET Core représente l'annulation de la requête, notamment lorsque le client abandonne la connexion.

Tu peux aussi rencontrer :

```csharp
HttpContext.RequestAborted
```

qui représente la même idée à la frontière HTTP.

Le bon réflexe est de **propager** le token :

```text
Controller
  ↓ token
Service
  ↓ token
Repository
  ↓ token
EF Core / HttpClient
```

---

## 8. Validation d'entrée avec DataAnnotations

Un DTO peut exprimer certaines contraintes d'entrée simples :

```csharp
public class AddOrderItemRequest
{
    public Guid ProductId { get; init; }

    [Range(1, 1000)]
    public int Quantity { get; init; }
}
```

Autre exemple :

```csharp
public class CreateProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; init; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Price { get; init; }
}
```

Avec un Controller marqué `[ApiController]`, un modèle invalide provoque normalement automatiquement une réponse client 400 avant l'exécution normale de l'action.

### Ne pas confondre validation d'entrée et invariant métier

```text
Quantity manquante / hors plage
→ problème du contrat d'entrée

Confirmer une commande vide
→ règle métier
```

Une DataAnnotation n'est pas le bon endroit pour toutes les règles du domaine.

---

## 9. Codes HTTP essentiels

Quelques cas fréquents :

```text
200 OK           lecture réussie
201 Created      ressource créée
204 No Content   succès sans body
400 Bad Request  requête invalide
404 Not Found    ressource absente
409 Conflict     conflit avec l'état courant
500 Internal Server Error erreur inattendue côté serveur
```

Le code HTTP fait partie du contrat de l'API.

### Lecture

```csharp
var order = await service.GetByIdAsync(id, cancellationToken);

if (order is null)
    return NotFound();

return Ok(ToResponse(order));
```

### Création avec localisation de la ressource

Pour une création réussie, on peut utiliser :

```csharp
return CreatedAtAction(
    nameof(GetById),
    new { id = order.Id },
    ToResponse(order));
```

Cela produit un `201 Created` et peut renseigner l'URL permettant de relire la ressource créée.

---

## 10. `ProblemDetails` et gestion globale des erreurs

Évite ceci dans chaque endpoint :

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

Les erreurs inattendues sont généralement mieux gérées à un niveau transversal.

ASP.NET Core dispose de mécanismes permettant de produire des réponses d'erreur structurées suivant le format **Problem Details**.

Une configuration peut commencer par :

```csharp
builder.Services.AddProblemDetails();
```

puis :

```csharp
app.UseExceptionHandler();
```

L'idée importante pour le workbook n'est pas de construire immédiatement un système d'erreurs sophistiqué, mais de séparer :

```text
exception inattendue
→ gestion transversale

ressource absente
→ résultat applicatif traduit en 404

entrée invalide
→ validation / 400

conflit métier
→ traduction cohérente, par exemple 409 selon le contrat
```

---

## 11. DI dans ASP.NET Core

Enregistrement :

```csharp
builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
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

Le Controller ne construit pas son service. Le conteneur fournit l'instance.

Rappelle-toi que le lifetime du repository mémoire est un cas pédagogique particulier : si sa collection constitue le stockage lui-même, il doit survivre entre les requêtes ou déléguer à un store qui survit entre les requêtes.

---

## 12. Configuration

Exemple `appsettings.json` :

```json
{
  "ExternalApi": {
    "BaseUrl": "https://example.test"
  }
}
```

On peut lire la configuration directement :

```csharp
builder.Configuration["ExternalApi:BaseUrl"]
```

mais pour des configurations structurées, l'Options pattern est souvent plus lisible.

```csharp
public class ExternalApiOptions
{
    public required string BaseUrl { get; init; }
}
```

Enregistrement :

```csharp
builder.Services.Configure<ExternalApiOptions>(
    builder.Configuration.GetSection("ExternalApi"));
```

Utilisation :

```csharp
public class CatalogClient
{
    private readonly ExternalApiOptions _options;

    public CatalogClient(IOptions<ExternalApiOptions> options)
    {
        _options = options.Value;
    }
}
```

### Environnements

Tu peux rencontrer :

```text
appsettings.json
appsettings.Development.json
variables d'environnement
user secrets en développement
```

Les sources de configuration peuvent se surcharger selon leur ordre et l'environnement.

### Règle importante

Ne versionne pas des mots de passe, tokens ou secrets réels dans `appsettings.json` du dépôt.

---

## 13. Logging structuré

Injecter :

```csharp
ILogger<OrderService>
```

Puis :

```csharp
_logger.LogInformation(
    "Creating order for customer {CustomerId}",
    customerId);
```

Ce logging structuré est préférable à :

```csharp
Console.WriteLine("Creating order " + customerId);
```

La valeur `CustomerId` reste une propriété structurée du message plutôt qu'une simple concaténation de texte.

### Attention

Ne logue pas arbitrairement :

- mots de passe ;
- tokens ;
- secrets ;
- données personnelles sensibles.

Et évite de logger la même exception à tous les niveaux de la pile, ce qui peut créer plusieurs événements identiques pour une seule erreur.

---

## 14. Appels HTTP sortants

Parallèle front-end :

```text
fetch / Axios  ↔  HttpClient
```

Évite de disperser partout :

```csharp
new HttpClient()
```

Dans une application ASP.NET Core, on utilise souvent `IHttpClientFactory`.

### Client nommé ou simple

```csharp
builder.Services.AddHttpClient("catalog", client =>
{
    client.BaseAddress = new Uri("https://example.test");
});
```

### Client typé

```csharp
public class CatalogClient
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
        return _httpClient.GetAsync($"/products/{id}", cancellationToken);
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

Le client typé regroupe naturellement la configuration et les appels vers une API particulière.

---

## 15. CORS : pourquoi le front peut être bloqué alors que l'API fonctionne

Un développeur front-end rencontrera rapidement CORS.

Situation :

```text
Front : http://localhost:5173
API   : https://localhost:7001
```

Ces URLs n'ont pas la même **origin**. Le navigateur applique alors les règles Same-Origin Policy et peut exiger que le serveur autorise explicitement l'origine du front.

CORS est donc principalement une politique appliquée par les navigateurs aux requêtes cross-origin.

Exemple de configuration de développement :

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

Puis :

```csharp
app.UseCors("frontend");
```

### À ne pas retenir

> « CORS sécurise l'API contre tous les clients externes ».

Un serveur ou un outil comme `curl` n'est pas soumis à la politique du navigateur de la même manière. L'autorisation métier de l'API ne doit donc jamais reposer uniquement sur CORS.

---

## 16. OpenAPI : rendre le contrat visible

Une API autonome est beaucoup plus simple à apprendre lorsqu'on peut voir et tester son contrat.

ASP.NET Core moderne sait générer un document OpenAPI avec les services et packages adaptés au template utilisé.

L'objectif pédagogique est de pouvoir inspecter :

- les routes ;
- les verbes HTTP ;
- les schémas JSON ;
- les codes de réponse attendus.

Quand tu ajoutes un endpoint, vérifie que sa représentation OpenAPI correspond réellement au contrat que tu voulais exposer.

Selon le projet, une interface interactive de type Swagger UI peut aussi être ajoutée en développement.

---

## Exercice — premier endpoint complet

Créer :

```text
GET /orders/{id}
```

Contraintes :

- utiliser `OrderService` injecté ;
- transmettre le `CancellationToken` ;
- retourner `404` si absent ;
- retourner `200` avec un `OrderResponse` sinon ;
- ne pas exposer directement l'entité de persistance ;
- vérifier le contrat dans OpenAPI.

Puis créer :

```text
POST /orders
```

et retourner `CreatedAtAction` vers `GET /orders/{id}`.

---

## Exercice — validation

Créer :

```text
POST /orders/{id}/items
```

avec :

```csharp
public class AddOrderItemRequest
{
    public Guid ProductId { get; init; }

    [Range(1, 100)]
    public int Quantity { get; init; }
}
```

Tester avec une quantité `0` et observer le comportement de `[ApiController]` avant d'ajouter du code manuel dans le Controller.

---

## Application au projet fil rouge

À ce stade, l'application doit exposer au minimum :

```text
GET  /products
GET  /products/{id}
POST /orders
POST /orders/{id}/items
GET  /orders/{id}
GET  /orders
POST /orders/{id}/confirm
```

La persistance peut encore être en mémoire. Le but est d'abord de maîtriser le flux :

```text
HTTP
 ↓
Controller
 ↓
Application service
 ↓
Repository / Catalog
```

### Checkpoint final

Tu dois pouvoir expliquer :

1. différence entre middleware et endpoint ;
2. rôle de `[ApiController]` ;
3. différence entre validation d'entrée et invariant métier ;
4. pourquoi `CreatedAtAction` est utile lors d'un POST ;
5. comment `CancellationToken` voyage de HTTP vers EF Core / HttpClient ;
6. pourquoi utiliser `ProblemDetails` pour structurer les erreurs ;
7. pourquoi le logging structuré est préférable à une concaténation ;
8. ce que résout `IHttpClientFactory` ;
9. pourquoi CORS concerne particulièrement les navigateurs ;
10. pourquoi OpenAPI aide à vérifier le contrat de l'API.
