# 9 — Tester une application .NET

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer test unitaire et test d'intégration ;
- utiliser Arrange / Act / Assert ;
- tester un objet métier sans framework de mock ;
- utiliser un fake simple ;
- comprendre quand un mock est utile ;
- lancer l'API avec `WebApplicationFactory` ;
- tester EF Core avec une base relationnelle SQLite in-memory ;
- isoler les données entre scénarios de test ;
- comprendre les limites d'EF Core `InMemory` ;
- choisir une frontière de test en fonction du risque à couvrir.

---

# 1. Pourquoi tester ?

Un test automatisé vérifie qu'un comportement important reste vrai lorsque le code évolue.

Exemple :

> une commande vide ne peut pas être confirmée.

Le test devient une forme de documentation exécutable.

Un test utile décrit surtout un **comportement observable**, pas tous les détails internes du code.

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

---

# 3. Commencer par les objets simples

```csharp
[Fact]
public void AddItem_UpdatesTotal()
{
    var order = new Order(Guid.NewGuid());

    order.AddItem(
        Guid.NewGuid(),
        "Keyboard",
        10m,
        2);

    Assert.Equal(20m, order.Total);
}
```

Ce test n'a besoin ni de DI, ni de mock, ni de base de données.

### Test du prix historique

```csharp
[Fact]
public void AddItem_CapturesUnitPrice()
{
    var order = new Order(Guid.NewGuid());

    order.AddItem(
        Guid.NewGuid(),
        "Keyboard",
        10m,
        2);

    var item = Assert.Single(order.Items);

    Assert.Equal(10m, item.UnitPrice);
    Assert.Equal(20m, item.Subtotal);
    Assert.Equal(order.Id, item.OrderId);
}
```

Ce test documente deux décisions : le prix est capturé et la ligne appartient immédiatement à la bonne commande.

---

# 4. Fake vs mock

## Fake

Une implémentation simple réellement utilisable pour le test :

```csharp
public sealed class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];

    public void Add(Order order)
    {
        _orders[order.Id] = order;
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

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

Dans ce fake, `Add` et les modifications changent déjà les objets en mémoire. `SaveChangesAsync` n'a donc rien à écrire.

## Mock

Un mock est utile lorsqu'on veut configurer précisément un collaborateur ou vérifier une interaction :

```text
« si le catalogue retourne ce produit... »
« le notifier doit être appelé après confirmation »
```

### Piège

Un test qui vérifie chaque appel interne peut devenir fragile.

> Vérifie le comportement observable dès que possible ; vérifie les interactions lorsque l'interaction elle-même est importante.

---

# 5. DI et testabilité

Fortement couplé :

```csharp
public class OrderService
{
    private readonly SqlOrderRepository _repository = new();
}
```

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

Ce test traverse réellement routing, binding, DI, Controller et service.

---

# 8. Pourquoi ne pas utiliser EF Core `InMemory` comme preuve SQL ?

Le provider EF Core `InMemory` n'est **pas une base relationnelle**.

Il peut se comporter différemment concernant :

- contraintes relationnelles ;
- transactions ;
- traduction de requêtes ;
- SQL brut ;
- comportement propre au provider.

Donc :

> un test qui passe avec EF `InMemory` ne prouve pas qu'une requête fonctionnera avec une vraie base relationnelle.

SQLite in-memory est plus pertinent pour ce workbook car SQLite est une vraie base relationnelle.

Il reste différent de SQL Server ou PostgreSQL : pour un comportement spécifique au moteur de production, teste avec ce moteur.

---

# 9. Factory d'intégration avec SQLite in-memory

La base SQLite `:memory:` vit tant que sa connexion reste ouverte.

Ajoute si nécessaire :

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
    }
}
```

---

# 10. Isoler les tests

Une connexion SQLite in-memory gardée ouverte signifie aussi que les données peuvent survivre entre plusieurs tests utilisant la même fixture.

Un test doit pouvoir s'exécuter seul ou avec les autres et donner le même résultat.

Ajoute cette méthode à `OrderApiFactory` :

```csharp
public async Task ResetDatabaseAsync()
{
    await using var scope = Services.CreateAsyncScope();

    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
}
```

Au début d'un scénario :

```csharp
await _factory.ResetDatabaseAsync();
```

Puis initialise seulement les données nécessaires au test.

### Idée clé

> Un test ne doit pas dépendre de l'ordre dans lequel les autres tests ont été exécutés.

---

# 11. Test de bout en bout simple

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
        await _factory.ResetDatabaseAsync();

        var response = await _client.GetAsync(
            $"/orders/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}
```

Puis ajoute un scénario plus significatif :

```text
reset DB
→ seed du catalogue nécessaire
→ POST /orders
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

# 12. Quelle infrastructure remplacer ?

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

Pose toujours la question :

> Quel risque ce test est-il censé détecter ?

---

# 13. Plan de tests du projet fil rouge

## Domaine

- quantité <= 0 refusée ;
- commande vide non confirmable ;
- commande confirmée non modifiable ;
- total correct ;
- prix unitaire capturé et stable ;
- `OrderId` de la ligne cohérent.

## Service

- commande créée et enregistrée ;
- produit absent géré ;
- ajout d'un item capture le prix courant ;
- modification suivie de `SaveChangesAsync` ;
- notification demandée au bon moment.

## API

- POST retourne `201` ;
- GET inconnu retourne `404` ;
- validation invalide retourne `400` ;
- `Location` pointe vers une ressource relisible.

## Persistence

- relation Order/OrderItems correcte ;
- projection de total traduite et exécutable ;
- mapping de la collection privée fonctionnel ;
- modification persistée après `SaveChangesAsync`.

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

Les trois tests ne doivent pas vérifier exactement les mêmes détails. Le test domaine vérifie l'invariant, le test service l'orchestration et la sauvegarde, et le test HTTP le contrat et l'intégration des composants.
</details>
