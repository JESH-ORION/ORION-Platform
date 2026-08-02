-- =============================================================================
-- SCRIPT DE CRIAÇÃO E CONFIGURAÇÃO DO BANCO DE DADOS POSTGRESQL
-- PROJETO ORION
-- =============================================================================

CREATE DATABASE orion_dev
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;
	
COMMENT ON DATABASE orion_dev
    IS 'Banco de dados principal da plataforma ORION';

ALTER DATABASE orion_dev
    SET timezone TO 'America/Sao_Paulo';	
