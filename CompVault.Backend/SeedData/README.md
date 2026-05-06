# SeedData

SeedData inneholder all mock-data som brukes for å populere databasen ved oppstart i utviklingsmiljøet. Data representerer en fiktiv barnehage — "Lekestua" — og dekker alt fra ansatte og stillingstitler til kompetansebevis, dokumenter og utstyr.

## Struktur

```text
SeedData/
├── BarnehageData.cs     <- All seed-data definert i én stor fil (4 roller, 7 stillingstitler, 10 avdelinger, 31 ansatte, ++)
├── DatabaseSeeder.cs    <- Kjører seeding i riktig rekkefølge (roles > permissions > users > departments > ++)
└── Seeders/             <- Individuelle seedere per domene, alle orkestrert av DatabaseSeeder
    ├── RoleSeeder, PermissionSeeder, RolePermissionSeeder
    ├── JobTitleSeeder, DepartmentSeeder, UserSeeder
    ├── CompetencyTypeSeeder, CompetencySeeder
    ├── DocumentTypeSeeder, DocumentCategorySeeder, DocumentSeeder, DocumentSignatureSeeder
    └── EquipmentSeeder
```

## Hvordan seeding fungerer

`DatabaseSeeder.SeedAsync()` kalles fra `Program.cs` kun i Development-miljøet. Den går gjennom seedere i en bestemt rekkefølge for å unngå foreign key-problemer — man kan ikke seede en bruker før avdelingen hennes finnes, og man kan ikke seede en kompetanse før både bruker og kompetansetype er på plass.

Alle endringer wrappes i én transaksjon, så hvis noe feiler ruller alt tilbake.

## Når du legger til ny seed-data

1. Oppdater `BarnehageData.cs` med nye entiteter (eller endre eksisterende).
2. Hvis du har lagt til en ny entitetstype, opprett en `*Seeder.cs` i `Seeders/`-mappen.
3. Registrer seedere i `DatabaseSeeder` i riktig rekkefølge.

**Husk:** Endringer her påvirker kun utviklingsmiljøet. Produksjonsdata håndteres separat.
