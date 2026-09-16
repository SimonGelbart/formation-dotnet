# 11 — Étude de cas : décisions de conception de l'Order API

> **Prérequis conseillé :** avoir terminé les chapitres principaux du [workbook v2](../workbook_v2/README.md).
>
> **Niveau :** À approfondir pour les décisions de conception · Nuance pour les compromis architecture/EF/tests.

La v2 te fait construire et modifier une application. Ce chapitre ne demande pas de construire une seconde Order API.

Il analyse plutôt les décisions derrière un modèle de commandes afin de répondre à une question différente :

> **pourquoi ce code est-il organisé ainsi, et quels problèmes chaque choix cherche-t-il à éviter ?**

## 1. Pourquoi `Order` protège-t-il son état ?

Version fragile :

```csharp
public decimal Total { get; set; }
public OrderStatus Status { get; set; }
public List<OrderItem> Items { get; } = [];
```

N'importe quel consommateur peut inventer un total, vider la collection ou forcer le statut.

Une version métier protège les invariants :

```csharp
public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;

    public decimal Total => _items.Sum(x => x.Subtotal);

    public void Confirm()
    {
        if (_items.Count == 0)
            throw new InvalidOperationException(
                "An empty order cannot be confirmed.");

        Status = OrderStatus.Confirmed;
    }
}
```

Décision :

```text
le domaine expose des opérations
plutôt que des setters permettant de contourner les règles
```

## 2. Pourquoi copier le prix dans `OrderItem` ?

Le catalogue représente le prix **actuel** :

```text
Product.Price = 50
```

Une commande représente un événement historique :

```text
OrderItem.UnitPrice = 40
```

Si le catalogue passe de 40 à 50, une commande déjà créée ne doit pas changer rétroactivement.

C'est pourquoi la ligne capture au minimum :

```text
ProductId
ProductName
UnitPrice
Quantity
```

Le total reste alors stable :

```csharp
public decimal Subtotal => UnitPrice * Quantity;
```

## 3. Pourquoi la commande crée-t-elle ses propres lignes ?

```csharp
public void AddItem(
    Guid productId,
    string productName,
    decimal unitPrice,
    int quantity)
{
    if (Status != OrderStatus.Draft)
        throw new InvalidOperationException();

    _items.Add(new OrderItem(
        Id,
        productId,
        productName,
        unitPrice,
        quantity));
}
```

Ainsi la commande peut garantir que :

- une ligne appartient immédiatement à la bonne commande ;
- une commande confirmée ne reçoit plus de ligne ;
- la quantité et le prix sont validés au bon endroit ;
- le code extérieur ne modifie pas directement la collection.

Cette décision n'est pas une règle universelle de DDD. Elle est simplement cohérente avec les invariants de ce petit modèle.

## 4. Pourquoi `Total` n'est-il pas nécessairement une colonne SQL ?

Dans le domaine :

```csharp
public decimal Total =>
    _items.Sum(x => x.Subtotal);
```

Dans la base :

```text
Orders
- Id
- Status
- CreatedAt

OrderItems
- OrderId
- UnitPrice
- Quantity
```

Le total peut être calculé à la lecture :

```sql
SELECT o.Id,
       COALESCE(SUM(i.UnitPrice * i.Quantity), 0) AS Total
FROM Orders o
LEFT JOIN OrderItems i ON i.OrderId = o.Id
GROUP BY o.Id;
```

Stocker également `Total` serait possible, mais introduirait une nouvelle question :

> comment garantir que la colonne reste cohérente avec les lignes ?

Dans ce modèle d'apprentissage, le calcul évite cette duplication d'état.

## 5. Pourquoi `LEFT JOIN` pour les commandes vides ?

Une commande `Draft` peut exister avant d'avoir des lignes.

```text
INNER JOIN
→ élimine les commandes sans ligne

LEFT JOIN
→ conserve la commande
```

Avec `COALESCE`, son total devient `0` plutôt que `NULL`.

