# 00 — Démarrer et observer

**Pratiquer :** créer, lancer et déboguer un programme. **Comprendre :** projet, SDK et runtime.

Résultat : le terminal affiche `Bonjour .NET`.

## Préparer l'environnement

Installe le **SDK .NET 10** depuis la [page officielle .NET](https://dotnet.microsoft.com/download/dotnet/10.0). Le runtime exécute une application ; le SDK contient les outils pour la développer. Installe le SDK correspondant à ton système et à l'architecture de ta machine, puis rouvre le terminal.

Utilise Visual Studio, Rider ou VS Code avec son extension C# pour ouvrir le dossier de travail. Garde le même éditeur pendant la formation.

```bash
dotnet --info
dotnet --list-sdks
```

Une version `10.0.x` doit apparaître dans la liste des SDK. Si `dotnet` est inconnu, vérifie l'installation et rouvre le terminal avant de continuer. Si plusieurs SDK sont installés, le projet créé doit bien cibler `net10.0`.

Dans un dossier de travail personnel, en dehors des applications de référence :

```bash
dotnet new console -n Sandbox --framework net10.0
cd Sandbox
```

`Program.cs` est le point d'entrée. `Sandbox.csproj` décrit le projet, notamment sa version de .NET. Ne modifie pas encore son XML.

## Programme complet — `Program.cs`

```csharp
Console.WriteLine("Bonjour .NET");
```

Depuis le dossier `Sandbox` :

```bash
dotnet run
```

Résultat : `Bonjour .NET`. `run` compile puis lance le programme. C# vérifie les types avant l'exécution ; une erreur de compilation empêche le programme de démarrer.

## Premier débogage

Remplace le programme par :

```csharp
var price = 30m;
var quantity = 2;
var total = price * quantity;
Console.WriteLine(total);
```

Le suffixe `m` indique un nombre `decimal`, utilisé ici pour les prix.

Dans l'éditeur, place un breakpoint sur la dernière ligne en cliquant dans la marge, puis lance avec le débogueur. Observe `price`, `quantity` et `total`. Si l'éditeur demande une configuration, sélectionne le projet console `Sandbox`.

## Exercice

Avant de lancer, prédis le total pour trois produits à 12 euros. Modifie le programme, puis vérifie avec le débogueur.

<details>
<summary>Correction</summary>

Remplace `price` par `12m` et `quantity` par `3`. Le total vaut `36`. Le breakpoint permet d'observer les valeurs avant l'affichage.
</details>

## Si cela bloque

| Symptôme | Premier contrôle |
|---|---|
| Aucun projet trouvé | Le terminal est-il dans `Sandbox`, à côté du `.csproj` ? |
| Le framework cible n'est pas pris en charge | Le SDK 10 est-il installé et sélectionné ? |
| Erreur de compilation | Lire la première erreur, son fichier et sa ligne |
| Le breakpoint n'est pas atteint | As-tu lancé avec le débogueur et le bon projet ? |

## Trois questions

1. Dans quel fichier écris-tu ce programme ?
2. Quelle différence entre compiler et exécuter ?
3. Comment observer une variable sans ajouter un affichage ?

Suite : [01 — C#](01-csharp.md).
