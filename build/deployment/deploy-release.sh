#!/usr/bin/env bash
set -Eeuo pipefail
umask 022

[[ $# == 5 ]] || exit 2
root=$1
service=$2
health_origin=$3
release_id=$4
upload=$5
[[ "$root" =~ ^/var/www/[a-zA-Z0-9][a-zA-Z0-9_-]*$ ]] || exit 2
[[ "$service" =~ ^[a-zA-Z0-9_-]+\.service$ ]] || exit 2
[[ "$health_origin" =~ ^http://127\.0\.0\.1:[1-9][0-9]{0,4}$ ]] || exit 2
[[ "$release_id" =~ ^build-[0-9]+-[0-9]+-[a-zA-Z0-9]{8}$ ]] || exit 2
[[ "$upload" =~ ^/tmp/igel-portal-deploy\.[a-zA-Z0-9]{8}$ ]] || exit 2

test -d "$root/releases"
test -s "$root/shared/appsettings.json"
exec 9>"$root/.deploy.lock"
flock -w 120 9

previous=''
if [[ -L "$root/current" ]]; then
    previous=$(readlink -f "$root/current")
    [[ "$previous" == "$root/releases/"* && -d "$previous" ]] || exit 2
elif [[ -e "$root/current" ]]; then
    echo 'current must be a symbolic link.' >&2
    exit 2
fi

(cd "$upload" && sha256sum --check --strict web.tar.gz.sha256)
release="$root/releases/$release_id"
mkdir -- "$release"
tar --extract --gzip --no-same-owner --no-same-permissions --file="$upload/web.tar.gz" --directory="$release"
test -s "$release/IgelPortal.Web.dll"
test -s "$release/IgelPortal.Web.runtimeconfig.json"
for config in "$root"/shared/appsettings*.json; do
    ln -s -- "$config" "$release/$(basename "$config")"
done

switch_current() {
    local target=$1
    local link="$root/.current-$release_id"
    ln -s -- "$target" "$link" || return
    mv -Tf -- "$link" "$root/current"
}

verify_health() {
    local probe status attempt
    for probe in live ready; do
        status=''
        for attempt in {1..12}; do
            status=$(curl --silent --output /dev/null --write-out '%{http_code}' \
                --connect-timeout 3 --max-time 5 --header 'X-Forwarded-Proto: https' "$health_origin/health/$probe") || status=''
            [[ "$status" == 200 ]] && break
            sleep 3
        done
        [[ "$status" == 200 ]] || return 1
    done
    systemctl is-active --quiet "$service"
}

rollback() {
    trap - EXIT
    if [[ -n "$previous" ]]; then
        echo 'Activation failed. Restoring the previous application release.' >&2
        switch_current "$previous" && sudo -n systemctl restart "$service" && verify_health ||
            echo 'Rollback health check failed.' >&2
    else
        echo 'First activation failed.' >&2
        sudo -n systemctl stop "$service"
        rm -f -- "$root/current"
    fi
    exit 1
}

switch_current "$release"
trap rollback EXIT
trap 'exit 143' TERM
trap 'exit 130' INT
sudo -n systemctl restart "$service"
verify_health
trap - EXIT TERM INT
echo "Activated $release_id."
