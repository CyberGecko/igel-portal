# Betrieb und Deployment

## Brevo

Kontobestätigung, Passwort-Reset und Pflegeanfragen nutzen die Brevo-Transaktionsmail-API über HTTPS. Entwicklung startet mit deaktiviertem Versand. Für einen Versandtest mit verifiziertem Absender folgende Einstellungen über User Secrets oder Umgebungsvariablen setzen:

| Variable | Wert |
| --- | --- |
| `MailService__SendMail` | `true` |
| `MailService__Brevo__ApiKey` | Brevo-API-Schlüssel für Transaktionsmails |
| `MailService__Sender__Address` | Bei Brevo verifizierte Absenderadresse |
| `MailService__Sender__Name` | Anzeigename, etwa `Igel Portal` |

Für die Produktion benötigt der Dienst ausgehendes HTTPS zu `api.brevo.com` auf Port 443. Bei aktiviertem Versand prüft die Anwendung beim Start, ob Schlüssel und Absender konfiguriert sind. Nach erfolgreicher Umstellung können die alten `Smtp`-Einstellungen entfernt werden.

## Schutz der Registrierung

Die Registrierung mit Passwort und die erstmalige Kontoerstellung per Google verlangen eine lokal gehostete ALTCHA-Sicherheitsprüfung. Aufgaben sind fünf Minuten gültig, an die Browser-Sitzung gebunden und nur einmal verwendbar. Die Anwendung begrenzt pro IP Aufgaben und Formular-POSTs getrennt auf 20 beziehungsweise 5 pro Minute. Der Browser benötigt HTTPS und JavaScript. Bestehende Anmeldungen mit Passwort oder Passkey verwenden kein CAPTCHA. Eine verknüpfte Google-Anmeldung verlangt die Aufgabe nur bei der erstmaligen Kontoerstellung.

In Produktion `RegistrationCaptcha__HmacSecret` über die geschützte Serverkonfiguration setzen. Ein mit `openssl rand -hex 32` erzeugter Wert ist geeignet. Die Anwendung startet nur mit einem Schlüssel von mindestens 32 Zeichen. In Development kann die Anwendung für den lokalen Test selbst einen temporären Schlüssel erzeugen. Die Startmigration legt `AltchaReplays` an, damit eine Lösung auch nach einem Neustart nur einmal verwendbar ist.

## Azure DevOps

| Pipeline | Auslöser und Aufgabe |
| --- | --- |
| [azure-pipelines.yml](../azure-pipelines.yml) | Änderungen und Pull Requests für `main`. NuGet-Audit, Release-Build, Scan der mitgelieferten JavaScript-Bibliotheken und Starttest gegen MariaDB. |
| [azure-deploy.yml](../azure-deploy.yml) | Ein Tag wie `v1.2.3` auf einem Commit aus `main`. Baut und prüft denselben Commit, veröffentlicht das Webpaket, lädt es im selben Lauf und aktiviert es nach den Azure-Umgebungsprüfungen per SSH. |

Beide Definitionen in Azure DevOps als getrennte YAML-Pipelines mit dem GitHub-Repository verbinden. Sie verwenden Microsoft-gehostete Linux-Agents. Bei Azure Repos zusätzlich eine Build-Validation-Branch-Policy für Pull Requests in `main` setzen.

Für das Deployment die Azure-Umgebung `igel-portal-production` mit Approval und **Exclusive lock** anlegen. Den SSH-Schlüssel als Secure File `igel-portal-deploy-key` hinterlegen. Umgebung, Secure File und die Variable Group `Igel Portal - Production` für die Deploy-Pipeline autorisieren. Diese Variablen stammen aus der Servereinrichtung:

| Variable | Wert |
| --- | --- |
| `DeployHost`, `DeployPort`, `DeployUser` | Erreichbarer SSH-Host, Port und dedizierter Benutzer |
| `DeployKnownHosts` | Separat verifizierte OpenSSH-Hostschlüsselzeile für den Zielhost, bei Sonderport im Format `[host]:port` |
| `DeployRoot` | Separater Pfad unter `/var/www`, zum Beispiel `/var/www/igel-portal` |
| `DeployService` | systemd-Unit, zum Beispiel `kestrel-igel-portal.service` |
| `DeployHealthOrigin` | Lokaler Kestrel-Endpunkt, zum Beispiel `http://127.0.0.1:5006` |

