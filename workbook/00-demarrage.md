# 0 — Démarrer et exécuter les exemples

## Objectif

Avant de parler de C#, de POO ou d'ASP.NET Core, il faut savoir **où écrire le code et comment l'exécuter**.

Ce chapitre est volontairement court. Les détails sur le SDK, les projets, NuGet et les solutions viendront au chapitre 6.

À la fin de ce chapitre, tu dois savoir :

- vérifier que le SDK .NET est installé ;
- créer un petit projet console ;
- lancer un programme ;
- repérer `Program.cs` et le fichier `.csproj` ;
- modifier puis réexécuter un exemple du workbook.

---

## 1. Vérifier .NET

Dans un terminal :

```bash
dotnet --info
```

Le workbook utilise .NET 10 comme référence. Tu dois au minimum voir un SDK .NET 10 installé.

Si `dotnet` n'est pas reconnu, l'environnement n'est pas encore prêt : corrige cela avant de continuer.

---

## 2. Créer un bac à sable

Crée un projet console dédié aux petits exemples :

```bash
dotnet new console -n Sandbox
cd Sandbox
```

Le dossier contient notamment :

```text
Sandbox/
├── Program.cs
└── Sandbox.csproj
```

Pour l'instant, retiens simplement :

- `Program.cs` contient le code exécuté ;
- `Sandbox.csproj` décrit le projet ;
- `dotnet run` compile puis exécute le programme.

---

## 3. Premier programme

Remplace le contenu de `Program.cs` par :

```csharp
var language = "C#";
Console.WriteLine($"Hello {language}");
```

Puis :

```bash
dotnet run
```

Tu dois obtenir :

```text
Hello C#
```

---

## 4. La méthode de travail du workbook

Pour les petits exemples, utilise ce projet `Sandbox` et suis ce cycle :

```text
1. lire le code
2. prédire ce qu'il va faire
3. l'exécuter
4. expliquer le résultat avec tes propres mots
5. modifier une chose
6. réexécuter et observer
```

Exemple :

```csharp
var value = 10;
var copy = value;
copy = 20;

Console.WriteLine(value);
```

Avant d'exécuter : **prédit le résultat**.

Ensuite seulement, lance :

```bash
dotnet run
```

Cette habitude sera utilisée dans tout le workbook.

---

## 5. IDE ou éditeur

Utilise l'environnement de développement choisi par l'équipe : Visual Studio, JetBrains Rider, Visual Studio Code ou un autre éditeur compatible C#.

L'outil n'est pas le sujet de la formation. En revanche, apprends rapidement à :

- lancer le projet ;
- lancer les tests ;
- naviguer vers la définition d'un type ;
- lire les erreurs et warnings du compilateur ;
- utiliser le débogueur et poser un breakpoint.

### Mini-exercice de debug

Place un breakpoint sur :

```csharp
Console.WriteLine(value);
```

Observe la valeur de `value` et de `copy` avant l'affichage.

---

## Checkpoint

Avant de continuer, tu dois être capable de répondre oui à ces questions :

- `dotnet --info` fonctionne ;
- je peux créer un projet console ;
- je sais où se trouve `Program.cs` ;
- je peux exécuter avec `dotnet run` ;
- je sais modifier un exemple et observer son comportement ;
- je sais au minimum poser un breakpoint dans mon IDE.

Si c'est le cas, passe au chapitre 1 : **De TypeScript à C#**.
