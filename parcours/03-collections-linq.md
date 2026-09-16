# 03 — Manipuler un catalogue

**Pratiquer :** listes, dictionnaires, lambdas et LINQ courant. **Comprendre :** génériques, `IEnumerable<T>` et exécution différée. **Repérer :** hachage et complexité.

Résultat : filtrer les produits, les trier et afficher leurs noms.

## Programme complet — `Sandbox/Program.cs`

```csharp
var products = new List<Product>
{
    new Product(1, "Clavier", 30m),
    new Product(2, "Souris", 15m),
    new Product(3, "Écran", 200m)
};

var names = products
    .Where(product => product.Price <= 50m)
    .OrderBy(product => product.Price)
    .Select(product => product.Name)
    .ToList();

foreach (var name in names)
    Console.WriteLine(name);

var byId = products.ToDictionary(product => product.Id);
if (byId.TryGetValue(2, out var found))
    Console.WriteLine(found.Name);

public record Product(int Id, string Name, decimal Price);
```

Attendu : `Souris`, `Clavier`, puis `Souris`. Ce petit record est un modèle d'exercice indépendant ; le domaine du projet restera une classe protectrice.

`List<Product>` est une liste d'objets Product. Le paramètre `T` dans `List<T>` permet au même type de collection de fonctionner avec différents éléments tout en conservant leur type : c'est un générique.

## Choisir une collection

| Besoin | Choix de départ |
|---|---|
| Parcourir des produits, en ajouter | `List<Product>` |
| Retrouver souvent un produit par sa clé | `Dictionary<Guid, Product>` |
| Conserver des valeurs uniques | `HashSet<string>` |
| Taille fixée à la création | Tableau, par exemple `int[]` |

Une recherche dans une liste peut devoir lire tous les éléments. Un dictionnaire est conçu pour retrouver efficacement une clé. On parle de `O(n)` pour la recherche linéaire et de `O(1)` en moyenne pour le dictionnaire. Pas besoin d'un cours de complexité pour faire ce premier choix.

`TryGetValue` traite explicitement une clé absente. `dictionary[id]` lève une exception si la clé n'existe pas. `ToDictionary` échoue si plusieurs éléments donnent la même clé.

## LINQ et les fonctions

| JavaScript | C# |
|---|---|
| `filter` | `Where` |
| `map` | `Select` |
| `some` | `Any` |
| `every` | `All` |
| `find` | `FirstOrDefault` pour notre liste d'objets |

`product => product.Price <= 50m` est une lambda : une fonction sans nom. On pourrait la stocker dans `Func<Product, bool>`. `Func` décrit une fonction qui retourne une valeur ; `Action<Product>` une action sans résultat. Ce sont des types de delegates, c'est-à-dire des références typées vers des méthodes.

`FirstOrDefault` retourne le premier résultat ou la valeur par défaut, ici `null`. `SingleOrDefault` vérifie en plus qu'il n'y en a pas plusieurs. `Any` répond à « existe-t-il ? ». `Sum` calcule une somme. `GroupBy` constitue des groupes.

## Une requête n'est pas nécessairement exécutée tout de suite

Dans le programme, avant la déclaration du record, essaie :

```csharp
var cheap = products.Where(p => p.Price < 20m);
products.Add(new Product(4, "Câble", 5m));
Console.WriteLine(cheap.Count()); // 2
```

Le filtre est parcouru lors de `Count`, après l'ajout du câble. Si tu ajoutes `.ToList()` à la première ligne, le résultat est calculé avant l'ajout : le compte vaut `1`.

`IEnumerable<Product>` signifie « une séquence que l'on peut parcourir ». Cela ne garantit ni une liste, ni un résultat déjà calculé. Une nouvelle énumération peut refaire le travail.

`Where` utilise une méthode d'extension : on l'appelle comme une méthode de la liste, bien qu'elle soit définie ailleurs. Créer ses propres extensions n'est pas nécessaire maintenant.

## Exercice

Calcule le total des prix, puis récupère le nom du produit le plus cher. Utilise les trois produits initiaux, sans le câble.

<details>
<summary>Correction</summary>

```csharp
var total = products.Sum(p => p.Price); // 245
var mostExpensive = products.OrderByDescending(p => p.Price).FirstOrDefault();
Console.WriteLine(mostExpensive?.Name ?? "Catalogue vide"); // Écran
```
</details>

## Pour repérer : l'égalité dans les ensembles

`HashSet` utilise `GetHashCode` pour chercher une zone, puis `Equals` pour tester l'égalité. Deux objets égaux doivent avoir le même hash code. Pour débuter, utilise des clés simples (`Guid`, `string`, `int`) et laisse ces méthodes aux types existants. Les structures `Queue` et `Stack` servent respectivement à traiter dans l'ordre d'arrivée ou en commençant par le dernier ajouté.

## Trois questions

1. Quand choisir un dictionnaire plutôt qu'une liste ?
2. Quelle différence entre `Where` et `Select` ?
3. Que change `ToList` dans l'expérience du câble ?

Suite : [04 — Dépendances](04-dependances.md).
