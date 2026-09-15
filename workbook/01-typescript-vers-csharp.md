# 1 — De TypeScript à C#

## Objectifs

À la fin de ce chapitre, tu dois savoir expliquer :

- pourquoi `var` en C# n'est pas l'équivalent de `any` ;
- la différence entre type valeur et type référence ;
- le rôle de `null` et des Nullable Reference Types ;
- la différence entre champ et propriété ;
- ce que changent `const`, `readonly`, `init` et `record`.

---

## 1. `var` n'est pas `any`

En TypeScript, `any` désactive en grande partie la vérification de type. En C#, `var` signifie simplement : **laisse le compilateur déduire le type**.

```csharp
var name = "Alice";
```

est équivalent, du point de vue du type, à :

```csharp
string name = "Alice";
```

Le type reste `string`. Ceci ne compile donc pas :

```csharp
var value = "Hello";
value = 42;
```

### À retenir

> `var` ne rend pas une variable dynamique. Le type est déterminé à la compilation.

---

## 2. Types valeur et types référence

### Type valeur

Une affectation copie la valeur.

```csharp
int a = 10;
int b = a;

b = 20;

Console.WriteLine(a); // 10
```

### Type référence

Avec une classe, plusieurs variables peuvent référencer le même objet.

```csharp
var user1 = new User { Name = "Alice" };
var user2 = user1;

user2.Name = "Bob";

Console.WriteLine(user1.Name); // Bob
```

Représentation mentale :

```text
user1 ───────┐
             ▼
         User object
             ▲
user2 ───────┘
```

La variable contient une référence vers l'objet, pas une copie indépendante de l'objet.

### Exercice

Sans exécuter le code, prédis le résultat :

```csharp
var product1 = new Product
{
    Name = "Keyboard",
    Price = 100m
};

var product2 = product1;
product2.Price = 80m;

Console.WriteLine(product1.Price);
```

<details>
<summary>Correction</summary>

`80`. `product1` et `product2` référencent le même objet.

Pour obtenir deux objets indépendants, il faut explicitement créer/copier un nouvel objet.
</details>

---

## 3. `null` et Nullable Reference Types

Avec les Nullable Reference Types activés :

```csharp
string firstName = "Alice";
string? middleName = null;
```

`string?` indique que l'absence de valeur fait partie du contrat attendu.

Opérateurs utiles :

```csharp
user?.Name
name ?? "Unknown"
name ??= "Unknown";
```

### Attention à `!`

```csharp
User? user = FindUser();
Console.WriteLine(user!.Name);
```

Le `!` ne protège pas contre `null`. Il demande seulement au compilateur de ne plus signaler l'avertissement.

Si `user` vaut réellement `null`, le programme peut toujours lever une `NullReferenceException`.

### Checkpoint

Explique avec tes propres mots la différence entre :

```csharp
string value
```

et :

```csharp
string? value
```

---

## 4. Champs et propriétés

Un champ représente directement une donnée stockée dans l'objet :

```csharp
private string _name;
```

Une propriété expose un accès contrôlé :

```csharp
public string Name { get; set; }
```

Elle peut restreindre l'écriture :

```csharp
public decimal Balance { get; private set; }
```

Ce qui permet de protéger les invariants de l'objet :

```csharp
public void Deposit(decimal amount)
{
    if (amount <= 0)
        throw new ArgumentOutOfRangeException(nameof(amount));

    Balance += amount;
}
```

Le code extérieur ne peut plus écrire arbitrairement :

```csharp
account.Balance = -500; // interdit
```

---

## 5. Mutabilité, `const`, `readonly`, `init`, `record`

### `const`

Pour une constante connue à la compilation :

```csharp
private const int MaxItems = 100;
```

### `readonly`

Un champ `readonly` ne peut plus être réassigné après la construction :

```csharp
private readonly IOrderRepository _repository;
```

Attention : `readonly` ne rend pas l'objet référencé immuable.

```csharp
private readonly List<string> _items = new();

_items.Add("A"); // autorisé
```

Ce qui est interdit est :

```csharp
_items = new List<string>();
```

### `init`

Permet l'initialisation à la création sans laisser ensuite un setter classique :

```csharp
public string Name { get; init; } = string.Empty;
```

### `record`

Pratique pour représenter des données principalement descriptives :

```csharp
public record UserDto(string Name, string Email);
```

Les records offrent notamment une sémantique de comparaison par valeur plus naturelle que les classes classiques.

---

## Application au projet fil rouge

Créer les premiers types :

```csharp
public class Product
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
```

Puis un DTO :

```csharp
public record ProductDto(Guid Id, string Name, decimal Price);
```

### Exercice

1. Pourquoi `Price` ne devrait-il probablement pas être un `double` ?
2. Dans quels cas laisserais-tu `Name` modifiable après construction ?
3. Que gagnerais-tu à empêcher la création d'un produit avec un prix négatif ?

L'objectif n'est pas encore de trouver l'architecture parfaite, mais de commencer à réfléchir aux **invariants** que le modèle doit protéger.
