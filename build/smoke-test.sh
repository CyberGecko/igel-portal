#!/usr/bin/env bash
set -Eeuo pipefail
trap 'printf "Smoke test failed at line %s.\n" "$LINENO" >&2' ERR

project=src/IgelPortal.Web
temporary=$(mktemp -d)
database=''
app_pid=''
cleanup() {
    local status=$?
    if (( status != 0 )); then
        tail -n 60 "$temporary/web.log" 2>/dev/null || true
    fi
    if [[ -n "$app_pid" ]]; then
        kill "$app_pid" 2>/dev/null || true
        wait "$app_pid" 2>/dev/null || true
    fi
    if [[ -n "$database" ]]; then
        docker rm -fv "$database" >/dev/null
    fi
    rm -r -- "$temporary"
}
trap cleanup EXIT

export MARIADB_ROOT_PASSWORD
MARIADB_ROOT_PASSWORD=$(openssl rand -hex 24)
export MARIADB_PASSWORD
MARIADB_PASSWORD=$(openssl rand -hex 24)
database=$(docker run --detach --publish 127.0.0.1::3306 \
    --env MARIADB_ROOT_PASSWORD --env MARIADB_PASSWORD --env MARIADB_DATABASE=igel_ci \
    --env MARIADB_USER=igel_ci mariadb:10.11)

for attempt in {1..60}; do
    if docker exec "$database" healthcheck.sh --connect --innodb_initialized >/dev/null 2>&1; then
        break
    fi
    sleep 1
done
docker exec "$database" healthcheck.sh --connect --innodb_initialized >/dev/null
database_port=$(docker port "$database" 3306/tcp)
export ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=${database_port##*:};Database=igel_ci;User ID=igel_ci;Password=$MARIADB_PASSWORD"
export ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:0 MailService__SendMail=false

(
    cd "$project"
    exec env Logging__LogLevel__Microsoft.Hosting.Lifetime=Information dotnet bin/Release/net10.0/IgelPortal.Web.dll
) >"$temporary/web.log" 2>&1 &
app_pid=$!

origin=''
for attempt in {1..60}; do
    kill -0 "$app_pid"
    origin=$(sed -n 's/.*Now listening on: \(http:\/\/127\.0\.0\.1:[0-9]*\).*/\1/p' "$temporary/web.log" | tail -n 1)
    [[ -n "$origin" ]] && break
    sleep 1
done
test -n "$origin"
origin=${origin/127.0.0.1/localhost}
for path in /health/live /health/ready /; do
    status=$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
        --header 'X-Forwarded-Proto: https' --max-time 10 "$origin$path")
    [[ "$status" == 200 ]]
done
passkeys='/Identity/Account/Manage/PasskeyAuthentication'
status=$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
    --header 'X-Forwarded-Proto: https' --max-time 10 "$origin$passkeys")
[[ "$status" == 302 || "$status" == 401 ]]
cookies="$temporary/cookies.txt"
html="$temporary/form.html"
browser=(curl --silent --show-error --max-time 10 --header 'X-Forwarded-Proto: https' --cookie "$cookies" --cookie-jar "$cookies")
read_csrf() {
    local status
    status=$("${browser[@]}" --output "$html" --write-out '%{http_code}' "$origin$1")
    if [[ "$status" != 200 ]]; then
        printf 'Form returned HTTP %s.\n' "$status" >&2
        return 1
    fi
    sed -n '/name="__RequestVerificationToken"[^>]*value="/{s/.*name="__RequestVerificationToken"[^>]*value="\([^"]*\)".*/\1/p;q;}' "$html"
}

email='igel-ci@example.invalid'
password="Aa9!$(openssl rand -hex 20)"
register='/Identity/Account/Register'
token=$(read_csrf "$register")
test -n "$token"
grep -q '<altcha-widget' "$html"
challenge=$("${browser[@]}" --fail "$origin/registration-captcha")
solution=$(dotnet run --file build/solve-registration-captcha.cs --verbosity quiet <<< "$challenge")

post_registration() {
    "${browser[@]}" --output "$html" --write-out '%{http_code}' \
        --header 'Accept-Language: en-GB' \
        --data-urlencode "__RequestVerificationToken=$token" \
        --data-urlencode 'Input.FirstName=Igel' --data-urlencode 'Input.LastName=CI' \
        --data-urlencode "Input.Email=$1" --data-urlencode "Input.Password=$password" \
        --data-urlencode "Input.ConfirmPassword=$password" \
        --data-urlencode "Altcha=$2" "$origin$register"
}

db() {
    docker exec -i "$database" sh -c 'MYSQL_PWD="$MARIADB_PASSWORD" exec mariadb --batch --skip-column-names --user="$MARIADB_USER" "$MARIADB_DATABASE"'
}

for payload in '' 'invalid'; do
    [[ "$(post_registration "$email" "$payload")" == 200 ]]
    grep -q 'Complete the security check again before registering.' "$html"
