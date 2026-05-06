# Infrastructure/Jobs

`Infrastructure/Jobs/` inneholder bakgrunnsjobber som kjører utenfor vanlige HTTP-requests. Dette brukes til oppgaver som må gå periodisk i bakgrunnen, for eksempel opprydning eller oppdatering av statusfelt.

## Struktur

```text
Infrastructure/Jobs/
└── <JobName>Job.cs    <- Hver jobb ligger i sin egen fil
```

## Hvordan vi bruker denne mappen

Jobbene her kjører som hosted services og er ment for arbeid som ikke passer inn i en vanlig request/response-flyt. I prosjektet brukes dette blant annet til å rydde opp i tokens og til å oppdatere kompetansestatus automatisk.

Eksisterende jobber akkurat nå er:

- **CompetencyStatusJob** — oppdaterer status på kompetansebevis (Valid/ExpiringSoon/Expired) og logger endringer i AuditLog
- **TokenCleanupJob** — rydder opp i utløpte og revokerte refresh tokens og OTP-koder
- **ExpiryNotificationJob** — sender e-postvarsler til ansatte og ledere når kompetansebevis nærmer seg utløp

## Registrering

Jobber registreres i `Infrastructure/Extensions/ServiceCollectionExtensions.cs` via `AddInfrastructure()`:

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services)
{
    services.AddHostedService<TokenCleanupJob>();
    services.AddHostedService<CompetencyStatusJob>();
    services.AddHostedService<ExpiryNotificationJob>();
    return services;
}
```

## Når du lager en ny jobb

1. Opprett `Infrastructure/Jobs/<JobName>Job.cs`.
2. Implementer `IHostedService` eller, som oftest, arv fra `BackgroundService`.
3. Registrer jobben i oppstarten.

```csharp
public class MyJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

## Retningslinjer

- `BackgroundService` er som regel et fint utgangspunkt.
- Husk å respektere `stoppingToken` slik at applikasjonen kan stenge ryddig ned.
- Langvarige oppgaver hører bedre hjemme her enn i vanlige controllere eller services som bare er laget for requests.