Cette décision montre qu'un choix SQL doit refléter le comportement métier attendu.

## 6. Pourquoi garder une collection privée avec EF Core ?

Le domaine veut :

```csharp
private readonly List<OrderItem> _items = [];
public IReadOnlyCollection<OrderItem> Items => _items;
```

Il ne devrait pas être obligé de devenir :

```csharp
public List<OrderItem> Items { get; set; } = [];
```

uniquement pour satisfaire l'ORM.

EF Core peut mapper le backing field :

```csharp
order.Navigation(x => x.Items)
    .HasField("_items")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

Décision :

```text
l'infrastructure s'adapte au modèle
quand le coût du mapping reste raisonnable
```

## 7. Pourquoi `Add` peut-il rester synchrone ?

Avec EF Core :

```csharp
dbContext.Orders.Add(order);
```

ajoute l'entité au change tracker. Aucune écriture réseau n'est encore nécessaire.

L'I/O arrive ici :

```csharp
await dbContext.SaveChangesAsync(cancellationToken);
```

D'où une frontière possible :

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    void Add(Order order);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
```

Le point important n'est pas cette interface précise. C'est de ne pas rendre artificiellement toutes les opérations `async`.

## 8. Pourquoi `SaveChangesAsync` doit-il être explicite ?

```text
charger
→ modifier l'objet
→ change tracker observe la modification
→ SaveChangesAsync
→ SQL envoyé à la base
```

Sans sauvegarde :

```csharp
order.Confirm();
```

modifie bien l'objet C#, mais pas la base persistante.

Une implémentation mémoire peut faire de `SaveChangesAsync` un no-op parce que ses objets sont déjà stockés en mémoire. Cela montre justement que les deux systèmes n'ont pas le même mécanisme de persistance.

## 9. Repository ou `DbContext` direct ?

Deux choix peuvent être valides.

### Repository

Utile lorsque l'application veut une frontière explicite adaptée à ses besoins :

```text
OrderService
    ↓
IOrderRepository
    ↑
EF / mémoire / autre infrastructure
```

### `DbContext` direct

Peut être plus simple lorsqu'un service applicatif travaille déjà naturellement avec EF et qu'une couche supplémentaire ne cache rien d'utile.

EF Core fournit déjà des abstractions importantes via `DbContext` et `DbSet<T>`.

Conclusion :

> un repository doit résoudre un problème, pas satisfaire une convention.

## 10. Pourquoi les lifetimes changent-ils entre mémoire et EF ?

Un repository mémoire qui contient lui-même les données doit survivre entre requêtes :

```text
Singleton repository
→ la collection est le stockage
```

Avec EF :

```text
Scoped DbContext / repository
→ la base est le stockage durable
→ le contexte représente une unité de travail courte
```

Attention :

```csharp
ConcurrentDictionary<Guid, Order>
```

peut protéger les opérations sur le dictionnaire, mais ne rend pas automatiquement les objets `Order` contenus thread-safe.

Le faux stockage mémoire sert à apprendre ; ce n'est pas une base de production.

## 11. Pourquoi projeter au lieu de charger tout le graphe ?

Pour une liste de résumés :

```csharp
var query = dbContext.Orders
    .AsNoTracking()
    .Where(x => x.Status == OrderStatus.Confirmed)
    .Select(x => new OrderSummary(
        x.Id,
        x.Items.Sum(i => i.UnitPrice * i.Quantity),
        x.Status));
```

Projection :

```text
sélectionner uniquement les données nécessaires
→ moins de données matérialisées
→ calcul potentiellement traduit en SQL
```

Avant d'exécuter :

```csharp
Console.WriteLine(query.ToQueryString());
```

La vraie question est :

> quel SQL mon LINQ demande-t-il réellement ?

## 12. Pourquoi `AsNoTracking()` pour certaines lectures ?

Si les entités ne seront pas modifiées puis sauvegardées, le change tracker apporte parfois un travail inutile.

