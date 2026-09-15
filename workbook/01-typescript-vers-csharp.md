# 1 — De TypeScript à C#

## Objectifs

À la fin de ce chapitre, tu dois savoir expliquer :

- pourquoi `var` en C# n'est pas l'équivalent de `any` ;
- la différence entre type valeur et type référence ;
- pourquoi `string` est un type référence malgré son comportement souvent immuable ;
- le rôle de `null` et des Nullable Reference Types ;
- la différence entre champ et propriété ;
- ce que changent `const`, `readonly`, `init`, `required`, `record` et `enum` ;
- pourquoi `decimal` est généralement préféré à `double` pour représenter de l'argent.

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

### Types valeur

Un type valeur contient directement sa valeur. Une affectation crée une copie indépendante.

```csharp
int a = 10;
int b = a;

b = 20;

Console.WriteLine(a); // 10
```

Les types numériques, `bool`, les `enum` et les `struct` sont notamment des types valeur.

Un `struct` permet de le constater avec un type personnalisé :

```csharp
public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
}

var p1 = new Point { X = 10, Y = 20 };
var p2 = p1;
p2.X = 99;

Console.WriteLine(p1.X); // 10
```

### Types référence

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

### Le cas particulier de `string`

`string` est un **type référence**, mais les chaînes sont immuables : une opération qui semble les modifier produit en réalité une nouvelle chaîne.

```csharp
var a = "Hello";
var b = a;

b = b + " world";

Console.WriteLine(a); // Hello
Console.WriteLine(b); // Hello world
```

Ne déduis donc pas « mutable = référence » et « immutable = valeur ». Ce sont deux notions différentes.

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

### Une information surtout utilisée par le compilateur

Pour les **types référence**, `string` et `string?` restent le même type au runtime. Le `?` fournit surtout des informations au compilateur pour analyser les chemins où une valeur pourrait être `null` et produire des avertissements.

À ne pas confondre avec un nullable value type comme :

```csharp
int? age = null;
```

qui correspond à `Nullable<int>`.

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

Puis explique pourquoi le deuxième n'est pas un « autre type runtime » comme `int?`.

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

## 5. Mutabilité, `const`, `readonly`, `init`, `required`, `record`

### `const`

Pour une constante dont la valeur est connue à la compilation :

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

Ce qui est interdit après la construction est :

```csharp
_items = new List<string>();
```

### `init`

Permet l'initialisation à la création sans laisser ensuite un setter classique :

```csharp
public string Name { get; init; } = string.Empty;
```

### `required`

Permet d'indiquer qu'une propriété doit être initialisée lors de la construction :

```csharp
public required string Name { get; init; }
```

Cela évite par exemple de masquer une donnée obligatoire derrière une valeur par défaut artificielle comme `string.Empty`.

### `record`

Pratique pour représenter des données principalement descriptives :

```csharp
public record UserDto(string Name, string Email);
```

Les records offrent notamment une sémantique de comparaison par valeur plus naturelle que les classes classiques.

Mais un `record` n'est **pas automatiquement profondément immuable** :

```csharp
public record Basket(List<string> Items);
```

Même si la propriété n'est pas réassignée, la liste qu'elle référence peut encore être modifiée :

```csharp
basket.Items.Add("Keyboard");
```

L'immutabilité dépend donc aussi des types contenus dans l'objet.

---

## 6. `enum` : représenter un ensemble fini d'états

Lorsqu'une valeur appartient à un ensemble fermé, un `enum` est souvent plus sûr qu'une chaîne libre.

Fragile :

```csharp
order.Status = "Confrimed"; // faute de frappe possible
```

Plus explicite :

```csharp
public enum OrderStatus
{
    Draft,
    Confirmed
}
```

Puis :

```csharp
public OrderStatus Status { get; private set; } = OrderStatus.Draft;
```

Le compilateur peut désormais empêcher une grande partie des valeurs incohérentes.

---

## 7. Pourquoi `decimal` pour l'argent ?

Les nombres à virgule flottante binaires comme `double` ne peuvent pas représenter exactement de nombreux nombres décimaux usuels.

Pour des calculs scientifiques, `double` est souvent parfaitement adapté. Pour de l'argent ou d'autres calculs décimaux où l'arrondi métier doit être prévisible, `decimal` est généralement plus approprié.

```csharp
decimal price = 19.99m;
```

Le suffixe `m` indique un littéral `decimal`.

### Exercice

Recherche ou expérimente la différence entre :

```csharp
0.1 + 0.2
```

avec `double`, puis avec `decimal`. L'objectif est de comprendre que le choix d'un type numérique dépend du domaine.

---

## Application au projet fil rouge

Créer les premiers types :

```csharp
public class Product
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
}
```

Puis un DTO :

```csharp
public record ProductDto(Guid Id, string Name, decimal Price);
```

Et définir dès maintenant :

```csharp
public enum OrderStatus
{
    Draft,
    Confirmed
}
```

### Exercice

1. Pourquoi `Price` est-il un `decimal` plutôt qu'un `double` ?
2. Dans quels cas laisserais-tu `Name` modifiable après construction ?
3. Que gagnerais-tu à empêcher la création d'un produit avec un prix négatif ?
4. Pourquoi `OrderStatus` est-il préférable à une chaîne arbitraire dans ce domaine ?
5. Un `record` contenant une `List<T>` est-il profondément immuable ? Pourquoi ?

L'objectif n'est pas encore de trouver l'architecture parfaite, mais de commencer à réfléchir aux **invariants** que le modèle doit protéger.
