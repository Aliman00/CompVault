namespace CompVault.Backend.SeedData;

using CompVault.Shared.Enums;

/// <summary>
/// All statisk seed-data for den fiktive barnehagen "Lekestua".
/// </summary>
public static class BarnehageData
{
    private const string Domain = "lekestua.no";

    // ======================== Roller ========================
    public static readonly (string Name, string Description)[] Roles =
    [
        ("Admin", "Full tilgang til alle funksjoner i systemet."),
        ("Avdelingsleder", "Leder tilgang – kan se og styre egen avdeling med underavdelinger."),
        ("Gruppeleder", "Gruppeleder tilgang – kan se og styre egen underavdeling."),
        ("Ansatt", "Vanlig ansatt – kan føre egne data, men ikke se andre brukere."),
    ];

    // ======================== Stillinger ========================
    public static readonly (string Name, bool IsLeader)[] JobTitles =
    [
        ("Systemadministrator", false),
        ("Daglig leder", true),
        ("Rådgiver", false),
        ("Avdelingsleder", true),
        ("Gruppeleder", true),
        ("Pedagog", false),
        ("Assistent", false),
    ];

    // ======================== Avdelinger ========================
    // (Navn, Beskrivelse, ParentNavn)
    public static readonly (string Name, string Description, string? ParentName)[] Departments =
    [
        ("System", "Systemadministratorer og teknisk støtte.", null),
        ("Ledelse", "Overordnet ledelse av barnehagen.", null),
        ("Storebarns avdeling", "Avdeling for store barn (3–6 år).", null),
        ("Småbarns avdeling", "Avdeling for små barn (1–3 år).", null),
        ("Sol", "Gruppe for store barn – Sol.", "Storebarns avdeling"),
        ("Måne", "Gruppe for store barn – Måne.", "Storebarns avdeling"),
        ("Stjerne", "Gruppe for store barn – Stjerne.", "Storebarns avdeling"),
        ("Gresshoppe", "Gruppe for små barn – Gresshoppe.", "Småbarns avdeling"),
        ("Sommerfugl", "Gruppe for små barn – Sommerfugl.", "Småbarns avdeling"),
        ("Humle", "Gruppe for små barn – Humle.", "Småbarns avdeling"),
    ];

