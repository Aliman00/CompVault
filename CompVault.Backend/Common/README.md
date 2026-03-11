# Common

> Delt infrastrukturkode uten forretningsverdi, brukt på tvers av alle features i Backend.

## Innhold

| Fil | Ansvar |
|---|---|
| `Middleware/GlobalExceptionHandler.cs` | Fanger opp ubehandlede exceptions og returnerer et strukturert feilsvar |

## Regler

- Legg **ikke** feature-spesifikk kode her
- Legg **ikke** domene-typer (DTOs, enums, konstanter) her — de hører hjemme i `CompVault.Shared`
