# 2 — Modéliser avec les objets

## Objectifs

À la fin de ce chapitre, tu dois savoir :

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

Puis :

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

N'importe quel code peut alors écrire :

```csharp
order.Total = -1000;
order.Status = "Anything";
```

Version plus protectrice :

```csharp
public class Order
{
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    public void Confirm()
    {
        if (Total <= 0)
            throw new InvalidOperationException("An empty order cannot be confirmed.");

        Status = OrderStatus.Confirmed;
    }
}
```

### Idée clé

> L'objet doit autant que possible empêcher lui-même les états qui n'ont pas de sens.

C'est ce qu'on appelle protéger ses **invariants**.

---

## 3. Encapsuler aussi les collections

Ceci expose une liste modifiable à tout le monde :

```csharp
public List<OrderItem> Items { get; } = [];
```

Du code extérieur peut alors faire :

```csharp
order.Items.Clear();
order.Items.Add(invalidItem);
```

et contourner les règles prévues par `Order`.

Une approche plus protectrice :

```csharp
public class Order
{
    private readonly List<OrderItem> _items = [];

    public IReadOnlyCollection<OrderItem> Items => _items;

    public void AddItem(OrderItem item)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Confirmed orders cannot be modified.");

        _items.Add(item);
    }
}
```

Le consommateur peut lire les items, mais doit passer par les comportements du domaine pour modifier la commande.

### Nuance

`IReadOnlyCollection<T>` empêche surtout la modification de **la collection via ce contrat**. Il ne rend pas automatiquement les objets contenus immuables.

---

## 4. Héritage et polymorphisme

Supposons plusieurs types de notification :

```text
NotificationSender
├── EmailSender
└── SmsSender
```

On pourrait représenter le concept commun avec une classe abstraite :

```csharp
public abstract class NotificationSender
{
    public abstract Task SendAsync(string message);
}
```

Puis :

```csharp
public class EmailSender : NotificationSender
{
    public override Task SendAsync(string message)
    {
        Console.WriteLine($"Email: {message}");
        return Task.CompletedTask;
    }
}
```

Le point important est :

```csharp
NotificationSender sender = new EmailSender();
```

La variable est typée avec l'abstraction, mais l'objet concret est un `EmailSender`.

C'est une forme essentielle de **polymorphisme**.

### Mais fallait-il vraiment une classe abstraite ?

Dans cet exemple, si les implémentations ne partagent ni état ni comportement, une interface est probablement plus simple :

```csharp
public interface INotificationSender
{
    Task SendAsync(string message);
}
```

Ce contraste est important : ne choisis pas l'héritage uniquement parce que plusieurs classes « se ressemblent ».

---

## 5. Héritage ou composition ?

L'héritage exprime une relation forte :

> `Dog` est un `Animal`.

La composition exprime :

> `OrderService` utilise un `IOrderRepository`.

Exemple :

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

`OrderService` n'est pas un repository. Il **collabore avec** un repository.

### Règle de réflexion

Avant d'hériter, demande-toi :

1. existe-t-il réellement une relation « est un » ?
2. la classe dérivée peut-elle être utilisée partout où la classe de base est attendue ?
3. ai-je besoin d'état/comportement commun, ou seulement d'un contrat ?
4. une composition rendrait-elle la relation plus claire et plus flexible ?

---

## 6. `virtual`, `override`, `abstract`, `sealed`

### `virtual`

Une méthode possède une implémentation par défaut mais peut être redéfinie.

```csharp
public virtual decimal CalculatePrice()
{
    return 10m;
}
```

### `override`

Une classe dérivée remplace l'implémentation virtuelle.

```csharp
public override decimal CalculatePrice()
{
    return 20m;
}
```

### `abstract`

Une classe abstraite ne peut pas être instanciée directement. Une méthode abstraite impose aux classes concrètes dérivées de fournir une implémentation.

### `sealed`

Sur une classe : empêche toute nouvelle dérivation.

```csharp
public sealed class EmailSender
{
}
```

On peut également sceller une redéfinition :

