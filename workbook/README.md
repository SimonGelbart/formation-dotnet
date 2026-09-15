# Workbook — Formation .NET

Ce dossier contient la version **autoformation** de la formation .NET. Le lecteur est supposé avoir déjà une expérience front-end, en particulier JavaScript/TypeScript.

L'objectif n'est pas d'apprendre la programmation depuis zéro, mais de comprendre les différences de modèle mental entre le front-end TypeScript et le développement backend en C#/.NET.

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

La cible de sortie est la suivante : être capable de lire, comprendre, modifier et construire proprement une API .NET classique utilisant ASP.NET Core, l'injection de dépendances, LINQ, EF Core et des tests.

## Hors périmètre initial

Ces sujets sont volontairement gardés pour une seconde étape : authentification / autorisation, Docker, cache distribué, messaging, microservices, CQRS / MediatR, observabilité avancée, Kubernetes et cloud.