    // ======================== Brukere ========================
    // (Fornavn, Etternavn, E-post, Avdeling, Stilling, LederEpost, Roller[], Ansettelsesdato)
    // LederEpost = null hvis ingen leder, ellers e-post til leder
    public static readonly (string FirstName, string LastName, string Email, string Department, string JobTitle, string? ManagerEmail, string[] Roles, DateTime CreatedAt)[] Users =
    [
        // System (Admin)
        ("Almin", "Colakovic", "almin.dev@pm.me", "System", "Systemadministrator", null, ["Admin"], new DateTime(2023, 1, 5, 8, 30, 0, DateTimeKind.Utc)),
        ("Majlinda", "Lajci", "gamingnerd824@gmail.com", "System", "Systemadministrator", null, ["Admin"], new DateTime(2023, 3, 12, 9, 0, 0, DateTimeKind.Utc)),
        ("Fredrik", "Magee", "fredrik@magee.no", "System", "Systemadministrator", null, ["Admin"], new DateTime(2023, 6, 20, 8, 45, 0, DateTimeKind.Utc)),

        // Ledelse
        ("Lise", "Hansen", $"lise.hansen@{Domain}", "Ledelse", "Daglig leder", null, ["Admin"], new DateTime(2020, 8, 1, 7, 0, 0, DateTimeKind.Utc)),
        ("Ola", "Nordmann", $"ola.nordmann@{Domain}", "Ledelse", "Rådgiver", $"lise.hansen@{Domain}", ["Ansatt"], new DateTime(2022, 1, 10, 8, 0, 0, DateTimeKind.Utc)),
        ("Kari", "Nordmann", $"kari.nordmann@{Domain}", "Ledelse", "Rådgiver", $"lise.hansen@{Domain}", ["Ansatt"], new DateTime(2022, 4, 3, 8, 15, 0, DateTimeKind.Utc)),
        ("Tobias", "Lie", $"tobias.lie@{Domain}", "Ledelse", "Rådgiver", $"lise.hansen@{Domain}", ["Ansatt"], new DateTime(2022, 9, 15, 8, 30, 0, DateTimeKind.Utc)),

        // Storebarns avdeling
        ("Anne", "Berg", $"anne.berg@{Domain}", "Storebarns avdeling", "Avdelingsleder", $"lise.hansen@{Domain}", ["Avdelingsleder"], new DateTime(2021, 2, 20, 8, 0, 0, DateTimeKind.Utc)),

        // Sol
        ("Sofie", "Sol", $"sofie.sol@{Domain}", "Sol", "Gruppeleder", $"anne.berg@{Domain}", ["Gruppeleder"], new DateTime(2021, 5, 15, 8, 30, 0, DateTimeKind.Utc)),
        ("Lars", "Sol", $"lars.sol@{Domain}", "Sol", "Pedagog", $"sofie.sol@{Domain}", ["Ansatt"], new DateTime(2023, 8, 12, 9, 0, 0, DateTimeKind.Utc)),
        ("Ingrid", "Sol", $"ingrid.sol@{Domain}", "Sol", "Pedagog", $"sofie.sol@{Domain}", ["Ansatt"], new DateTime(2024, 1, 8, 8, 45, 0, DateTimeKind.Utc)),
        ("Erik", "Sol", $"erik.sol@{Domain}", "Sol", "Assistent", $"sofie.sol@{Domain}", ["Ansatt"], new DateTime(2024, 6, 3, 8, 30, 0, DateTimeKind.Utc)),

        // Måne
        ("Sara", "Måne", $"sara.mane@{Domain}", "Måne", "Gruppeleder", $"anne.berg@{Domain}", ["Gruppeleder"], new DateTime(2021, 3, 10, 8, 0, 0, DateTimeKind.Utc)),
        ("Per", "Måne", $"per.mane@{Domain}", "Måne", "Pedagog", $"sara.mane@{Domain}", ["Ansatt"], new DateTime(2022, 11, 22, 9, 0, 0, DateTimeKind.Utc)),
        ("Emma", "Måne", $"emma.mane@{Domain}", "Måne", "Pedagog", $"sara.mane@{Domain}", ["Ansatt"], new DateTime(2024, 2, 5, 8, 45, 0, DateTimeKind.Utc)),
        ("Noah", "Måne", $"noah.mane@{Domain}", "Måne", "Assistent", $"sara.mane@{Domain}", ["Ansatt"], new DateTime(2024, 9, 1, 8, 30, 0, DateTimeKind.Utc)),

        // Stjerne
        ("Nora", "Stjerne", $"nora.stjerne@{Domain}", "Stjerne", "Gruppeleder", $"anne.berg@{Domain}", ["Gruppeleder"], new DateTime(2021, 6, 1, 8, 15, 0, DateTimeKind.Utc)),
        ("Erik", "Stjerne", $"erik.stjerne@{Domain}", "Stjerne", "Pedagog", $"nora.stjerne@{Domain}", ["Ansatt"], new DateTime(2023, 3, 20, 9, 0, 0, DateTimeKind.Utc)),
        ("Mia", "Stjerne", $"mia.stjerne@{Domain}", "Stjerne", "Pedagog", $"nora.stjerne@{Domain}", ["Ansatt"], new DateTime(2024, 4, 8, 8, 30, 0, DateTimeKind.Utc)),
        ("Leo", "Stjerne", $"leo.stjerne@{Domain}", "Stjerne", "Assistent", $"nora.stjerne@{Domain}", ["Ansatt"], new DateTime(2025, 1, 15, 8, 45, 0, DateTimeKind.Utc)),

        // Småbarns avdeling
        ("Bente", "Gress", $"bente.gress@{Domain}", "Småbarns avdeling", "Avdelingsleder", $"lise.hansen@{Domain}", ["Avdelingsleder"], new DateTime(2020, 11, 10, 7, 30, 0, DateTimeKind.Utc)),

        // Gresshoppe
        ("Hans", "Gresshoppe", $"hans.gresshoppe@{Domain}", "Gresshoppe", "Gruppeleder", $"bente.gress@{Domain}", ["Gruppeleder"], new DateTime(2021, 8, 5, 8, 0, 0, DateTimeKind.Utc)),
        ("Eva", "Gresshoppe", $"eva.gresshoppe@{Domain}", "Gresshoppe", "Pedagog", $"hans.gresshoppe@{Domain}", ["Ansatt"], new DateTime(2023, 5, 12, 9, 0, 0, DateTimeKind.Utc)),
        ("Knut", "Gresshoppe", $"knut.gresshoppe@{Domain}", "Gresshoppe", "Pedagog", $"hans.gresshoppe@{Domain}", ["Ansatt"], new DateTime(2024, 3, 1, 8, 45, 0, DateTimeKind.Utc)),
        ("Pia", "Gresshoppe", $"pia.gresshoppe@{Domain}", "Gresshoppe", "Assistent", $"hans.gresshoppe@{Domain}", ["Ansatt"], new DateTime(2024, 8, 20, 8, 30, 0, DateTimeKind.Utc)),

        // Sommerfugl
        ("Kari", "Sommerfugl", $"kari.sommerfugl@{Domain}", "Sommerfugl", "Gruppeleder", $"bente.gress@{Domain}", ["Gruppeleder"], new DateTime(2021, 9, 15, 8, 15, 0, DateTimeKind.Utc)),
        ("Ole", "Sommerfugl", $"ole.sommerfugl@{Domain}", "Sommerfugl", "Pedagog", $"kari.sommerfugl@{Domain}", ["Ansatt"], new DateTime(2022, 6, 8, 9, 0, 0, DateTimeKind.Utc)),
        ("Liv", "Sommerfugl", $"liv.sommerfugl@{Domain}", "Sommerfugl", "Pedagog", $"kari.sommerfugl@{Domain}", ["Ansatt"], new DateTime(2024, 5, 10, 8, 30, 0, DateTimeKind.Utc)),
        ("Tom", "Sommerfugl", $"tom.sommerfugl@{Domain}", "Sommerfugl", "Assistent", $"kari.sommerfugl@{Domain}", ["Ansatt"], new DateTime(2025, 2, 18, 8, 45, 0, DateTimeKind.Utc)),

        // Humle
        ("Grete", "Humle", $"grete.humle@{Domain}", "Humle", "Gruppeleder", $"bente.gress@{Domain}", ["Gruppeleder"], new DateTime(2021, 4, 20, 8, 0, 0, DateTimeKind.Utc)),
        ("Arne", "Humle", $"arne.humle@{Domain}", "Humle", "Pedagog", $"grete.humle@{Domain}", ["Ansatt"], new DateTime(2023, 1, 25, 9, 0, 0, DateTimeKind.Utc)),
        ("Ruth", "Humle", $"ruth.humle@{Domain}", "Humle", "Pedagog", $"grete.humle@{Domain}", ["Ansatt"], new DateTime(2024, 7, 15, 8, 30, 0, DateTimeKind.Utc)),
        ("Finn", "Humle", $"finn.humle@{Domain}", "Humle", "Assistent", $"grete.humle@{Domain}", ["Ansatt"], new DateTime(2025, 3, 5, 8, 45, 0, DateTimeKind.Utc)),
    ];

