-- ORION 009 - Patch 001
-- Remove trigger de data_atualizacao aplicado indevidamente em interacao_ia.
-- A tabela interacao_ia possui data_inicio/data_fim e nao possui data_atualizacao.
-- Script idempotente.

DO $$
DECLARE
    r record;
BEGIN
    FOR r IN
        SELECT t.tgname
        FROM pg_trigger t
        JOIN pg_proc p ON p.oid = t.tgfoid
        WHERE t.tgrelid = 'public.interacao_ia'::regclass
          AND NOT t.tgisinternal
          AND p.proname = 'fn_atualizar_data_atualizacao'
    LOOP
        EXECUTE format(
            'DROP TRIGGER IF EXISTS %I ON public.interacao_ia',
            r.tgname
        );

        RAISE NOTICE 'Trigger removido de interacao_ia: %', r.tgname;
    END LOOP;
END
$$;
