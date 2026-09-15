# 6 — Écosystème .NET et NuGet

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer SDK et runtime ;
- vérifier l'environnement avec `dotnet --info` ;
- comprendre projet, solution et `.csproj` ;
- lire `TargetFramework`, `Nullable` et `ImplicitUsings` ;
- comprendre `.slnx` et `.sln` ;
- comprendre `global.json` ;
- utiliser les commandes `dotnet` principales ;
- distinguer `ProjectReference`, `PackageReference` et outil .NET ;
- comprendre NuGet et les dépendances transitives ;
- installer et identifier `dotnet-ef`.

Le chapitre 0 t'a appris juste assez de CLI pour lancer du code. Ici, on comprend réellement l'écosystème.

---

# 1. SDK vs runtime

## Runtime

Permet d'exécuter une application .NET déjà construite.

## SDK

Contient les outils de développement :

```text
création de projets
compilation
restore NuGet
tests
publication
outils CLI
```

Sur un poste de développement, on installe généralement le SDK.

### Vérifier

```bash
dotnet --info
```

Observe :

- SDK installés ;
- runtimes ;
- architecture ;
- système ;
- SDK réellement sélectionné.

---

# 2. Lire un `.csproj`

Exemple :

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

Ne le regarde pas comme du XML mystérieux.

## `Sdk="Microsoft.NET.Sdk.Web"`

Indique que le projet utilise le SDK web, avec les conventions et références nécessaires à ASP.NET Core.

## `TargetFramework`

```xml
<TargetFramework>net10.0</TargetFramework>
```

Le projet cible .NET 10.

## `Nullable`

```xml
<Nullable>enable</Nullable>
```

Active notamment l'analyse des Nullable Reference Types utilisée au chapitre 1.

## `ImplicitUsings`

```xml
<ImplicitUsings>enable</ImplicitUsings>
```

Le SDK ajoute automatiquement certains `using` courants selon le type de projet.

### Exercice

Désactive temporairement `ImplicitUsings`, compile et observe les erreurs. Remets ensuite la configuration initiale.

---

# 3. Projet et solution

Le `.csproj` décrit **un projet**.

Une solution regroupe plusieurs projets qui travaillent ensemble.

Avec .NET 10 :

```bash
dotnet new sln -n Formation
```

crée par défaut :

```text
Formation.slnx
```

Exemple :

```text
Formation.slnx
├── Formation.Api
├── Formation.Application
├── Formation.Domain
└── Formation.Infrastructure
```

Pour demander explicitement l'ancien format :

```bash
dotnet new sln -n Formation --format sln
```

La présence de plusieurs projets n'est pas un signe automatique de qualité. Une petite application peut parfaitement commencer avec un seul projet applicatif.

---

# 4. `global.json`

Une machine peut avoir plusieurs SDK.

Un dépôt peut préciser sa politique de sélection :

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

Le numéro exact dépend du dépôt.

`global.json` n'est pas obligatoire, mais permet de rendre l'environnement attendu plus explicite.

---

# 5. Commandes de base

Créer une API Controllers :

```bash
dotnet new webapi --use-controllers -n Formation.Api
```

Restaurer :

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

Tester :

```bash
dotnet test
```

Publier :

```bash
dotnet publish
```

`dotnet build` effectue généralement un restore implicite s'il est nécessaire.

---

# 6. Références entre projets

Avec .NET 10, la forme noun-first est :

```bash
dotnet reference add \
  Formation.Application/Formation.Application.csproj \
  --project Formation.Api/Formation.Api.csproj
```

L'ancienne forme `dotnet add ... reference ...` continue d'exister dans de nombreux projets et documentations.

Dans le `.csproj` :

```xml
<ItemGroup>
  <ProjectReference Include="..\Formation.Application\Formation.Application.csproj" />
</ItemGroup>
```

### Impact architectural

Une `ProjectReference` n'est pas qu'un détail de build : elle crée une dépendance de code entre deux projets.

