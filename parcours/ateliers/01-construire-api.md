# Atelier — Construire une API du GET au service

À faire avec le [chapitre 5](../05-api-http.md), après les chapitres 0 à 4. Le but est d'écrire les éléments successivement, puis de comparer avec les corrections. On utilise un service concret avant d'étudier le repository de la référence.

## Préparer ton projet personnel

Depuis `mes-exercices-dotnet`, créé dans le [guide](../GUIDE.md) :

```bash
dotnet new web -n MonCatalogue --framework net10.0
cd MonCatalogue
```

Le template `web` est volontairement vide. Il sait accueillir des Controllers dès que tu les enregistres. Tous les fichiers ci-dessous sont à la racine de MonCatalogue. Les classes n'ont pas de namespace dans cet atelier pour limiter les déplacements ; la référence en utilise un commun.

## Étape 1 — Retourner un produit

Remplace tout Program.cs par :

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

Crée Product.cs avec ce modèle :

```csharp
public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public decimal Price { get; private set; }
    private Product() { }
    public Product(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nom obligatoire", nameof(name));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        Id = Guid.NewGuid();
        Name = name.Trim();
        Price = price;
    }
}
```

Crée ProductsController.cs :

```csharp
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
```

Lance `dotnet run --urls http://localhost:5080`, puis GET `http://localhost:5080/products` depuis un second terminal ou un fichier HTTP. Attendu : 200 et un tableau contenant Clavier à 30. L'identifiant change entre appels car l'objet est créé dans l'action : il n'y a pas encore de stockage.

**Vérification personnelle :** ajoute un second produit dans le tableau. Arrête puis relance l'application pour observer ta modification. Si tu bloques, compare avec [la correction GET](corrections/01-get/ProductsController.cs).

## Étape 2 — Créer, stocker et relire

Arrête l'API. Ajoute avant `builder.Build()` :

```csharp
builder.Services.AddSingleton<List<Product>>();
```

La liste doit vivre entre deux requêtes. C'est un premier stockage local pour des appels séquentiels, pas une base concurrente de production.

Crée ProductRequest.cs :

```csharp
using System.ComponentModel.DataAnnotations;

public sealed class ProductRequest
{
    [Required]
    public string Name { get; set; } = "";
    [Range(typeof(decimal), "0", "1000000")]
    public decimal Price { get; set; }
}
```

Remplace ProductsController.cs par :

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("products")]
public sealed class ProductsController : ControllerBase
{
    private readonly List<Product> _products;
    public ProductsController(List<Product> products) => _products = products;

    [HttpGet]
    public IActionResult GetAll() => Ok(_products);

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product is null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public IActionResult Create(ProductRequest request)
    {
        var product = new Product(request.Name, request.Price);
        _products.Add(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
```

Lance de nouveau l'API. Le catalogue commence maintenant vide : le produit codé en dur a disparu. Crée un fichier `requetes.http` dans ton projet :

```http
@baseUrl = http://localhost:5080

### Créer un produit : 201
POST {{baseUrl}}/products
Content-Type: application/json

{"name":"Clavier","price":30}

### Lister : 200, un produit après le POST
GET {{baseUrl}}/products

### Entrée invalide : 400
POST {{baseUrl}}/products
Content-Type: application/json

{"name":"Clavier","price":-1}
```

Copie ensuite l'URL de Location dans un GET : le produit doit être relisible. Un id inconnu retourne 404. Vérifie que tu peux expliquer pourquoi il faut lancer le POST avant le GET par identifiant.

Correction complète de cette étape : [02-post](corrections/02-post/Program.cs).

## Étape 3 — Extraire le service sans changer HTTP

Le Controller crée et range les produits. Déplace ce travail dans ProductService.cs :

```csharp
public sealed class ProductService
{
    private readonly List<Product> _products = new();
    public IReadOnlyList<Product> GetAll() => _products;
    public Product? GetById(Guid id) => _products.FirstOrDefault(p => p.Id == id);
    public Product Create(string name, decimal price)
    {
        var product = new Product(name, price);
        _products.Add(product);
        return product;
    }
}
```

Dans Program.cs, **remplace** l'enregistrement de `List<Product>` par `builder.Services.AddSingleton<ProductService>();`. Ici le service contient les données, il doit donc survivre aux requêtes. Dans la référence complète, ce stockage sera isolé dans un repository singleton et le service pourra être scoped.

Dans le Controller, remplace le constructeur et le champ par :

```csharp
private readonly ProductService _service;
public ProductsController(ProductService service) => _service = service;
```

Puis remplace l'accès à la liste dans chaque action : `GetAll` appelle `_service.GetAll()`, `GetById` appelle `_service.GetById(id)`, et Create utilise `_service.Create(request.Name, request.Price)` au lieu de construire et ajouter le produit. Le choix des statuts reste dans le Controller.

**À faire avant de regarder :** réécris les trois actions et rejoue exactement les mêmes requêtes. Aucun changement du contrat HTTP n'est attendu.

<details>
<summary>Correction du Controller</summary>

Voir [ProductsController.cs](corrections/03-service/ProductsController.cs). La [version complète](corrections/03-service/Program.cs) se lance avec `dotnet run --urls http://localhost:5080` depuis son dossier, après arrêt de ton API.
</details>

## Point de contrôle

Tu as écrit GET, POST et la DI d'un service. Le produit est encore retourné directement pour garder cet atelier court. Reviens au [chapitre 5](../05-api-http.md) pour comprendre le DTO de réponse et comparer avec la référence, qui ajoute un repository, PUT et DELETE. Ne copie pas ces fichiers sur les tiens sans examiner les différences.

Fais ensuite le [défi recherche](03-defis-autonomes.md#défi-1--rechercher-des-produits). Conserve un point de contrôle de MonCatalogue avant de passer à SQLite. Si ton défi a ajouté une recherche, tu la réintroduiras après avoir validé la première lecture SQLite.
