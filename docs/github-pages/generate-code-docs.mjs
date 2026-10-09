import { cp, readFile, rm, writeFile } from "node:fs/promises";
import { execFileSync } from "node:child_process";
import { join, relative } from "node:path";

const argumentsAfterNode = process.argv.slice(2);
const inPlace = argumentsAfterNode.includes("--in-place");
const outputDirectory = argumentsAfterNode.find((argument) => !argument.startsWith("--")) ?? "_site";
const sourceDirectory = "docs/github-pages/site";

const documentSets = [
  { key: "backend", sourceRoot: "src/backend/GamMaSite/", mapPage: "html/kodebase/mappe-backend.html" },
  { key: "frontend", sourceRoot: "src/frontend/", mapPage: "html/kodebase/mappe-frontend.html" },
  { key: "backend-tests", sourceRoot: "src/test/GamMaSite.Tests/", mapPage: "html/kodebase/mappe-backend-tests.html" },
  { key: "frontend-tests", sourceRoot: "src/test/frontend/", mapPage: "html/kodebase/mappe-frontend-tests.html" }
];

const excludedPath = /\/(?:bin|obj|node_modules|wwwroot\/(?:lib|react-assets|js\/thirdparty))\//;
const excludedName = /(?:README\.md|appsettings(?:\.[^.]+)?\.json|serviceDependencies(?:\.local)?\.json|\.pubxml|favicon\.ico|\.env(?:\..*)?)$/i;

