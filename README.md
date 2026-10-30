# Argos

Plateforme de resolution de personne a partir d'un identifiant Discord, d'un pseudo, d'un email, d'un telephone ou d'un nom.

## Architecture

- `Argos.Core` : logique metier, sans couplage a l'interface.
- `Argos.App` : interface WinForms.
- `tests` : tests unitaires.

## Build

Le workflow `build.yml` compile et publie l'exe en artefact.

## Usage

Bouton Token, coller un token utilisateur Discord. Saisir un ID, Entree. Double-clic sur un pivot.
