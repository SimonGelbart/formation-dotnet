"""Contrôles hors ligne du support : liens locaux, blocs, JSON et projets XML."""
from pathlib import Path
import json
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []
files = [p for p in ROOT.rglob('*') if p.is_file() and not any(
    part in {'.git', 'bin', 'obj', 'TestResults'} for part in p.relative_to(ROOT).parts)]
for path in files:
    try:
        if path.suffix == '.json':
            json.loads(path.read_text(encoding='utf-8'))
        elif path.suffix == '.csproj':
            ET.parse(path)
        elif path.suffix == '.md':
            content = path.read_text(encoding='utf-8')
            if len(re.findall(r'^```', content, re.M)) % 2:
                errors.append(f'{path.relative_to(ROOT)} : bloc de code non fermé')
            for target in re.findall(r'\]\(([^)]+)\)', content):
                if '://' in target or target.startswith(('#', 'mailto:')):
                    continue
                file_target = target.split('#')[0]
                if file_target and not (path.parent / file_target).exists():
                    errors.append(f'{path.relative_to(ROOT)} : lien absent {target}')
    except (ValueError, ET.ParseError) as exception:
        errors.append(f'{path.relative_to(ROOT)} : {exception}')
for error in errors:
    print(error, file=sys.stderr)
print(f'{len(files)} fichiers inspectés ; {len(errors)} erreur(s).')
sys.exit(bool(errors))