Si `Domain` référence `Infrastructure`, ce choix change le sens de dépendance de l'architecture.

---

# 7. NuGet

NuGet joue un rôle comparable à npm dans l'écosystème JavaScript, même si les modèles de projet diffèrent.

Ajouter un package en .NET 10 :

```bash
dotnet package add Some.Package
```

Dans le projet :

```xml
<ItemGroup>
  <PackageReference Include="Some.Package" Version="1.2.3" />
</ItemGroup>
```

### ProjectReference vs PackageReference

```text
ProjectReference
→ autre projet source de la solution

PackageReference
→ package NuGet versionné
```

---

# 8. Restore

`dotnet restore` :

1. lit les références ;
2. résout les versions ;
3. récupère les packages absents ;
4. prépare les informations nécessaires au build.

Le dépôt Git ne versionne généralement pas tous les binaires des packages.

---

# 9. Dépendances transitives

```text
Application
  ↓
Package A
  ↓
Package B
```

`Package B` est une dépendance transitive.

Avec .NET 10 :

```bash
dotnet package list --include-transitive
```

Tu peux aussi examiner les packages obsolètes :

```bash
dotnet package list --outdated
```

### Question

Pourquoi une application peut-elle charger beaucoup plus de packages qu'il n'y a de `PackageReference` visibles ?

<details>
<summary>Réponse</summary>

Parce que chaque package direct peut lui-même dépendre d'autres packages. Le graphe final contient les dépendances directes et transitives.
</details>

---

# 10. Package vs outil .NET

EF Core utilise un outil CLI :

```bash
dotnet ef
```

Installation globale possible :

```bash
dotnet tool install --global dotnet-ef
```

Vérification :

```bash
dotnet ef --version
```

Distinction :

```text
PackageReference
→ bibliothèque utilisée par le projet

dotnet tool
→ commande utilisée pendant développement / build
```

Un outil peut aussi être installé localement au dépôt via un tool manifest ; une installation globale n'est donc pas la seule stratégie.

---

# 11. Ne pas installer un package pour chaque problème

Avant d'ajouter une dépendance :

- le framework fait-il déjà cela ?
- le package est-il maintenu ?
- quelles dépendances transitives apporte-t-il ?
- quelle surface de sécurité et de maintenance ajoute-t-il ?
- quelques lignes simples suffiraient-elles ?

Un package est du code tiers qu'il faudra mettre à jour et comprendre.

---

# 12. `bin/` et `obj/`

```text
bin/
obj/
```

contiennent des artefacts de compilation et fichiers intermédiaires.

Ils ne représentent généralement pas du code source à versionner.

---

## Exercice — autopsie d'un projet

Dans l'Order API :

1. ouvre le `.csproj` ;
2. explique `Sdk`, `TargetFramework`, `Nullable`, `ImplicitUsings` ;
3. liste les `PackageReference` ;
4. liste les `ProjectReference` ;
5. lance `dotnet package list --include-transitive` ;
6. vérifie `dotnet --info` ;
7. vérifie si un `global.json` existe ;
8. exécute `dotnet restore`, `build`, `test` ;
9. vérifie `dotnet ef --version`.

---

## Application au projet fil rouge

À ce stade, la solution minimale peut rester :

```text
OrderApi.slnx
├── OrderApi
└── OrderApi.Tests
```

Ne crée pas quatre couches uniquement parce que tu connais leur nom. Le chapitre architecture introduira ce découpage **après** avoir rencontré les problèmes qu'il peut résoudre.

### Checkpoint

Tu dois pouvoir expliquer :

- SDK vs runtime ;
- `.csproj` vs `.slnx` ;
- rôle de `global.json` ;
- `ProjectReference` vs `PackageReference` ;
- package vs `dotnet tool` ;
- dépendance directe vs transitive ;
- pourquoi `dotnet build` peut restaurer implicitement les dépendances.