```csharp
public sealed override decimal CalculatePrice()
{
    return 20m;
}
```

Les classes encore plus dérivées ne pourront alors plus remplacer cette méthode.

---

## 7. `ToString()`, `Equals()` et `GetHashCode()`

Toutes les classes C# héritent indirectement de `object`.

Parmi ses méthodes importantes :

- `ToString()` ;
- `Equals()` ;
- `GetHashCode()`.

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

`override` est possible parce que `object.ToString()` est virtuelle.

### Égalité des classes

Deux classes distinctes ayant les mêmes données ne sont pas automatiquement considérées comme égales par valeur :

```csharp
var a = new Product { Name = "Keyboard", Price = 100m };
var b = new Product { Name = "Keyboard", Price = 100m };

Console.WriteLine(a.Equals(b)); // généralement false sans sémantique personnalisée
```

Un `record` fournit au contraire par défaut une sémantique de valeur plus naturelle pour ses composants.

### Contrat `Equals` / `GetHashCode`

Si deux objets sont considérés comme égaux par `Equals`, ils doivent produire le même hash code :

```text
Equals(a, b) == true
        ↓
a.GetHashCode() == b.GetHashCode()
```

L'inverse n'est pas garanti : deux objets différents peuvent avoir le même hash code.

Pourquoi est-ce important ? Parce que `Dictionary<TKey,TValue>` et `HashSet<T>` utilisent le hash code puis l'égalité pour organiser et retrouver leurs éléments.

### Règle pratique

Ne surcharge pas `Equals` et `GetHashCode` au hasard. Si ton objet a une vraie sémantique de valeur, utilise éventuellement un `record` ou implémente les deux de manière cohérente.

---

## 8. `static` vs instance

Méthode d'instance :

```csharp
order.Confirm();
```

Elle agit sur un objet particulier.

Méthode statique :

```csharp
Math.Max(10, 20);
```

Elle appartient au type lui-même.

### Quand `static` est naturel

Pour une opération pure qui n'a pas besoin d'état ni de dépendance :

```csharp
public static decimal AddVat(decimal amount, decimal rate)
{
    return amount * (1 + rate);
}
```

### Piège : état global

```csharp
public static class Database
{
    public static List<Order> Orders { get; } = [];
}
```

Cela introduit un état global partagé, ce qui peut compliquer :

- les tests ;
- la concurrence ;
- la compréhension du cycle de vie ;
- le remplacement de l'implémentation.

`static` n'est donc pas mauvais en soi ; **l'état global mutable** mérite surtout d'être traité avec prudence.

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

- le solde ne puisse pas être écrit directement depuis l'extérieur ;
- `Deposit` refuse les montants <= 0 ;
- `Withdraw` refuse les montants <= 0 ;
- `Withdraw` refuse de rendre le solde négatif.

<details>
<summary>Critères de réussite</summary>

Le setter de `Balance` ne doit pas être public. Les règles doivent être centralisées dans la classe plutôt que dupliquées chez les consommateurs.
</details>

---

## Exercice — héritage ou composition ?

Pour chaque relation, choisis d'abord entre héritage, interface ou composition et justifie :

1. `EmailSender` / « peut envoyer une notification » ;
2. `OrderService` / `OrderRepository` ;
3. `Circle` / `Shape` si toutes les formes partagent un contrat de calcul d'aire ;
4. `Car` / `Engine`.

L'objectif n'est pas d'obtenir une réponse unique à tout prix, mais de savoir **exprimer la nature de la relation**.

---

## Application au projet fil rouge

Faire évoluer `Order` pour qu'une commande :

- possède une collection privée d'items ;
- expose cette collection en lecture seule ;
- refuse une quantité <= 0 via la création de l'item ;
- calcule son total à partir de ses items ;
- ne laisse pas un consommateur écrire directement un total arbitraire ;
- puisse être confirmée uniquement si elle contient au moins un item ;
- utilise `OrderStatus` plutôt qu'une chaîne libre.

Le but est de traiter `Order` comme un objet métier et non comme un simple sac de propriétés.
