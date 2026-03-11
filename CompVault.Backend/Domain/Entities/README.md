# Domain / Entities

> Rene C#-klasser som representerer databasetabellene. Ingen rammeverksavhengigheter her.

## Struktur

Entiteter er gruppert etter domeneområde i undermapper:

```
Domain/Entities/
  Identity/        <- ApplicationUser, ApplicationRole, Department, Permission, RolePermission
  <Domene>/        <- ny undermappe per domeneområde som legges til
```

## Regler

- Ingen import av EF Core, ASP.NET eller andre rammeverk
- Ingen avhengighet til andre lag i prosjektet
- Enkel forretningslogikk som kun opererer på egne felt er OK
- Enums legges i `CompVault.Shared/Enums/` slik at Frontend også kan bruke dem

## Ny entitet? Gjør slik

1. Opprett filen i riktig undermappe under `Domain/Entities/<Domene>/`
2. Opprett tilhørende EF Core-konfigurasjon i `Infrastructure/Data/Configurations/<Domene>/`