function escapeHtml(value) {
  return value.replace(/[&<>'"]/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" })[character]);
}

function readableName(name) {
  return name
    .replace(/\.(?:cshtml\.cs|cshtml|jsx|js|cs)$/i, "")
    .replace(/([a-zæøå])([A-ZÆØÅ])/g, "$1 $2")
    .replace(/([A-ZÆØÅ]+)([A-ZÆØÅ][a-zæøå])/g, "$1 $2")
    .replace(/[_-]/g, " ")
    .trim();
}

const backendDescriptions = {
  "Controllers/ApiAccountController.cs": "API for loginstatus, konto og profiloplysninger i React-klienten.",
  "Controllers/ApiCalendarController.cs": "API for kalenderdata, som vises i den offentlige og interne kalender.",
  "Controllers/ApiContentController.cs": "API for events, nyheder, indholdselementer og tilmeldinger.",
  "Controllers/ApiEditorController.cs": "API for editorrelaterede data og indholdsredigering.",
  "Controllers/ApiEmailTemplatesController.cs": "API for administration og forhåndsvisning af e-mailskabeloner.",
  "Controllers/ApiFinanceController.cs": "API for finansoversigter, posteringer, budgetter, import og eksport.",
  "Controllers/ApiLibraryController.cs": "API for bibliotekets dokumenter og materialer.",
  "Controllers/ApiMeController.cs": "API, der leverer den aktuelle brugers loginstatus, roller og rettigheder.",
  "Controllers/ApiMembersController.cs": "API for medlemslister og medlemsdata i administrationsflader.",
  "Controllers/ApiMessagesController.cs": "API for oprettelse og afsendelse af medlemsbeskeder.",
  "Controllers/ApiPaymentsController.cs": "API for betalinger, checkout og betalingsrelaterede data.",
  "Controllers/ApiRolesController.cs": "API for roller, rollesammensætning og tildelte rettigheder.",
  "Data/ApplicationDbContext.cs": "Entity Framework-databasekontekst for Identity, indhold, roller, rettigheder og øvrige applikationsdata.",
  "Data/DanishIdentityErrorDescriber.cs": "Danske validerings- og fejltekster fra ASP.NET Core Identity.",
  "Data/ValueConversionExtensions.cs": "Fælles konverteringer mellem lagrede værdier og applikationens datatyper.",
  "Models/ContentConstants.cs": "Fælles konstanter for indholdstyper, statusser og relaterede indholdsregler.",
  "Models/ContentItem.cs": "Datamodel for et publicerbart indholdselement, fx en event eller nyhed.",
  "Models/ContentLink.cs": "Datamodel for et link, der er knyttet til et indholdselement.",
  "Models/EmailTemplate.cs": "Datamodel for en genbrugelig e-mailskabelon med emne og indhold.",
  "Models/ErrorViewModel.cs": "Viewmodel med fejloplysninger til serverrenderede fejlsider.",
  "Models/EventRegistration.cs": "Datamodel for et medlems tilmelding og rolle på en event.",
  "Models/MessageMedia.cs": "Datamodel for medier eller vedhæftninger, der knyttes til beskeder.",
  "Models/Permission.cs": "Datamodel for en navngiven systemrettighed, der kan tildeles roller.",
  "Models/RoleEdit.cs": "Model for de redigerbare oplysninger på en rolle.",
  "Models/RoleModification.cs": "Model for tilføjelser og fjernelser af medlemmer på en rolle.",
  "Models/RolePermission.cs": "Koblingsmodel mellem en rolle og en systemrettighed.",
  "Models/SiteUser.cs": "Applikationens udvidede Identity-bruger med medlems- og profiloplysninger.",
  "Models/UserCategories.cs": "Samling af brugerstatusser og roller til ældre administrationsflows.",
  "Models/UserStatus.cs": "Definition af de statusværdier, et medlem kan have.",
  "Models/VisibilityStatus.cs": "Definition af synlighedsstatus for indhold og visninger.",
  "Services/AccessControlService.cs": "Kontrollerer brugerrettigheder, roller og adgang til beskyttede funktioner.",
  "Services/ContentService.cs": "Håndterer oprettelse, publicering og hentning af events, nyheder og links.",
  "Services/EmailService.cs": "Sender e-mails via den konfigurerede e-mailintegration.",
  "Services/EmailTemplateService.cs": "Håndterer lagring, rendering og forhåndsvisning af e-mailskabeloner.",
  "Services/EventRegistrationService.cs": "Håndterer tilmelding, afmelding og deltagerroller på events.",
  "Services/FinanceImportService.cs": "Validerer og importerer bank- og MobilePay-filer til finansdata.",
  "Services/FinanceReportService.cs": "Udarbejder finansoversigter, posteringer, budgetter og kontoplan-data.",
  "Services/GithubService.cs": "Integration til GitHub-relaterede funktioner i applikationen.",
  "Services/GitlabService.cs": "Integration til GitLab-relaterede funktioner i applikationen.",
  "Services/GoogleCalendarService.cs": "Integration til synkronisering eller hentning af Google Calendar-data.",
  "Services/SmsSender.cs": "Afsender SMS-beskeder gennem den konfigurerede SMS-tjeneste.",
  "Services/StripeService.cs": "Opretter og håndterer Stripe-betalinger og checkout-sessioner.",
  "Services/SystemEmailTemplateService.cs": "Leverer systemets standardiserede e-mailskabeloner.",
  "ViewModels/Api/AccountDtos.cs": "API-kontrakter for konto, login og profiloplysninger.",
  "ViewModels/Api/ApiDtoMapper.cs": "Mapper, der omsætter datamodeller til API'ets DTO-kontrakter.",
  "ViewModels/Api/ContentDtos.cs": "API-kontrakter for events, nyheder, links og indholdsredigering.",
  "ViewModels/Api/EmailTemplateDtos.cs": "API-kontrakter for e-mailskabeloner og forhåndsvisning.",
  "ViewModels/Api/EventRegistrationDtos.cs": "API-kontrakter for eventtilmeldinger og deltageroplysninger.",
  "ViewModels/Api/MembershipDtos.cs": "API-kontrakter for medlemmer, roller og rettigheder.",
  "ViewModels/Api/SiteFeatureDtos.cs": "API-kontrakter for konfigurerbare funktioner i sitet.",
  "ViewModels/ProductInfo.cs": "Viewmodel med produktinformation til betalingsflowet."
};

function backendPurpose(path) {
  const name = path.split("/").at(-1);
  const localPath = path.replace("src/backend/GamMaSite/", "");
  if (backendDescriptions[localPath]) return backendDescriptions[localPath];
  if (path.endsWith("/Program.cs")) return "Applikationens samlingspunkt for hosting, afhængigheder, autorisation og middleware.";
  if (path.endsWith(".csproj")) return "Projektdefinitionen for backendens build, framework og NuGet-afhængigheder.";
  if (path.includes("/Controllers/")) return name.startsWith("Api") ? `API-controller for ${readableName(name.replace(/^Api/, ""))}.` : `MVC-controller for ${readableName(name.replace(/Controller\.cs$/, ""))}-flowet.`;
  if (path.includes("/Services/")) return name.startsWith("I") ? "Interface, som definerer kontrakten for en applikationsservice eller integration." : "Service, som samler domænelogik eller en ekstern integration.";
  if (path.includes("/Data/")) return "Dataadgang eller databasekonfiguration for applikationen.";
  if (path.includes("/Models/")) return `Datamodel for ${readableName(name)} i applikationens domæne.`;
  if (path.includes("/ViewModels/")) return `DTO eller viewmodel for ${readableName(name)} mellem backend og brugergrænseflade.`;
  if (path.includes("/Areas/Identity/Pages/")) return name.endsWith(".cshtml.cs") ? `PageModel med serverlogik til Identity-flowet “${readableName(name)}”.` : `Razor-side til Identity-flowet “${readableName(name)}”.`;
  if (path.includes("/Views/")) return `Razor-view til ${localPath.replace(/^Views\//, "").replace(/\.cshtml$/, "")}.`;
  if (path.includes("/Configuration/")) return "Lokal hjælpekonfiguration til applikationens runtime-miljø.";
  if (path.includes("/Properties/")) return "Projekt- eller hostingsmetadata. Ingen lokale værdier dokumenteres her.";
  if (path.includes("/wwwroot/css/")) return "Førsteparts CSS, der understøtter serverede frontend-flows.";
  if (path.includes("/wwwroot/js/")) return "Førsteparts JavaScript til serverede frontend-flows.";
  if (name.endsWith(".md") || name.endsWith(".txt")) return "Supplerende projekt- eller scaffoldingnotat.";
  return "Førstepartsfil i ASP.NET Core-applikationen.";
}

function frontendPurpose(path) {
  const name = path.split("/").at(-1);
  const localPath = path.replace("src/frontend/", "");
  const frontendDescriptions = {
    "components/ConfirmationDialog.jsx": "Genbrugelig dialog til bekræftelse af handlinger, før ændringer udføres.",
    "components/ContentCard.jsx": "Genbrugelig kortvisning af et event, en nyhed eller andet indhold.",
    "components/RichTextEditor.jsx": "Rich-text-editor til redigering af formateret indhold i administrationen.",
    "layouts/AdminLayout.jsx": "Fælles ramme for administrationssider med navigationsmenu og rettighedsfiltrering.",
    "layouts/MenuLayout.jsx": "Fælles layout for menurelaterede visninger i React-klienten.",
    "pages/AdminFinancePage.jsx": "Administrationssider for finansoversigt, posteringer, budgetter, kontoplan og import.",
    "pages/AdminContentPage.jsx": "Administrationssider for oprettelse og redigering af events og nyheder.",
    "pages/AdminMembersPage.jsx": "Administrationsside for medlemsoversigt og medlemsoplysninger.",
    "pages/AdminMessagesPage.jsx": "Administrationsside for beskeder til medlemmer.",
    "pages/AdminRolesPage.jsx": "Administrationssider for roller og systemrettigheder.",
    "pages/AdminTemplatesPage.jsx": "Administrationssider for e-mail- og beskedskabeloner.",
    "pages/AccountPages.jsx": "Sider til login, registrering, adgangskode og kontoindstillinger.",
    "pages/CalendarPage.jsx": "Kalendervisning for arrangementer og relevante datoer.",
    "pages/ContentDetailPage.jsx": "Detaljevisning for et enkelt event eller en nyhed.",
    "pages/EventRegistrationsPage.jsx": "Administration af deltagere og tilmeldinger for en event.",
    "pages/FinancePage.jsx": "Medlemmets personlige finansoversigt og egne posteringer.",
    "pages/FrontPage.jsx": "Forside og lister over offentlige events og nyheder.",
    "pages/LibraryPage.jsx": "Visning af bibliotekets materialer og dokumenter.",
    "pages/MembersPage.jsx": "Medlemskatalog og søgning efter medlemmer.",
    "pages/PaymentPage.jsx": "Betalings- og checkoutflows, herunder status efter betaling.",
    "pages/StaticPage.jsx": "Statiske informationssider som om-siden, betingelser og cookies.",
    "routes/navigation.jsx": "Klientside-navigation og linkhåndtering for React-ruter.",
    "services/api.js": "Fælles API-klient til konto, indhold, medlemmer og øvrige JSON-kald.",
    "services/financeApi.js": "API-klient for finansoversigter, posteringer, budgetter og import.",
    "utils/access.js": "Definitioner og hjælpefunktioner til at kontrollere brugerrettigheder.",
    "utils/avatar.js": "Hjælpefunktioner til profilbilleder eller avatarvisning.",
    "utils/financePostingDates.js": "Hjælpefunktioner til at fastlægge posteringers relevante dato.",
    "utils/format.js": "Fælles formattering af værdier, datoer og tekst i klienten.",
    "utils/registrationOpen.js": "Regler for, om eventtilmelding fortsat er åben.",
    "utils/registrationSort.js": "Sortering af eventtilmeldinger til administrationsvisninger.",
    "utils/richText.js": "Hjælpefunktioner til behandling og visning af rich-text-indhold.",
    "styles/app.css": "Overordnet styling for React-klienten, navigation og fælles komponenter.",
    "styles/finance.css": "Styling for finansoversigter, tabeller og finansadministration."
  };
  if (frontendDescriptions[localPath]) return frontendDescriptions[localPath];
  if (name === "package.json") return "Projektmanifest for frontendens scripts og afhængigheder.";
  if (name === "package-lock.json") return "Låst versionstræ for frontendens npm-afhængigheder.";
  if (name === "vite.config.js") return "Vite-konfiguration til udvikling og produktionsbuild.";
  if (name === "index.html") return "HTML-indgangspunktet, som Vite bruger til at indlæse React-applikationen.";
  if (name === "README.md") return "Kort dokumentation for frontendprojektet.";
  if (path.includes("/app/")) return "Klientens opstart og samling af de overordnede React-afhængigheder.";
  if (path.includes("/components/")) return `Genbrugelig React-komponent til ${readableName(name)}.`;
  if (path.includes("/layouts/")) return `Fælles layout for ${readableName(name)}.`;
  if (path.includes("/pages/")) return `React-side for ${readableName(name)}.`;
  if (path.includes("/routes/")) return "Navigation og ruteopsætning for React-klienten.";
  if (path.includes("/services/")) return "Klientservice, som afgrænser kald til backendens API.";
  if (path.includes("/styles/")) return "Førsteparts styling for React-klientens visuelle udtryk.";
  if (path.includes("/utils/")) return `Hjælpefunktioner til ${readableName(name)}.`;
  return "Førstepartsfil i React/Vite-klienten.";
}

function testPurpose(path, set) {
  const name = path.split("/").at(-1);
  if (name.endsWith(".csproj")) return "Projektdefinitionen for backendens automatiske testpakke.";
  if (name === "README.md") return "Kort dokumentation for testprojektet.";
  if (name === "TestDoubles.cs") return "Testdobbelt-typer, som isolerer tests fra eksterne afhængigheder.";
  if (name === "FinanceTestHelpers.cs") return "Fælles testhjælpere til finansrelaterede scenarier.";
  if (name.endsWith(".test.js")) return `Automatiseret frontendtest af ${readableName(name.replace(/\.test\.js$/, ""))}.`;
  if (name.endsWith("Tests.cs")) return `Automatiseret testklasse for ${readableName(name.replace(/Tests\.cs$/, ""))}.`;
  return set.key === "frontend-tests" ? "Førstepartsfil i frontendens testpakke." : "Førstepartsfil i backendens testpakke.";
}

function purposeFor(path, set) {
  if (set.key === "backend") return backendPurpose(path);
  if (set.key === "frontend") return frontendPurpose(path);
  return testPurpose(path, set);
}

if (inPlace && outputDirectory !== sourceDirectory) {
  throw new Error("--in-place må kun bruges med docs/github-pages/site som outputmappe.");
}

if (!inPlace) {
  await rm(outputDirectory, { recursive: true, force: true });
  await cp(sourceDirectory, outputDirectory, { recursive: true });
  await writeFile(join(outputDirectory, "index.html"), "<!doctype html><html lang=\"da\"><meta charset=\"utf-8\"><meta http-equiv=\"refresh\" content=\"0; url=html/\"><title>GamMaSite dokumentation</title><a href=\"html/\">Åbn dokumentationen</a></html>", "utf8");
}

await rm(join(outputDirectory, "html", "kodebase", "filer"), { recursive: true, force: true });

for (const set of documentSets) {
  const trackedFiles = execFileSync("git", ["ls-files", set.sourceRoot], { encoding: "utf8" })
    .split(/\r?\n/)
    .filter(Boolean)
    .filter((path) => !excludedPath.test(path) && !excludedName.test(path));
  const listItems = trackedFiles.map((path) => {
    const localPath = path.replace(set.sourceRoot, "");
    const purpose = purposeFor(path, set);
    const searchableText = `${localPath} ${purpose}`.toLocaleLowerCase("da-DK");
    return `<tr data-file-row data-file-search="${escapeHtml(searchableText)}"><td><code>${escapeHtml(localPath)}</code></td><td>${escapeHtml(purpose)}</td></tr>`;
  }).join("");
  const listMarkup = `<!-- FILE_LIST_START --><section class="section file-documentation"><p class="file-documentation__intro">Filerne nedenfor er projektets egne, versionsstyrede filer. Tredjepart, build-output og lokale konfigurationsfiler er udeladt.</p><label class="file-search"><span>Søg filer</span><input type="search" data-file-search-input placeholder="Filnavn eller formål" autocomplete="off"></label><div class="file-table-wrap"><table class="table file-table"><thead><tr><th>Fil</th><th>Formål</th></tr></thead><tbody>${listItems}</tbody></table></div><p class="file-search-empty" data-file-search-empty hidden>Ingen filer matcher søgningen.</p></section><!-- FILE_LIST_END -->`;
  const mapPath = join(outputDirectory, set.mapPage);
  const mapPage = await readFile(mapPath, "utf8");
  const marker = /<!-- FILE_LIST_START -->[\s\S]*?<!-- FILE_LIST_END -->/;
  if (!marker.test(mapPage)) {
    throw new Error(`Mangler filoversigtsmarkør i ${set.mapPage}.`);
  }
  const updatedMapPage = mapPage.replace(marker, listMarkup);
  await writeFile(mapPath, updatedMapPage, "utf8");
  console.log(`Embedded ${trackedFiles.length} ${set.key} files in ${relative(process.cwd(), mapPath)}.`);
}
