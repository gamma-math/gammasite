// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function initializeDatatable() {
    $(document).ready(function () {
        $('table.datatable').DataTable(
            {
                responsive: {
                    details: {
                        type: 'column'
                    }
                },
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