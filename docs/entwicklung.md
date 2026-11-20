# Entwicklung

Das Igel-Portal ist eine ASP.NET-Core-Razor-Pages-Anwendung. ASP.NET Core Identity verwaltet Konten, Rollen und Zwei-Faktor-Anmeldung. WebAuthn-Passkeys und Google-Anmeldung sind integriert. MariaDB speichert die Daten, Nominatim ermittelt Koordinaten für Revieradressen.

Die folgenden Befehle werden im Stammverzeichnis des Repositorys ausgeführt. Hinweise zu E-Mail-Versand, Registrierungsschutz und Deployment stehen in der [Betriebsdokumentation](betrieb.md).

## Lokaler Start

Das Projekt benötigt das .NET-10-SDK und eine erreichbare MySQL- oder MariaDB-Datenbank. Die Anwendung erkennt die Datenbankversion beim Start, wendet vorhandene EF-Migrationen an und prüft die Identity-Rollen. Die bestehenden Migrationen verwenden auch mit dem Microting-Paket die vom Provider beibehaltenen Pomelo-Namespaces.

```bash
dotnet user-secrets --project src/IgelPortal.Web set 'ConnectionStrings:DefaultConnection' '<lokale Verbindungszeichenfolge>'
dotnet run --project src/IgelPortal.Web
```

Für einen Release-Build mit Schwachstellenprüfung der NuGet-Abhängigkeiten und einem Starttest gegen eine leere MariaDB 10.11 in Docker:

```bash
dotnet restore src/IgelPortal.sln -p:NuGetAuditMode=all -p:WarningsAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904
dotnet build src/IgelPortal.sln -c Release --no-restore
bash build/smoke-test.sh
```
