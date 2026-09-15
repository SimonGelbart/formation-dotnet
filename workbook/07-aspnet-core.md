# 7 — Construire une API ASP.NET Core

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- suivre le trajet d'une requête HTTP dans une application ASP.NET Core ;
- comprendre `Program.cs`, le routing et le pipeline middleware ;
- créer des endpoints avec Controllers ou Minimal APIs ;
- distinguer modèle métier et DTO HTTP ;
- comprendre le model binding et la validation ;
- choisir des codes HTTP cohérents ;
- utiliser le conteneur de DI .NET dans une API ;
- utiliser configuration, logging et `HttpClient`.

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

Il faut distinguer deux grandes phases :

### Enregistrement des services

```csharp
builder.Services.AddControllers();
builder.Services.AddScoped<IOrderService, OrderService>();
```

### Construction du pipeline HTTP

```csharp
app.UseExceptionHandler();
app.MapControllers();
```

---

## 3. Middleware

Un middleware peut observer ou traiter une requête avant et/ou après le composant suivant.

Représentation :

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

### Minimal API

Une alternative plus légère :

```csharp
app.MapGet("/orders/{id:guid}", (Guid id) =>
{
    ...
});
```

Le workbook privilégie les Controllers pour rendre les responsabilités visibles, mais il faut savoir que les deux approches existent.

---

## 5. DTOs : ne pas confondre HTTP, domaine et base

Supposons une requête de création :

```json
{
  "customerId": "...",
  "items": [
    { "productId": "...", "quantity": 2 }
  ]
}
```

On peut représenter l'entrée avec :

```csharp
public record CreateOrderRequest(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderItemRequest> Items);
```

Puis la réponse avec un autre type :

```csharp
public record OrderResponse(
    Guid Id,
    decimal Total,
    string Status);
```

### Pourquoi ne pas utiliser directement l'entité `Order` ?

Parce que le contrat HTTP, le modèle métier et le modèle de persistance peuvent évoluer pour des raisons différentes.

Exemples :

- certaines propriétés internes ne doivent pas être exposées ;
- le client peut envoyer une forme différente du modèle métier ;
- une colonne de DB n'a pas forcément de sens dans l'API ;
- le contrat HTTP doit parfois rester stable alors que l'interne change.

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

Le framework récupère notamment :

- `id` depuis la route ;
- le body JSON pour un DTO complexe ;
- certaines valeurs depuis la query string ;
- le `CancellationToken` lié à la requête.

---

## 7. Validation

Il faut distinguer deux catégories.

### Validation d'entrée

Exemple : champ obligatoire, chaîne trop longue, quantité absente.

### Règle métier

Exemple : une commande ne peut pas être confirmée sans item.

Ne mets pas nécessairement toutes les règles métier dans le Controller. Le Controller est surtout une frontière HTTP.

---

## 8. Codes HTTP essentiels

Quelques cas fréquents :

```text
200 OK          lecture réussie
201 Created     ressource créée
204 No Content  succès sans body
400 Bad Request requête invalide
404 Not Found   ressource absente
409 Conflict    conflit avec l'état courant
500 Internal Server Error erreur inattendue côté serveur
```

Le code HTTP fait partie du contrat de l'API.

### Exemple

```csharp
var order = await service.GetByIdAsync(id, cancellationToken);

if (order is null)
    return NotFound();

return Ok(order);
```

---

## 9. Gestion globale des erreurs

Évite ceci dans chaque endpoint :

```csharp
try
{
    ...
}
catch (Exception ex)
{
    return StatusCode(500);
}
```

Les erreurs inattendues sont généralement mieux gérées à un niveau transversal, par exemple avec le mécanisme global de gestion d'erreurs.

Le Controller peut alors rester concentré sur la traduction entre HTTP et application.

---

## 10. DI dans ASP.NET Core

Enregistrement :

```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
```

Utilisation :

```csharp
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }
}
```

Le Controller ne construit pas son service. Le conteneur fournit l'instance.

---

## 11. Configuration

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

mais pour des configurations structurées, l'Options pattern est souvent plus lisible :

```csharp
public class ExternalApiOptions
{
    public string BaseUrl { get; init; } = string.Empty;
}
```

Le principe important : **la configuration dépend de l'environnement, le code ne doit pas contenir les secrets ou URLs variables en dur**.

---

## 12. Logging

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

car le système de logs conserve mieux les propriétés et s'intègre aux outils d'observabilité.

### Attention

Ne logue pas arbitrairement des secrets, tokens ou données personnelles sensibles.

---

## 13. Appels HTTP sortants

Parallèle front-end :

```text
fetch / Axios  ↔  HttpClient
```

Exemple :

```csharp
var response = await httpClient.GetAsync(
    "/catalog/products",
    cancellationToken);
```

Dans ASP.NET Core, on utilise fréquemment `IHttpClientFactory` ou les clients typés pour centraliser configuration et cycle de vie.

---

## Exercice — premier endpoint

Créer :

```text
GET /orders/{id}
```

Contraintes :

- utiliser `IOrderService` injecté ;
- transmettre le `CancellationToken` ;
- retourner `404` si absent ;
- retourner `200` avec un `OrderResponse` sinon ;
- ne pas exposer directement l'objet de persistence.

---

## Application au projet fil rouge

À ce stade, l'application doit exposer au minimum :

```text
POST /orders
GET  /orders/{id}
GET  /orders
```

La persistance peut encore être en mémoire. Le but est d'abord de maîtriser le flux HTTP et la séparation entre :

```text
HTTP → Controller → Service → Repository
```
