# MixoLogger — Déploiement

> Mettre MixoLogger en ligne sur un serveur, en HTTPS, pour les membres.
> Document compagnon de [MVP.md](MVP.md) (lot D).
>
> Dernière mise à jour : 2026-09-26

---

## 1. Vue d'ensemble

```
Internet ──443/80──▶ web (Caddy) ──┬── /api/* ──▶ api (.NET, :8080, non exposée)
                                   └── le reste : front Angular (fichiers statiques)
```

| Conteneur | Image | Rôle |
|-----------|-------|------|
| `web` | `deploiement/web.Dockerfile` (Caddy + front) | Seul point d'entrée public. **HTTPS automatique** (Let's Encrypt), redirection de `http://`, en-têtes de sécurité, front Angular, relais de `/api/*` vers l'API |
| `api` | `deploiement/api.Dockerfile` (.NET) | L'API, joignable seulement par Caddy sur le réseau interne |

Front et API partagent la même origine (`https://<domaine>`) : le cookie de session `SameSite=Strict`
et `Secure` fonctionne tel quel, et CORS n'intervient pas.

Tout ce qui doit survivre à un redéploiement est dans des **volumes** :

| Volume | Contenu |
|--------|---------|
| `mixologger_donnees` | Base SQLite, sauvegardes automatiques (`Sauvegardes/`), clés de chiffrement des cookies (`Cles/`) |
| `mixologger_caddy_data` | Certificats Let's Encrypt |
| `mixologger_caddy_config` | Configuration interne de Caddy |

> ⚠️ **.NET 11 est encore en préversion** jusqu'au 10/11/2026 (docs/MVP.md §2.1). Tout fonctionne et
> la CI le vérifie, mais le plan reste de mettre en ligne **après la GA** : passer alors les images de
> `deploiement/api.Dockerfile` sur les tags `11.0`, en même temps que `global.json`.

---

## 2. Prérequis

