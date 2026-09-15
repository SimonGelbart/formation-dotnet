# 9 — Tester une application .NET

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer test unitaire et test d'intégration ;
- utiliser Arrange / Act / Assert ;
- tester un objet métier sans framework de mock ;
- utiliser un fake simple ;
- comprendre quand un mock est utile ;
- lancer l'API avec `WebApplicationFactory` ;
- remplacer une infrastructure dans un test ;
- tester EF Core avec une base relationnelle SQLite in-memory ;
- comprendre les limites d'EF Core `InMemory` ;
- choisir une frontière de test en fonction du risque à couvrir.

---

# 1. Pourquoi tester ?

Un test automatisé vérifie qu'un comportement important reste vrai lorsque le code évolue.

Exemple de règle :

> une commande vide ne peut pas être confirmée.

Le test devient une forme de documentation exécutable.

Un test utile décrit surtout un **comportement observable**. Il ne doit pas nécessairement reproduire toute la structure interne du code.

---

# 2. Arrange / Act / Assert

```csharp
[Fact]
public void Confirm_EmptyOrder_Throws()
{
    // Arrange
    var order = new Order(Guid.NewGuid());

    // Act
    var action = () => order.Confirm();

    // Assert
    Assert.Throws<InvalidOperationException>(action);
}
```

```text
Arrange → préparer
Act     → agir
Assert  → vérifier
```

Tu n'as pas besoin d'écrire les commentaires dans chaque test si la structure est déjà claire.

---

# 3. Commencer par les objets simples

```csharp
[Fact]
public void AddItem_UpdatesTotal()
{
    var order = new Order(Guid.NewGuid());
    var item = new OrderItem(
        Guid.NewGuid(),
        "Keyboard",
        10m,
        2);

    order.AddItem(item);

    Assert.Equal(20m, order.Total);
}
```

Ce test :

- n'a pas besoin de DI ;
- n'a pas besoin de mock ;
- n'a pas besoin de base de données ;
- décrit directement une règle métier.

### Test historique important

```csharp
[Fact]
public void OrderItem_KeepsCapturedPrice()
{
    var item = new OrderItem(
        Guid.NewGuid(),
        "Keyboard",
        10m,
        2);

    Assert.Equal(10m, item.UnitPrice);
    Assert.Equal(20m, item.Subtotal);
}
```

L'item conserve le prix capturé au moment de la commande.

---

# 4. Fake vs mock

## Fake

Une implémentation simple réellement utilisable pour le test :

```csharp
public sealed class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];

    public Task AddAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyCollection<Order>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Order> result =
            _orders.Values.ToList();

        return Task.FromResult(result);
    }
}
```

Puis :

```csharp
var repository = new FakeOrderRepository();
var service = new OrderService(repository, ...);
```

## Mock

Un mock est utile lorsqu'on souhaite configurer précisément un collaborateur ou vérifier une interaction :

```text
« si le catalogue retourne ce produit... »
« le notifier doit être appelé après confirmation »
```

### Piège

Un test qui vérifie chaque appel interne peut devenir très fragile.

> Vérifie le comportement observable dès que possible ; vérifie les interactions lorsque l'interaction est elle-même importante.

---

# 5. DI et testabilité

Fortement couplé :

```csharp
public class OrderService
{
    private readonly SqlOrderRepository _repository = new();
}
```

Le test doit potentiellement utiliser SQL.

Avec injection :

```csharp
public OrderService(IOrderRepository repository)
{
    _repository = repository;
}
```

le test peut fournir un fake.

La testabilité n'est pas la seule raison d'utiliser DI, mais elle rend immédiatement visible le bénéfice du découplage.

---

# 6. Test unitaire vs test d'intégration

## Test unitaire

On contrôle les frontières autour d'une petite unité logique :

```text
OrderService
   ↓
Fake repository
```

## Test d'intégration

On vérifie plusieurs composants réels ensemble :

```text
HTTP
 ↓
ASP.NET Core
 ↓
Routing / Binding / DI
 ↓
Controller
 ↓
Service
 ↓
EF Core
 ↓
Database de test
```

La différence n'est pas simplement « rapide vs lent ». Elle concerne surtout **la frontière réellement testée**.

---

# 7. Premier test HTTP avec `WebApplicationFactory`

Dans le projet de tests :

```bash
dotnet package add Microsoft.AspNetCore.Mvc.Testing
```

Avec des top-level statements dans `Program.cs`, ajoute à la fin du projet API :

```csharp
public partial class Program
{
}
```

Premier test :

```csharp
public class OrdersApiTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrdersApiTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_UnknownOrder_Returns404()
    {
        var response = await _client.GetAsync(
            $"/orders/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}
```

Ce test traverse réellement :

```text
HttpClient
 ↓
routing
 ↓
model binding
 ↓
DI
 ↓
Controller
 ↓
service
```

Il apporte donc plus d'information qu'un test faisant directement :

```csharp
new OrdersController(...)
```

