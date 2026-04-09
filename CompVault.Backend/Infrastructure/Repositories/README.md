# Infrastructure/Repositories

`Infrastructure/Repositories/` inneholder den generiske repository-basen som resten av repositoryene bygger på. Her ligger felles interface og baseimplementasjon, slik at domenespesifikke repositories slipper å gjenta det samme grunnoppsettet.

## Struktur

```text
Infrastructure/Repositories/
├── IRepository.cs        <- Generisk base-interface med standard CRUD
└── BaseRepository.cs     <- Generisk base-implementasjon
```

## Hvordan vi bruker denne mappen

Tanken med denne mappen er å samle det som går igjen i flere repositories. Når et domene trenger egne oppslag eller mer spesifikk dataaksess, kan repositoryet bygge videre på `BaseRepository<T>` i stedet for å starte helt fra bunnen av.

Det betyr at denne mappen ikke prøver å beskrive alle konkrete repositories i prosjektet, bare den felles basen de bygger på.

## Lagring

Siden alle repositories arver fra `BaseRepository`, har de også tilgang til `SaveChangesAsync()`:

```csharp
await competencyRepository.SaveChangesAsync(ct);
```

Dette er greit for enkle operasjoner der ett repository alene gjør endringer. Når flere operasjoner må skje samlet i én transaksjon, brukes `IUnitOfWork` fra `Infrastructure/Data/` i stedet.

## Retningslinjer

- Repositories skal håndtere dataaksess og change tracking, ikke forretningslogikk.
- `IQueryable` kan brukes internt, men det er som regel bedre å returnere materialiserte resultater ut av repositoryet.
- Metoder bør ta `CancellationToken ct = default` der det er naturlig.
- `SaveChangesAsync()` kan brukes for enkel lagring, mens transaksjoner håndteres via `IUnitOfWork`.
