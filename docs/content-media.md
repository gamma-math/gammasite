# Indholdsbilleder

GamMaSite bruger den eksisterende `ContentItem.PictureUrl`-kolonne til både eksterne billed-URL'er og uploadede billeder. Uploadede billeder ligger uden for den publicerede applikationsmappe og vises via `/media/...`.

## Mapper

Konfigurationen i `appsettings.json` deklarerer allerede `ContentMedia:RootPath = ..\\gammasite-media`. TEST og PROD må gerne dele denne ene permanente mediemappe, fordi begge deploymentmapper ligger som søstermapper i Simply.

```text
<Simply-kontorod>\\gammasite-media\\content\\events
<Simply-kontorod>\\gammasite-media\\content\\news
```

I Simplys Fil Manager, hvor roden viser mapperne `public_html` og `test`, skal du oprette denne struktur direkte i roden `/`:

```text
/
├── gammasite-media/
│   └── content/
│       ├── events/
│       └── news/
├── public_html/   # PROD APP_PATH
└── test/          # TEST APP_PATH
```

Opret altså ikke mappen inde i `test` eller `public_html`; så ville den kunne blive påvirket af en deployment. Der skal ikke sættes en ekstra miljøvariabel i Simply, så længe TEST og PROD bruger denne fælles struktur:

```text
TEST APP_PATH: /test          -> ../gammasite-media
PROD APP_PATH: /public_html  -> ../gammasite-media
```

`GAMMASITE_MEDIA_ROOT` er kun nødvendig, hvis mediemappen senere skal ligge et andet sted end denne fælles søstermappe.

IIS-applikationspoolens identitet skal have **Read**, **Write** og **Modify** på `gammasite-media` og undermapperne. Backend opretter ikke mediemappen ved upload; hvis den mangler eller ikke kan skrives til, returneres en tydelig fejl.

En Simply-miljøvariabel `GAMMASITE_MEDIA_ROOT` kan bruges til at overstyre den relative standardsti med en absolut fysisk sti. Det er den anbefalede løsning, hvis TEST og PROD har forskellige fysiske rodmapper, der ikke kan udtrykkes ens relativt til `APP_PATH`.

## Backup og deployment

GitHub Actions deployer via MSDeploy og laver en MSDeploy-applikationsbackup, men en søstermappe uden for `APP_PATH` er ikke dækket af denne backup. Derfor skal `gammasite-media` inkluderes eksplicit i Simplys filbackup eller den eksisterende Cloudflare/backup-rutine. En gendannelse skal lægge mappen tilbage samme sted med samme IIS-rettigheder, før applikationen startes.

Deployment må ikke slette eller erstatte `gammasite-media`. Uploadede filer bruger sikre, unikke navne baseret på slug og GUID. Sletning kontrollerer, at URL'en er en lokal `/media/...`-URL, og at filen ikke bruges af andet indhold; eksterne URL'er røres aldrig.

Lokalt kan samme mappe oprettes relativt til backendens content root, eller `GAMMASITE_MEDIA_ROOT` kan sættes til en midlertidig testmappe.
