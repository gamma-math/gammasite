-- Keep timestamp columns and update triggers available when the seed runner
-- is executed against an existing local database.

ALTER TABLE public.account ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.account ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.bank_account ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.bank_account ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.mobilepay ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.mobilepay ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.postering_group ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.postering_group ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.forecast ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.forecast ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.posteringer ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.posteringer ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.posteringer ADD COLUMN IF NOT EXISTS posting_date date;
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'posteringer'
          AND column_name = 'posterings_date'
    ) THEN
        UPDATE public.posteringer
        SET posting_date = COALESCE(posting_date, posterings_date, date)
        WHERE posting_date IS NULL;
        ALTER TABLE public.posteringer DROP COLUMN posterings_date;
    ELSE
        UPDATE public.posteringer
        SET posting_date = COALESCE(posting_date, date)
        WHERE posting_date IS NULL;
    END IF;
END $$;
ALTER TABLE public.import_history ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.import_history ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;

CREATE OR REPLACE FUNCTION public.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS account_set_updated_at ON public.account;
CREATE TRIGGER account_set_updated_at BEFORE UPDATE ON public.account FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
DROP TRIGGER IF EXISTS bank_account_set_updated_at ON public.bank_account;
CREATE TRIGGER bank_account_set_updated_at BEFORE UPDATE ON public.bank_account FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
DROP TRIGGER IF EXISTS mobilepay_set_updated_at ON public.mobilepay;
CREATE TRIGGER mobilepay_set_updated_at BEFORE UPDATE ON public.mobilepay FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
DROP TRIGGER IF EXISTS postering_group_set_updated_at ON public.postering_group;
CREATE TRIGGER postering_group_set_updated_at BEFORE UPDATE ON public.postering_group FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
DROP TRIGGER IF EXISTS forecast_set_updated_at ON public.forecast;
CREATE TRIGGER forecast_set_updated_at BEFORE UPDATE ON public.forecast FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
DROP TRIGGER IF EXISTS posteringer_set_updated_at ON public.posteringer;
CREATE TRIGGER posteringer_set_updated_at BEFORE UPDATE ON public.posteringer FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
DROP TRIGGER IF EXISTS import_history_set_updated_at ON public.import_history;
CREATE TRIGGER import_history_set_updated_at BEFORE UPDATE ON public.import_history FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
