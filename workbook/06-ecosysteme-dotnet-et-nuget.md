# 6 — Écosystème .NET et NuGet

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer SDK et runtime ;
- vérifier l'environnement installé avec `dotnet --info` ;
- comprendre la structure d'une solution et d'un projet ;
- comprendre la différence entre `.slnx` et `.sln` dans l'écosystème actuel ;
- lire les éléments essentiels d'un fichier `.csproj` ;
- comprendre le rôle d'un `global.json` ;
- utiliser les commandes `dotnet` principales ;
- comprendre les références entre projets ;
- comprendre le rôle de NuGet ;
- ajouter, restaurer et mettre à jour une dépendance ;
- comprendre les dépendances transitives ;
- comprendre le principe des outils .NET comme `dotnet-ef`.

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

### Vérifier l'environnement

```bash
dotnet --info
```

Cette commande permet notamment d'identifier :

- les SDK installés ;
- les runtimes installés ;
- l'architecture et le système ;
- la version réellement sélectionnée.

C'est souvent la première commande utile lorsqu'un projet fonctionne sur une machine mais pas sur une autre.

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

### Solution

Une solution sert principalement à regrouper plusieurs projets travaillant ensemble.

Avec .NET 10, la commande :

```bash
dotnet new sln -n Formation
```

crée par défaut le format moderne **`.slnx`**.

```text
Formation.slnx
├── Formation.Api
├── Formation.Application
├── Formation.Domain
└── Formation.Infrastructure
```

Si un environnement exige explicitement l'ancien format `.sln`, on peut le demander :

```bash
dotnet new sln -n Formation --format sln
```

Le concept important est la solution, pas l'extension à mémoriser.

Cette séparation n'est pas obligatoire. Une petite application peut parfaitement commencer avec un seul projet.

---

## 3. `global.json` : choisir le SDK du dépôt

Une machine peut avoir plusieurs SDK installés.

Un dépôt peut fournir un `global.json` afin de préciser la version ou la politique de sélection du SDK attendue.

Exemple :

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

Le numéro exact dépend évidemment de la version choisie par l'équipe.

### Pourquoi c'est utile ?

Sans règle de sélection, deux développeurs peuvent construire le même dépôt avec des SDK différents et obtenir des comportements ou warnings différents.

Le `global.json` n'est pas obligatoire, mais il rend l'environnement de développement plus explicite.

---

## 4. Les commandes `dotnet` à connaître

Créer un projet :

```bash
dotnet new webapi --use-controllers -n Formation.Api
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

Publier :

```bash
dotnet publish
```

### Exercice

Crée une solution vide, ajoute un projet Web API utilisant des controllers puis un projet de tests. Lance `dotnet build` depuis la racine.

Vérifie ensuite la version utilisée avec :

```bash
dotnet --info
```

---

## 5. Références entre projets

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

## 6. NuGet

NuGet joue un rôle comparable à npm dans l'écosystème JavaScript.

```text
npm package
   ↕
NuGet package
```

Avec la CLI moderne, on peut ajouter un package avec :

```bash
dotnet package add Some.Package
```

Tu rencontreras également encore beaucoup l'ancienne forme :

```bash
dotnet add package Some.Package
```

Le projet obtient ensuite une référence similaire à :

```xml
<ItemGroup>
  <PackageReference Include="Some.Package" Version="1.2.3" />
</ItemGroup>
```

Le point à retenir est le `PackageReference` dans le projet, pas seulement la syntaxe de la commande.

---

## 7. Restore

Le dépôt Git ne contient généralement pas les binaires de toutes les dépendances.

`dotnet restore` :

1. lit les références de packages ;
2. détermine les versions nécessaires ;
3. récupère les packages absents ;
4. prépare les informations utilisées par le build.

`dotnet build` déclenche généralement aussi un restore si nécessaire.

On lance néanmoins explicitement `restore` dans certains pipelines ou pour diagnostiquer un problème de dépendances.

---

## 8. Dépendances transitives

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

Cela explique pourquoi une application peut dépendre de beaucoup plus de packages qu'il n'y a de `PackageReference` directement visibles dans un projet.

### Exercice

Dans un projet réel, utilise :

```bash
dotnet list package --include-transitive
```

ou la commande équivalente disponible dans ton SDK pour observer les dépendances directes et transitives.

L'objectif est de comprendre qu'un package peut apporter tout un sous-graphe de dépendances.

---

## 9. Outils .NET : l'exemple `dotnet-ef`

Tous les outils utilisés dans un projet ne sont pas forcément des bibliothèques référencées par le code.

EF Core utilise notamment un outil CLI pour les migrations :

```bash
dotnet ef
```

Selon l'organisation du projet, il peut être installé comme outil global ou local.

Exemple d'installation globale :

```bash
dotnet tool install --global dotnet-ef
```

Puis :

```bash
dotnet ef --version
```

### Package vs tool

- un **package NuGet référencé par le projet** fournit généralement du code utilisé par l'application ;
- un **outil `dotnet`** fournit une commande utilisée pendant le développement ou le build.

Cette distinction deviendra concrète lors des migrations EF Core.

---

## 10. Ne pas installer un package pour chaque problème

Avant d'ajouter un package, demande-toi :

- le framework fournit-il déjà cette fonctionnalité ?
- le package est-il maintenu ?
- quel est son coût en dépendances ?
- est-ce raisonnable pour le problème à résoudre ?
- est-ce qu'une dizaine de lignes simples suffiraient ?

Un package ajoute du code tiers, des versions à maintenir et une surface de risque supplémentaire.

---

## 11. Fichiers générés

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
5. vérifie si le dépôt contient un `global.json` ;
6. lance `dotnet restore` puis `dotnet build` ;
7. explique la différence entre ces deux commandes ;
8. vérifie si `dotnet ef` est disponible.

---

## Application au projet fil rouge

Créer une solution contenant au minimum :

```text
OrderApi.slnx
├── OrderApi
└── OrderApi.Tests
```

Dans un premier temps, ne crée pas automatiquement quatre ou cinq couches. La séparation sera introduite lorsque le besoin architectural deviendra concret.

Ajoute ensuite les packages et outils nécessaires au fur et à mesure du workbook, notamment ceux liés à EF Core et aux tests.

### Checkpoint

Tu dois savoir expliquer :

- différence entre SDK et runtime ;
- rôle de `.csproj`, `.slnx`/`.sln` et `global.json` ;
- différence entre `ProjectReference` et `PackageReference` ;
- différence entre un package et un outil `dotnet` ;
- pourquoi `dotnet build` peut fonctionner sans avoir lancé manuellement `dotnet restore` juste avant.
