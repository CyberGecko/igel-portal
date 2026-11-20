#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

: "${DEPLOY_KNOWN_HOSTS:?Configure the verified SSH host key}"
: "${DEPLOY_KEY_PATH:?Missing SSH secure file}"
: "${AGENT_TEMPDIRECTORY:?}"
: "${PIPELINE_WORKSPACE:?}"

[[ "${DEPLOY_HOST:-}" =~ ^[a-zA-Z0-9][a-zA-Z0-9.-]*$ ]] || exit 2
[[ "${DEPLOY_PORT:-}" =~ ^[1-9][0-9]{0,4}$ ]] && (( DEPLOY_PORT <= 65535 )) || exit 2
[[ "${DEPLOY_USER:-}" =~ ^[a-z_][a-z0-9_-]*$ && "$DEPLOY_USER" != root ]] || exit 2
[[ "${DEPLOY_ROOT:-}" =~ ^/var/www/[a-zA-Z0-9][a-zA-Z0-9_-]*$ ]] || exit 2
[[ "${DEPLOY_SERVICE:-}" =~ ^[a-zA-Z0-9_-]+\.service$ ]] || exit 2
[[ "${DEPLOY_HEALTH_ORIGIN:-}" =~ ^http://127\.0\.0\.1:[1-9][0-9]{0,4}$ ]] || exit 2
[[ "${BUILD_BUILDID:-}" =~ ^[0-9]+$ && "${SYSTEM_JOBATTEMPT:-}" =~ ^[0-9]+$ ]] || exit 2

artifact="$PIPELINE_WORKSPACE/igel-download/igel-web"
test -s "$artifact/deploy-release.sh"
(cd "$artifact" && sha256sum --check --strict web.tar.gz.sha256)
test -s "$DEPLOY_KEY_PATH"
chmod 0600 "$DEPLOY_KEY_PATH"
ssh-keygen -y -P '' -f "$DEPLOY_KEY_PATH" >/dev/null

known_hosts=$(mktemp "$AGENT_TEMPDIRECTORY/igel-known-hosts.XXXXXX")
remote=''
destination="$DEPLOY_USER@$DEPLOY_HOST"
ssh_options=(-i "$DEPLOY_KEY_PATH" -o BatchMode=yes -o IdentitiesOnly=yes
    -o StrictHostKeyChecking=yes -o GlobalKnownHostsFile=/dev/null -o "UserKnownHostsFile=$known_hosts"
    -o ConnectTimeout=20 -o ServerAliveInterval=15 -o ServerAliveCountMax=4)
cleanup() {
    if [[ -n "$remote" ]]; then
        ssh -T "${ssh_options[@]}" -p "$DEPLOY_PORT" "$destination" \
            "rm -f -- '$remote/web.tar.gz' '$remote/web.tar.gz.sha256' '$remote/deploy-release.sh'; rmdir -- '$remote'" ||
            echo 'Remote upload cleanup failed.' >&2
    fi
    rm -f -- "$known_hosts"
}
trap cleanup EXIT
printf '%s\n' "$DEPLOY_KNOWN_HOSTS" | tr -d '\r' > "$known_hosts"

upload=$(ssh -T "${ssh_options[@]}" -p "$DEPLOY_PORT" "$destination" \
    'umask 077; mktemp -d /tmp/igel-portal-deploy.XXXXXXXX')
[[ "$upload" =~ ^/tmp/igel-portal-deploy\.[a-zA-Z0-9]{8}$ ]] || exit 2
remote=$upload
scp "${ssh_options[@]}" -P "$DEPLOY_PORT" \
    "$artifact/web.tar.gz" "$artifact/web.tar.gz.sha256" "$artifact/deploy-release.sh" "$destination:$remote/"
release="build-$BUILD_BUILDID-$SYSTEM_JOBATTEMPT-${remote##*.}"
ssh -T "${ssh_options[@]}" -p "$DEPLOY_PORT" "$destination" \
    "bash '$remote/deploy-release.sh' '$DEPLOY_ROOT' '$DEPLOY_SERVICE' '$DEPLOY_HEALTH_ORIGIN' '$release' '$remote'"