    // ======================== Kompetansetyper ========================
    // (Navn, Beskrivelse, Kategori, KreverUtløpsdato)
    public static readonly (string Name, string? Description, string? Category, bool RequiresExpiration)[] CompetencyTypes =
    [
        ("HMS-kurs (årlig)", "Pliktig HMS-opplæring for alle ansatte.", "HMS", true),
        ("Førstehjelp", "Kurs i førstehjelp og livredning.", "HMS", true),
        ("Pedagogisk grunnkurs", "Grunnleggende pedagogisk opplæring for barnehagepersonell.", "Kurs", true),
        ("Barne- og ungdomsarbeiderfag (BUF)", "Fagbrev som barne- og ungdomsarbeider.", "Sertifikat", true),
        ("Spesialpedagogikk", "Videreutdanning i spesialpedagogikk.", "Kurs", true),
        ("Matallergi og intoleranse", "Opplæring i håndtering av matallergier i barnehagen.", "Kurs", true),
        ("Lekbasert læring", "Kurs i lekbasert læring og utvikling.", "Kurs", false),
    ];

    // ======================== Kompetanser ========================
    // (BrukerEpost, KompetanseTypeNavn, UtstedtDagerFraIdag, UtløperDagerFraIdag, SertifikatNummer)
    public static readonly (string UserEmail, string CompetencyTypeName, int IssuedOffsetDays, int? ExpiryOffsetDays, string? CertificateNumber)[] Competencies =
    [
        // Alle ansatte: HMS-kurs
        ($"lise.hansen@{Domain}", "HMS-kurs (årlig)", -180, 180, "HMS-2025-001"),
        ($"ola.nordmann@{Domain}", "HMS-kurs (årlig)", -200, 160, "HMS-2025-002"),
        ($"kari.nordmann@{Domain}", "HMS-kurs (årlig)", -150, 210, "HMS-2025-003"),
        ($"tobias.lie@{Domain}", "HMS-kurs (årlig)", -190, 170, "HMS-2025-004"),
        ($"anne.berg@{Domain}", "HMS-kurs (årlig)", -170, 190, "HMS-2025-005"),
        ($"sofie.sol@{Domain}", "HMS-kurs (årlig)", -160, 200, "HMS-2025-006"),
        ($"lars.sol@{Domain}", "HMS-kurs (årlig)", -140, 220, "HMS-2025-007"),
        ($"ingrid.sol@{Domain}", "HMS-kurs (årlig)", -130, -20, "HMS-2025-008"),
        ($"sara.mane@{Domain}", "HMS-kurs (årlig)", -180, 180, "HMS-2025-009"),
        ($"per.mane@{Domain}", "HMS-kurs (årlig)", -120, 30, "HMS-2025-010"),
        ($"nora.stjerne@{Domain}", "HMS-kurs (årlig)", -170, 190, "HMS-2025-011"),
        ($"bente.gress@{Domain}", "HMS-kurs (årlig)", -165, 195, "HMS-2025-012"),
        ($"hans.gresshoppe@{Domain}", "HMS-kurs (årlig)", -155, 205, "HMS-2025-013"),
        ($"kari.sommerfugl@{Domain}", "HMS-kurs (årlig)", -145, 215, "HMS-2025-014"),
        ($"grete.humle@{Domain}", "HMS-kurs (årlig)", -135, -10, "HMS-2025-015"),

        // Førstehjelp – noen få
        ($"lise.hansen@{Domain}", "Førstehjelp", -200, 120, "FH-2025-001"),
        ($"anne.berg@{Domain}", "Førstehjelp", -180, 140, "FH-2025-002"),
        ($"bente.gress@{Domain}", "Førstehjelp", -170, 150, "FH-2025-003"),
        ($"sofie.sol@{Domain}", "Førstehjelp", -160, 160, "FH-2025-004"),
        ($"sara.mane@{Domain}", "Førstehjelp", -150, 170, "FH-2025-005"),
        ($"nora.stjerne@{Domain}", "Førstehjelp", -140, 180, "FH-2025-006"),
        ($"hans.gresshoppe@{Domain}", "Førstehjelp", -130, 190, "FH-2025-007"),
        ($"kari.sommerfugl@{Domain}", "Førstehjelp", -120, 200, "FH-2025-008"),
        ($"grete.humle@{Domain}", "Førstehjelp", -110, -5, "FH-2025-009"),

        // Pedagogisk grunnkurs – pedagoger
        ($"lars.sol@{Domain}", "Pedagogisk grunnkurs", -730, 365, "PG-2024-001"),
        ($"ingrid.sol@{Domain}", "Pedagogisk grunnkurs", -600, 495, "PG-2024-002"),
        ($"per.mane@{Domain}", "Pedagogisk grunnkurs", -650, 445, "PG-2024-003"),
        ($"emma.mane@{Domain}", "Pedagogisk grunnkurs", -540, 555, "PG-2024-004"),
        ($"erik.stjerne@{Domain}", "Pedagogisk grunnkurs", -700, 395, "PG-2024-005"),
        ($"mia.stjerne@{Domain}", "Pedagogisk grunnkurs", -480, 615, "PG-2024-006"),
        ($"eva.gresshoppe@{Domain}", "Pedagogisk grunnkurs", -620, 475, "PG-2024-007"),
        ($"knut.gresshoppe@{Domain}", "Pedagogisk grunnkurs", -500, 595, "PG-2024-008"),
        ($"ole.sommerfugl@{Domain}", "Pedagogisk grunnkurs", -550, 545, "PG-2024-009"),
        ($"liv.sommerfugl@{Domain}", "Pedagogisk grunnkurs", -400, 695, "PG-2024-010"),
        ($"arne.humle@{Domain}", "Pedagogisk grunnkurs", -450, 645, "PG-2024-011"),
        ($"ruth.humle@{Domain}", "Pedagogisk grunnkurs", -520, 575, "PG-2024-012"),

        // BUF – noen få
        ($"lars.sol@{Domain}", "Barne- og ungdomsarbeiderfag (BUF)", -1095, 365, "BUF-2023-001"),
        ($"per.mane@{Domain}", "Barne- og ungdomsarbeiderfag (BUF)", -900, 560, "BUF-2023-002"),
        ($"erik.stjerne@{Domain}", "Barne- og ungdomsarbeiderfag (BUF)", -800, 660, "BUF-2023-003"),
        ($"eva.gresshoppe@{Domain}", "Barne- og ungdomsarbeiderfag (BUF)", -1100, 360, "BUF-2023-004"),
        ($"ole.sommerfugl@{Domain}", "Barne- og ungdomsarbeiderfag (BUF)", -950, 510, "BUF-2023-005"),
        ($"arne.humle@{Domain}", "Barne- og ungdomsarbeiderfag (BUF)", -1050, 410, "BUF-2023-006"),

        // Spesialpedagogikk – noen
        ($"anne.berg@{Domain}", "Spesialpedagogikk", -400, 700, "SP-2024-001"),
        ($"bente.gress@{Domain}", "Spesialpedagogikk", -300, 800, "SP-2024-002"),
        ($"sofie.sol@{Domain}", "Spesialpedagogikk", -500, 600, "SP-2024-003"),
        ($"sara.mane@{Domain}", "Spesialpedagogikk", -450, 650, "SP-2024-004"),

        // Matallergi
        ($"ingrid.sol@{Domain}", "Matallergi og intoleranse", -90, 270, "MAI-2025-001"),
        ($"emma.mane@{Domain}", "Matallergi og intoleranse", -120, 240, "MAI-2025-002"),
        ($"mia.stjerne@{Domain}", "Matallergi og intoleranse", -60, 300, "MAI-2025-003"),
        ($"knut.gresshoppe@{Domain}", "Matallergi og intoleranse", -150, 210, "MAI-2025-004"),
        ($"liv.sommerfugl@{Domain}", "Matallergi og intoleranse", -80, 280, "MAI-2025-005"),
        ($"ruth.humle@{Domain}", "Matallergi og intoleranse", -110, 250, "MAI-2025-006"),

        // Lekbasert læring (krever ikke utløpsdato)
        ($"erik.sol@{Domain}", "Lekbasert læring", -200, null, "LBL-2025-001"),
        ($"noah.mane@{Domain}", "Lekbasert læring", -180, null, "LBL-2025-002"),
        ($"leo.stjerne@{Domain}", "Lekbasert læring", -220, null, "LBL-2025-003"),
        ($"pia.gresshoppe@{Domain}", "Lekbasert læring", -190, null, "LBL-2025-004"),
        ($"tom.sommerfugl@{Domain}", "Lekbasert læring", -170, null, "LBL-2025-005"),
        ($"finn.humle@{Domain}", "Lekbasert læring", -210, null, "LBL-2025-006"),
    ];

