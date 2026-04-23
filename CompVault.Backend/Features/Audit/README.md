# AuditLog – Revisjonslogg-API

 Sentral revisjonslogg som fanger alle vesentlige endringer i CompVault. Følger Arbeidstilsynets krav til dokumentasjon (hvem, hva, når, hvorfor).

---

## Endepunkt

```
GET /api/audit-log
```

Krever tillatelse: `audit:read` (kun Admin)

### Query-parametere

| Parameter     | Type       | Beskrivelse                                        |
|--------------|------------|---------------------------------------------------|
| `action`     | `string`   | Filtrer på handlingstype, f.eks. `competency.revoke` |
| `entityType` | `string`   | Filtrer på entitetstype, f.eks. `Competency`       |
| `entityId`    | `Guid`     | Filtrer på spesifikk entitet                        |
| `userId`     | `Guid`     | Filtrer på hvem som utførte handlingen              |
| `from`       | `DateTime` | Fra-dato (inclusive), ISO 8601                     |
| `to`         | `DateTime` | Til-dato (exclusive), ISO 8601                      |
| `page`       | `int`      | Side (1-basert, default 1)                         |
| `pageSize`   | `int`      | Antall per side (default 50, maks 100)             |

### Eksempler

```bash
# Hent alt (side 1, 50 per side)
GET /api/audit-log

# Alle signaturer på dokumenter
GET /api/audit-log?entityType=DocumentSignature

# Alle tilbakekallinger av kompetanser
GET /api/audit-log?action=competency.revoke

# Hendelser på en spesifikk kompetanse
GET /api/audit-log?entityType=Competency&entityId=<guid>

# Alle handlinger utført av en bruker
GET /api/audit-log?userId=<guid>

# Hendelser i april 2026
GET /api/audit-log?from=2026-04-01T00:00:00Z&to=2026-05-01T00:00:00Z

# Fjernede signaturer (ved dokumentversjon-oppdatering)
GET /api/audit-log?action=document.signature_removed

# Automatiske statusoppdateringer (bakgrunnsjobb)
GET /api/audit-log?action=competency.status_auto_update

# Rolle-tillatelser tildelt
GET /api/audit-log?action=role.permissions_assigned
```

### Responsformat

```json
{
  "items": [
    {
      "id": "guid",
      "action": "competency.revoke",
      "entityType": "Competency",
      "entityId": "guid",
      "userId": "guid | null",
      "userName": "Kari Nordmann | System",
      "userEmail": "kari@example.com | null",
      "details": { ... },
      "createdAt": "2026-04-22T15:30:00Z"
    }
  ],
  "totalCount": 156,
  "page": 1,
  "pageSize": 50
}
```

---

## Action-typer

Hver innføring har en `action` på formatet `{entity}.{verb}`:

| Action                            | Utløst av                                            | Details-felt                                      |
|----------------------------------|------------------------------------------------------|----------------------------------------------------|
| `competency.create`              | Opprette kompetansebevis via API                     | Egenskaper ved opprettelse                          |
| `competency.update`              | Redigere kompetansebevis                             | `changed_fields` med old/new-verdier               |
| `competency.revoke`              | Tilbakekalle kompetansebevis                         | `changed_fields` + `reason` (revoked_reason)       |
| `competency.delete`              | Soft-delete (slette) kompetansebevis                | —                                                  |
| `competency.status_auto_update`  | Bakgrunnsjobb oppdaterer Expired/ExpiringSoon        | `old_status`, `new_status`, `trigger: "expiry_check_job"` |
| `document.create`                | Opprette dokument                                    | Titel, versjon, requiresSignature osv.             |
| `document.update`                | Redigere dokument                                    | `changed_fields` med old/new-verdier               |
| `document.upload_version`        | Laste opp ny versjon av dokument                     | `changed_fields` + dokumenttittel, gammel/ny versjon |
| `document.delete`                | Soft-delete dokument                                 | —                                                  |
| `document.signature_removed`     | Hard-delete av signatur ved ny versjon               | `document_id`, `removed_user_id`, `old_version`, `signed_at` (+ `document_title`, `new_version` hvis tilgjengelig) |
| `document_signature.create`      | Bruker signerer et dokument                          | Egenskaper ved signaturen                          |
| `document_type.create`           | Opprette dokumenttype                                | Navn, slug, targetMode osv.                        |
| `document_type.update`           | Redigere dokumenttype                               | `changed_fields`                                   |
| `document_type_category.create`  | Opprette dokumenttypekategori                        | Navn, slug, isActive                                |
| `department.create`              | Opprette avdeling                                    | Navn, beskrivelse                                   |
| `department.update`              | Redigere avdeling                                    | `changed_fields`                                    |
| `department.delete`              | Soft-delete avdeling                                 | —                                                  |
| `job_title.create`               | Opprette stillingstittel                             | Navn                                                |
| `job_title.update`               | Redigere stillingstittel                             | `changed_fields`                                    |
| `job_title.delete`               | Soft-delete stillingstittel                          | —                                                  |
| `equipment_category.create`      | Opprette utstyrskategori                             | Navn, beskrivelse                                   |
| `equipment_item.create`           | Opprette utstyr                                      | Navn, hasSize                                       |
| `equipment_issuance.create`      | Utlevere utstyr til ansatt                           | Quantity, size, issuedDate                          |
| `application_user.update`        | Redigere brukerprofil                               | `changed_fields`                                    |
| `application_user.delete`        | Soft-delete (deaktivere) bruker                      | —                                                  |
| `application_role.create`        | Opprette rolle                                      | Navn, beskrivelse                                   |
| `application_role.update`        | Redigere rolle                                       | `changed_fields`                                    |
| `application_role.delete`        | Slette rolle (hard-delete)                           | Opprinnelige verdier                                |
| `role.permissions_assigned`      | Tildele/endre tillatelser for en rolle                | `added_permissions`, `removed_permissions`, `role_name` |

