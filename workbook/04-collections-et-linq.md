# 4 — Génériques, collections et LINQ

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- comprendre à quoi servent les génériques ;
- choisir une collection adaptée au besoin ;
- raisonner simplement en termes de complexité `O(1)`, `O(n)`, `O(n²)` ;
- lire et écrire des lambdas ;
- comprendre `Func<T>` et `Action<T>` ;
- utiliser les principaux opérateurs LINQ ;
- comprendre quels opérateurs sont différés et lesquels déclenchent une énumération ;
- distinguer une séquence `IEnumerable<T>` d'une collection matérialisée ;
- comprendre l'intérêt de `IReadOnlyCollection<T>` ;
- comprendre le principe des méthodes d'extension.

---

## 1. Génériques

En TypeScript, tu connais déjà ce principe :

```ts
Array<User>
Promise<User>
```

En C# :

```csharp
List<User>
Task<User>
Dictionary<Guid, User>
```

Le paramètre générique permet de réutiliser une structure sans perdre l'information de type.

Exemple :

```csharp
public class Result<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }
}
```

On peut alors écrire :

```csharp
Result<User>
Result<Order>
Result<Product>
```

### Pourquoi ne pas utiliser `object` ?

Avec `T`, le compilateur conserve le vrai type. On évite les casts et on obtient une API plus claire.

---

## 2. Les collections courantes

### `Array`

Taille fixe après création.

```csharp
var values = new int[] { 1, 2, 3 };
```

### `List<T>`

Collection ordonnée dynamique.

```csharp
var users = new List<User>();
users.Add(user);
```

### `Dictionary<TKey, TValue>`

Associe une clé à une valeur.

```csharp
var usersById = new Dictionary<Guid, User>();
usersById[user.Id] = user;
```

Si l'absence d'une clé est un cas normal, préfère souvent :

```csharp
if (usersById.TryGetValue(id, out var user))
{
    Console.WriteLine(user.Name);
}
```

à :

```csharp
var user = usersById[id];
```

car l'indexeur lève une exception si la clé n'existe pas.

### `HashSet<T>`

Ensemble de valeurs uniques. Très utile lorsqu'on veut tester rapidement l'appartenance à un ensemble.

### `Queue<T>`

Premier entré, premier sorti.

### `Stack<T>`

Dernier entré, premier sorti.

### `IReadOnlyCollection<T>`

Exprime qu'un consommateur peut parcourir les éléments et connaître leur nombre sans recevoir une API de modification de la collection.

```csharp
public IReadOnlyCollection<OrderItem> Items => _items;
```

Cela ne garantit pas à lui seul une immutabilité profonde des objets contenus, mais réduit les possibilités de modifier directement la structure de la collection.

---

## 3. Complexité : choisir selon l'usage

Quelques ordres de grandeur utiles :

| Opération | `List<T>` | `Dictionary<TKey,TValue>` | `HashSet<T>` |
|---|---:|---:|---:|
| Accès par index | O(1) | — | — |
| Recherche par valeur | O(n) | — | O(1) moyen |
| Recherche par clé | — | O(1) moyen | — |
| Ajout | O(1) amorti | O(1) moyen | O(1) moyen |
| Suppression par valeur | O(n) | — | O(1) moyen |

Ces valeurs sont des modèles utiles, pas des garanties absolues de temps réel.

Supposons 100 000 utilisateurs et une recherche répétée par ID.

Avec une liste :

```csharp
var user = users.FirstOrDefault(x => x.Id == id);
```

Dans le pire cas, on inspecte chaque élément : environ `O(n)`.

Avec un dictionnaire :

```csharp
usersById.TryGetValue(id, out var user);
```

La recherche par clé est en moyenne proche de `O(1)`.

### Intuition

```text
O(1)   : le coût reste globalement stable
O(n)   : le travail augmente avec le nombre d'éléments
O(n²)  : le travail peut exploser avec deux parcours imbriqués dépendants de n
```