```csharp
.AsNoTracking()
```

exprime alors clairement une lecture seule.

À l'inverse, une commande chargée pour être confirmée bénéficie du tracking :

```text
charger tracked
→ order.Confirm()
→ SaveChangesAsync
```

## 13. Pourquoi charger les items avant certaines règles ?

Pour savoir si une commande est vide ou calculer son total, les lignes doivent être disponibles.

```csharp
var order = await dbContext.Orders
    .Include(x => x.Items)
    .FirstOrDefaultAsync(
        x => x.Id == id,
        cancellationToken);
```

Mais charger systématiquement chaque graphe complet peut coûter cher. Pour des listes et rapports, une projection est souvent préférable.

Cette tension mène directement au problème N+1 et aux choix de stratégie de chargement.

## 14. Pourquoi les tests ont-ils plusieurs niveaux ?

Même fonctionnalité : « confirmer une commande ».

### Domaine

```text
Order.Confirm()
→ protège l'invariant
```

### Service

```text
charger
→ appeler la règle
→ sauvegarder
→ éventuellement notifier
```

### HTTP + DB

```text
route
→ binding
→ DI
→ service
→ EF
→ SQLite
→ réponse HTTP
```

Chaque test couvre un risque différent. Les dupliquer exactement aux trois niveaux n'apporte pas automatiquement plus de valeur.

## 15. Pourquoi SQLite in-memory plutôt qu'EF `InMemory` ?

Le provider EF `InMemory` n'est pas relationnel. Il ne reproduit pas fidèlement :

- contraintes SQL ;
- certaines traductions ;
- transactions ;
- comportement propre au provider.

SQLite in-memory fournit une vraie base relationnelle légère.

Il reste différent du moteur de production : lorsqu'un comportement dépend spécifiquement de SQL Server, PostgreSQL ou autre, il faut tester ce moteur.

## 16. Pourquoi une base de test doit-elle être isolée ?

```text
test A ajoute des données
↓
test B ne doit pas dépendre de ces données
```

Une stratégie simple : nouvelle base/connexion par scénario, ou reset explicite avant le test.

Principe :

> un test doit produire le même résultat seul ou au milieu de la suite.

## 17. Pourquoi ne pas découper immédiatement en quatre projets ?

Commencer par :

```text
Api
Tests
```

peut être largement suffisant.

Puis observer des symptômes :

- règles métier noyées dans HTTP ;
- dépendances EF partout ;
- DTOs utilisés comme modèles métier ;
- composition difficile à lire ;
- frontières impossibles à tester proprement.

Alors seulement envisager :

```text
Api
Application
Domain
Infrastructure
Tests
```

Une couche doit avoir une raison d'exister.

## 18. Où les patterns deviennent-ils pertinents ?

```text
plusieurs algorithmes de livraison
→ Strategy

SDK externe incompatible avec notre contrat
→ Adapter

logging/cache autour d'un collaborateur
→ Decorator

construction non triviale choisie au runtime
→ éventuellement Factory
```

Ne commence pas par le nom du pattern. Commence par le problème.

## Questions de revue

Après la v2, tu dois pouvoir utiliser cette étude de cas pour expliquer :

1. pourquoi le prix historique est copié ;
2. pourquoi la commande contrôle ses items ;
3. pourquoi `Total` peut rester calculé ;
4. pourquoi `SaveChangesAsync` existe ;
5. pourquoi `Add` n'a pas besoin d'être async ;
6. pourquoi `DbContext` est scoped ;
7. pourquoi projection et `AsNoTracking()` existent ;
8. pourquoi `Include` n'est pas toujours la bonne réponse ;
9. pourquoi un test domaine et un test HTTP ne couvrent pas le même risque ;
10. pourquoi repository, couches et patterns ne sont jamais automatiques.

Si une de ces questions reste floue, retourne au chapitre d'approfondissement correspondant plutôt que de refaire toute l'application.