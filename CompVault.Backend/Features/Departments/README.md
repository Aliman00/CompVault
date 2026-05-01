# Departments

Her ligger alt som har med avdelinger å gjøre: selve avdelingsstrukturen, hvem som er avdelingsleder, og ikke minst avdelingsscope — altså hvem som får se hva basert på hvilken avdeling de tilhører.

## Avdelingsscope

Dette er en sentral del av sikkerhetsmodellen i CompVault. Hver bruker har en avdeling, og den avdelingen styrer hvilke data brukeren får tilgang til. Det finnes tre nivåer:

- **Egen avdeling** — som standard ser du bare ting i din egen avdeling
- **Underavdelinger** — en permission som `users:read:sub` eller `departments:read:sub` lar deg også se ting i avdelinger som ligger under din egen
- **Alle avdelinger** — permissions som `users:read:all` eller `departments:read:all` gir full tilgang på tvers av hele organisasjonen

Dette håndteres av `DepartmentScopeService` som bygger opp listen over tillatte avdelinger når en request kommer inn. Servicen brukes av både query-filtre i databasen og av interceptoren som beskytter skriveoperasjoner.

## UserDepartmentWriteInterceptor

I denne mappen ligger også `UserDepartmentWriteInterceptor` — en EF Core interceptor som kjører før hver `SaveChangesAsync`. Den sjekker at du ikke prøver å opprette eller endre en bruker i en avdeling du ikke har tilgang til. Dette er siste forsvarslinje — selv om noen skulle klare å sende en request til en avdeling de ikke skal røre, stopper interceptoren det før det treffer databasen.

Interceptoren er den eneste i prosjektet som ligger under `Features/` i stedet for `Infrastructure/Data/Interceptors/`. Grunnen til det er at den er tett knyttet til `DepartmentScopeService` og hører logisk hjemme sammen med scope-logikken.

## Struktur

```
Features/Departments/
├── DepartmentMapper.cs                         <- Mapper fra entitet til DTO
├── Controllers/
│   └── DepartmentsController.cs                <- CRUD for avdelinger
└── Services/
    ├── IDepartmentService.cs / DepartmentService.cs            <- Avdelings-CRUD
    ├── IDepartmentScopeService.cs / DepartmentScopeService.cs  <- Hvem får se hva
    └── UserDepartmentWriteInterceptor.cs                       <- Beskytter skriving av brukere
```