Attention : **deux boucles imbriquées ne signifient pas automatiquement O(n²)**. Cela dépend de la taille réellement parcourue par chaque boucle.

### Exercice

Tu dois vérifier pour chaque commande si son `CustomerId` existe dans une liste de 50 000 clients.

Version A :

```csharp
foreach (var order in orders)
{
    var customer = customers.FirstOrDefault(c => c.Id == order.CustomerId);
}
```

Comment améliorer la structure de données si cette recherche est fréquente ?

<details>
<summary>Correction</summary>

Construire par exemple un `Dictionary<Guid, Customer>` indexé par ID pour éviter de rescanner toute la liste pour chaque commande.
</details>

---

## 4. Lambdas

JavaScript :

```js
users.filter(user => user.age >= 18)
```

C# :

```csharp
users.Where(user => user.Age >= 18)
```

La partie :

```csharp
user => user.Age >= 18
```

est une lambda : une fonction anonyme.

---

## 5. `Func` et `Action`

Une lambda peut être stockée dans un delegate.

```csharp
Func<User, bool> isAdult = user => user.Age >= 18;
```

`Func<User, bool>` signifie :

> fonction qui reçoit un `User` et retourne un `bool`.

`Action<T>` représente une fonction qui ne retourne pas de valeur :

```csharp
Action<User> printUser = user => Console.WriteLine(user.Name);
```

### Pourquoi c'est important ?

LINQ accepte justement des fonctions comme paramètres.

---

## 6. `IEnumerable<T>`

`IEnumerable<T>` représente avant tout une séquence que l'on peut énumérer.

```csharp
IEnumerable<User> users = ...;
```

Cela ne garantit ni :

- que la source soit une `List<User>` ;
- que tous les éléments soient déjà calculés ;
- que les données soient nécessairement stockées en mémoire sous cette forme.

Une API peut retourner `IEnumerable<User>` lorsqu'elle veut surtout exprimer :

> « voici une séquence d'utilisateurs que tu peux parcourir ».

Une `List<T>` est donc un objet concret avec des capacités supplémentaires (`Count`, indexation, modification...), tandis que `IEnumerable<T>` est un contrat beaucoup plus minimal.

---

## 7. Les opérateurs LINQ essentiels

### `Where`

Filtrer :

```csharp
var activeUsers = users.Where(x => x.IsActive);
```

### `Select`

Transformer :

```csharp
var names = users.Select(x => x.Name);
```

### `Any` / `All`

```csharp
var hasAdmin = users.Any(x => x.IsAdmin);
var allActive = users.All(x => x.IsActive);
```

### `First` / `FirstOrDefault`

```csharp
var user = users.FirstOrDefault(x => x.Id == id);
```

`First` lève une exception si aucun élément n'est trouvé. `FirstOrDefault` retourne la valeur par défaut, généralement `null` pour un type référence nullable dans ce contexte.

### `Single` / `SingleOrDefault`

Exprime qu'il ne doit y avoir **au maximum qu'un élément correspondant**. Une seconde correspondance provoque une exception. `Single` provoque également une exception si aucun élément n'existe.

Utilise-le quand l'unicité fait partie du contrat, pas simplement comme variante de `First`.

### `OrderBy`

```csharp
var sorted = users.OrderBy(x => x.Name);
```

### `GroupBy`

```csharp
var byCountry = users.GroupBy(x => x.Country);
```

### `Count`, `Sum`

```csharp
var count = users.Count();
var total = orders.Sum(x => x.Total);
```

### `SelectMany`

Permet d'aplatir plusieurs séquences imbriquées :

```csharp
var allItems = orders.SelectMany(x => x.Items);
```

### `ToDictionary`

```csharp
var byId = users.ToDictionary(x => x.Id);
```

Pratique pour construire un index de recherche en mémoire.

### `ToList`

Matérialise la séquence dans une liste.

---

## 8. Lire une chaîne LINQ

