-- Full seed data for public.account.
BEGIN;

INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-1-1', 'Arrangement', '1', 'Deltagerbetaling', '1', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-1-2', 'Arrangement', '1', 'Deltagerbetaling', '1', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-2-1', 'Arrangement', '1', 'Lokale', '2', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-2-2', 'Arrangement', '1', 'Lokale', '2', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-3-1', 'Arrangement', '1', 'Hytteleje', '3', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-3-2', 'Arrangement', '1', 'Hytteleje', '3', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-4-1', 'Arrangement', '1', 'Mad og drikke', '4', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-4-2', 'Arrangement', '1', 'Mad og drikke', '4', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-5-1', 'Arrangement', '1', 'Øvrige', '5', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('A-1-5-2', 'Arrangement', '1', 'Øvrige', '5', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-10-1', 'Drift', '2', 'Øvrige', '10', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-10-2', 'Drift', '2', 'Øvrige', '10', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-6-1', 'Drift', '2', 'IT', '6', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-6-2', 'Drift', '2', 'IT', '6', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-7-1', 'Drift', '2', 'Bank', '7', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-7-2', 'Drift', '2', 'Bank', '7', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-8-1', 'Drift', '2', 'Mobilepay', '8', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-8-2', 'Drift', '2', 'Mobilepay', '8', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-9-1', 'Drift', '2', 'Bestyelse', '9', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('D-2-9-2', 'Drift', '2', 'Bestyelse', '9', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('K-3-11-1', 'Kontigent', '3', 'Virksomhedskontigent', '11', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('K-3-11-2', 'Kontigent', '3', 'Virksomhedskontigent', '11', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('K-3-12-1', 'Kontigent', '3', 'Medlemskontigent', '12', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('K-3-12-2', 'Kontigent', '3', 'Medlemskontigent', '12', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('Ø-4-13-1', 'Øvrige', '4', 'Rekruttering', '13', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('Ø-4-13-2', 'Øvrige', '4', 'Rekruttering', '13', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('Ø-4-14-1', 'Øvrige', '4', 'Øvrige', '14', 'Indtægt', '1') ON CONFLICT (id) DO NOTHING;
INSERT INTO public.account (id, main_account, account_key, sub_account, sub_account_key, context, context_key) VALUES ('Ø-4-14-2', 'Øvrige', '4', 'Øvrige', '14', 'Udgift', '2') ON CONFLICT (id) DO NOTHING;

COMMIT;