    // ======================== Dokumenttyper ========================
    // (Navn, Slug, Beskrivelse, TargetMode)
    public static readonly (string Name, string Slug, string? Description, DocumentTargetMode TargetMode)[] DocumentTypes =
    [
        ("HMS Dokumenter", "hms-documents",
            "Helse-, miljø- og sikkerhetsdokumenter for barnehagen.",
            DocumentTargetMode.Department),
        ("Stillingsinstrukser", "position-instructions",
            "Arbeidsinstrukser og stillingsbeskrivelser.",
            DocumentTargetMode.JobTitle),
        ("Kursmateriell", "course-materials",
            "Kursmateriell og opplæringsdokumenter.",
            DocumentTargetMode.None),
        ("Onboarding", "onboarding",
            "Dokumenter og sjekklister for nye ansatte.",
            DocumentTargetMode.Department),
    ];

    // ======================== Dokumentkategorier ========================
    // (DocumentTypeSlug, Name, Slug)
    public static readonly (string DocumentTypeSlug, string Name, string Slug)[] DocumentCategories =
    [
        // HMS
        ("hms-documents", "Nødsprosedyrer", "emergency-procedure"),
        ("hms-documents", "Sikkerhetsinstrukser", "safety-instruction"),
        ("hms-documents", "Risikovurderinger", "risk-assessment"),
        ("hms-documents", "Sjekklister", "checklist"),
        ("hms-documents", "Opplæringsmateriell", "training-material"),
        ("hms-documents", "Retningslinjer", "policy"),
        ("hms-documents", "Rapporter", "report"),
        // Stillingsinstrukser
        ("position-instructions", "Stillingsbeskrivelser", "job-description"),
        ("position-instructions", "Arbeidsinstrukser", "work-instructions"),
        ("position-instructions", "Ansvarsområder", "responsibilities"),
        ("position-instructions", "Prosedyrer", "procedures"),
        ("position-instructions", "Kompetansekrav", "competency-requirements"),
        // Onboarding
        ("onboarding", "Generelt", "general"),
        ("onboarding", "IT-oppsett", "it-setup"),
        ("onboarding", "Sikkerhet og HMS", "safety-hms"),
        ("onboarding", "Sjekklister", "checklists"),
    ];

