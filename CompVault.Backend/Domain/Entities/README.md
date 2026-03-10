# Domain / Entities

Rene C#-klasser som representerer databasetabellene.

## Struktur

Entiteter er gruppert etter domeneområde i undermapper:

- `Identity/` — ApplicationUser, ApplicationRole, Department, Permission, RolePermission
- `Competencies/` — opprettes i fase 4
- `Documents/` — opprettes i fase 5
- `Equipment/` — opprettes i fase 6
- `Requirements/` — opprettes i fase 7
- `Onboarding/` — opprettes i fase 8
- `Notifications/` — opprettes i fase 10
- `Audit/` — opprettes i fase 11

**Regler:**
- Ingen import av EF Core, ASP.NET eller andre rammeverk her
- Ingen avhengighet til andre lag
- Enkel forretningslogikk som kun bruker egne felt er OK
- Enums legges **ikke** her — de hører hjemme i `CompVault.Shared/Enums/` så Frontend kan bruke dem
