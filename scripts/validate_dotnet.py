"""Build, tests et vraie migration/relecture après redémarrage, dans une copie temporaire.

N'altère ni les migrations ni les données du checkout ou des exercices personnels.
Python 3.10+ et SDK .NET 10 nécessaires ; accès NuGet nécessaire au restore.
"""
from contextlib import contextmanager
from pathlib import Path
import json
import os
import shutil
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]


def run(args, cwd, env=None):
    print(f'\n[{cwd.name}] ' + ' '.join(args), flush=True)
    subprocess.run(args, cwd=cwd, env=env, check=True, timeout=300)


def request(base, method, path, body=None, expected=200):
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(base + path, data=data, method=method)
    if body is not None:
        req.add_header('Content-Type', 'application/json')
    try:
        response = urllib.request.urlopen(req, timeout=10)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        payload = response.read().decode()
        if response.status != expected:
            raise AssertionError(f'{method} {path}: {response.status}, attendu {expected}: {payload}')
        return json.loads(payload) if payload else None


@contextmanager
def server(project, env):
    with socket.socket() as sock:
        sock.bind(('127.0.0.1', 0))
        port = sock.getsockname()[1]
    base = f'http://127.0.0.1:{port}'
    log_path = project / 'validation-server.log'
    with log_path.open('w') as log:
        process = subprocess.Popen(
            ['dotnet', 'run', '--no-build', '--no-launch-profile', '--urls', base],
            cwd=project, env=env, stdout=log, stderr=subprocess.STDOUT)
        try:
            deadline = time.monotonic() + 45
            while True:
                if process.poll() is not None:
                    raise RuntimeError('Le serveur a quitté : ' + log_path.read_text())
                try:
                    request(base, 'GET', '/products')
                    break
                except urllib.error.URLError:
                    if time.monotonic() > deadline:
                        raise RuntimeError('Démarrage trop long : ' + log_path.read_text())
                    time.sleep(0.2)
            yield base
        except Exception:
            print(log_path.read_text(), flush=True)
            raise
        finally:
            process.terminate()
            try:
                process.wait(timeout=15)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=10)


def migrate_and_check(project, has_orders=False):
    env = os.environ.copy()
    env['ConnectionStrings__Database'] = 'Data Source=' + str(project / 'validation.db')
    env['ASPNETCORE_ENVIRONMENT'] = 'Production'
    run(['dotnet', 'tool', 'restore'], project, env)
    run(['dotnet', 'ef', 'migrations', 'add', 'ValidationInitial'], project, env)
    run(['dotnet', 'ef', 'database', 'update'], project, env)
    with server(project, env) as base:
        product = request(base, 'POST', '/products', {'name': 'Clavier', 'price': 30}, 201)
        request(base, 'POST', '/products', {'name': 'Erreur', 'price': -1}, 400)
        request(base, 'GET', '/products/00000000-0000-0000-0000-000000000000', expected=404)
        if has_orders:
            order = request(base, 'POST', '/orders', expected=201)
            request(base, 'POST', f'/orders/{order["id"]}/confirm', expected=409)
            request(base, 'POST', f'/orders/{order["id"]}/items',
                    {'productId': product['id'], 'quantity': 2}, 204)
            request(base, 'PUT', f'/products/{product["id"]}', {'name': 'Clavier neuf', 'price': 50}, 204)
            request(base, 'POST', f'/orders/{order["id"]}/confirm', expected=204)
    # Un nouveau processus et un nouveau DbContext relisent la base créée par migration.
    with server(project, env) as base:
        persisted = request(base, 'GET', f'/products/{product["id"]}')
        assert persisted['price'] == (50 if has_orders else 30)
        if has_orders:
            persisted_order = request(base, 'GET', f'/orders/{order["id"]}')
            assert persisted_order['status'] == 'Confirmed'
            assert persisted_order['total'] == 60
            assert persisted_order['items'][0]['productName'] == 'Clavier'
            request(base, 'POST', f'/orders/{order["id"]}/confirm', expected=409)
    print('Migration, HTTP et persistance après redémarrage : OK', flush=True)


def main():
    if shutil.which('dotnet') is None:
        raise SystemExit('SDK .NET 10 introuvable. Installer le SDK ou consulter la validation GitHub Actions.')
    with tempfile.TemporaryDirectory(prefix='formation-dotnet-') as temp:
        work = Path(temp) / 'repo'
        shutil.copytree(ROOT, work, ignore=shutil.ignore_patterns(
            '.git', 'bin', 'obj', '*.db', '*.db-shm', '*.db-wal', 'TestResults', 'Migrations'))
        examples = work / 'parcours/exemples'
        run(['dotnet', 'build'], examples / '01-api-memoire')
        with server(examples / '01-api-memoire', os.environ.copy()) as base:
            product = request(base, 'POST', '/products', {'name': 'Clavier', 'price': 30}, 201)
            assert request(base, 'GET', f'/products/{product["id"]}')['price'] == 30
        run(['dotnet', 'test', 'Catalogue.sln'], examples / '02-api-sqlite')
        ateliers = work / 'parcours/ateliers/corrections'
        for project in sorted(ateliers.iterdir()):
            if list(project.glob('*.csproj')):
                run(['dotnet', 'build'], project)
                if project.name != '04-sqlite':
                    with server(project, os.environ.copy()) as base:
                        assert isinstance(request(base, 'GET', '/products'), list)
                        if project.name != '01-get':
                            product = request(base, 'POST', '/products', {'name': 'Clavier', 'price': 30}, 201)
                            assert request(base, 'GET', f'/products/{product["id"]}')['price'] == 30
        migrate_and_check(examples / '02-api-sqlite/Catalogue.Api', has_orders=True)
        migrate_and_check(ateliers / '04-sqlite')
    print('Toutes les vérifications .NET sont terminées.', flush=True)


if __name__ == '__main__':
    main()
