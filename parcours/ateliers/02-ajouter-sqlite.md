# Atelier — Remplacer la mémoire par SQLite

À faire après l'atelier HTTP et le chapitre 6, avec le [chapitre 7](../07-persistance.md). Repars de ton étape « service » sauvegardée, ou copie tout [03-service](corrections/03-service/Program.cs) dans ton dossier personnel. Ne lance pas les deux APIs sur le même port.

L'objectif est de conserver les routes et le JSON tout en changeant le stockage. Les commandes ne sont pas encore présentes : une seule table Products.

## Étape 1 — Préparer le projet et la base

Depuis ton dossier MonCatalogue, API arrêtée :

```bash
dotnet package add Microsoft.EntityFrameworkCore.Sqlite --version 10.0.12
dotnet package add Microsoft.EntityFrameworkCore.Design --version 10.0.12
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.12
```

Si un manifeste existe déjà dans ce projet, ne le recrée pas : restaure ses outils avec `dotnet tool restore`. Si dotnet-ef y est déjà déclaré, ne le réinstalle pas. Les versions d'EF sont alignées sur les références.

Crée AppDbContext.cs :

```csharp
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
}
```

Ajoute une section ConnectionStrings dans appsettings.json en conservant les autres sections éventuelles :

```json
{"ConnectionStrings":{"Database":"Data Source=catalogue.db"}}
```

Dans Program.cs, ajoute `using Microsoft.EntityFrameworkCore;`, puis remplace le service singleton par ces deux enregistrements avant Build :

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddScoped<ProductService>();
```

La base gardera les données, le service ne doit plus les porter entre requêtes.

## Étape 2 — Remplacer une lecture, puis une écriture

Modifie ProductService pour qu'il reçoive AppDbContext par constructeur. Supprime la liste privée. Écris d'abord la lecture `GetAllAsync` :

```csharp
public Task<List<Product>> GetAllAsync(CancellationToken ct)
    => _db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
```

Ajoute `using Microsoft.EntityFrameworkCore;`. Écris ensuite `GetByIdAsync` avec `FirstOrDefaultAsync`. Pour CreateAsync : construis le Product, ajoute-le au DbSet, attends SaveChangesAsync, puis retourne le produit. `Add` ne fait pas la sauvegarde.

<details>
<summary>Correction complète du service</summary>

```csharp
using Microsoft.EntityFrameworkCore;

public sealed class ProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public Task<List<Product>> GetAllAsync(CancellationToken ct)
        => _db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product> CreateAsync(string name, decimal price, CancellationToken ct)
    {
        var product = new Product(name, price);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }
}
```

</details>

Les anciens appels synchrones du Controller ne compilent plus : c'est une conséquence normale du changement de contrat. Fais-les évoluer un par un. Exemple pour GET :

```csharp
[HttpGet]
public async Task<IActionResult> GetAll(CancellationToken ct)
    => Ok(await _service.GetAllAsync(ct));
```

Ajoute de même `async Task<IActionResult>`, le token et `await` à GetById et Create. Ne mets pas `.Result` pour masquer les erreurs de type.

<details>
<summary>Correction complète du Controller</summary>

[ProductsController.cs](corrections/04-sqlite/ProductsController.cs). Compare aussi [Program.cs](corrections/04-sqlite/Program.cs) si la résolution du service échoue.
</details>

## Étape 3 — Créer le schéma et vérifier la sauvegarde

Depuis MonCatalogue :

```bash
dotnet build
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run --urls http://localhost:5080
```

Lis la migration : elle doit créer Products, sans commandes. Rejoue ton POST, conserve son identifiant, puis fais le GET par identifiant. Arrête le serveur et relance-le **depuis le même dossier** : le même GET retrouve le produit.

Si `InitialCreate` existe déjà, reprends avec `database update`. Un GET avant application du schéma peut échouer avec une table absente. Ne corrige pas cela en remplaçant le processus de migration par EnsureCreated.

## Expérience de sauvegarde

Commente temporairement SaveChangesAsync dans CreateAsync. Fais un POST, puis un nouveau GET par son id. Le POST retourne un objet mais le GET ne le retrouve pas. Rétablis la sauvegarde et refais l'essai avec un nouveau produit.

## Point de contrôle

Tu sais quelles signatures sont devenues asynchrones, pourquoi le service est scoped et quand la base est modifiée. La correction [04-sqlite](corrections/04-sqlite/Program.cs) est indépendante : elle exige elle aussi `dotnet tool restore`, puis la génération et l'application d'InitialCreate au premier lancement.

Si tu avais réalisé le défi recherche, réintroduis son filtre avant ToListAsync. Reviens au [chapitre 7](../07-persistance.md), puis utilise la référence Catalogue.Api pour découvrir les commandes. Les modèles des ateliers ne sont pas des fichiers à fusionner automatiquement avec cette référence.
