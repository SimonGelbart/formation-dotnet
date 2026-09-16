# 01 — Lire et écrire du C#

**Pratiquer :** variables, types, classe, propriétés, méthodes et `null`. **Comprendre :** le parallèle TypeScript.

Résultat : créer un produit et calculer son prix pour une quantité.

## Les repères utiles

| TypeScript | C# dans ce cours |
|---|---|
| `string` | `string` |
| `boolean` | `bool` |
| `number` entier | `int` |
| `number` pour un prix | `decimal`, avec un littéral comme `30m` |
| `const name = "Alice"` | `var name = "Alice"` infère aussi le type, mais reste réassignable |
| `null` ou `undefined` | Principalement `null` pour représenter une absence |

`var` ne signifie pas `any`. Après `var name = "Alice"`, affecter `42` à `name` ne compile pas. On peut aussi écrire explicitement `string name = "Alice"`.

Pour les prix, `decimal` évite les surprises courantes de représentation binaire de nombres comme `0.1`. Les règles d'arrondi restent une décision du programme.

## Programme complet — remplacer `Sandbox/Program.cs`

```csharp
var product = new Product();
product.Name = "Clavier";
product.Price = 30m;

Console.WriteLine(product.Name);
Console.WriteLine(product.TotalFor(2));

public class Product
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    public decimal TotalFor(int quantity)
    {
        return Price * quantity;
    }
}
```

Attendu : `Clavier`, puis `60`.

`Product` est la classe : elle décrit une forme d'objet. `new Product()` crée une instance. `Name` et `Price` sont des propriétés ; `get` autorise la lecture, `set` l'écriture. `TotalFor` est une méthode : elle reçoit un entier et retourne un `decimal`.

`public` rend ces membres accessibles au code qui utilise le produit. Les types sont écrits après les instructions du programme console ; on peut aussi les déplacer dans leurs propres fichiers.

Une autre écriture de la création serait :

```csharp
// Extrait : remplace uniquement les trois premières lignes.
var product = new Product { Name = "Clavier", Price = 30m };
```

## Conditions et boucles

La syntaxe est proche de JavaScript : `if`, `else`, `for`, `while`, `return`. Les accolades délimitent un bloc. Les instructions se terminent généralement par `;`.

```csharp
// Extrait à placer avant la déclaration de Product.
if (product.Price > 20m)
{
    Console.WriteLine("Plus de 20 euros");
}
```

## Une valeur peut manquer

```csharp
// Extrait indépendant à essayer dans Program.cs.
string? description = null;
Console.WriteLine(description ?? "Sans description");
Console.WriteLine(description?.Length);
```

`string?` annonce une absence possible. `??` fournit une valeur de remplacement. `?.` accède au membre seulement si la valeur existe. Sans `?`, le compilateur avertit de nombreux usages potentiellement dangereux lorsque l'analyse nullable est activée.

N'ajoute pas `!` uniquement pour cacher un warning : il ne vérifie rien à l'exécution.

## Exercice

Ajoute une méthode `IsAffordable(decimal budget)` qui renvoie `true` si le prix est inférieur ou égal au budget. Affiche son résultat avec les budgets `20m` et `30m`.

<details>
<summary>Correction</summary>

Dans la classe :

```csharp
public bool IsAffordable(decimal budget)
{
    return Price <= budget;
}
```

Avant la classe :

```csharp
Console.WriteLine(product.IsAffordable(20m)); // False
Console.WriteLine(product.IsAffordable(30m)); // True
```
</details>

## Trois questions

1. Pourquoi `var price = 30m` reste-t-il typé ?
2. Quelle différence entre la classe et l'objet créé avec `new` ?
3. Comment exprimer qu'une description peut manquer ?

Suite : [02 — Objets](02-objets.md).