```csharp
var names = users
    .Where(user => user.IsActive)
    .OrderBy(user => user.Name)
    .Select(user => user.Name)
    .ToList();
```

Lis-la comme une pipeline :

```text
users
 ↓ garder les actifs
 ↓ trier par nom
 ↓ ne garder que le nom
 ↓ matérialiser dans une List<string>
```

### Exercice

Avant d'exécuter ce code, décris chaque transformation en français :

```csharp
var result = orders
    .Where(x => x.Total >= 100m)
    .OrderByDescending(x => x.Total)
    .Take(5)
    .Select(x => new { x.Id, x.Total })
    .ToList();
```

---

## 9. Exécution différée et exécution immédiate

Tous les opérateurs LINQ ne se comportent pas de la même manière.

### Opérateurs souvent différés

Par exemple :

```text
Where
Select
OrderBy
Take
Skip
```

Ils peuvent construire une séquence qui sera réellement parcourue plus tard.

Considère :

```csharp
var adults = users.Where(x => x.Age >= 18);

users.Add(new User { Name = "Bob", Age = 30 });

foreach (var adult in adults)
{
    Console.WriteLine(adult.Name);
}
```

`Where` n'a pas produit une copie figée au moment de l'appel. La source est énumérée lorsque le `foreach` commence ; Bob peut donc apparaître.

### Opérations qui déclenchent l'énumération

Par exemple :

```text
ToList
ToArray
First
Single
Count
Any
Sum
```

Elles ont besoin de parcourir tout ou partie de la séquence pour produire leur résultat.

Avec :

```csharp
var adults = users.Where(x => x.Age >= 18).ToList();
```

la liste obtenue est matérialisée immédiatement.

### Piège : énumérer plusieurs fois

```csharp
var query = ExpensiveSequence();

var count = query.Count();
foreach (var item in query)
{
    ...
}
```

Selon la source, le calcul peut être exécuté deux fois. Une séquence différée n'est pas automatiquement un cache.

### À retenir

> Une expression LINQ peut représenter un calcul à exécuter plus tard, pas nécessairement un résultat déjà calculé.

---

## 10. Méthodes d'extension

Lorsque tu écris :

```csharp
users.Where(...)
```

`Where` n'est pas nécessairement une méthode définie directement dans le type concret de `users`.

On peut créer ses propres méthodes d'extension :

```csharp
public static class OrderEnumerableExtensions
{
    public static IEnumerable<Order> Confirmed(
        this IEnumerable<Order> orders)
    {
        return orders.Where(x => x.Status == OrderStatus.Confirmed);
    }
}
```

Puis :

```csharp
var confirmed = orders.Confirmed();
```

Une méthode d'extension est une méthode statique que la syntaxe permet d'appeler comme si elle appartenait au type étendu. Elle n'ajoute pas réellement une nouvelle méthode d'instance au type original.

---

## Application au projet fil rouge

Ajouter des opérations permettant :

- de lister uniquement les commandes confirmées ;
- de calculer le total des commandes ;
- de récupérer les cinq commandes les plus chères ;
- de grouper les commandes par client ;
- de rechercher efficacement une commande par ID dans l'implémentation mémoire.

Pour le repository mémoire, comparer :

```csharp
List<Order>
```

et :

```csharp
Dictionary<Guid, Order>
```

puis justifier le choix selon les opérations dominantes.

### Checkpoint

Tu dois pouvoir expliquer sans regarder le chapitre :

1. différence entre `List<T>` et `Dictionary<TKey,TValue>` ;
2. pourquoi `TryGetValue` est souvent préférable à l'indexeur si l'absence est normale ;
3. différence entre `Where` et `Select` ;
4. différence entre `FirstOrDefault` et `SingleOrDefault` ;
5. pourquoi `ToList()` change le moment d'exécution d'une requête LINQ ;
6. pourquoi `IEnumerable<T>` ne signifie pas simplement « liste déjà en mémoire » ;
7. pourquoi une séquence différée énumérée deux fois peut refaire le travail deux fois.