done
[[ "$(db <<< 'SELECT COUNT(*) FROM AspNetUsers;')" == 0 ]]
[[ "$(post_registration "$email" "$solution")" == 302 ]]
[[ "$(post_registration 'replay@example.invalid' "$solution")" == 200 ]]
grep -q 'Complete the security check again before registering.' "$html"
[[ "$(db <<< 'SELECT COUNT(*) FROM AspNetUsers;')" == 1 ]]
[[ "$(db <<< 'SELECT COUNT(*) FROM AltchaReplays;')" == 1 ]]
[[ "$(post_registration "$email" '')" == 200 ]]
[[ "$(post_registration "$email" '')" == 429 ]]

# Confirm the test account and create data for authenticated page checks.
db <<'SQL'
UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE Email = 'igel-ci@example.invalid';
INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT u.Id, r.Id FROM AspNetUsers u JOIN AspNetRoles r ON r.NormalizedName = 'USER'
    WHERE u.Email = 'igel-ci@example.invalid';
SET @community = '10000000-0000-0000-0000-000000000001';
SET @hedgehog = '10000000-0000-0000-0000-000000000002';
SET @service = '10000000-0000-0000-0000-000000000003';
INSERT INTO Communities (Id, UserName, ContactName, ContactMail, Name, CreationDate, ChangeDate)
    VALUES (@community, 'igel-ci@example.invalid', 'Igel CI', 'igel-ci@example.invalid', 'CI community', NOW(), NOW());
INSERT INTO CommunityMembers (UserName, CommunityId, CommunityRoleName)
    VALUES ('igel-ci@example.invalid', @community, 'Member');
INSERT INTO Hedgehogs (Id, Name, AttachedUser, CommunityId, BiologicalSex, CreationDate, ChangeDate)
    VALUES (@hedgehog, 'CI hedgehog', 'igel-ci@example.invalid', @community, 0, NOW(), NOW());
INSERT INTO TerritoryServices (Id, Name, CreationDate, ChangeDate)
    VALUES (@service, 'CI territory service', NOW(), NOW());
INSERT INTO ServiceRequests (Id, CommunityId, ServiceId, HedgehogId, RequestToken, CreationDate, ChangeDate)
    VALUES ('10000000-0000-0000-0000-000000000004', @community, @service, @hedgehog,
        '10000000-0000-0000-0000-000000000005', NOW(), NOW());
SQL

login='/Identity/Account/Login'
token=$(read_csrf "$login")
test -n "$token"
grep -q 'id="passkey-sign-in"' "$html"
status=$("${browser[@]}" --output /dev/null --write-out '%{http_code}' \
    --data '' "$origin$login?handler=PasskeyOptions")
[[ "$status" == 400 ]]
assertion=$("${browser[@]}" --fail --data-urlencode "__RequestVerificationToken=$token" \
    "$origin$login?handler=PasskeyOptions")
[[ "$assertion" == *'"userVerification":"required"'* ]]
status=$("${browser[@]}" --output /dev/null --write-out '%{http_code}' \
    --data-urlencode "__RequestVerificationToken=$token" --data-urlencode "Input.Email=$email" \
    --data-urlencode "Input.Password=$password" "$origin$login")
[[ "$status" == 302 ]]
token=$(read_csrf "$passkeys")
test -n "$token"
attestation=$("${browser[@]}" --fail --data-urlencode "__RequestVerificationToken=$token" \
    "$origin$passkeys?handler=CreateOptions")
[[ "$attestation" == *'"userVerification":"required"'* ]]
[[ "$attestation" == *'"residentKey":"required"'* ]]
[[ "$(db <<< 'SELECT COUNT(*) FROM AspNetUserPasskeys;')" == 0 ]]
for path in '/Hedgehogs?filter=my' '/Territories?filter=my'; do
    status=$("${browser[@]}" --output /dev/null --write-out '%{http_code}' "$origin$path")
    [[ "$status" == 200 ]]
done
map='/Territories?filter=10000000-0000-0000-0000-000000000001'
"${browser[@]}" --fail --output "$html" "$origin$map"
grep -q 'ej2-maps.min.js' "$html"

response='/Hedgehogs/RequestService/ServiceResponse'
request_token='10000000-0000-0000-0000-000000000005'
token=$(read_csrf "$response?requestToken=$request_token")
test -n "$token"
[[ "$(db <<< 'SELECT COUNT(*) FROM ServiceRequestResponses;')" == 0 ]]
for attempt in 1 2; do
    status=$("${browser[@]}" --output /dev/null --write-out '%{http_code}' \
        --data-urlencode "__RequestVerificationToken=$token" --data-urlencode "requestToken=$request_token" "$origin$response")
    [[ "$status" == 302 ]]
done
[[ "$(db <<< 'SELECT COUNT(*) FROM ServiceRequestResponses;')" == 1 ]]
echo 'MariaDB migrations, health, registration CAPTCHA, login, authenticated lists, map, service response and passkey authorization verified.'

# Exercise medication permissions and the once-only recording against MariaDB.
db <<'SQL'
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash,
    SecurityStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName)
