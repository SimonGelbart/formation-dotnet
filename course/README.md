# Cours HTML interactif — proposition Codebase-to-Course

Ce dossier propose une vue interactive du workbook sans remplacer les Markdown. Les fichiers `parcours/` et `approfondissements/` restent la source éditoriale détaillée ; le cours HTML met en scène les mêmes objectifs sous forme de flux, traductions code → explication, quiz et checkpoints.

## Construire

Depuis la racine du dépôt :

```bash
bash course/build.sh
```

Puis ouvrir `course/index.html` dans un navigateur.

## Découpage

| Module interactif | Workbook couvert |
|---|---|
| 1 — Prendre pied dans .NET | `00-demarrer.md`, `01-csharp.md` |
| 2 — Objets, LINQ et dépendances | `02-objets.md`, `03-collections-linq.md`, `04-dependances.md` |
| 3 — HTTP et async | `05-api-http.md`, `06-async-erreurs.md`, atelier HTTP, défi recherche |
| 4 — EF Core et commandes | `07-persistance.md`, `08-commandes.md`, atelier SQLite |
| 5 — Tests et conception | `09-tests.md`, `10-conception.md`, défi renommage |
| 6 — Bilan et carte de progression | `11-bilan.md`, `notions.md`, liens vers les approfondissements |

## Principes

- Les exemples de code sont repris du workbook ou des applications de référence.
- Les interactions servent la méthode déjà présente dans le support : prédire → exécuter → observer → expliquer → modifier → comparer.
- Chaque module contient au moins un bloc code ↔ explication et un quiz d'application.
- Le module HTTP contient une conversation entre composants et un flux requête/réponse animé.
- Les ateliers restent des activités à réaliser dans le projet personnel ; la version HTML fournit des checkpoints et des critères, pas un éditeur C# factice.
- Les approfondissements restent accessibles à la demande plutôt que d'être injectés dans le flux principal.

## Maintenance

`_base.html` + `modules/*.html` + `_footer.html` sont assemblés par `build.sh`. `styles.css` et `main.js` sont autonomes et spécifiques à ce dépôt. Le design reprend le modèle d'interaction de `codebase-to-course` sans dépendance JavaScript externe.
