const root = document.body.dataset.root || "";
const active = document.body.dataset.page || "";

const links = [
  ["Forside", "index.html", "home"],
  ["QA-vejledninger", "qa/index.html", "qa"],
  ["Events og nyheder", "qa/events-news.html", "qa.events"],
  ["Tilmeldinger", "qa/tilmeldinger.html", "qa.registrations"],
  ["Beskeder", "qa/beskeder.html", "qa.messages"],
  ["Beskedskabeloner", "qa/skabeloner.html", "qa.templates"],
  ["Brugerstatus", "qa/brugerstatus.html", "qa.status"],
  ["Roller og rettigheder", "qa/rettigheder.html", "qa.permissions"],
  ["Kompromitterede data", "qa/kompromitterede-data.html", "code.incident"],
  ["Finans", "finans/index.html", "finance"],
  ["Posteringer", "finans/posteringer.html", "finance.postings"],
  ["Posteringsgrupper", "finans/posteringsgrupper.html", "finance.groups"],
  ["Eksport", "finans/eksport.html", "finance.export"],
  ["CSV-import", "finans/import.html", "finance.import"],
  ["Budgetter", "finans/budgetter.html", "finance.budgets"],
  ["Systemoversigt", "systemer/index.html", "systems"],
  ["Lokal opsætning", "kodebase/lokal-opsætning.html", "code.setup"],
  ["Deploy Hjemmesiden", "kodebase/pipeline.html", "code.pipeline"],
  ["GitHub Pages", "kodebase/github-pages.html", "code.pages"],
  ["Kodestruktur", "kodebase/struktur.html", "code.structure"],
  [".github/", "kodebase/mappe-github.html", "code.github"],
  [".vscode/", "kodebase/mappe-vscode.html", "code.vscode"],
  ["docs/", "kodebase/mappe-docs.html", "code.docs"],
  ["SQL/", "kodebase/mappe-sql.html", "code.sql"],
  ["src/backend/GamMaSite/", "kodebase/mappe-backend.html", "code.backend"],
  ["src/frontend/", "kodebase/mappe-frontend.html", "code.frontend"],
  ["src/test/GamMaSite.Tests/", "kodebase/mappe-backend-tests.html", "code.backend-tests"],
  ["src/test/frontend/", "kodebase/mappe-frontend-tests.html", "code.frontend-tests"]
];

const groups = [
  ["QA-vejledninger", ["qa", "qa.events", "qa.registrations", "qa.messages", "qa.templates", "qa.status", "qa.permissions"]],
  ["Finans", ["finance", "finance.postings", "finance.groups", "finance.export", "finance.import", "finance.budgets"]],
  ["Teknik", ["systems", "code.setup", "code.pipeline", "code.pages", "code.incident"]],
  ["Kodestruktur", ["code.structure", "code.github", "code.vscode", "code.docs", "code.sql", "code.backend", "code.frontend", "code.backend-tests", "code.frontend-tests"]]
];

const byId = new Map(links.map((link) => [link[2], link]));
const layout = document.querySelector(".docs-layout");
const isActive = (id) => active === id || active.startsWith(`${id}.`);

function renderLink(id) {
  const [label, href] = byId.get(id);
  return `<a data-sub ${isActive(id) ? 'aria-current="page"' : ""} href="${root}${href}">${label}</a>`;
}

document.querySelector("#site-header").innerHTML = `<div class="site-header__inner"><a class="brand" href="${root}index.html">GamMa<span>Site</span> · Dokumentation</a></div>`;
function renderGroup(label, ids) {
  const open = ids.some(isActive);
  const [, overviewHref, overviewId] = byId.get(ids[0]);
  return `<details class="sidebar-group" ${open ? "open" : ""}><summary><a ${active === overviewId ? 'aria-current="page"' : ""} href="${root}${overviewHref}">${label}</a></summary>${ids.slice(1).map(renderLink).join("")}</details>`;
}

document.querySelector("#sidebar").innerHTML = `<div class="sidebar__header"><h2>Indhold</h2><button class="sidebar-toggle" type="button" aria-expanded="true" aria-label="Skjul sidebjælke" title="Skjul sidebjælke">‹</button></div><nav><a class="sidebar-home" ${active === "home" ? 'aria-current="page"' : ""} href="${root}index.html">Forside</a>${groups.map(([label, ids]) => renderGroup(label, ids)).join("")}</nav>`;

const toggle = document.querySelector(".sidebar-toggle");
toggle.addEventListener("click", () => {
  const collapsed = layout.classList.toggle("docs-layout--sidebar-collapsed");
  toggle.setAttribute("aria-expanded", String(!collapsed));
  toggle.setAttribute("aria-label", collapsed ? "Vis sidebjælke" : "Skjul sidebjælke");
  toggle.setAttribute("title", collapsed ? "Vis sidebjælke" : "Skjul sidebjælke");
  toggle.textContent = collapsed ? "›" : "‹";
});

document.querySelectorAll("[data-file-search-input]").forEach((input) => {
  const documentation = input.closest(".file-documentation");
  const rows = [...documentation.querySelectorAll("[data-file-row]")];
  const empty = documentation.querySelector("[data-file-search-empty]");
  input.addEventListener("input", () => {
    const query = input.value.trim().toLocaleLowerCase("da-DK");
    let visibleRows = 0;
    rows.forEach((row) => {
      const matches = !query || row.dataset.fileSearch.includes(query);
      row.hidden = !matches;
      if (matches) visibleRows += 1;
    });
    empty.hidden = visibleRows > 0;
  });
});

