document.addEventListener("DOMContentLoaded", () => {
  const toast = document.querySelector("[data-finance-toast]");

  const showToast = (message) => {
    if (!toast) return;
    toast.textContent = message;
    toast.classList.add("is-visible");
    window.clearTimeout(window.financeToastTimer);
    window.financeToastTimer = window.setTimeout(() => toast.classList.remove("is-visible"), 2600);
  };

  const adminFinancePages = new Set([
    "finans-admin.html",
    "finans-posteringer.html",
    "finans-postering-detalje.html",
    "finans-import.html",
    "finans-kontoplan.html",
    "finans-posteringsgrupper.html",
    "finans-budgetter.html"
  ]);
  const currentFinancePage = window.location.pathname.split("/").pop();
  const adminFinanceNav = document.querySelector(".finance-nav");
  if (adminFinanceNav && adminFinancePages.has(currentFinancePage)) {
    const activePage = currentFinancePage === "finans-postering-detalje.html" ? "finans-posteringer.html" : currentFinancePage;
    adminFinanceNav.innerHTML = `
      <a class="${activePage === "finans-admin.html" ? "is-active" : ""}" href="finans-admin.html">Overblik</a>
      <a class="${activePage === "finans-posteringer.html" ? "is-active" : ""}" href="finans-posteringer.html">Posteringer</a>
      <a class="${activePage === "finans-import.html" ? "is-active" : ""}" href="finans-import.html">CSV-import</a>
      <span class="nav-divider"></span>
      <span class="nav-label">Stamdata</span>
      <a class="${activePage === "finans-kontoplan.html" ? "is-active" : ""}" href="finans-kontoplan.html">Kontoplan</a>
      <a class="${activePage === "finans-posteringsgrupper.html" ? "is-active" : ""}" href="finans-posteringsgrupper.html">Posteringsgrupper</a>
      <a class="${activePage === "finans-budgetter.html" ? "is-active" : ""}" href="finans-budgetter.html">Budgetter</a>`;
  }

  const adminHeroDescriptions = {
    "finans-admin.html": "Samlet økonomisk overblik",
    "finans-posteringer.html": "Se og redigér finansposteringer",
    "finans-postering-detalje.html": "Se detaljer om en finanspostering",
    "finans-import.html": "Upload finansdata",
    "finans-kontoplan.html": "Se kontoplan",
    "finans-posteringsgrupper.html": "Administrér posteringsgrupper",
    "finans-budgetter.html": "Administrér budgetter"
  };
  if (adminFinancePages.has(currentFinancePage)) {
    const heroCopy = document.querySelector(".finance-hero > div:first-child");
    const eyebrow = heroCopy?.querySelector(".finance-eyebrow");
    const title = heroCopy?.querySelector("h1");
    if (eyebrow) eyebrow.textContent = "Finans · admin";
    if (title) title.remove();
    let description = heroCopy ? [...heroCopy.children].find((element) => element.tagName === "P" && !element.classList.contains("finance-eyebrow")) : null;
    if (!description && heroCopy) {
      description = document.createElement("p");
      heroCopy.append(description);
    }
    if (description) description.textContent = adminHeroDescriptions[currentFinancePage];
  }

  const stamdataPages = new Set(["finans-kontoplan.html", "finans-posteringsgrupper.html", "finans-budgetter.html"]);
  if (stamdataPages.has(currentFinancePage)) {
    document.querySelectorAll(".finance-editor-panel .finance-badge.dark, .finance-editor-panel .finance-note").forEach((element) => element.remove());
    if (currentFinancePage !== "finans-kontoplan.html") {
      const panelHead = document.querySelector(".finance-editor-panel .finance-panel-head");
      if (panelHead) {
        const createButton = document.createElement("button");
        createButton.className = "finance-button finance-button-primary";
        createButton.type = "button";
        createButton.dataset.demoAction = "other";
        createButton.textContent = "+ Ny post";
        panelHead.append(createButton);
      }
    }
  }

  const updateYearLabels = (year, period) => {
    const realizedLabel = document.querySelector('[data-year-label="realized"]');
    const budgetLabel = document.querySelector('[data-year-label="budget"]');
    const previousLabel = document.querySelector('[data-year-label="previous"]');
    if (!realizedLabel || !budgetLabel || !previousLabel) return;
    if (period === "previous") {
      realizedLabel.textContent = `Realiseret (${year})`;
      budgetLabel.textContent = `Budget (${year})`;
      previousLabel.textContent = `Sidste år (${Number(year) - 1})`;
    } else {
      realizedLabel.textContent = "Realiseret YTD";
      budgetLabel.innerHTML = `Budget (<span data-selected-year>${year}</span>)`;
      previousLabel.textContent = "Sidste år YTD";
    }
  };

  document.querySelectorAll("[data-year-toggle] button").forEach((button) => {
    button.addEventListener("click", () => {
      button.parentElement.querySelectorAll("button").forEach((item) => item.classList.remove("is-active"));
      button.classList.add("is-active");
      const year = button.dataset.year;
      document.querySelectorAll("[data-selected-year]").forEach((target) => { target.textContent = year; });
      updateYearLabels(year, button.dataset.period);
      showToast(`Viser regnskabsstatus for ${year}`);
    });
  });

  const activeYearButton = document.querySelector("[data-year-toggle] button.is-active");
  if (activeYearButton) updateYearLabels(activeYearButton.dataset.year, activeYearButton.dataset.period);

  document.querySelectorAll("[data-table-filter]").forEach((filter) => {
    filter.addEventListener("input", () => {
      const query = filter.value.trim().toLowerCase();
      const table = document.querySelector(filter.dataset.tableFilter);
      if (!table) return;
      const rows = table.querySelectorAll("tbody tr[data-posting-row], tbody tr:not(.finance-posting-detail-row)");
      rows.forEach((row) => {
        row.hidden = query && !row.textContent.toLowerCase().includes(query);
      });
    });
  });

  const postingRows = () => document.querySelectorAll("#postering-table tbody tr[data-posting-row]");
  const initialPostingSelections = {
    "BA-852": { account: "arrangement_deltagerbetaling", group: "mad_drikke" },
    "MP-1973": { account: "arrangement_deltagerbetaling", group: "deltagerbetaling" },
    "MP-1974": { account: "", group: "mobilepay" }
  };
  postingRows().forEach((row) => {
    const dateField = row.querySelector('[data-posting-field="date"]');
    if (dateField) dateField.readOnly = true;
    const id = row.querySelector('[data-posting-field="id"]')?.value;
    const initial = initialPostingSelections[id];
    if (!initial) return;
    row.querySelector('[data-posting-field="account"]').value = initial.account;
    row.querySelector('[data-posting-field="group"]').value = initial.group;
  });
  const postingValueText = (row) => [...row.querySelectorAll("input, select")].map((field) => field.value || field.options?.[field.selectedIndex]?.text || "").join(" ").toLowerCase();
  const updatePostingStatus = (row) => {
    const account = row.querySelector('[data-posting-field="account"]')?.value;
    const group = row.querySelector('[data-posting-field="group"]')?.value;
    const badge = row.querySelector("[data-posting-status]");
    if (!badge) return;
    const categorized = Boolean(account && group);
    const missingDocument = categorized && row.dataset.source === "Bank";
    badge.className = `finance-badge ${categorized ? (missingDocument ? "warning" : "success") : "danger"}`;
    badge.textContent = categorized ? (missingDocument ? "Mangler Bilag" : "Match") : "Ukategoriseret";
  };
  const filterPostings = () => {
    const query = document.querySelector("#postering-search")?.value.trim().toLowerCase() || "";
    const status = document.querySelector("#postering-status")?.value || "Alle";
    const source = document.querySelector("#postering-source")?.value || "Alle kilder";
    postingRows().forEach((row) => {
      updatePostingStatus(row);
      const rowStatus = row.querySelector("[data-posting-status]")?.textContent || "";
      const matches = (!query || postingValueText(row).includes(query)) &&
        (status === "Alle" || rowStatus === status) &&
        (source === "Alle kilder" || row.dataset.source === source);
      row.hidden = !matches;
      const detail = row.nextElementSibling;
      if (detail?.classList.contains("finance-posting-detail-row") && !matches) detail.hidden = true;
    });
  };
  document.querySelectorAll("#postering-search, #postering-status, #postering-source").forEach((control) => {
    control.addEventListener("input", filterPostings);
    control.addEventListener("change", filterPostings);
  });
  document.querySelectorAll("#postering-table [data-posting-field]").forEach((field) => {
    field.addEventListener("input", () => filterPostings());
    field.addEventListener("change", () => filterPostings());
  });
  postingRows().forEach(updatePostingStatus);

  document.addEventListener("click", (event) => {
    const button = event.target.closest("[data-posting-action]");
    if (!button) return;
    const row = button.closest("tr[data-posting-row]");
    if (!row) return;
    const detail = row.nextElementSibling?.classList.contains("finance-posting-detail-row") ? row.nextElementSibling : null;
    const action = button.dataset.postingAction;
    if (action === "details") {
      const id = row.querySelector('[data-posting-field="id"]')?.value || "";
      window.location.href = `finans-postering-detalje.html?id=${encodeURIComponent(id)}`;
    } else if (action === "duplicate") {
      const copy = row.cloneNode(true);
      const id = copy.querySelector('[data-posting-field="id"]');
      if (id) id.value = `${id.value}-COPY`;
      row.after(copy);
      if (detail) {
        const detailCopy = detail.cloneNode(true);
        detailCopy.hidden = true;
        copy.after(detailCopy);
      }
      updatePostingStatus(copy);
      filterPostings();
      showToast("Posteringen er duplikeret i mockupen");
    } else if (action === "delete") {
      detail?.remove();
      row.remove();
      showToast("Posteringen er slettet i mockupen");
    }
  });

  document.querySelectorAll("[data-pnl-toggle]").forEach((button) => {
    button.addEventListener("click", () => {
      const key = button.dataset.pnlToggle;
      const isExpanded = button.getAttribute("aria-expanded") === "true";
      const nextState = !isExpanded;
      const parent = document.querySelector(`[data-pnl-parent="${key}"]`);
      const detailHead = document.querySelector(`[data-pnl-detail-head="${key}"]`);
      if (parent) parent.hidden = nextState;
      if (detailHead) detailHead.hidden = !nextState;
      document.querySelectorAll(`[data-pnl-child="${key}"]`).forEach((row) => { row.hidden = !nextState; });
      document.querySelectorAll(`[data-pnl-toggle="${key}"]`).forEach((toggle) => {
        toggle.setAttribute("aria-expanded", String(nextState));
        toggle.textContent = nextState ? "−" : "+";
      });
    });
  });

  document.querySelectorAll("[data-tab-button]").forEach((button) => {
    button.addEventListener("click", () => {
      const group = button.closest("[data-tabs]");
      const targetId = button.dataset.tabButton;
      group.querySelectorAll("[data-tab-button]").forEach((item) => item.classList.remove("is-active"));
      group.querySelectorAll("[data-tab-panel]").forEach((panel) => panel.classList.remove("is-active"));
      button.classList.add("is-active");
      group.querySelector(`[data-tab-panel="${targetId}"]`)?.classList.add("is-active");
      if (window.location.hash !== `#${targetId}`) history.replaceState(null, "", `#${targetId}`);
    });
  });

  const editorButtons = document.querySelectorAll("[data-editor-tab]");
  const activateEditor = (button) => {
    const target = document.getElementById(button.dataset.editorTab);
    if (!target) return;
    editorButtons.forEach((item) => item.classList.toggle("is-active", item === button));
    document.querySelectorAll(".finance-editor-panel").forEach((panel) => { panel.hidden = panel !== target; });
  };
  editorButtons.forEach((button) => button.addEventListener("click", () => activateEditor(button)));
  const editorFromHash = document.getElementById(window.location.hash.slice(1));
  const initialEditor = [...editorButtons].find((button) => button.dataset.editorTab === editorFromHash?.id) || editorButtons[0];
  if (initialEditor) activateEditor(initialEditor);

  const initialTab = window.location.hash.slice(1);
  if (initialTab) document.querySelector(`[data-tab-button="${initialTab}"]`)?.click();

  document.querySelectorAll("[data-demo-action]").forEach((button) => {
    button.addEventListener("click", () => {
      const action = button.dataset.demoAction;
      if (action === "upload") {
        button.closest(".finance-upload-card")?.querySelector(".finance-upload-status")?.classList.add("is-visible");
        showToast("Demo-fil valgt – klar til import");
      } else if (action === "save") {
        showToast("Posteringsændringer gemt i mockupen");
      } else if (action === "reset") {
        document.querySelectorAll("[data-table-filter]").forEach((input) => { input.value = ""; });
        document.querySelectorAll("#postering-status, #postering-source").forEach((select) => { select.selectedIndex = 0; });
        document.querySelectorAll("[data-table-filter]").forEach((filter) => filter.dispatchEvent(new Event("input")));
        showToast("Filtre nulstillet");
      } else {
        showToast("Denne handling er kun en klikbar mockup-state");
      }
    });
  });

  document.querySelectorAll("form[data-demo-form]").forEach((form) => {
    form.addEventListener("submit", (event) => {
      event.preventDefault();
      showToast("Mockupen gemmer ikke data endnu");
    });
  });
});
