# Guide de départ, de travail et de reprise

## 1. Vérifier les prérequis

Tu sais déjà écrire une fonction, une condition et une boucle en JavaScript ou TypeScript. Tu sais ouvrir un terminal et naviguer entre des dossiers. Tu peux lire un JSON simple. SQL et .NET ne sont pas requis.

Si ces bases sont fragiles, prévois une remise à niveau avant les chapitres HTTP. Le parcours apprend le backend .NET, pas la programmation depuis zéro.

## 2. Récupérer le dépôt

Avec Git :

```bash
git clone https://github.com/SimonGelbart/formation-dotnet.git
cd formation-dotnet
```

Sans Git : sur la page GitHub, utilise **Code → Download ZIP**, extrais l'archive puis ouvre le dossier contenant le README. Le nom peut être `formation-dotnet-main` : c'est aussi la racine du dépôt.

## 3. Choisir un environnement

Chemin conseillé : **VS Code**, extension **C# Dev Kit** (éditeur Microsoft), SDK **.NET 10**, extension **REST Client** (éditeur Huachao Mao) pour les fichiers `.http`. Les liens d'installation sont dans le [chapitre 0](00-demarrer.md) pour .NET ; installe les extensions depuis le panneau Extensions de VS Code en vérifiant leur éditeur.

Rider ou Visual Studio conviennent aussi si tu les utilises déjà. Dans ce cas, utilise leur débogueur et leur client HTTP intégré lorsqu'il est disponible. Tu n'as pas besoin d'installer plusieurs éditeurs.

Vérifie dans un nouveau terminal :

```bash
dotnet --list-sdks
```

Une version `10.0.x` doit apparaître. Reprends ensuite le [chapitre 0](00-demarrer.md) : crée Sandbox, exécute le programme et vérifie un breakpoint. Dans VS Code, ouvre le dossier du projet, attends son chargement C#, puis utilise **Exécuter et déboguer** et sélectionne le projet .NET proposé.

## 4. Séparer support et travail personnel

Crée un dossier `mes-exercices-dotnet` **à côté** du dépôt, pas à l'intérieur. Il contiendra :

- `Sandbox` pour les petits programmes qui se remplacent d'un exercice à l'autre ;
- `MonCatalogue` pour l'API construite dans les ateliers ;
- une note `progression.md` pour reprendre ta séance.

Le dépôt reste une référence propre. Pour une expérience qui demande de modifier une correction, copie son dossier complet dans ton espace personnel. Pour SQLite, copie tout `02-api-sqlite`, y compris son dossier `.config`, et pas seulement Catalogue.Api.

Les commandes « depuis la racine du dépôt » concernent le support. Dans les ateliers, les commandes partent de `mes-exercices-dotnet` ou `MonCatalogue`, comme indiqué. Les deux espaces sont indépendants.

Avec Git dans ton travail personnel, fais un commit après chaque étape fonctionnelle. Sans Git, duplique le dossier avant une transformation importante, en excluant bin, obj et les bases de données si tu veux repartir avec des données neuves.

## 5. Découper la formation en séances

Estimations de travail actif, à ajuster selon ton aisance. Elles incluent les exercices et la recherche d'erreurs, pas seulement la lecture. Les approfondissements sont exclus.

| Bloc | Supports | Temps indicatif | Preuve de progression |
|---|---|---|---|
| Installation et C# | 00–02 | 3–5 h | Déboguer une règle de prix |
| Collections et objets collaborateurs | 03–04 | 3–5 h | Remplacer un catalogue derrière une interface |
| Première API | 05 + atelier HTTP + défi recherche | 4–6 h | GET filtré et POST écrits soi-même |
| Async et base | 06–07 + atelier SQLite | 4–6 h | Retrouver un produit après redémarrage |
| Commandes et tests | 08–09 + défi renommage | 4–6 h | Prix historique et erreurs HTTP vérifiés |
| Conception et bilan | 10–11 | 3–5 h | Ajouter Description de bout en bout |

Soit environ **21–33 heures**, à considérer comme un point de départ. Fractionne en séances de 60 à 90 minutes si cela te convient. Un bloc n'est pas une séance obligatoire.

## 6. Exécuter une requête HTTP

Dans VS Code avec REST Client, ouvre un fichier `.http`. Clique **Send Request** au-dessus d'une requête. Une réponse doit montrer le statut, les headers et le corps. Lance d'abord l'API dans un terminal, puis garde ce terminal ouvert.

Les lignes `@productId` et `@orderId` contiennent des variables : copie les vrais identifiants des réponses à la place des zéros. `###` sépare les requêtes. Exécute le scénario dans l'ordre et lis les résultats attendus.

Sans client `.http`, commence par le GET avec curl. Les commandes avec JSON du chapitre 5 sont données pour Bash ; sous PowerShell, préfère le fichier HTTP pour éviter les différences de guillemets.

Un statut 204 signifie qu'il n'y a pas de corps. Une fenêtre JSON vide n'est donc pas un échec.

## 7. Finir et reprendre une séance

Dans ta note de progression, écris : chapitre/étape, dossier travaillé, dernière commande réussie, résultat observé et prochaine action. Exemple : « atelier SQLite, étape 3, MonCatalogue, GET retourne le clavier après redémarrage ; prochaine action : défi renommage ».

Avant d'arrêter : sauvegarde les fichiers, fais ton point de contrôle, arrête les serveurs avec Ctrl+C. Pour reprendre : ouvre le bon dossier, vérifie `dotnet build`, lance le serveur et rejoue un GET simple.

Une migration initiale se crée une seule fois. Reprendre la séance ne demande pas de refaire `migrations add InitialCreate`.

## 8. Quand continuer ? Quand demander de l'aide ?

Continue si tu peux refaire l'action attendue et expliquer son résultat. Consulter la syntaxe est normal. Si tu ne sais pas ce qui s'exécute, suis une requête avec un breakpoint avant d'ajouter du code.

Après environ 20 minutes sans nouvelle piste, prépare une demande d'aide contenant :

- le chapitre, l'étape et le dossier exact ;
- la commande exécutée et le premier message d'erreur complet ;
- le résultat attendu et ce que tu observes ;
- la modification faite depuis le dernier état fonctionnel.

Compare ensuite avec la correction correspondante. Si une correction propre échoue aussi, consulte la dernière validation du support et vérifie ta version du SDK. Une CI verte n'exclut pas un problème local.

## 9. Repartir avec une base vide

Dans **ton projet d'exercice uniquement**, arrête l'API, vérifie le dossier et la chaîne de connexion, puis utilise `dotnet ef database drop` et confirme la suppression de cette base d'exercice. Réapplique ensuite `dotnet ef database update`. Les produits précédents seront perdus, les migrations conservées.

Ne supprime pas les migrations pour réparer un problème de données. Ne lance pas une suppression de base sur une connexion dont tu ne connais pas la destination.
