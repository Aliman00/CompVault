# Common

`Common/` inneholder det som deles av flere features. Layouts, globale komponenter, tjenester som holder tilstand på tvers av hele appen — alt som ikke er knyttet til ett spesifikt fagområde.

## Struktur

```text
Common/
├── <Kategori>/       <- f.eks. Layouts, Components, Services, Models
├── <Kategori>/
└── <Kategori>/       <- ny kategori ved behov
```

## Hva hører hjemme her?

Spør deg selv: "brukes dette i mer enn én feature?"

- **Ja** → legg det i `Common/`
- **Nei** → legg det inne i feature-mappen den tilhører

Eksempler på ting som hører hjemme her:

- **Layouts** — hoved-layout med sidebar/navbar, autentiserings-layout, feil-sider
- **Komponenter** — gjenbrukbare komponenter som laste-indikatorer, bekreftelsesdialoger, tom-tilstandsvisning
- **Tjenester** — autentiseringstilstand, tema, toast-varsler, token-oppfrisking
- **Modeller** — view-modeller som brukes på tvers av flere features

## Hva hører IKKE hjemme her?

- Feature-spesifikke API-klienter (f.eks. `UserApiClient`) — de hører til i `Features/Users/`
- Feature-spesifikke sider (f.eks. `UserList.razor`) — de hører til i `Features/Users/Pages/`
- Feature-spesifikke modeller som bare én feature bruker

## Registrering

Tjenester i `Common/Services/` registreres i `Extensions/ServiceCollectionExtensions.cs`. Se `Extensions/README.md`.
