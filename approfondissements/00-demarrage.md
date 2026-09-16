# 0 — Bac à sable et débogage

> **Prérequis :** avoir commencé le [parcours principal](../parcours/README.md).
>
> **Niveau :** À approfondir pour le débogage · Référence pour le CLI de base.

Ce chapitre ne remplace pas le démarrage guidé du parcours principal. Il sert simplement de rappel lorsque tu veux isoler une notion C# dans un petit programme sans lancer toute l'API.

## Créer un bac à sable

```bash
dotnet new console -n Sandbox --framework net10.0
cd Sandbox
dotnet run
```

Les deux fichiers à repérer sont :

```text
Sandbox/
├── Program.cs
└── Sandbox.csproj
```

`Program.cs` contient le code exécuté. Le `.csproj` décrit le projet ; son contenu est approfondi dans [Écosystème .NET et NuGet](06-ecosysteme-dotnet-et-nuget.md).

## Cycle d'expérimentation

Pour une nuance de cette référence :

```text
1. lire
2. prédire
3. exécuter
4. expliquer
5. modifier une variable
6. réexécuter
```

Exemple :

```csharp
var value = 10;
var copy = value;
copy = 20;

Console.WriteLine(value);
```

Ne lance pas immédiatement. Prédis d'abord le résultat, puis vérifie :

```bash
dotnet run
```

## Utiliser le débogueur plutôt que multiplier les `Console.WriteLine`

Pour les exemples un peu moins évidents, pose un breakpoint et observe :

- les variables locales ;
- leur type ;
- la pile d'appels ;
- le chemin réellement exécuté ;
- les exceptions et warnings.

Exemple : place un breakpoint sur `Console.WriteLine(value)` et compare `value` et `copy`.

## Quand revenir ici ?

Utilise le `Sandbox` pour expérimenter notamment :

```text
value type vs reference type
Equals / GetHashCode
exécution différée LINQ
exceptions
Task / await
petites expériences de performance ou de collections
```

Pour ASP.NET Core, EF Core et les tests HTTP, préfère les applications de référence de `parcours/` plutôt que de reconstruire une nouvelle application depuis ce dossier.
