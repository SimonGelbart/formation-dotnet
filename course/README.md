# Cours HTML interactif

Cette interface transforme les fichiers Markdown du dépôt en un cours HTML navigable, sans dupliquer le contenu pédagogique.

## Ouvrir le cours

Ouvrir `course/index.html` dans un navigateur moderne.

Par défaut, l'interface charge les fichiers `.md` depuis la branche `feat/cours-html-interactif`. Pour afficher une autre branche ou un tag, ajouter `?ref=<nom>` à l'URL, par exemple :

```text
index.html?ref=main
```

## Fonctionnement

- découverte automatique de tous les fichiers `.md` du dépôt via l'API GitHub ;
- classement en parcours principal, ateliers, annexes, approfondissements et documentation ;
- rendu Markdown compatible GitHub (GFM) ;
- navigation précédent/suivant et raccourcis clavier ← / → ;
- recherche dans le sommaire ;
- progression sauvegardée dans `localStorage` ;
- copie des blocs de code ;
- lien direct vers le fichier Markdown source ;
- mise en page responsive et impression d'un chapitre.

Le dossier `course/` est exclu de la découverte des chapitres afin que cette documentation ne s'ajoute pas elle-même au cours.

## Dépendances navigateur

L'interface charge `marked`, `DOMPurify` et les polices Google depuis des CDN. Le contenu des chapitres est lu depuis GitHub au moment de la consultation, ce qui garantit que les Markdown restent la source de vérité.
