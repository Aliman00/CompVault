# CompVault.Tests

> xUnit-testprosjekt for CompVault. Mappestrukturen speiler `CompVault.Backend`.

## Struktur

```
CompVault.Tests/
  Features/              <- enhetstester for services (opprettes per feature)
  Controllers/           <- opprettes ved behov
  Infrastructure/
    Email/               <- integrasjonstester for e-posttjenesten
```

## Teststrategi

| Testtype | Verktøy | Plassering |
|---|---|---|
| Enhetstester (services, logikk) | xUnit + Moq | `Features/<FeatureName>/` |
| Integrasjonstester (infrastruktur) | xUnit | `Infrastructure/<Komponent>/` |
| Controller-tester | xUnit + Moq | `Controllers/` |

## Konvensjoner

- **Klassenavn:** `<KlasseUnderTest>Tests`
- **Metodenavn:** `<Metode>_<Scenario>_<ForventetResultat>`
  - Eksempel: `SendAsync_WithValidEmail_SendsSuccessfully`
- Mock avhengigheter med `Mock<T>` — test aldri mot ekte database eller SMTP i enhetstester

## Ny test? Gjør slik

1. Opprett testklasse i riktig undermappe (speil `CompVault.Backend`-strukturen)
2. Injiser avhengigheter som `Mock<T>` i konstruktøren
3. Følg Arrange / Act / Assert — én paastand per testmetode
