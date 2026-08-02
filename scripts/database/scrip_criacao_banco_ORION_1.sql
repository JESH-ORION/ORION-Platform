-- =============================================================================
-- SCRIPT DE CRIAÇÃO E CONFIGURAÇÃO DO BANCO DE DADOS POSTGRESQL
-- =============================================================================

-- 1. Criação do Banco de Dados
-- Define a codificação UTF8 (para acentuação e caracteres especiais)
-- e as regras de ordenação de texto.
CREATE DATABASE orion_dev
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'pt_BR.UTF-8'
    LC_CTYPE = 'pt_BR.UTF-8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;