    // ======================== Dokumenter ========================
    // (DocumentTypeSlug, CategorySlug, Title, RequiresSignature, TargetDepartmentName, TargetJobTitleName)
    public static readonly (string DocumentTypeSlug, string? CategorySlug, string Title, bool RequiresSignature, string? TargetDepartmentName, string? TargetJobTitleName)[] Documents =
    [
        // HMS
        ("hms-documents", "emergency-procedure", "Brannvern og evakuering i barnehagen", true, "Ledelse", null),
        ("hms-documents", "safety-instruction", "Lekeplass-sikkerhet", true, "Storebarns avdeling", null),
        ("hms-documents", "risk-assessment", "Risikovurdering småbarnsavdeling", true, "Småbarns avdeling", null),
        ("hms-documents", "checklist", "Daglig HMS-sjekkliste", false, "Storebarns avdeling", null),
        ("hms-documents", "policy", "HMS-policy for Lekestua barnehage", true, "Ledelse", null),
        ("hms-documents", "training-material", "HMS-introduksjon for nye ansatte", false, "Småbarns avdeling", null),

        // Stillingsinstrukser
        ("position-instructions", "job-description", "Daglig leder – stillingsbeskrivelse", false, null, "Daglig leder"),
        ("position-instructions", "job-description", "Gruppeleder – stillingsbeskrivelse", false, null, "Gruppeleder"),
        ("position-instructions", "work-instructions", "Pedagog – arbeidsinstruks", false, null, "Pedagog"),
        ("position-instructions", "responsibilities", "Avdelingsleder – ansvarsområder", false, null, "Avdelingsleder"),
        ("position-instructions", "competency-requirements", "Kompetansekrav for pedagoger", false, null, "Pedagog"),

        // Kursmateriell (ingen målretting)
        ("course-materials", null, "Lekbasert læring – opplæringshefte", false, null, null),
        ("course-materials", null, "Språkstimulering i barnehagen", false, null, null),
        ("course-materials", null, "Naturoppdagelser for barn", false, null, null),

        // Onboarding
        ("onboarding", "general", "Velkomstbrev og oppstartsinfo", true, "Storebarns avdeling", null),
        ("onboarding", "general", "Virksomhetsoversikt Lekestua", true, "Småbarns avdeling", null),
        ("onboarding", "safety-hms", "HMS-introduksjon for nye ansatte", true, "Storebarns avdeling", null),
        ("onboarding", "safety-hms", "Brannvern og evakueringsplan", true, "Småbarns avdeling", null),
        ("onboarding", "checklists", "Sjekkliste – første dag i barnehagen", true, "Storebarns avdeling", null),
        ("onboarding", "checklists", "Sjekkliste – første uke", true, "Småbarns avdeling", null),
    ];

