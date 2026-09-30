-- Static import events for the local Finance UI.
BEGIN;

TRUNCATE TABLE public.import_history RESTART IDENTITY;

INSERT INTO public.import_history (
    import_type,
    imported_at,
    file_name,
    status,
    rows_processed,
    rows_inserted,
    rows_updated,
    rows_requiring_review,
    notes
) VALUES (
    'bank_csv',
    '2026-09-03 10:42:00+02',
    'bank.csv',
    'completed',
    1284,
    1272,
    0,
    12,
    'Seneste lokale testimport'
);

INSERT INTO public.import_history (
    import_type,
    imported_at,
    file_name,
    status,
    rows_processed,
    rows_inserted,
    rows_updated,
    rows_requiring_review,
    notes
) VALUES (
    'mobilepay_csv',
    '2026-09-03 10:42:00+02',
    'mobilepay.csv',
    'completed',
    518,
    518,
    0,
    0,
    'Seneste lokale testimport'
);

COMMIT;
