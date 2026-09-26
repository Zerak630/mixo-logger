# Front MixoLogger servi par Caddy, qui est aussi le reverse proxy de l'API et termine le HTTPS.
# Construite depuis la racine du dépôt.

FROM node:24-alpine AS construction
WORKDIR /src
COPY MixoLoggerFront/package.json MixoLoggerFront/package-lock.json ./
RUN npm ci
COPY MixoLoggerFront/ ./
# Configuration « production » : l'API est sur la même origine, sous /api (src/env/env.production.json).
RUN npm run build


FROM caddy:2-alpine
COPY deploiement/Caddyfile /etc/caddy/Caddyfile
COPY --from=construction /src/dist/MixoLoggerFront/browser /srv
