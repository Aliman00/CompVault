# Features

> All forretningslogikk i Backend. Én mappe per domene-feature med et interface og én implementasjon.

## Struktur

```
Features/
  Auth/    <- IAuthService, AuthService   — autentisering og OTP-flyt
  Users/   <- IUserService, UserService   — brukeradministrasjon
  Test/    <- TestController, dtos        — utviklingsverktøy, fjernes før produksjon
```

## Mønster per feature

```
Features/<FeatureName>/
  I<FeatureName>Service.cs   <- interface (kontrakten som controllers og tester bruker)
  <FeatureName>Service.cs    <- implementasjon
```

## Regler

- Services injiseres alltid via interface — aldri direkte implementasjon
- Bruk `Result<T>` fra `CompVault.Shared/Result/` som returtype fra alle public metoder
- DTOs og request-modeller legges i `CompVault.Shared/DTOs/<FeatureName>/` — ikke her
- Alle asynkrone metoder skal ta en `CancellationToken ct = default`-parameter

## Ny feature? Gjør slik

1. Opprett `Features/<FeatureName>/I<FeatureName>Service.cs`
2. Opprett `Features/<FeatureName>/<FeatureName>Service.cs`
3. Registrer i `Infrastructure/Extensions/ServiceCollectionExtensions.cs`
4. Opprett tilhørende DTO-mappe i `CompVault.Shared/DTOs/<FeatureName>/`
