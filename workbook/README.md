# Workbook — Formation .NET

Ce dossier contient la version **autoformation** de la formation .NET. Le lecteur est supposé avoir déjà une expérience front-end, en particulier JavaScript/TypeScript.

L'objectif n'est pas d'apprendre la programmation depuis zéro, mais de comprendre les différences de modèle mental entre le front-end TypeScript et le développement backend en C#/.NET.

## Référence technique

Le workbook prend **.NET 10** comme environnement de référence. Certains concepts sont valables sur de nombreuses versions de .NET, mais les commandes et exemples de tooling sont écrits pour l'écosystème actuel :

```text
.NET 10
ASP.NET Core
Entity Framework Core
xUnit
```

Le but n'est pas d'apprendre les nouveautés de C# ou .NET par cœur. La priorité reste les fondamentaux durables : système de types, POO, dépendances, LINQ, async, HTTP, persistance, tests et architecture.

## Mode d'emploi

Chaque chapitre suit autant que possible la même structure :

1. objectifs ;
2. explication de la notion ;
3. parallèle avec TypeScript / le front-end ;
4. exemple C# minimal ;
5. exemple réaliste ;
6. pièges fréquents ;
7. exercice ou checkpoint ;
8. application au projet fil rouge.

Le lecteur est encouragé à **prédire le comportement du code avant de l'exécuter** et à réaliser les exercices dans un projet local.

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

Les chapitres contiennent volontairement certains exercices d'observation : lifetimes DI, exécution différée LINQ, SQL généré par EF Core, validation HTTP, etc.

## Parcours

1. [De TypeScript à C#](01-typescript-vers-csharp.md)
2. [Modéliser avec les objets](02-modelisation-objet.md)
3. [Abstraction, interfaces et dépendances](03-abstraction-et-dependances.md)
4. [Génériques, collections et LINQ](04-collections-et-linq.md)
5. [Exceptions, ressources et asynchronisme](05-exceptions-et-async.md)
6. [Écosystème .NET et NuGet](06-ecosysteme-dotnet-et-nuget.md)
7. [Construire une API ASP.NET Core](07-aspnet-core.md)
8. [SQL et Entity Framework Core](08-sql-et-ef-core.md)
9. [Tests, architecture et design patterns](09-tests-architecture-patterns.md)
10. [Projet fil rouge — Order API](10-projet-fil-rouge.md)

## Projet fil rouge

Tout au long du workbook, une petite **Order API** sert de support. Elle évolue progressivement :

```text
Code métier simple
    ↓
POO et encapsulation
    ↓
Interfaces et injection de dépendances
    ↓
API HTTP
    ↓
Persistance avec EF Core
    ↓
Tests
    ↓
Architecture et refactoring
```

Le projet conserve notamment une distinction importante entre :

```text
Product.Price
→ prix courant du catalogue

OrderItem.UnitPrice
→ prix capturé au moment de la commande
```

Cette décision évite qu'un changement du catalogue modifie l'historique d'une ancienne commande.

La cible de sortie est la suivante : être capable de lire, comprendre, modifier et construire proprement une API .NET classique utilisant ASP.NET Core, l'injection de dépendances, LINQ, EF Core et des tests.

## Critère de réussite

Le workbook n'est pas validé uniquement lorsque « le code compile ».

Le lecteur doit être capable d'expliquer les décisions principales, par exemple :

- pourquoi un objet protège ses invariants ;
- pourquoi un service reçoit ses dépendances ;
- pourquoi un lifetime DI change le comportement ;
- quand LINQ exécute réellement une séquence ;
- quelle partie d'une requête EF Core s'exécute en SQL ;
- pourquoi une API utilise des DTOs ;
- différence entre test unitaire et test d'intégration ;
- pourquoi une couche ou un design pattern est présent.

## Hors périmètre initial

Ces sujets sont volontairement gardés pour une seconde étape : authentification / autorisation, Docker, cache distribué, messaging, microservices, CQRS / MediatR, observabilité avancée, Kubernetes et cloud.
