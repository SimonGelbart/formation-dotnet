# 4 — Génériques, collections et LINQ

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- comprendre à quoi servent les génériques ;
- choisir une collection adaptée ;
- raisonner en `O(1)`, `O(n)`, `O(n²)` ;
- relier `HashSet<T>` / `Dictionary<TKey,TValue>` à `Equals` / `GetHashCode` ;
- lire et écrire des lambdas ;
- comprendre `Func<T>` et `Action<T>` ;
- utiliser les principaux opérateurs LINQ ;
- distinguer opérateurs différés et opérations qui déclenchent une énumération ;
- comprendre `IEnumerable<T>` ;
- comprendre `IReadOnlyCollection<T>` ;
- comprendre les méthodes d'extension.

---

# 1. Génériques

TypeScript :

```ts
Array<User>
Record<string, User>
```

C# :

```csharp
List<User>
Dictionary<Guid, User>
Result<User>
```

Exemple générique :

```csharp
public class Result<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }
}
```

```csharp
Result<User>
Result<Order>
Result<Product>
```

Avec `T`, le compilateur conserve l'information de type : moins de casts, contrats plus précis.

---

# 2. Collections courantes

## `Array`

Taille fixe après création :

```csharp
var values = new[] { 1, 2, 3 };
```

## `List<T>`

Collection ordonnée dynamique :

```csharp
var users = new List<User>();
users.Add(user);
```

## `Dictionary<TKey,TValue>`

Association clé → valeur :

```csharp
var usersById = new Dictionary<Guid, User>();
usersById[user.Id] = user;
```

Si l'absence est normale :

```csharp
if (usersById.TryGetValue(id, out var user))
{
    Console.WriteLine(user.Name);
}
```

L'indexeur :

```csharp
var user = usersById[id];
```

lève une exception si la clé n'existe pas.

## `HashSet<T>`

Ensemble de valeurs uniques, utile pour tester rapidement l'appartenance.

## `Queue<T>` / `Stack<T>`

```text
Queue → FIFO
Stack → LIFO
```

## `IReadOnlyCollection<T>`

Exprime qu'un consommateur peut parcourir les éléments et lire leur nombre sans recevoir directement une API de modification de collection.

```csharp
public IReadOnlyCollection<OrderItem> Items => _items;
```

Cela ne rend pas les objets contenus profondément immuables.

---

# 3. Complexité : choisir selon l'usage

| Opération | `List<T>` | `Dictionary<TKey,TValue>` | `HashSet<T>` |
|---|---:|---:|---:|
| Accès par index | O(1) | — | — |
| Recherche par valeur | O(n) | — | O(1) moyen |
| Recherche par clé | — | O(1) moyen | — |
| Ajout | O(1) amorti | O(1) moyen | O(1) moyen |
| Suppression par valeur | O(n) | — | O(1) moyen |

Ces valeurs décrivent des ordres de grandeur, pas une durée garantie.

### Exemple

```csharp
var user = users.FirstOrDefault(x => x.Id == id);
```

→ recherche linéaire, environ `O(n)` dans le pire cas.

```csharp
usersById.TryGetValue(id, out var user);
```

→ `O(1)` moyen.

### Attention

Deux boucles imbriquées ne signifient pas automatiquement `O(n²)` : cela dépend du nombre d'éléments réellement parcourus par chacune.

---

# 4. Pourquoi `HashSet<T>` réactive `Equals` et `GetHashCode`

Considère une classe sans égalité personnalisée :

```csharp
public class ProductCode
{
    public required string Value { get; init; }
}
```

Puis :

```csharp
var codes = new HashSet<ProductCode>();

codes.Add(new ProductCode { Value = "ABC" });
codes.Add(new ProductCode { Value = "ABC" });

Console.WriteLine(codes.Count);
```

Avant d'exécuter, prédis le résultat.

Avec une classe classique, les deux instances sont généralement distinctes selon l'égalité par défaut.

Compare avec :

```csharp
public record ProductCode(string Value);
```

Le `record` possède une sémantique de valeur qui rend le résultat différent.

### Pourquoi ?

Les collections basées sur le hachage utilisent :

```text
GetHashCode
    ↓
trouver une zone candidate
    ↓
Equals
    ↓
confirmer l'égalité
```

Le chapitre 2 sur `Equals` / `GetHashCode` devient donc concret ici.

---

# 5. Exercice complexité

Pour chaque commande, retrouver son client dans 50 000 clients :

```csharp
foreach (var order in orders)
{
    var customer = customers.FirstOrDefault(
        c => c.Id == order.CustomerId);
}
```

Comment améliorer le cas si la recherche est fréquente ?

<details>
<summary>Correction</summary>

Construire une fois un `Dictionary<Guid, Customer>` indexé par ID puis effectuer les recherches avec `TryGetValue`.
</details>

---

# 6. Lambdas

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

est une fonction anonyme : une lambda.

