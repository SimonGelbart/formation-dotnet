# 2 — Modéliser avec les objets

> **Prérequis conseillé :** [parcours principal — 02](../parcours/02-objets.md).
>
> **Niveau :** À approfondir pour encapsulation · Nuance pour égalité et mutabilité · Référence pour héritage avancé.
>
> **Statut des exemples :** extraits indépendants et variantes de conception. Ils ne constituent pas une suite de modifications à appliquer à Catalogue.Api. Les types manquants sont à définir dans une expérience séparée. Pour le code exécutable et ses signatures exactes, consulte les [applications du parcours](../parcours/README.md#environnement-et-applications-de-référence).

## Questions abordées

Cette référence aide à comprendre, selon ton besoin :

- distinguer classe et instance ;
- expliquer l'encapsulation ;
- protéger les invariants d'un objet ;
- exposer une collection sans donner un accès de modification arbitraire ;
- comprendre héritage et polymorphisme ;
- distinguer héritage, interface et composition ;
- utiliser `virtual`, `override`, `abstract` et `sealed` ;
- comprendre le rôle de `ToString()`, `Equals()` et `GetHashCode()` ;
- comprendre le contrat entre égalité et hash code ;
- savoir quand `static` est pertinent ou problématique.

---

## 1. Classe et instance

Une classe définit une structure et des comportements. Une instance est un objet concret créé à partir de cette classe.

```csharp
public class Order
{
    public Guid Id { get; init; }
}
```

```csharp
var order = new Order();
```

`Order` est le type. `order` est une instance.

---

## 2. Encapsulation : protéger l'état

Une classe ne devrait pas seulement stocker des données. Elle peut aussi empêcher des états incohérents.

Version fragile :

```csharp
public class Order
{
    public decimal Total { get; set; }
    public string Status { get; set; } = "Draft";
}
```

N'importe quel code peut écrire :

```csharp
order.Total = -1000;
order.Status = "Anything";
```

Version plus protectrice :

```csharp
public class Order
{
    private readonly List<OrderItem> _items = [];

    public IReadOnlyCollection<OrderItem> Items => _items;
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
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

### Pourquoi `_items.Count == 0` plutôt que `Total <= 0` ?

Parce que la règle métier est :

> une commande vide ne peut pas être confirmée.

Une commande peut contenir un produit gratuit et avoir un total à `0`.

> Un invariant doit exprimer la règle métier réelle, pas seulement un indicateur qui lui ressemble.

---

## 3. Encapsuler aussi les collections

Ceci expose une liste modifiable :

```csharp
public List<OrderItem> Items { get; } = [];
```

Du code extérieur peut alors faire :

```csharp
order.Items.Clear();
```

Approche plus protectrice :

```csharp
public class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; } = Guid.NewGuid();
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public IReadOnlyCollection<OrderItem> Items => _items;

    public void AddItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                "Confirmed orders cannot be modified.");

        _items.Add(new OrderItem(
            Id,
            productId,
            productName,
            unitPrice,
            quantity));
    }
}
```

La commande contrôle désormais **comment** une ligne entre dans sa collection. Elle peut garantir que la ligne appartient à la bonne commande et appliquer ses règles avant l'ajout.

`IReadOnlyCollection<T>` empêche surtout la modification de la collection via ce contrat. Il ne rend pas les objets contenus profondément immuables.

---

## 4. Héritage et polymorphisme

Supposons plusieurs types de notification :

```text
NotificationSender
├── EmailSender
└── SmsSender
```

On pourrait utiliser une classe abstraite :

```csharp
public abstract class NotificationSender
{
    public abstract void Send(string message);
}
```

Puis :

```csharp
public class EmailSender : NotificationSender
{
    public override void Send(string message)
    {
        Console.WriteLine($"Email: {message}");
    }
}
```

```csharp
NotificationSender sender = new EmailSender();
```

La variable est typée avec l'abstraction, mais l'objet concret est un `EmailSender` : c'est une forme essentielle de **polymorphisme**.

### Mais fallait-il vraiment une classe abstraite ?

Si les implémentations ne partagent ni état ni comportement, une interface est probablement plus simple :

```csharp
public interface INotificationSender
{
    void Send(string message);
}
```

Ne choisis pas l'héritage uniquement parce que plusieurs classes « se ressemblent ».

---

## 5. Héritage ou composition ?

L'héritage exprime une relation forte :

> `Dog` est un `Animal`.

La composition exprime :

> `OrderService` utilise un `IOrderRepository`.

```csharp
public class OrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
}
```

Avant d'hériter, demande-toi :

1. existe-t-il réellement une relation « est un » ?
2. la classe dérivée peut-elle être utilisée partout où la classe de base est attendue ?
3. ai-je besoin d'état/comportement commun, ou seulement d'un contrat ?
4. une composition rendrait-elle la relation plus claire ?

---

## 6. `virtual`, `override`, `abstract`, `sealed`

### `virtual`

```csharp
public virtual decimal CalculatePrice()
{
    return 10m;
}
```

Une implémentation par défaut existe mais peut être redéfinie.

### `override`

```csharp
public override decimal CalculatePrice()
{
    return 20m;
}
```

### `abstract`

Une classe abstraite ne peut pas être instanciée directement. Une méthode abstraite impose aux classes concrètes dérivées une implémentation.

### `sealed`

Sur une classe : empêche toute nouvelle dérivation.

```csharp
public sealed class EmailSender
{
}
```

On peut aussi sceller une redéfinition :

```csharp
public sealed override decimal CalculatePrice()
{
    return 20m;
}
```

---

## 7. `ToString()`, `Equals()` et `GetHashCode()`

Toutes les classes C# héritent indirectement de `object`.

### `ToString()`

```csharp
public class Product
{
    public required string Name { get; init; }
    public decimal Price { get; init; }

