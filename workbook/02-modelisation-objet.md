# 2 — Modéliser avec les objets

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer classe et instance ;
- expliquer l'encapsulation ;
- protéger les invariants d'un objet ;
- comprendre héritage et polymorphisme ;
- utiliser `virtual`, `override`, `abstract` et `sealed` ;
- comprendre le rôle de `ToString()`, `Equals()` et `GetHashCode()` ;
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
    public string Status { get; private set; } = "Draft";

    public void Confirm()
    {
        if (Total <= 0)
            throw new InvalidOperationException("An empty order cannot be confirmed.");

        Status = "Confirmed";
    }
}
```

### Idée clé

> L'objet doit autant que possible empêcher lui-même les états qui n'ont pas de sens.

C'est ce qu'on appelle protéger ses **invariants**.

---

## 3. Héritage et polymorphisme

Supposons plusieurs types de notification :

```text
NotificationSender
├── EmailSender
└── SmsSender
```

On peut représenter le concept commun avec une classe abstraite :

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

Le point important est le suivant :

```csharp
NotificationSender sender = new EmailSender();
```

La variable est typée avec l'abstraction, mais l'objet concret est un `EmailSender`.

C'est une forme essentielle de **polymorphisme**.

---

## 4. `virtual`, `override`, `abstract`, `sealed`

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

La classe ou la méthode ne fournit pas une implémentation complète et impose aux classes dérivées de le faire.

### `sealed`

Empêche l'héritage d'une classe ou certaines redéfinitions supplémentaires.

### Question à se poser

Avant d'utiliser l'héritage :

> Est-ce réellement une relation « est un » ?

Un `EmailSender` **est un** `NotificationSender`. En revanche, un `OrderService` n'**est pas un** `OrderRepository`.

---

## 5. `ToString()`, `Equals()` et `GetHashCode()`

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

### `Equals()` et `GetHashCode()`

Ils deviennent particulièrement importants lorsqu'on compare des objets ou qu'on les utilise dans des collections comme `HashSet<T>` ou comme clés de dictionnaire.

Il n'est pas nécessaire de réimplémenter ces méthodes systématiquement. Il faut surtout comprendre qu'une classe classique est, par défaut, très liée à l'identité de l'instance, alors qu'un `record` fournit une sémantique de valeur plus naturelle.

---

## 6. `static` vs instance

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

## Application au projet fil rouge

Faire évoluer `Order` pour qu'une commande :

- possède une collection d'items ;
- refuse une quantité <= 0 ;
- calcule son total à partir de ses items ;
- ne laisse pas un consommateur écrire directement un total arbitraire ;
- puisse être confirmée uniquement si elle contient au moins un item.

Le but est de commencer à traiter `Order` comme un objet métier et non comme un simple sac de propriétés.
