-- =============================================================================
-- SCRIPT DE CRIAÇÃO DO BANCO DE DADOS E TABELAS - POSTGRESQL (PROJETO ORION)
-- =============================================================================

-- 1. Extensões necessárias (UUID e btree_gist para restrição de sobreposição de horários)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "btree_gist";

-- 2. Enums (Tipos Personalizados)
CREATE TYPE tp_status_agendamento AS ENUM (
    'PENDENTE', 
    'CONFIRMADO', 
    'EM_ANDAMENTO', 
    'CONCLUIDO', 
    'CANCELADO'
);

CREATE TYPE tp_status_pagamento AS ENUM (
    'PENDENTE', 
    'PAGO', 
    'REEMBOLSADO', 
    'FALHOU'
);

CREATE TYPE tp_documento AS ENUM (
    'CPF',
    'CNPJ'
);

-- =============================================================================
-- TABELAS PRINCIPAIS
-- =============================================================================

-- 3. Perfis / Roles
CREATE TABLE perfil (
    id_perfil SERIAL PRIMARY KEY,
    nome VARCHAR(50) NOT NULL UNIQUE,
    descricao TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 4. Usuários
CREATE TABLE usuario (
    id_usuario UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    id_perfil INT NOT NULL,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE, -- Cria índice único automaticamente
    senha_hash VARCHAR(255) NOT NULL,
    telefone VARCHAR(20),
    documento VARCHAR(20) UNIQUE,
    tipo_documento tp_documento,
    ativo BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_usuario_perfil FOREIGN KEY (id_perfil) 
        REFERENCES perfil(id_perfil) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- 5. Empresas
CREATE TABLE empresa (
    id_empresa UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    id_proprietario UUID NOT NULL,
    razao_social VARCHAR(150) NOT NULL,
    nome_fantasia VARCHAR(150),
    cnpj VARCHAR(20) UNIQUE,
    telefone VARCHAR(20),
    endereco TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_empresa_proprietario FOREIGN KEY (id_proprietario) 
        REFERENCES usuario(id_usuario) ON DELETE CASCADE ON UPDATE CASCADE
);

-- 5.1. Vínculo Empresa <-> Prestadores/Profissionais
CREATE TABLE empresa_usuario (
    id_empresa_usuario UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    id_empresa UUID NOT NULL,
    id_usuario UUID NOT NULL,
    cargo_funcao VARCHAR(100),
    ativo BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_eu_empresa FOREIGN KEY (id_empresa) 
        REFERENCES empresa(id_empresa) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_eu_usuario FOREIGN KEY (id_usuario) 
        REFERENCES usuario(id_usuario) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT unq_empresa_usuario UNIQUE (id_empresa, id_usuario)
);

-- 6. Categorias
CREATE TABLE categoria (
    id_categoria SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL UNIQUE,
    descricao TEXT,
    ativo BOOLEAN DEFAULT TRUE
);

-- 7. Serviços
CREATE TABLE servico (
    id_servico UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    id_empresa UUID NOT NULL,
    id_categoria INT NOT NULL,
    nome VARCHAR(120) NOT NULL,
    descricao TEXT,
    preco NUMERIC(10, 2) NOT NULL CHECK (preco >= 0),
    duracao_minutos INT NOT NULL CHECK (duracao_minutos > 0),
    ativo BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_servico_empresa FOREIGN KEY (id_empresa) 
        REFERENCES empresa(id_empresa) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_servico_categoria FOREIGN KEY (id_categoria) 
        REFERENCES categoria(id_categoria) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- 8. Agendamentos
CREATE TABLE agendamento (
    id_agendamento UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    id_cliente UUID NOT NULL,
    id_prestador UUID NOT NULL, -- Aponta para o registro do prestador na empresa (empresa_usuario)
    id_servico UUID NOT NULL,
    data_hora_inicio TIMESTAMP WITH TIME ZONE NOT NULL,
    data_hora_fim TIMESTAMP WITH TIME ZONE NOT NULL,
    status tp_status_agendamento DEFAULT 'PENDENTE',
    observacoes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_agendamento_cliente FOREIGN KEY (id_cliente) 
        REFERENCES usuario(id_usuario) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_agendamento_prestador FOREIGN KEY (id_prestador) 
        REFERENCES empresa_usuario(id_empresa_usuario) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_agendamento_servico FOREIGN KEY (id_servico) 
        REFERENCES servico(id_servico) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT chk_datas_agendamento CHECK (data_hora_fim > data_hora_inicio),
    -- Garante no banco que o mesmo prestador não receba agendamentos conflitantes no mesmo horário
    CONSTRAINT excl_agendamento_prestador_horario EXCLUDE USING GIST (
        id_prestador WITH =,
        tstzrange(data_hora_inicio, data_hora_fim) WITH &&
    )
);

-- 9. Métodos de Pagamento
CREATE TABLE metodo_pagamento (
    id_metodo_pagamento SERIAL PRIMARY KEY,
    nome VARCHAR(50) NOT NULL UNIQUE,
    ativo BOOLEAN DEFAULT TRUE
);

-- 10. Pagamentos (Relação 1:N permitindo tentativas de pagamento)
CREATE TABLE pagamento (
    id_pagamento UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    id_agendamento UUID NOT NULL,
    id_metodo_pagamento INT NOT NULL,
    valor NUMERIC(10, 2) NOT NULL CHECK (valor >= 0),
    status tp_status_pagamento DEFAULT 'PENDENTE',
    data_pagamento TIMESTAMP WITH TIME ZONE,
    transacao_externa_id VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_pagamento_agendamento FOREIGN KEY (id_agendamento) 
        REFERENCES agendamento(id_agendamento) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_pagamento_metodo FOREIGN KEY (id_metodo_pagamento) 
        REFERENCES metodo_pagamento(id_metodo_pagamento) ON DELETE RESTRICT ON UPDATE CASCADE
);

-- 11. Avaliações (Sem id_cliente redundante)
CREATE TABLE avaliacao (
    id_avaliacao UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    id_agendamento UUID NOT NULL UNIQUE,
    nota INT NOT NULL CHECK (nota BETWEEN 1 AND 5),
    comentario TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_avaliacao_agendamento FOREIGN KEY (id_agendamento) 
        REFERENCES agendamento(id_agendamento) ON DELETE CASCADE ON UPDATE CASCADE
);

-- =============================================================================
-- ÍNDICES PARA OTIMIZAÇÃO DE BUSCAS
-- =============================================================================

CREATE INDEX idx_servico_empresa ON servico(id_empresa);
CREATE INDEX idx_agendamento_cliente ON agendamento(id_cliente);
CREATE INDEX idx_agendamento_prestador ON agendamento(id_prestador);
CREATE INDEX idx_agendamento_data ON agendamento(data_hora_inicio, data_hora_fim);
CREATE INDEX idx_pagamento_agendamento ON pagamento(id_agendamento);
CREATE INDEX idx_pagamento_status ON pagamento(status);

-- =============================================================================
-- TRIGGERS PARA ATUALIZAÇÃO AUTOMÁTICA DE updated_at
-- =============================================================================

CREATE OR REPLACE FUNCTION fn_update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_usuario_updated_at
    BEFORE UPDATE ON usuario
    FOR EACH ROW EXECUTE FUNCTION fn_update_updated_at_column();

CREATE TRIGGER trg_update_empresa_updated_at
    BEFORE UPDATE ON empresa
    FOR EACH ROW EXECUTE FUNCTION fn_update_updated_at_column();

CREATE TRIGGER trg_update_servico_updated_at
    BEFORE UPDATE ON servico
    FOR EACH ROW EXECUTE FUNCTION fn_update_updated_at_column();

CREATE TRIGGER trg_update_agendamento_updated_at
    BEFORE UPDATE ON agendamento
    FOR EACH ROW EXECUTE FUNCTION fn_update_updated_at_column();

CREATE TRIGGER trg_update_pagamento_updated_at
    BEFORE UPDATE ON pagamento
    FOR EACH ROW EXECUTE FUNCTION fn_update_updated_at_column();

-- =============================================================================
-- CARGA INICIAL DE DADOS
-- =============================================================================

INSERT INTO perfil (nome, descricao) VALUES
('ADMINISTRADOR', 'Acesso completo às configurações e gerenciamento do sistema'),
('PRESTADOR', 'Usuário prestador de serviços/proprietário de empresa'),
('CLIENTE', 'Usuário consumidor de serviços');

INSERT INTO metodo_pagamento (nome) VALUES
('PIX'),
('CARTAO_CREDITO'),
('CARTAO_DEBITO'),
('BOLETO'),
('DINHEIRO');