    public override string ToString()
    {
        return $"{Name} — {Price:C}";
    }
}
```

### Égalité des classes

```csharp
var a = new Product { Name = "Keyboard", Price = 100m };
var b = new Product { Name = "Keyboard", Price = 100m };

Console.WriteLine(a.Equals(b)); // généralement false
```

Un `record` fournit par défaut une sémantique de valeur plus naturelle.

### Contrat `Equals` / `GetHashCode`

Si deux objets sont égaux selon `Equals`, ils doivent produire le même hash code :

```text
Equals(a, b) == true
        ↓
a.GetHashCode() == b.GetHashCode()
```

L'inverse n'est pas garanti.

`Dictionary<TKey,TValue>` et `HashSet<T>` utilisent cette combinaison pour organiser et retrouver leurs éléments.

### Expérience

Crée deux instances de classe contenant les mêmes données puis ajoute-les à un `HashSet<T>`. Compare ensuite avec un `record` équivalent.

---

## 8. `static` vs instance

Méthode d'instance :

```csharp
order.Confirm();
```

Méthode statique :

```csharp
Math.Max(10, 20);
```

`static` est naturel pour une opération qui n'a pas besoin d'état d'instance ou de dépendance :

```csharp
public static decimal AddVat(decimal amount, decimal rate)
{
    return amount * (1 + rate);
}
```

### Piège : état global mutable

```csharp
public static class Database
{
    public static List<Order> Orders { get; } = [];
}
```

Cela complique potentiellement les tests, la concurrence, le cycle de vie et le remplacement de l'implémentation.

`static` n'est pas mauvais en soi ; **l'état global mutable** mérite surtout d'être traité avec prudence.

---

## Exercice — protéger un compte bancaire

Partir de :

```csharp
public class BankAccount
{
    public decimal Balance { get; set; }
}
```

Modifier la classe pour que :

- le solde ne puisse pas être écrit directement ;
- `Deposit` refuse les montants <= 0 ;
- `Withdraw` refuse les montants <= 0 ;
- `Withdraw` refuse de rendre le solde négatif.

<details>
<summary>Critères de réussite</summary>

Le setter de `Balance` ne doit pas être public. Les règles doivent être centralisées dans la classe plutôt que dupliquées chez les consommateurs.
</details>

---

## Exercice — héritage ou composition ?

Pour chaque relation, choisis entre héritage, interface ou composition et justifie :

1. `EmailSender` / « peut envoyer une notification » ;
2. `OrderService` / `OrderRepository` ;
3. `Circle` / `Shape` ;
4. `Car` / `Engine`.

---

## Expérience facultative — variante indépendante

Faire évoluer `Order` pour qu'une commande :

- possède une collection privée d'items ;
- expose cette collection en lecture seule ;
- crée elle-même ses items avec son propre `Id` ;
- refuse une quantité <= 0 via la création de l'item ;
- calcule son total à partir de ses items ;
- ne laisse pas un consommateur écrire directement un total arbitraire ;
- puisse être confirmée uniquement si elle contient au moins un item ;
- utilise `OrderStatus` plutôt qu'une chaîne libre.

Le but est de traiter `Order` comme un objet métier et non comme un simple sac de propriétés.