    // ======================== Utstyr ========================
    // (Navn, Beskrivelse)
    public static readonly (string Name, string Description)[] EquipmentCategories =
    [
        ("IT-utstyr", "Datamaskiner, nettbrett og telefoner til arbeidsbruk."),
        ("Barnehageuniform", "Klær med barnehagens logo."),
        ("Inne- og uteklær", "Tøfler, regntøy, ullundertøy og varmejakker."),
        ("Personlig verneutstyr", "Refleksvester, hansker/votter og lue for uteaktiviteter."),
        ("Pedagogisk verktøy", "Materialer for aktiviteter og observasjoner."),
    ];

    // (KategoriNavn, Navn, HarStørrelse)
    public static readonly (string CategoryName, string Name, bool HasSize)[] EquipmentItems =
    [
        ("IT-utstyr", "Laptop", false),
        ("IT-utstyr", "Nettbrett", false),
        ("IT-utstyr", "Mobiltelefon", false),
        ("Barnehageuniform", "T-skjorte", true),
        ("Barnehageuniform", "Bukse", true),
        ("Barnehageuniform", "Jakke", true),
        ("Inne- og uteklær", "Innesko / tøfler", true),
        ("Inne- og uteklær", "Regntøy (jakke + bukse)", true),
        ("Inne- og uteklær", "Ullundertøy", true),
        ("Inne- og uteklær", "Varmjakke / vinterjakke", true),
        ("Personlig verneutstyr", "Refleksvest", false),
        ("Personlig verneutstyr", "Hansker / votter", true),
        ("Personlig verneutstyr", "Lue", true),
        ("Pedagogisk verktøy", "Læremidler- og aktivitetssett", false),
        ("Pedagogisk verktøy", "Observasjonsnotatblokk", false),
    ];