SELECT 'care-helper-fixture', 'helper-ci@example.invalid', 'HELPER-CI@EXAMPLE.INVALID', 'helper-ci@example.invalid', 'HELPER-CI@EXAMPLE.INVALID',
    1, PasswordHash, UUID(), 0, 0, 1, 0, 'Helper', 'CI'
FROM AspNetUsers WHERE Email = 'igel-ci@example.invalid';
INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT 'care-helper-fixture', Id FROM AspNetRoles WHERE NormalizedName = 'USER';
INSERT INTO CommunityMembers (UserName, CommunityId, CommunityRoleName) VALUES
 ('igel-ci@example.invalid','10000000-0000-0000-0000-000000000001','Owner'),
 ('helper-ci@example.invalid','10000000-0000-0000-0000-000000000001','Member'),
 ('helper-ci@example.invalid','10000000-0000-0000-0000-000000000001','Moderator');
SQL

care='/Hedgehogs/Care/10000000-0000-0000-0000-000000000002'
post_care() {
    local handler=$1
    shift
    curl --silent --show-error --max-time 10 --header 'X-Forwarded-Proto: https' \
        --cookie "$cookies" --output /dev/null --write-out '%{http_code}' \
        --data-urlencode "__RequestVerificationToken=$token" "$@" "$origin$care?handler=$handler"
}

token=$(read_csrf "$care")
test -n "$token"
# Use yesterday for admission so the same-day schedule stays valid even at midnight.
admission=$(TZ=Europe/Berlin date --date='yesterday 12:00' +%Y-%m-%dT%H:%M)
[[ "$(post_care Start --data-urlencode "startedAt=$admission")" == 302 ]]
episode=$(db <<< 'SELECT Id FROM CareEpisodes WHERE OpenHedgehogId IS NOT NULL;')
test -n "$episode"
[[ "$(post_care Team --data-urlencode 'helperId=care-helper-fixture' --data-urlencode 'add=true')" == 302 ]]

today=$(TZ=Europe/Berlin date +%F)
plan='10000000-0000-0000-0000-000000000007'
plan_form=(--data-urlencode "episodeId=$episode" --data-urlencode "planId=$plan"
    --data-urlencode 'medication=CI fixture' --data-urlencode 'amount=CI amount'
    --data-urlencode "startDate=$today" --data-urlencode "endDate=$today" --data-urlencode 'dailyTimes=12:00')
for attempt in 1 2; do
    [[ "$(post_care Plan "${plan_form[@]}")" == 302 ]]
done
[[ "$(db <<< 'SELECT COUNT(*) FROM MedicationPlans;')" == 1 ]]
dose=$(db <<< 'SELECT Id FROM MedicationDoses;')
test -n "$dose"
[[ "$(post_care Offer --data-urlencode "planId=$plan" --data-urlencode 'helperId=care-helper-fixture')" == 302 ]]
[[ "$(post_care Respond --data-urlencode "planId=$plan" --data-urlencode 'accept=true')" == 302 ]]
[[ "$(db <<< 'SELECT Assignment FROM MedicationPlans;')" == 1 ]]

rm -f -- "$cookies"
token=$(read_csrf "$login")
test -n "$token"
status=$("${browser[@]}" --output /dev/null --write-out '%{http_code}' \
    --data-urlencode "__RequestVerificationToken=$token" --data-urlencode 'Input.Email=helper-ci@example.invalid' \
    --data-urlencode "Input.Password=$password" "$origin$login")
[[ "$status" == 302 ]]
token=$(read_csrf "$care")
test -n "$token"
plan_form[3]='planId=10000000-0000-0000-0000-000000000008'
[[ "$(post_care Plan "${plan_form[@]}")" == 302 ]]
[[ "$(db <<< 'SELECT COUNT(*) FROM MedicationPlans;')" == 1 ]]

given_at=$(TZ=Europe/Berlin date +%Y-%m-%dT%H:%M)
dose_form=(--data-urlencode "planId=$plan" --data-urlencode "doseId=$dose" --data-urlencode 'given=true'
    --data-urlencode "givenAt=$given_at" --data-urlencode 'actualAmount=CI amount')
[[ "$(post_care Dose "${dose_form[@]}")" == 302 ]]
[[ "$(db <<< 'SELECT Status FROM MedicationDoses;')" == 0 ]]
[[ "$(post_care Respond --data-urlencode "planId=$plan" --data-urlencode 'accept=true')" == 302 ]]
# The second request tries a different outcome so an overwrite would be detectable.
[[ "$(post_care Dose "${dose_form[@]}")" == 302 ]]
[[ "$(post_care Dose --data-urlencode "planId=$plan" --data-urlencode "doseId=$dose" \
    --data-urlencode 'given=false' --data-urlencode 'note=duplicate fixture')" == 302 ]]
[[ "$(db <<< "SELECT COUNT(*) FROM MedicationDoses WHERE Status=1 AND RecordedById='care-helper-fixture' AND Note IS NULL;")" == 1 ]]
echo 'Care permissions, plan acceptance and once-only medication recording verified.'
