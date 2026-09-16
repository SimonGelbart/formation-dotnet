# Validation technique du support

Le workflow [Validation formation](.github/workflows/formation.yml) s'exécute sur les pull requests, les changements de main et à la demande. Consulte le résultat du commit que tu utilises dans [GitHub Actions](https://github.com/SimonGelbart/formation-dotnet/actions/workflows/formation.yml).

## Contrôles effectués

- Liens Markdown vers les fichiers locaux, fermeture des blocs de code, syntaxe JSON et XML des projets.
- Compilation de l'API mémoire et des quatre états corrigés des ateliers.
- Appels HTTP de lecture/création sur les exemples mémoire.
- Tests domaine, service et HTTP de Catalogue.Tests.
- Génération et application d'une migration dans une copie temporaire de chacune des deux APIs SQLite.
- Création HTTP, arrêt du processus puis relecture après redémarrage.
- Pour les commandes : statut confirmé, prix/nom historiques et conflit de seconde confirmation.

Les vérifications de migration sont distinctes des tests d'intégration qui utilisent EnsureCreated. Les migrations d'exercice sont générées depuis le modèle actuel dans la copie temporaire ; le contrôle ne valide pas un historique de migrations de production.

Les références .NET 10, EF Core et `dotnet-ef` sont alignées sur **10.0.12**. La validation doit rester sans avertissement NuGet de vulnérabilité connu (`NU1901` à `NU1904`) ; si un tel avertissement apparaît après une mise à jour ou la publication d'un nouvel avis de sécurité, traite-le avant de considérer le support comme validé.

## Exécuter localement

Depuis la racine du dépôt, avec Python 3.10+ :

```bash
python scripts/check_support.py
```

Avec le SDK .NET 10 et un accès à NuGet :

```bash
python scripts/validate_dotnet.py
```

Le second script travaille dans un dossier temporaire et n'altère pas tes bases ni les migrations du checkout. Il choisit des ports locaux libres, démarre et arrête ses propres serveurs. Un échec produit un code de sortie non nul ; les logs serveur sont affichés en cas d'échec de scénario.

Pour un contrôle ciblé sans Python :

```bash
dotnet build parcours/exemples/01-api-memoire/Catalogue.Memory.csproj
dotnet test parcours/exemples/02-api-sqlite/Catalogue.sln
```

## Ce que la validation ne garantit pas

Elle ne compile pas tous les extraits des approfondissements, ne vérifie pas les ancres Markdown ou les liens externes, et ne valide pas le projet personnel du participant. Le job .NET utilise Linux : une différence de terminal ou d'éditeur sur Windows/macOS reste possible.

Les corrections des défis sont des indications pédagogiques, pas des fonctionnalités déjà ajoutées à l'API de référence. Après les avoir appliquées dans ton projet, exécute tes tests et les scénarios d'acceptation de l'énoncé.

Une exécution verte signifie que le commit testé a passé ces contrôles. Si le workflow est absent, en attente, désactivé ou rouge, ne présente pas les exemples comme automatiquement validés.