- Un serveur Linux avec **Docker** et le plugin **Compose** (`docker compose version`).
- Un **nom de domaine** dont l'enregistrement DNS `A` (et `AAAA` en IPv6) pointe vers le serveur.
- Les ports **80 et 443** ouverts vers le serveur (Let's Encrypt valide le domaine par le port 80).
- 1 Go de mémoire suffit pour 5 utilisateurs ; la construction des images en demande un peu plus.

---

## 3. Première mise en ligne

Sur le serveur, dans une copie du dépôt :

```bash
cp deploiement/.env.exemple deploiement/.env
```

Éditer `deploiement/.env` (il n'est **jamais** versionné) :
- `DOMAINE` : le nom de domaine public ;
- un bloc `Comptes__N__Identifiant` / `NomAffiche` / `MotDePasse` par membre, numérotés à partir de 0
  (mot de passe de 12 caractères au moins).

Puis :

```bash
docker compose -f deploiement/compose.yaml up -d --build
```

Au premier démarrage, l'API crée la base, applique les migrations, insère le jeu initial et crée les
comptes ; Caddy obtient le certificat. Vérifier ensuite l'instance de bout en bout :

```bash
VERIFIER_CERTIFICAT=1 deploiement/verifier.sh https://<domaine> <identifiant> '<mot de passe>'
```

> `verifier.sh` termine par 8 connexions ratées pour vérifier la limitation : la connexion depuis
> cette machine reste ensuite refusée **une minute**.

Enfin, une fois que chacun s'est connecté et a choisi son mot de passe dans « Mon compte », **retirer
les `MotDePasse` de `.env`** : seule leur empreinte est en base, ils ne servent plus.

---

## 4. Exploitation

### Mettre à jour

```bash
git pull
docker compose -f deploiement/compose.yaml up -d --build
```

Les migrations s'appliquent seules au démarrage de l'API, précédées d'une sauvegarde
`avant-migration`. Les sessions restent ouvertes : les clés des cookies sont dans le volume.

### Journaux

```bash
docker compose -f deploiement/compose.yaml logs -f api
docker compose -f deploiement/compose.yaml logs -f web
```

### Comptes

Tout se fait dans `.env`, puis `docker compose -f deploiement/compose.yaml up -d` (règles complètes :
docs/MVP.md §10.2) :
- **ajouter** un membre : un nouveau bloc `Comptes__N__…` ;
- **retirer** un membre : supprimer son bloc — le compte est désactivé, ses données restent ;
- **mot de passe oublié** : son `MotDePasse` et `Comptes__N__ReinitialiserMotDePasse=true`, redémarrer,
  puis **remettre à `false`** (sinon chaque démarrage l'écrase).

Ne pas changer `Comptes__N__Identifiant` pour renommer quelqu'un (cela créerait un second compte) :
chacun change son identifiant dans « Mon compte ».

---

## 5. Sauvegardes

L'API sauvegarde la base toute seule (docs/MVP.md §10.2) : une copie par jour et une avant chaque
migration, les 7 dernières de chaque sorte gardées, dans le volume `mixologger_donnees`, sous
`Sauvegardes/`.

**Ces copies sont sur le même disque que la base** : il faut les recopier ailleurs. Par exemple, chaque
nuit, vers un autre serveur ou un stockage externe (à adapter) :

```bash
# crontab -e
30 4 * * * docker compose -f /chemin/vers/mixo-logger/deploiement/compose.yaml cp api:/app/Donnees/Sauvegardes /srv/copies-mixologger && rsync -a /srv/copies-mixologger/ sauvegarde@autre-machine:mixologger/
```

### Restaurer

1. Arrêter l'API : `docker compose -f deploiement/compose.yaml stop api`.
2. Remplacer la base par la copie choisie, en supprimant les fichiers `-wal` et `-shm` de l'ancienne :

   ```bash
   docker compose -f deploiement/compose.yaml run --rm --no-deps --entrypoint sh api -c \
     'cd /app/Donnees && rm -f mixologger.db-wal mixologger.db-shm && cp Sauvegardes/<copie>.db mixologger.db'
   ```

3. Redémarrer : `docker compose -f deploiement/compose.yaml start api`.

Pour restaurer une copie venue d'ailleurs, la déposer d'abord dans le volume :
`docker compose -f deploiement/compose.yaml cp <copie>.db api:/app/Donnees/Sauvegardes/`.

---

## 6. Sécurité

| Point | Mise en œuvre |
|-------|---------------|
| HTTPS | Certificat Let's Encrypt renouvelé par Caddy ; `http://` redirige ; HSTS un an |
| API non exposée | Aucun port publié : seul Caddy, sur le réseau interne `172.30.0.0/24`, la joint |
| Adresse du client | Caddy transmet `X-Forwarded-For` / `X-Forwarded-Proto` ; l'API ne les croit **que** depuis `172.30.0.0/24` (`ReverseProxy__ReseauxDeConfiance__0`). Un `X-Forwarded-For` forgé par le client est remplacé par Caddy : la limitation par IP porte sur le vrai client |
| Cookies | `Secure` (production), `HttpOnly`, `SameSite=Strict` ; clés de chiffrement dans `Donnees/Cles`, conservées d'un redéploiement à l'autre. Elles y sont en clair (avertissement « No XML encryptor configured » dans le journal) : le volume doit rester réservé au serveur |
| En-têtes | CSP sans script en ligne (styles en ligne permis pour OptimusUI ; photos depuis tout hôte `https`), `nosniff`, `frame-ancestors 'none'`, pas d'en-tête `Server` |
| Documentation de l'API | Swagger UI et `/openapi/v1.json` seulement en développement |
| Conteneur de l'API | Utilisateur non privilégié (`APP_UID`) |

---

## 7. Essayer en local

La même pile tourne sur un poste avec Docker, sur `https://localhost` (certificat de l'autorité locale
de Caddy, à accepter dans le navigateur) : `DOMAINE=localhost` dans `deploiement/.env`, puis les
commandes du §3. La CI fait exactement cela à chaque pull request (job « Déploiement ») : construction
des images, `verifier.sh`, puis redéploiement pour vérifier que session et données survivent.
