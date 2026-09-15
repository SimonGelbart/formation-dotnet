# 6 — Écosystème .NET et NuGet

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer SDK et runtime ;
- comprendre la structure d'une solution et d'un projet ;
- lire les éléments essentiels d'un fichier `.csproj` ;
- utiliser les commandes `dotnet` principales ;
- comprendre les références entre projets ;
- comprendre le rôle de NuGet ;
- ajouter, restaurer et mettre à jour une dépendance ;
- comprendre les dépendances transitives.

---

## 1. SDK vs runtime

### Runtime

Le runtime permet **d'exécuter** une application .NET déjà construite.

### SDK

Le SDK contient les outils nécessaires pour développer :

- compiler ;
- créer des projets ;
- restaurer les packages ;
- lancer les tests ;
- publier l'application.

Pour un poste de développement, on installe généralement le SDK.

---

## 2. Projet et solution

Un projet .NET est décrit par un fichier `.csproj`.

Exemple simplifié :

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

La solution (`.sln` ou format de solution équivalent) sert principalement à regrouper plusieurs projets travaillant ensemble.

Exemple :

```text
Formation.sln
├── Formation.Api
├── Formation.Application
├── Formation.Domain
└── Formation.Infrastructure
```

Cette séparation n'est pas obligatoire. Une petite application peut parfaitement commencer avec un seul projet.

---

## 3. Les commandes `dotnet` à connaître

Créer un projet :

```bash
dotnet new webapi -n Formation.Api
```

Restaurer les packages :

```bash
dotnet restore
```

Compiler :

```bash
dotnet build
```

Lancer :

```bash
dotnet run
```

Lancer les tests :

```bash
dotnet test
```

### Exercice

Crée une solution vide, ajoute un projet Web API puis un projet de tests. Lance `dotnet build` depuis la racine.

---

## 4. Références entre projets

Supposons :

```text
Api
Application
Domain
Infrastructure
```

Un projet peut référencer un autre projet :

```bash
dotnet add Formation.Api reference Formation.Application
```

Cette relation apparaît dans le `.csproj` sous la forme d'un `ProjectReference`.

```xml
<ItemGroup>
  <ProjectReference Include="..\Formation.Application\Formation.Application.csproj" />
</ItemGroup>
```

### Question importante

Le sens de ces références forme aussi le **graphe de dépendances de l'architecture**.

Si `Domain` référence `Infrastructure`, ce choix a un impact architectural. Il ne faut donc pas ajouter des références mécaniquement simplement pour faire compiler le code.

---

## 5. NuGet

NuGet joue un rôle comparable à npm dans l'écosystème JavaScript.

```text
npm package
   ↕
NuGet package
```

Ajouter un package :

```bash
dotnet add package Some.Package
```

Le projet obtient ensuite une référence similaire à :

```xml
<ItemGroup>
  <PackageReference Include="Some.Package" Version="1.2.3" />
</ItemGroup>
```

---

## 6. Restore

Le dépôt Git ne contient généralement pas les binaires de toutes les dépendances.

`dotnet restore` :

1. lit les références de packages ;
2. détermine les versions nécessaires ;
3. récupère les packages absents ;
4. prépare les informations utilisées par le build.

`dotnet build` déclenche généralement aussi un restore si nécessaire.

---

## 7. Dépendances transitives

Si :

```text
Application
  ↓ utilise
Package A
  ↓ utilise
Package B
```

alors `Package B` est une dépendance transitive d'`Application`.

Tu ne l'as pas nécessairement ajoutée toi-même, mais elle fait partie du graphe de dépendances final.

Cela explique pourquoi une application peut embarquer beaucoup plus de packages qu'il n'y a de `PackageReference` directement visibles dans un projet.

---

## 8. Ne pas installer un package pour chaque problème

Avant d'ajouter un package, demande-toi :

- le framework fournit-il déjà cette fonctionnalité ?
- le package est-il maintenu ?
- quel est son coût en dépendances ?
- est-ce raisonnable pour le problème à résoudre ?
- est-ce qu'une dizaine de lignes simples suffiraient ?

Un package ajoute du code tiers, des versions à maintenir et une surface de risque supplémentaire.

---

## 9. Fichiers générés

Tu rencontreras notamment :

```text
bin/
obj/
```

Ces dossiers contiennent des artefacts de compilation et données intermédiaires. Ils ne représentent généralement pas du code source à versionner.

Le `.gitignore` des projets .NET standards les exclut normalement.

---

## Exercice — explorer un `.csproj`

Dans un projet existant :

1. ouvre le `.csproj` ;
2. identifie le `TargetFramework` ;
3. liste les `PackageReference` ;
4. liste les `ProjectReference` ;
5. lance `dotnet restore` puis `dotnet build` ;
6. explique la différence entre ces deux commandes.

---

## Application au projet fil rouge

Créer une solution contenant au minimum :

```text
OrderApi
OrderApi.Tests
```

Dans un premier temps, ne crée pas automatiquement quatre ou cinq couches. La séparation sera introduite lorsque le besoin architectural deviendra concret.

Ajoute ensuite les packages nécessaires au fur et à mesure du workbook, notamment ceux liés à EF Core et aux tests.