---

# 7. `Func` et `Action`

```csharp
Func<User, bool> isAdult =
    user => user.Age >= 18;
```

Signifie : fonction recevant `User` et retournant `bool`.

```csharp
Action<User> printUser =
    user => Console.WriteLine(user.Name);
```

`Action<T>` ne retourne pas de valeur.

LINQ reçoit justement des fonctions comme paramètres.

---

# 8. `IEnumerable<T>`

`IEnumerable<T>` exprime avant tout :

> cette séquence peut être énumérée.

```csharp
IEnumerable<User> users = ...;
```

Cela ne garantit ni :

- une `List<User>` ;
- des éléments déjà calculés ;
- une matérialisation préalable.

`List<T>` est un type concret riche. `IEnumerable<T>` est un contrat minimal d'énumération.

---

# 9. LINQ essentiel

## Filtrer

```csharp
users.Where(x => x.IsActive)
```

## Transformer

```csharp
users.Select(x => x.Name)
```

## Tester

```csharp
users.Any(x => x.IsAdmin)
users.All(x => x.IsActive)
```

## Premier élément

```csharp
users.FirstOrDefault(x => x.Id == id)
```

`First` échoue si aucun élément n'existe. `FirstOrDefault` retourne la valeur par défaut.

## Unicité

```csharp
users.SingleOrDefault(x => x.Email == email)
```

`Single` / `SingleOrDefault` échouent s'il existe plusieurs correspondances. Utilise-les lorsque l'unicité fait partie du contrat.

## Trier / grouper

```csharp
users.OrderBy(x => x.Name)
users.GroupBy(x => x.Country)
```

## Agréger

```csharp
orders.Count()
orders.Sum(x => x.Total)
```

## Aplatir

```csharp
orders.SelectMany(x => x.Items)
```

## Construire un index

```csharp
var byId = users.ToDictionary(x => x.Id);
```

---

# 10. Lire une pipeline LINQ

```csharp
var names = users
    .Where(user => user.IsActive)
    .OrderBy(user => user.Name)
    .Select(user => user.Name)
    .ToList();
```

Lis-la :

```text
users
 ↓ garder actifs
 ↓ trier
 ↓ transformer en nom
 ↓ matérialiser
```

### Exercice

Décris chaque étape avant d'exécuter :

```csharp
var result = orders
    .Where(x => x.Total >= 100m)
    .OrderByDescending(x => x.Total)
    .Take(5)
    .Select(x => new { x.Id, x.Total })
    .ToList();
```

---

# 11. Exécution différée

Opérateurs généralement différés :

```text
Where
Select
OrderBy
Take
Skip
```

```csharp
var adults = users.Where(x => x.Age >= 18);

users.Add(new User { Name = "Bob", Age = 30 });

foreach (var adult in adults)
{
    Console.WriteLine(adult.Name);
}
```

Bob peut apparaître car la source est énumérée plus tard.

Opérations qui déclenchent une énumération :

```text
ToList
ToArray
First
Single
Count
Any
Sum
```

```csharp
var adults = users
    .Where(x => x.Age >= 18)
    .ToList();
```

matérialise immédiatement le résultat.

### Ré-énumération

```csharp
var query = ExpensiveSequence();

var count = query.Count();
foreach (var item in query)
{
    ...
}
```

Selon la source, le travail peut être exécuté deux fois. Une séquence différée n'est pas un cache.

---

# 12. Méthodes d'extension

```csharp
users.Where(...)
```

`Where` n'est pas une méthode ajoutée réellement dans `List<T>`.

Exemple :

```csharp
public static class OrderEnumerableExtensions
{
    public static IEnumerable<Order> Confirmed(
        this IEnumerable<Order> orders)
    {
        return orders.Where(
            x => x.Status == OrderStatus.Confirmed);
    }
}
```

```csharp
var confirmed = orders.Confirmed();
```

Une méthode d'extension reste fondamentalement une méthode statique avec une syntaxe d'appel plus naturelle.

---

## Application au projet fil rouge

Utilise LINQ pour :

- lister les commandes confirmées ;
- grouper par client ;
- calculer des agrégats ;
- construire un dictionnaire pour le repository mémoire ;
- récupérer des résumés.

La différence entre LINQ sur objets et LINQ traduit par EF Core sera approfondie au chapitre 8.

### Checkpoint

Tu dois pouvoir expliquer :

1. `List<T>` vs `Dictionary<TKey,TValue>` ;
2. `TryGetValue` vs indexeur ;
3. lien `HashSet<T>` ↔ `Equals` / `GetHashCode` ;
4. `Where` vs `Select` ;
5. `FirstOrDefault` vs `SingleOrDefault` ;
6. ce que change `ToList()` ;
7. pourquoi `IEnumerable<T>` ne signifie pas « liste en mémoire » ;
8. pourquoi une séquence différée peut refaire le travail si elle est énumérée deux fois.
