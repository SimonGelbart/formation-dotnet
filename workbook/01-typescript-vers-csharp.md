# 1 — De TypeScript à C#

## Objectifs

À la fin de ce chapitre, tu dois savoir expliquer :

- pourquoi `var` en C# n'est pas l'équivalent de `any` ;
- la différence entre type valeur et type référence ;
- comment les paramètres sont passés par défaut ;
- le rôle de `public`, `private`, `protected` et `internal` ;
- pourquoi `string` est un type référence malgré son comportement immuable ;
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

```text
user1 ───────┐
             ▼
         User object
             ▲
user2 ───────┘
```

La variable contient une référence vers l'objet, pas une copie indépendante de l'objet.

### Le cas particulier de `string`

`string` est un **type référence**, mais les chaînes sont immuables : une opération qui semble les modifier produit une nouvelle chaîne.

```csharp
var a = "Hello";
var b = a;

b = b + " world";

Console.WriteLine(a); // Hello
Console.WriteLine(b); // Hello world
```

Ne déduis pas « mutable = référence » et « immutable = valeur ». Ce sont deux notions différentes.

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

## 3. Passage de paramètres : C# passe par valeur par défaut

Une confusion fréquente est de dire : « les objets sont passés par référence ». Ce n'est pas exactement ce qui se passe.

Par défaut, **C# passe les arguments par valeur**.

Pour un type référence, la valeur copiée est... la référence.

```csharp
public static void Rename(User user)
{
    user.Name = "Bob";
}

var user = new User { Name = "Alice" };
Rename(user);

Console.WriteLine(user.Name); // Bob
```

La méthode reçoit une copie de la référence ; les deux références pointent vers le même objet.

Mais réassigner le paramètre ne remplace pas la variable de l'appelant :

```csharp
public static void Replace(User user)
{
    user = new User { Name = "Charlie" };
}

var user = new User { Name = "Alice" };
Replace(user);

Console.WriteLine(user.Name); // Alice
```

Représentation mentale :

```text
appelant user ──→ objet Alice

entrée dans Replace :
paramètre user ─→ objet Alice

réassignation du paramètre :
paramètre user ─→ objet Charlie
appelant user ──→ objet Alice
```

C# possède aussi `ref`, `out` et `in`, qui modifient les règles de passage. Ils existent, mais ne sont pas nécessaires pour la majorité du code applicatif de ce workbook.

### Checkpoint

Explique la différence entre :

```text
copier un objet
```

et :

```text
copier une référence vers un objet
```

---

## 4. Modificateurs d'accès

Les modificateurs d'accès définissent **qui peut utiliser un membre ou un type**.

### `public`

Accessible depuis les consommateurs autorisés à voir le type.

```csharp
public void Confirm()
```

### `private`

Accessible uniquement depuis le type qui le déclare.

```csharp
private readonly List<OrderItem> _items = [];
```

### `protected`

Accessible depuis la classe et ses classes dérivées.

```csharp
protected virtual void OnConfirmed()
```

### `internal`

Accessible depuis le même assembly/projet compilé.

```csharp
internal class InternalHelper
```

Il existe des combinaisons plus avancées (`protected internal`, `private protected`), mais elles ne sont pas nécessaires maintenant.

### Idée importante

L'encapsulation commence aussi par la visibilité :

> n'expose pas publiquement ce qui n'a pas besoin de faire partie du contrat du type.

---

## 5. `null` et Nullable Reference Types

Avec les Nullable Reference Types activés :

```csharp
string firstName = "Alice";
string? middleName = null;
```

`string?` indique que l'absence de valeur fait partie du contrat attendu.

Pour les **types référence**, `string` et `string?` restent le même type au runtime. Le `?` fournit surtout des informations au compilateur pour analyser les chemins où une valeur pourrait être `null` et produire des avertissements.

À ne pas confondre avec :

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

---

## 6. Champs et propriétés

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

## 7. Mutabilité, `const`, `readonly`, `init`, `required`, `record`

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

### `init`

Permet l'initialisation à la création sans laisser ensuite un setter classique :

```csharp
public string Name { get; init; } = string.Empty;
```

### `required`

Indique au compilateur qu'un membre doit être initialisé lors de la construction :

```csharp
public required string Name { get; init; }
```

`required` améliore le contrat de construction, mais **ne remplace pas une validation métier ou runtime**. Une chaîne vide reste par exemple une valeur possible si aucune règle supplémentaire ne l'interdit.

### `record`

Pratique pour représenter des données principalement descriptives :

```csharp
public record UserDto(string Name, string Email);
```

Les records offrent notamment une sémantique de comparaison par valeur plus naturelle que les classes classiques.

Mais un `record` n'est pas automatiquement profondément immuable :

```csharp
public record Basket(List<string> Items);
```

```csharp
basket.Items.Add("Keyboard"); // possible
```

L'immutabilité dépend aussi des types contenus dans l'objet.

---

## 8. `enum` : représenter un ensemble fini d'états

Fragile :

```csharp
order.Status = "Confrimed";
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

---

## 9. Pourquoi `decimal` pour l'argent ?

Les nombres à virgule flottante binaires comme `double` ne peuvent pas représenter exactement de nombreux nombres décimaux usuels.

Pour des calculs scientifiques, `double` est souvent parfaitement adapté. Pour de l'argent ou d'autres calculs décimaux où l'arrondi métier doit être prévisible, `decimal` est généralement plus approprié.

```csharp
decimal price = 19.99m;
```

Le suffixe `m` indique un littéral `decimal`.

### Exercice

Expérimente la différence entre `0.1 + 0.2` avec `double`, puis avec `decimal`.

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

Et définir :

```csharp
public enum OrderStatus
{
    Draft,
    Confirmed
}
```

### Exercice final

1. Pourquoi `Price` est-il un `decimal` plutôt qu'un `double` ?
2. Que signifie réellement le passage d'un objet `User` à une méthode par défaut ?
3. Quelle différence entre `private` et `internal` ?
4. Que gagne-t-on à empêcher la création d'un produit avec un prix négatif ?
5. Pourquoi `OrderStatus` est-il préférable à une chaîne arbitraire ?
6. Un `record` contenant une `List<T>` est-il profondément immuable ?
7. Pourquoi `required` ne suffit-il pas à garantir qu'un nom est métierement valide ?