### Voraussetzungen auf dem Server

- ASP.NET-Core-10-Runtime bereitstellen und die systemd-Unit auf `<DeployRoot>/current/IgelPortal.Web.dll` mit `WorkingDirectory=<DeployRoot>/current` und `ASPNETCORE_ENVIRONMENT=Production` konfigurieren. Kestrel an Loopback binden und im lokalen Reverse-Proxy `Host` und `X-Forwarded-Proto` weitergeben. Bei einer bestehenden Installation den Altstand vor dem ersten Wechsel als Rückfalloption sichern.
- Der Deployment-Benutzer benötigt Schreibzugriff auf das Release-Verzeichnis und eng begrenzte sudo-Rechte für `systemctl restart` und `systemctl stop` dieser einen Unit. Der Dienstbenutzer benötigt Leserechte für Releases und die geschützte Konfiguration. Bei einem bereits verwendeten Verzeichnis muss `current` ein Symlink auf ein Verzeichnis unter `releases` sein.
- `<DeployRoot>/shared/appsettings.json` aus der bisherigen Serverkonfiguration und der Repository-Vorlage zusammenführen. Verbindungszeichenfolge, Brevo-Schlüssel, gegebenenfalls Google-Zugangsdaten und `Syncfusion__LicenseKey` geschützt bereitstellen. `AllowedHosts` auf öffentliche Hosts und Loopback für die Health-Probes setzen. Die FIDO2-Origins und die Relying-Party-Domain für vorhandene Passkeys beibehalten.
- `DataProtection__KeyRingPath` auf einen dauerhaften, nur für den Dienstbenutzer zugänglichen Ort setzen. Wenn bestehende Sitzungen und Identity-Token gültig bleiben sollen, die vorhandenen Data-Protection-Schlüssel übernehmen und die Anwendung auf die bisherige Data-Protection-Anwendungskennung konfigurieren.
- Vor dem Release ein Datenbank-Backup erstellen. Das Deployment prüft die Prüfsumme, legt ein neues Release-Verzeichnis an und wechselt `current` atomar. Bei Fehlern startet es das vorherige Release. Datenbankmigrationen bleiben bei einem Anwendungs-Rollback bestehen.

Nach dem Deployment prüfen, dass `/health/live` und `/health/ready` HTTP 200 liefern. Anschließend mit einem Testkonto Anmeldung, Passkey und eine Transaktionsmail prüfen.

### Neuinstallation mit einem bestehenden Konto

Für den Wechsel auf eine frische Datenbank `IgelPortalNew` die Version zunächst nur über den lokalen Port mit deaktiviertem Mailversand starten, damit EF das leere Schema anlegt. Die Quell-Datenbank `IgelPortal` bleibt bis zur Umschaltung beim alten Dienst. Die vorbereitete Kopie `IgelPortalPreview` ist kein Importziel.

Den neuen Dienst vor dem Import stoppen. Das [Importskript](../build/deployment/import-single-account.sql) aus dem bereitgestellten Release-Stand verwenden. Mit einem administrativen MariaDB-Konto `IgelPortalNew` auswählen und im selben Client die gewünschte Adresse setzen:

```sql
SET @igel_import_email = '<E-Mail des zu übernehmenden Kontos>';
SOURCE /absoluter/pfad/build/deployment/import-single-account.sql
```

Der Import prüft die ausgewählte Zieldatenbank, ihre leeren Fachdaten und die Eindeutigkeit des Kontos. Er übernimmt die Rollendefinitionen und Rollen-Claims sowie die Zuweisungen und vorhandenen Anmeldedaten dieses Kontos in einer Transaktion. Alte Sitzungen werden erneuert. Die Ausgabe zeigt nur Anzahl der Konten, Rollen und Zuweisungen.

Vor der öffentlichen Umschaltung Anmeldung, Rollen und Brevo mit dem neuen Dienst prüfen. Die alte Installation bis zum Abschluss der Prüfungen als Rückfallmöglichkeit bereithalten. Der öffentliche Host bleibt `www.igel-portal.de`, damit die Passkey-Domain erhalten bleibt.
