# Workbook interactif

Cette vue HTML accompagne le parcours Markdown sans le remplacer.

## Principe

- un chapitre Markdown = une section HTML principale ;
- les ateliers HTTP, SQLite et les défis restent des sections séparées ;
- les interactions sont utilisées avec parcimonie (quiz ciblé, détails révélables, checklist d'atelier) ;
- les Markdown restent la source détaillée et les exercices continuent à se faire dans le projet personnel.

## Générer

```bash
bash course/build.sh
```

Puis ouvrir `course/index.html` dans un navigateur.

## Structure

Le sommaire reprend explicitement 00 → 11 et distingue quatre groupes : Fondations, Backend, Qualité & conception, Ateliers. L'objectif est de conserver les repères du workbook au lieu de condenser plusieurs chapitres en macro-modules.