    // (BrukerEpost, ItemNavn, Antall, Størrelse, UtstedtAvEpost)
    public static readonly (string UserEmail, string ItemName, int Quantity, string? Size, string IssuedByEmail)[] EquipmentIssuances =
    [
        // Systemadministratorer
        ($"almin.dev@pm.me", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"gamingnerd824@gmail.com", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"fredrik@magee.no", "Laptop", 1, null, $"almin.dev@pm.me"),

        // Daglig leder
        ($"lise.hansen@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"lise.hansen@{Domain}", "Mobiltelefon", 1, null, $"almin.dev@pm.me"),

        // Rådgivere
        ($"ola.nordmann@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"kari.nordmann@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"tobias.lie@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),

        // Avdelingsledere
        ($"anne.berg@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"anne.berg@{Domain}", "Mobiltelefon", 1, null, $"almin.dev@pm.me"),
        ($"bente.gress@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"bente.gress@{Domain}", "Mobiltelefon", 1, null, $"almin.dev@pm.me"),

        // Gruppeledere
        ($"sofie.sol@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"sofie.sol@{Domain}", "Nettbrett", 1, null, $"almin.dev@pm.me"),
        ($"sara.mane@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"sara.mane@{Domain}", "Nettbrett", 1, null, $"almin.dev@pm.me"),
        ($"nora.stjerne@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"nora.stjerne@{Domain}", "Nettbrett", 1, null, $"almin.dev@pm.me"),
        ($"hans.gresshoppe@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"hans.gresshoppe@{Domain}", "Nettbrett", 1, null, $"almin.dev@pm.me"),
        ($"kari.sommerfugl@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"kari.sommerfugl@{Domain}", "Nettbrett", 1, null, $"almin.dev@pm.me"),
        ($"grete.humle@{Domain}", "Laptop", 1, null, $"almin.dev@pm.me"),
        ($"grete.humle@{Domain}", "Nettbrett", 1, null, $"almin.dev@pm.me"),

        // Eksempel på full utstyrsliste for Lise
        ($"lise.hansen@{Domain}", "T-skjorte", 4, "M", $"almin.dev@pm.me"),
        ($"lise.hansen@{Domain}", "Bukse", 2, "M", $"almin.dev@pm.me"),
        ($"lise.hansen@{Domain}", "Jakke", 1, "M", $"almin.dev@pm.me"),
        ($"lise.hansen@{Domain}", "Innesko / tøfler", 1, "39", $"almin.dev@pm.me"),
        ($"lise.hansen@{Domain}", "Refleksvest", 1, null, $"almin.dev@pm.me"),
        ($"lise.hansen@{Domain}", "Hansker / votter", 1, "M", $"almin.dev@pm.me"),
        ($"lise.hansen@{Domain}", "Lue", 1, "M", $"almin.dev@pm.me"),

        // Eksempel på full utstyrsliste for Anne
        ($"anne.berg@{Domain}", "T-skjorte", 4, "L", $"almin.dev@pm.me"),
        ($"anne.berg@{Domain}", "Bukse", 2, "L", $"almin.dev@pm.me"),
        ($"anne.berg@{Domain}", "Jakke", 1, "L", $"almin.dev@pm.me"),
        ($"anne.berg@{Domain}", "Innesko / tøfler", 1, "38", $"almin.dev@pm.me"),
        ($"anne.berg@{Domain}", "Regntøy (jakke + bukse)", 1, "L", $"almin.dev@pm.me"),
        ($"anne.berg@{Domain}", "Refleksvest", 1, null, $"almin.dev@pm.me"),

        // Eksempel på full utstyrsliste for Sofie (gruppeleder)
        ($"sofie.sol@{Domain}", "T-skjorte", 4, "S", $"almin.dev@pm.me"),
        ($"sofie.sol@{Domain}", "Bukse", 2, "S", $"almin.dev@pm.me"),
        ($"sofie.sol@{Domain}", "Jakke", 1, "S", $"almin.dev@pm.me"),
        ($"sofie.sol@{Domain}", "Innesko / tøfler", 1, "37", $"almin.dev@pm.me"),
        ($"sofie.sol@{Domain}", "Regntøy (jakke + bukse)", 1, "S", $"almin.dev@pm.me"),
        ($"sofie.sol@{Domain}", "Varmjakke / vinterjakke", 1, "S", $"almin.dev@pm.me"),

        // Eksempelpedagog: Lars
        ($"lars.sol@{Domain}", "T-skjorte", 4, "L", $"almin.dev@pm.me"),
        ($"lars.sol@{Domain}", "Bukse", 2, "L", $"almin.dev@pm.me"),
        ($"lars.sol@{Domain}", "Jakke", 1, "L", $"almin.dev@pm.me"),
        ($"lars.sol@{Domain}", "Innesko / tøfler", 1, "43", $"almin.dev@pm.me"),
        ($"lars.sol@{Domain}", "Hansker / votter", 1, "L", $"almin.dev@pm.me"),
        ($"lars.sol@{Domain}", "Lue", 1, "L", $"almin.dev@pm.me"),
        ($"lars.sol@{Domain}", "Observasjonsnotatblokk", 2, null, $"almin.dev@pm.me"),

        // Eksempelassistent: Erik
        ($"erik.sol@{Domain}", "T-skjorte", 4, "L", $"almin.dev@pm.me"),
        ($"erik.sol@{Domain}", "Bukse", 2, "L", $"almin.dev@pm.me"),
        ($"erik.sol@{Domain}", "Jakke", 1, "L", $"almin.dev@pm.me"),
        ($"erik.sol@{Domain}", "Ullundertøy", 2, "L", $"almin.dev@pm.me"),
        ($"erik.sol@{Domain}", "Hansker / votter", 1, "L", $"almin.dev@pm.me"),
    ];

    // ======================== Dokumentsignaturer ========================
    // (DokumentTittel, BrukerEpost, SignertDato)
    // Hvis et dokument krever signatur og en bruker IKKE er i denne listen,
    // vises det som "Venter på signatur".
    public static readonly (string DocumentTitle, string UserEmail, DateTime SignedAt)[] DocumentSignatures =
    [
        // Lise (daglig leder)
        ("Brannvern og evakuering i barnehagen", $"lise.hansen@{Domain}", new DateTime(2025, 11, 20, 14, 30, 0, DateTimeKind.Utc)),
        ("HMS-policy for Lekestua barnehage", $"lise.hansen@{Domain}", new DateTime(2025, 10, 5, 9, 15, 0, DateTimeKind.Utc)),

        // Anne (avdelingsleder Storebarn)
        ("Risikovurdering småbarnsavdeling", $"anne.berg@{Domain}", new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc)),
        ("HMS-introduksjon for nye ansatte", $"anne.berg@{Domain}", new DateTime(2026, 1, 15, 11, 45, 0, DateTimeKind.Utc)),

        // Bente (avdelingsleder Småbarn)
        ("Brannvern og evakueringsplan", $"bente.gress@{Domain}", new DateTime(2025, 8, 12, 13, 0, 0, DateTimeKind.Utc)),
        ("Sjekkliste – første uke", $"bente.gress@{Domain}", new DateTime(2025, 9, 3, 15, 30, 0, DateTimeKind.Utc)),

        // Sofie (gruppeleder Sol)
        ("Daglig HMS-sjekkliste", $"sofie.sol@{Domain}", new DateTime(2025, 11, 4, 8, 0, 0, DateTimeKind.Utc)),
        ("Velkomstbrev og oppstartsinfo", $"sofie.sol@{Domain}", new DateTime(2025, 10, 18, 9, 30, 0, DateTimeKind.Utc)),

        // Sara (gruppeleder Måne)
        ("Sjekkliste – første dag i barnehagen", $"sara.mane@{Domain}", new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc)),

        // Hans (gruppeleder Gresshoppe)
        ("Risikovurdering småbarnsavdeling", $"hans.gresshoppe@{Domain}", new DateTime(2025, 12, 10, 8, 30, 0, DateTimeKind.Utc)),

        // Lars (pedagog Sol) – noen signert, noen ikke
        ("Lekeplass-sikkerhet", $"lars.sol@{Domain}", new DateTime(2025, 7, 22, 11, 15, 0, DateTimeKind.Utc)),
        ("Daglig HMS-sjekkliste", $"lars.sol@{Domain}", new DateTime(2025, 11, 4, 8, 15, 0, DateTimeKind.Utc)),

        // Per (pedagog Måne)
        ("Daglig HMS-sjekkliste", $"per.mane@{Domain}", new DateTime(2025, 11, 4, 8, 30, 0, DateTimeKind.Utc)),

        // Knut (pedagog Gresshoppe)
        ("Daglig HMS-sjekkliste", $"knut.gresshoppe@{Domain}", new DateTime(2025, 11, 5, 9, 0, 0, DateTimeKind.Utc)),

        // Erik (assistent Sol) – IKKE signert: "Brannvern og evakueringsplan" (skal vises som VENTER)
        // Ingen signaturer for å demonstrere pending status

        // Ole (pedagog Sommerfugl)
        ("HMS-introduksjon for nye ansatte", $"ole.sommerfugl@{Domain}", new DateTime(2025, 8, 30, 14, 0, 0, DateTimeKind.Utc)),

        // Arne (pedagog Humle)
        ("HMS-introduksjon for nye ansatte", $"arne.humle@{Domain}", new DateTime(2025, 8, 31, 13, 45, 0, DateTimeKind.Utc)),
    ];
}