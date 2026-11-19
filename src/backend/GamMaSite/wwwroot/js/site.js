// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function initializeDatatable() {
    $(document).ready(function () {
        $('table.datatable').DataTable(
            {
                columnDefs: [{
                    className: 'dtr-control',
                    orderable: false,
                    targets: 0
                }],
                order: [1, 'asc'],
                language: {
                    paginate: {
                        previous: "Forrige",
                        next: "Næste"
                    },
                    lengthMenu: "_MENU_ per side",
                    search: "Søg:",
                    info: "Viser element _START_ til _END_",
                    infoEmpty: "Visning af element 0 til 0 på 0 elementer",
                    dataTables_empty: "Ingen data i tabellen",
                    zeroRecords: "Intet match på søgning",
                    infoFiltered: "(filtreret fra i alt _MAX_ elementer)",
                    emptyTable: "Ingen data i tabellen"
                },
                lengthMenu: [50, 75, 100, 200, 500, 1000],
            }
        );
    });
}

(() => {
    const modal = document.getElementById("contact-modal");
    const form = document.getElementById("contact-form");
    if (!modal || !form) return;

    const type = form.elements.contactType;
    const topic = form.elements.topic;
    const label = modal.querySelector("[data-contact-topic-label]");
    const status = document.getElementById("contact-status");
    const close = () => { modal.hidden = true; document.body.classList.remove("contact-modal-open"); };
    document.addEventListener("click", (event) => {
        if (event.target.closest?.("[data-contact-trigger]")) {
            event.preventDefault();
            modal.hidden = false;
            status.hidden = true;
            document.body.classList.add("contact-modal-open");
            modal.querySelector("input[name=email]")?.focus();
        }
        if (event.target.closest?.("[data-contact-close]") || event.target === modal) close();
    });
    document.addEventListener("keydown", (event) => { if (event.key === "Escape" && !modal.hidden) close(); });
    type.addEventListener("change", () => {
        const eventContact = type.value === "Arrangement";
        label.textContent = eventContact ? "Arrangementets titel" : "Emne";
        topic.placeholder = eventContact ? "Skriv arrangementets titel" : "Skriv et emne";
    });
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        const submit = form.querySelector('[type="submit"]');
        submit.disabled = true;
        status.hidden = true;
        try {
            const tokenResponse = await fetch("/api/account/csrf-token", { credentials: "same-origin", cache: "no-store" });
            const { token } = await tokenResponse.json();
            const payload = Object.fromEntries(new FormData(form).entries());
            const response = await fetch("/api/contact", {
                method: "POST", credentials: "same-origin",
                headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": token },
                body: JSON.stringify(payload)
            });
            const result = await response.json();
            if (!response.ok) throw new Error(result.error || "Henvendelsen kunne ikke sendes lige nu. Prøv igen senere.");
            status.textContent = result.message;
            form.reset();
            type.dispatchEvent(new Event("change"));
        } catch (error) {
            status.textContent = error.message || "Henvendelsen kunne ikke sendes lige nu. Prøv igen senere.";
        } finally {
            status.hidden = false;
            submit.disabled = false;
        }
    });
})();