---

## Details-feltet

`details` er et JSONB-objekt med fleksibelt innhold per action-type.

### Vanlige nøkler

| Nøkkel              | Når den brukes                                    | Eksempel                                         |
|---------------------|--------------------------------------------------|--------------------------------------------------|
| `changed_fields`   | Ved `.update`-handlinger                         | `{"Status": {"old": "Valid", "new": "Revoked"}}` |
| `reason`           | Ved `.revoke` og action override                 | `"Sikkerhetsbrudd ved truckkjøring"`              |
| `added_permissions`| Ved `role.permissions_assigned`                  | `["competencies:read", "documents:write"]`        |
| `removed_permissions`| Ved `role.permissions_assigned`                | `["documents:delete"]`                             |
| `old_status`       | Ved `competency.status_auto_update`              | `"Valid"`                                         |
| `new_status`       | Ved `competency.status_auto_update`              | `"Expired"`                                       |
| `trigger`          | Ved bakgrunnsjobb-hendelser                      | `"expiry_check_job"`                              |
| `document_id`      | Ved `document.signature_removed`                 | `"guid"`                                          |
| `removed_user_id`  | Ved `document.signature_removed`                 | `"guid"`                                          |
| `old_version`      | Ved `document.signature_removed`                 | `1`                                               |
| `new_version`      | Ved `document.upload_version` / signature_removed| `2`                                               |
| `document_title`   | Ved `document.signature_removed` (hvis tilgjengelig) | `"Brannverninstruks"`                        |
| `removed_user_name`| Ved `document.signature_removed` (hvis tilgjengelig) | `"Kari Nordmann"`                             |

---

## Brukeridentifikasjon

| Felt        | Innlogget bruker                    | Bakgrunnsjobb          |
|-------------|--------------------------------------|------------------------|
| `userId`    | Brukerens GUID                       | `null`                 |
| `userName`  | Fullt navn fra claims                | `"System"`             |
| `userEmail` | E-post fra claims                    | `null`                 |

Brukerinfo er **denormalisert** — dvs. at `userName` og `userEmail` lagres direkte
i revisjonsloggen og **ikke** påvirkes av at brukeren senere endrer navn,
deaktiveres eller soft-slettes.

---

## Ignorerte entitetstyper

Følgende entitetstyper logges **ikke** i revisjonsloggen:

- `OtpCode` — kortlevde engangskoder
- `RefreshToken` — token-rotasjon
- `AuditLog` — selvereferanse unngås
- `DocumentDepartment` — join-tabell (logges via `document.update`)
- `DocumentJobTitle` — join-tabell (logges via `document.update`)
- `DocumentVersion` — intern versjonering
- `RolePermission` — logges aggregert via `role.permissions_assigned`

---

## Arkitektur

Revisjonslogging skjer via to mekanismer:

### 1. SaveChangesInterceptor (automatisk)
`AuditSaveChangesInterceptor` fanger alle endringer som går gjennom EF Core sin ChangeTracker:
- **Added** → `{entity}.create`
- **Modified** → `{entity}.update` (eller `{entity}.delete` ved soft-delete hvis `DeletedAt` endres fra null til verdi)
- **Deleted** → `{entity}.delete` (hard-delete) / `document.signature_removed` (spesialhåndtering)

### 2. IAuditContext (manuell kontekst)
Services kan sette forretningskontekst før `SaveChangesAsync`:

```csharp
// I CompetencyService.RevokeAsync:
auditContext.SetActionOverride("competency.revoke");
auditContext.SetReason(request.RevokedReason);

// I DocumentVersioningService.UploadVersionAsync:
auditContext.SetActionOverride("document.upload_version");
```

Interceptoren leser disse og tømmer dem automatisk etter SaveChanges.

### 3. Manuell logging (bakgrunnsjobber)
`ExecuteUpdateAsync` går utenom ChangeTracker, så `CompetencyStatusJob` oppretter
AuditLog-entries manuelt med `action: "competency.status_auto_update"`.