---

# 8. Pourquoi ne pas utiliser EF Core `InMemory` comme preuve SQL ?

Le provider EF Core `InMemory` n'est **pas une base relationnelle**.

Il peut se comporter différemment concernant notamment :

- contraintes relationnelles ;
- transactions ;
- traduction de requêtes ;
- SQL brut ;
- comportement propre au provider.

Donc :

> un test qui passe avec EF `InMemory` ne prouve pas qu'une requête fonctionnera avec une vraie base relationnelle.

SQLite in-memory est souvent plus intéressant pour ce workbook car SQLite est une vraie base relationnelle.

Attention cependant : SQLite n'est pas identique à SQL Server ou PostgreSQL. Pour des comportements spécifiques au moteur de production, le test le plus fidèle utilise ce moteur.

---

# 9. Une vraie factory d'intégration avec SQLite in-memory

Le principe important avec SQLite `:memory:` est le suivant :

> la base vit tant que la connexion reste ouverte.

Dans le projet de tests, ajoute si nécessaire :

```bash
dotnet package add Microsoft.EntityFrameworkCore.Sqlite
```

Factory :

```csharp
public sealed class OrderApiFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbOptions = services.SingleOrDefault(
                descriptor => descriptor.ServiceType ==
                    typeof(IDbContextOptionsConfiguration<AppDbContext>));

            if (dbOptions is not null)
                services.Remove(dbOptions);

            var existingConnection = services.SingleOrDefault(
                descriptor => descriptor.ServiceType ==
                    typeof(DbConnection));

            if (existingConnection is not null)
                services.Remove(existingConnection);

            services.AddSingleton<DbConnection>(_ =>
            {
                var connection = new SqliteConnection(
                    "DataSource=:memory:");

                connection.Open();
                return connection;
            });

            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                var connection =
                    provider.GetRequiredService<DbConnection>();

                options.UseSqlite(connection);
            });
        });

        builder.UseEnvironment("Development");
    }
}
```

### Initialiser le schéma

Dans le setup du test :

```csharp
await using var scope = factory.Services.CreateAsyncScope();

var dbContext = scope.ServiceProvider
    .GetRequiredService<AppDbContext>();

await dbContext.Database.EnsureCreatedAsync();
```

Pour une suite réelle, centralise cette initialisation dans la fixture plutôt que de la recopier dans chaque test.

---

# 10. Test de bout en bout simple

```csharp
public sealed class OrdersIntegrationTests
    : IClassFixture<OrderApiFactory>
{
    private readonly OrderApiFactory _factory;
    private readonly HttpClient _client;

    public OrdersIntegrationTests(OrderApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnknownOrder_Returns404()
    {
        await EnsureDatabaseAsync();

        var response = await _client.GetAsync(
            $"/orders/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private async Task EnsureDatabaseAsync()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
    }
}
```

Puis ajoute un scénario plus significatif :

```text
POST /orders
→ 201
→ récupérer Location
→ GET de cette URL
→ 200
→ vérifier les données
```

Celui-ci valide réellement :

```text
HTTP + sérialisation + routing + DI + service + EF Core + SQLite
```

---

# 11. Quelle infrastructure remplacer ?

`WebApplicationFactory` permet de remplacer des dépendances.

Si tu testes seulement :

```text
HTTP + Controller + Service
```

un repository de test peut être approprié.

Si tu veux tester :

```text
requêtes EF + mapping + contraintes relationnelles
```

remplacer EF par un fake détruit précisément la frontière que tu voulais vérifier.

### Question à toujours poser

> Quel risque ce test est-il censé détecter ?

La réponse détermine quelles dépendances doivent être réelles.

---

# 12. Plan de tests du projet fil rouge

## Domaine

- quantité <= 0 refusée ;
- commande vide non confirmable ;
- commande confirmée non modifiable ;
- total correct ;
- prix unitaire capturé et stable.

## Service

- commande créée et enregistrée ;
- produit absent géré ;
- ajout d'un item capture le prix courant ;
- notification demandée au bon moment.

## API

- POST retourne `201` ;
- GET inconnu retourne `404` ;
- validation invalide retourne `400` ;
- `Location` pointe vers une ressource relisible.

## Persistence

- relation Order/OrderItems correcte ;
- projection de total traduite et exécutable ;
- mapping de la collection privée fonctionnel.

---

## Exercice final

Construis trois tests pour la même fonctionnalité « confirmer une commande » :

1. **test domaine** : `Order.Confirm()` ;
2. **test service** : repository + notifier contrôlés ;
3. **test HTTP** : appel réel `POST /orders/{id}/confirm`.

Pour chacun, écris une phrase :

> ce test détecte principalement...

<details>
<summary>Critères de réussite</summary>

Les trois tests ne doivent pas vérifier exactement les mêmes détails. Le test domaine vérifie l'invariant, le test service l'orchestration, et le test HTTP le contrat et l'intégration des composants.
</details>
