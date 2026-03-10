# Features

Én mappe per domene-feature. Her lever all forretningslogikk.

**Struktur per feature:**
- `IXyzService.cs` — interface (kontrakten)
- `XyzService.cs` — implementasjon

**Ny feature? Opprett:**
```
Features/MinFeature/IMinFeatureService.cs
Features/MinFeature/MinFeatureService.cs
```

> **Merk:** DTOs og request-modeller legges **ikke** i Features-mappa.
> De hører hjemme i `CompVault.Shared/DTOs/<FeatureName>/` slik at
> Frontend kan bruke de samme typene uten å duplisere kode.
