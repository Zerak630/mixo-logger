#!/usr/bin/env bash
# Vérifie une instance déployée de bout en bout, à travers Caddy : HTTPS, en-têtes de sécurité,
# front, API, session, limitation des tentatives.
#
#   deploiement/verifier.sh https://localhost alice 'mot-de-passe'
#
# Sur « localhost », le certificat est émis par l'autorité locale de Caddy : on ne le vérifie pas.
# Sur un vrai domaine, il doit être valide : passer VERIFIER_CERTIFICAT=1.
set -euo pipefail

# Pas d'apostrophe dans ces messages : dans ${…:?…}, bash la prendrait pour un guillemet.
BASE="${1:?URL de l instance, ex. https://localhost}"
IDENTIFIANT="${2:?identifiant du compte}"
MOT_DE_PASSE="${3:?mot de passe du compte}"

CURL=(curl --silent --show-error --max-time 20)
[[ "${VERIFIER_CERTIFICAT:-0}" == 1 ]] || CURL+=(--insecure)

TEMP="$(mktemp -d)"
trap 'rm -rf "$TEMP"' EXIT
ECHECS=0

ok()    { printf '  ok   %s\n' "$1"; }
echec() { printf '  ÉCHEC %s\n' "$1"; ECHECS=$((ECHECS + 1)); }
verifier() { if eval "$2"; then ok "$1"; else echec "$1"; fi; }

# Attend que Caddy et l'API répondent (premier démarrage : migrations, certificat).
for _ in $(seq 1 60); do
	[[ "$("${CURL[@]}" -o /dev/null -w '%{http_code}' "$BASE/api/auth/moi" || true)" == 401 ]] && break
	sleep 2
done

echo "HTTPS et en-têtes"
HOTE="${BASE#https://}"
code=$("${CURL[@]}" -o /dev/null -w '%{http_code}' "http://$HOTE/")
verifier "http:// redirige vers https:// ($code)" '[[ $code == 308 || $code == 301 ]]'
"${CURL[@]}" -D "$TEMP/entetes" -o "$TEMP/index.html" "$BASE/"
verifier "index.html servi" 'grep -q "<app-root>" "$TEMP/index.html"'
verifier "HSTS" 'grep -qi "^strict-transport-security: max-age=31536000" "$TEMP/entetes"'
verifier "CSP sans script en ligne" 'grep -qi "^content-security-policy: .*script-src '"'"'self'"'"';" "$TEMP/entetes"'
verifier "nosniff" 'grep -qi "^x-content-type-options: nosniff" "$TEMP/entetes"'
verifier "pas d'en-tête Server" '! grep -qi "^server:" "$TEMP/entetes"'
verifier "index.html non mis en cache" 'grep -qi "^cache-control: no-cache" "$TEMP/entetes"'

echo "Front"
code=$("${CURL[@]}" -o "$TEMP/route.html" -w '%{http_code}' "$BASE/cocktails/42")
verifier "route Angular servie par index.html ($code)" '[[ $code == 200 ]] && grep -q "<app-root>" "$TEMP/route.html"'
script=$(grep -o 'src="main-[A-Z0-9]*\.js"' "$TEMP/index.html" | head -1 | cut -d'"' -f2)
"${CURL[@]}" -D "$TEMP/entetes-js" -o /dev/null "$BASE/$script"
verifier "fichiers à empreinte en cache long ($script)" 'grep -qi "^cache-control: public, max-age=31536000, immutable" "$TEMP/entetes-js"'

echo "API et session"
code=$("${CURL[@]}" -o /dev/null -w '%{http_code}' "$BASE/api/cocktails")
verifier "API protégée sans session ($code)" '[[ $code == 401 ]]'
code=$("${CURL[@]}" -c "$TEMP/cookies" -D "$TEMP/entetes-connexion" -o /dev/null -w '%{http_code}' \
	-H 'Content-Type: application/json' \
	-d "{\"identifiant\":\"$IDENTIFIANT\",\"motDePasse\":\"$MOT_DE_PASSE\"}" "$BASE/api/auth/connexion")
verifier "connexion ($code)" '[[ $code == 200 ]]'
cookie=$(grep -i "^set-cookie: mixo_session=" "$TEMP/entetes-connexion" || true)
verifier "cookie Secure, HttpOnly, SameSite=Strict" '[[ $cookie == *[Ss]ecure* && $cookie == *[Hh]ttponly* && $cookie == *[Ss]amesite=[Ss]trict* ]]'
code=$("${CURL[@]}" -b "$TEMP/cookies" -o "$TEMP/cocktails.json" -w '%{http_code}' "$BASE/api/cocktails")
verifier "liste des cocktails avec la session ($code)" '[[ $code == 200 ]] && grep -q "\"name\"" "$TEMP/cocktails.json"'

echo "Limitation par adresse IP (derrière le proxy)"
# Caddy remplace l'X-Forwarded-For envoyé par le client : en changer à chaque essai ne contourne rien.
dernier=""
for i in $(seq 1 8); do
	dernier=$("${CURL[@]}" -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' \
		-H "X-Forwarded-For: 203.0.113.$i" \
		-d "{\"identifiant\":\"inconnu-$i-$RANDOM\",\"motDePasse\":\"faux-mot-de-passe\"}" "$BASE/api/auth/connexion")
done
verifier "429 malgré un X-Forwarded-For forgé ($dernier)" '[[ $dernier == 429 ]]'

echo
if (( ECHECS > 0 )); then
	echo "$ECHECS vérification(s) en échec."
	exit 1
fi
echo "Tout est bon."
