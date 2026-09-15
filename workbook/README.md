# Workbook — Formation .NET

Ce dossier contient la version **autoformation** de la formation .NET. Le lecteur est supposé avoir déjà une expérience front-end, en particulier JavaScript/TypeScript.

L'objectif n'est pas d'apprendre la programmation depuis zéro, mais de comprendre les différences de modèle mental entre le front-end TypeScript et le développement backend en C#/.NET.

## Référence technique

Le workbook prend **.NET 10** comme environnement de référence.

```text
.NET 10
ASP.NET Core
Entity Framework Core
xUnit
SQLite pour les exercices locaux
```

La priorité reste les fondamentaux durables : système de types, POO, dépendances, LINQ, async, HTTP, persistance, tests et architecture.

---

## Mode d'emploi

Chaque chapitre suit autant que possible le même cycle :

```text
objectif
 ↓
explication
 ↓
parallèle TypeScript / front
 ↓
exemple minimal
 ↓
contre-exemple / piège
 ↓
expérience ou exercice
 ↓
correction / critères de réussite
 ↓
application au projet fil rouge
```

### Ne pas lire passivement

Pour chaque exemple important :

```text
1. lire le code
2. prédire le résultat
3. l'exécuter
4. expliquer le résultat avec ses propres mots
5. modifier un élément
6. observer la conséquence
```

Le [chapitre 0](00-demarrage.md) montre comment créer un petit projet `Sandbox` pour tester les exemples.

---

## Parcours

0. [Démarrer et exécuter les exemples](00-demarrage.md)
1. [De TypeScript à C#](01-typescript-vers-csharp.md)
2. [Modéliser avec les objets](02-modelisation-objet.md)
3. [Abstraction, interfaces et dépendances](03-abstraction-et-dependances.md)
4. [Génériques, collections et LINQ](04-collections-et-linq.md)
5. [Exceptions, ressources et asynchronisme](05-exceptions-et-async.md)
6. [Écosystème .NET et NuGet](06-ecosysteme-dotnet-et-nuget.md)
7. [Construire une API ASP.NET Core](07-aspnet-core.md)
8. [SQL et Entity Framework Core](08-sql-et-ef-core.md)
9. [Tester une application .NET](09-tests.md)
10. [Architecture et design patterns](10-architecture-patterns.md)
11. [Projet fil rouge — Order API](11-projet-fil-rouge.md)

---

## Pourquoi certaines abstractions évoluent pendant le parcours ?

Le workbook évite volontairement d'utiliser des notions avant de les enseigner.

Par exemple, au chapitre 3, le repository commence simplement :

```csharp
public interface IOrderRepository
{
    Order? GetById(Guid id);
    void Add(Order order);
}
```

Puis, après l'apprentissage de `Task`, `async` et `CancellationToken`, le chapitre 5 le fait évoluer vers une frontière asynchrone.

L'objectif est de comprendre **pourquoi** une abstraction change, pas de recopier dès le début une signature complexe sans comprendre ses éléments.

---

## Projet fil rouge

Une petite **Order API** sert de support transversal.

Elle évolue progressivement :

```text
modèle métier simple
    ↓
encapsulation
    ↓
interfaces et DI
    ↓
async / cancellation
    ↓
API HTTP
    ↓
EF Core / SQL
    ↓
tests
    ↓
architecture / patterns
```

### Modèle métier commun à tout le workbook

```text
Product.Price
→ prix actuel du catalogue

OrderItem.UnitPrice
→ prix capturé au moment de la commande
```

Ainsi, modifier le catalogue ne réécrit pas l'histoire d'une ancienne commande.

Dans la version relationnelle utilisée dans le workbook :

```text
Orders
- Id
- CustomerId
- Status
- CreatedAt

OrderItems
- Id
- OrderId
- ProductId
- ProductName
- UnitPrice
- Quantity
```

`Order.Total` est calculé à partir des items ; il n'est pas présenté comme une colonne `Orders.Total` implicite.

---

## Critère de réussite

Le workbook n'est pas validé uniquement lorsque « le code compile ».

Le lecteur doit savoir expliquer notamment :

- value type vs reference type ;
- passage par valeur d'une référence ;
- pourquoi un objet protège ses invariants ;
- pourquoi un service reçoit ses dépendances ;
- ce que change un lifetime DI ;
- pourquoi certaines frontières deviennent asynchrones ;
- quand LINQ exécute réellement une séquence ;
- quelle partie d'une requête EF devient du SQL ;
- comment EF persiste un modèle encapsulé ;
- pourquoi une API utilise des DTOs ;
- différence test unitaire / intégration ;
- pourquoi une couche ou un design pattern est présent.

---

## Hors périmètre initial

Ces sujets sont volontairement gardés pour une seconde étape :

```text
authentification / autorisation
Docker
cache distribué
messaging
observabilité avancée
CQRS / MediatR
microservices
Kubernetes / cloud
```

La cible de sortie est déjà ambitieuse : être capable de lire, comprendre, modifier, tester et construire proprement une API .NET classique utilisant ASP.NET Core, DI, LINQ et EF Core.
