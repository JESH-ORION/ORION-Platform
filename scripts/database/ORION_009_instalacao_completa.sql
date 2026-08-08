BEGIN;
-- ================= 001_enable_extensions.sql =================
-- ORION 009
-- Extensões PostgreSQL necessárias
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- ================= 002_create_usuario_perfil.sql =================
CREATE TABLE perfil (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome varchar(100) NOT NULL,
    descricao text,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_perfil_nome UNIQUE (nome),
    CONSTRAINT ck_perfil_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE usuario (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    perfil_id uuid NOT NULL,
    nome varchar(150) NOT NULL,
    email varchar(254) NOT NULL,
    telefone varchar(30),
    senha_hash text NOT NULL,
    documento varchar(20),
    tipo_documento varchar(10),
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    ultimo_acesso timestamptz,
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_usuario_email UNIQUE (email),
    CONSTRAINT uq_usuario_documento UNIQUE (documento),
    CONSTRAINT fk_usuario_perfil FOREIGN KEY (perfil_id) REFERENCES perfil(id) ON DELETE RESTRICT,
    CONSTRAINT ck_usuario_status CHECK (status IN ('ATIVO','INATIVO','BLOQUEADO')),
    CONSTRAINT ck_usuario_tipo_documento CHECK (tipo_documento IS NULL OR tipo_documento IN ('CPF','CNPJ'))
);

CREATE INDEX ix_usuario_status ON usuario(status);

-- ================= 003_create_paciente_profissional.sql =================
CREATE TABLE paciente (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL,
    nome_completo varchar(200) NOT NULL,
    data_nascimento date,
    sexo varchar(30),
    cpf varchar(14),
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_paciente_usuario UNIQUE (usuario_id),
    CONSTRAINT uq_paciente_cpf UNIQUE (cpf),
    CONSTRAINT fk_paciente_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE RESTRICT,
    CONSTRAINT ck_paciente_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE profissional (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL,
    registro_profissional varchar(50) NOT NULL,
    conselho varchar(50) NOT NULL,
    uf_conselho varchar(2) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_profissional_usuario UNIQUE (usuario_id),
    CONSTRAINT uq_profissional_registro UNIQUE (conselho, registro_profissional, uf_conselho),
    CONSTRAINT fk_profissional_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE RESTRICT,
    CONSTRAINT ck_profissional_status CHECK (status IN ('ATIVO','INATIVO','BLOQUEADO'))
);

CREATE INDEX ix_profissional_registro ON profissional(registro_profissional);

-- ================= 004_create_especialidade.sql =================
CREATE TABLE especialidade (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome varchar(150) NOT NULL,
    descricao text,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_especialidade_nome UNIQUE (nome),
    CONSTRAINT ck_especialidade_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE profissional_especialidade (
    profissional_id uuid NOT NULL,
    especialidade_id uuid NOT NULL,
    data_criacao timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (profissional_id, especialidade_id),
    FOREIGN KEY (profissional_id) REFERENCES profissional(id) ON DELETE RESTRICT,
    FOREIGN KEY (especialidade_id) REFERENCES especialidade(id) ON DELETE RESTRICT
);

-- ================= 005_create_organizacao_empresa.sql =================
CREATE TABLE organizacao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome varchar(200) NOT NULL,
    nome_fantasia varchar(200),
    tipo varchar(50) NOT NULL,
    documento varchar(30),
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_organizacao_documento UNIQUE (documento),
    CONSTRAINT ck_organizacao_status CHECK (status IN ('ATIVO','INATIVO')),
    CONSTRAINT ck_organizacao_tipo CHECK (tipo IN ('CLINICA','HOSPITAL','LABORATORIO','EMPRESA','GRUPO_PROFISSIONAL'))
);

-- Compatibilidade semântica com o modelo anterior: empresa é a organização comercial.
CREATE TABLE empresa (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organizacao_id uuid UNIQUE,
    id_proprietario uuid NOT NULL,
    razao_social varchar(150) NOT NULL,
    nome_fantasia varchar(150),
    cnpj varchar(20),
    telefone varchar(30),
    endereco text,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_empresa_cnpj UNIQUE (cnpj),
    CONSTRAINT fk_empresa_organizacao FOREIGN KEY (organizacao_id) REFERENCES organizacao(id) ON DELETE RESTRICT,
    CONSTRAINT fk_empresa_proprietario FOREIGN KEY (id_proprietario) REFERENCES usuario(id) ON DELETE RESTRICT,
    CONSTRAINT ck_empresa_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE empresa_usuario (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id uuid NOT NULL,
    usuario_id uuid NOT NULL,
    profissional_id uuid,
    cargo_funcao varchar(100),
    ativo boolean NOT NULL DEFAULT true,
    data_criacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_empresa_usuario UNIQUE (empresa_id, usuario_id),
    CONSTRAINT uq_empresa_usuario_id_empresa UNIQUE (id, empresa_id),
    CONSTRAINT fk_eu_empresa FOREIGN KEY (empresa_id) REFERENCES empresa(id) ON DELETE RESTRICT,
    CONSTRAINT fk_eu_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE RESTRICT,
    CONSTRAINT fk_eu_profissional FOREIGN KEY (profissional_id) REFERENCES profissional(id) ON DELETE RESTRICT,
    CONSTRAINT uq_empresa_profissional UNIQUE (empresa_id, profissional_id)
);

CREATE INDEX ix_empresa_usuario_usuario ON empresa_usuario(usuario_id);

-- ================= 006_create_servico_categoria.sql =================
CREATE TABLE categoria (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome varchar(100) NOT NULL,
    descricao text,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_categoria_nome UNIQUE (nome),
    CONSTRAINT ck_categoria_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE servico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id uuid NOT NULL,
    categoria_id uuid NOT NULL,
    nome varchar(120) NOT NULL,
    descricao text,
    preco numeric(12,2) NOT NULL DEFAULT 0 CHECK (preco >= 0),
    duracao_minutos integer NOT NULL CHECK (duracao_minutos > 0),
    modalidade varchar(30) NOT NULL DEFAULT 'ONLINE',
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_servico_empresa_id UNIQUE (id, empresa_id),
    CONSTRAINT fk_servico_empresa FOREIGN KEY (empresa_id) REFERENCES empresa(id) ON DELETE RESTRICT,
    CONSTRAINT fk_servico_categoria FOREIGN KEY (categoria_id) REFERENCES categoria(id) ON DELETE RESTRICT,
    CONSTRAINT ck_servico_modalidade CHECK (modalidade IN ('ONLINE','PRESENCIAL','AMBAS')),
    CONSTRAINT ck_servico_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE INDEX ix_servico_empresa ON servico(empresa_id);

-- ================= 007_create_agendamento_consulta.sql =================
CREATE TABLE agendamento (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    paciente_id uuid NOT NULL,
    prestador_id uuid NOT NULL,
    empresa_id uuid NOT NULL,
    servico_id uuid NOT NULL,
    data_inicio timestamptz NOT NULL,
    data_fim timestamptz NOT NULL,
    modalidade varchar(30) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'AGENDADO',
    observacao text,
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_agendamento_paciente FOREIGN KEY (paciente_id) REFERENCES paciente(id) ON DELETE RESTRICT,
    CONSTRAINT fk_agendamento_prestador_empresa FOREIGN KEY (prestador_id, empresa_id) REFERENCES empresa_usuario(id, empresa_id) ON DELETE RESTRICT,
    CONSTRAINT fk_agendamento_servico_empresa FOREIGN KEY (servico_id, empresa_id) REFERENCES servico(id, empresa_id) ON DELETE RESTRICT,
    CONSTRAINT ck_agendamento_periodo CHECK (data_fim > data_inicio),
    CONSTRAINT ck_agendamento_modalidade CHECK (modalidade IN ('ONLINE','PRESENCIAL')),
    CONSTRAINT ck_agendamento_status CHECK (status IN ('AGENDADO','CONFIRMADO','EM_ANDAMENTO','CANCELADO','CONCLUIDO','NAO_COMPARECEU')),
    CONSTRAINT excl_agendamento_prestador_horario EXCLUDE USING gist (prestador_id WITH =, tstzrange(data_inicio,data_fim,'[)') WITH &&) WHERE (status IN ('AGENDADO','CONFIRMADO','EM_ANDAMENTO'))
);

CREATE INDEX ix_agendamento_paciente_data ON agendamento(paciente_id,data_inicio);
CREATE INDEX ix_agendamento_empresa_data ON agendamento(empresa_id,data_inicio);
CREATE INDEX ix_agendamento_prestador_data ON agendamento(prestador_id,data_inicio);

CREATE TABLE consulta (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    agendamento_id uuid NOT NULL UNIQUE,
    paciente_id uuid NOT NULL,
    profissional_id uuid NOT NULL,
    empresa_id uuid,
    modalidade varchar(30) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'INICIADA',
    inicio timestamptz,
    fim timestamptz,
    observacao text,
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_consulta_agendamento FOREIGN KEY (agendamento_id) REFERENCES agendamento(id) ON DELETE RESTRICT,
    CONSTRAINT fk_consulta_paciente FOREIGN KEY (paciente_id) REFERENCES paciente(id) ON DELETE RESTRICT,
    CONSTRAINT fk_consulta_profissional FOREIGN KEY (profissional_id) REFERENCES profissional(id) ON DELETE RESTRICT,
    CONSTRAINT fk_consulta_empresa FOREIGN KEY (empresa_id) REFERENCES empresa(id) ON DELETE RESTRICT,
    CONSTRAINT ck_consulta_modalidade CHECK (modalidade IN ('ONLINE','PRESENCIAL')),
    CONSTRAINT ck_consulta_status CHECK (status IN ('INICIADA','EM_ANDAMENTO','FINALIZADA','CANCELADA')),
    CONSTRAINT ck_consulta_periodo CHECK (fim IS NULL OR inicio IS NULL OR fim >= inicio)
);

-- ================= 008_create_triagem_prontuario_exame.sql =================
CREATE TABLE triagem (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    paciente_id uuid NOT NULL,
    consulta_id uuid,
    origem varchar(30) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'INICIADA',
    resumo text,
    classificacao varchar(50),
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (paciente_id) REFERENCES paciente(id) ON DELETE RESTRICT,
    FOREIGN KEY (consulta_id) REFERENCES consulta(id) ON DELETE RESTRICT,
    CONSTRAINT ck_triagem_origem CHECK (origem IN ('IA','PROFISSIONAL','MANUAL')),
    CONSTRAINT ck_triagem_status CHECK (status IN ('INICIADA','EM_ANDAMENTO','FINALIZADA','CANCELADA'))
);

CREATE TABLE prontuario (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    paciente_id uuid NOT NULL UNIQUE,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (paciente_id) REFERENCES paciente(id) ON DELETE RESTRICT,
    CONSTRAINT ck_prontuario_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE exame (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    paciente_id uuid NOT NULL,
    profissional_id uuid,
    consulta_id uuid,
    organizacao_id uuid,
    tipo varchar(150) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'SOLICITADO',
    data_solicitacao timestamptz NOT NULL DEFAULT now(),
    data_realizacao timestamptz,
    resultado_resumo text,
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (paciente_id) REFERENCES paciente(id) ON DELETE RESTRICT,
    FOREIGN KEY (profissional_id) REFERENCES profissional(id) ON DELETE RESTRICT,
    FOREIGN KEY (consulta_id) REFERENCES consulta(id) ON DELETE RESTRICT,
    FOREIGN KEY (organizacao_id) REFERENCES organizacao(id) ON DELETE RESTRICT,
    CONSTRAINT ck_exame_status CHECK (status IN ('SOLICITADO','AGENDADO','REALIZADO','RESULTADO_DISPONIVEL','CANCELADO'))
);

CREATE INDEX ix_exame_paciente_data ON exame(paciente_id,data_realizacao);

-- ================= 009_create_arquivo_conversa.sql =================
CREATE TABLE arquivo (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome_original varchar(255) NOT NULL,
    storage_key text NOT NULL UNIQUE,
    content_type varchar(150) NOT NULL,
    tamanho_bytes bigint NOT NULL CHECK (tamanho_bytes >= 0),
    hash varchar(128),
    categoria varchar(50) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_upload timestamptz NOT NULL DEFAULT now(),
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_arquivo_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE exame_arquivo (
    exame_id uuid NOT NULL,
    arquivo_id uuid NOT NULL,
    tipo varchar(50),
    data_criacao timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (exame_id,arquivo_id),
    FOREIGN KEY (exame_id) REFERENCES exame(id) ON DELETE RESTRICT,
    FOREIGN KEY (arquivo_id) REFERENCES arquivo(id) ON DELETE RESTRICT
);

CREATE TABLE conversa (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tipo varchar(40) NOT NULL,
    paciente_id uuid,
    profissional_id uuid,
    status varchar(30) NOT NULL DEFAULT 'ATIVA',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (paciente_id) REFERENCES paciente(id) ON DELETE RESTRICT,
    FOREIGN KEY (profissional_id) REFERENCES profissional(id) ON DELETE RESTRICT,
    CONSTRAINT ck_conversa_status CHECK (status IN ('ATIVA','ENCERRADA','ARQUIVADA'))
);

CREATE TABLE mensagem (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    conversa_id uuid NOT NULL,
    usuario_id uuid,
    conteudo text NOT NULL,
    tipo varchar(30) NOT NULL,
    data_envio timestamptz NOT NULL DEFAULT now(),
    lida_em timestamptz,
    FOREIGN KEY (conversa_id) REFERENCES conversa(id) ON DELETE RESTRICT,
    FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE RESTRICT
);

CREATE INDEX ix_mensagem_conversa_data ON mensagem(conversa_id,data_envio);

-- ================= 010_create_ia.sql =================
CREATE TABLE agente (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome varchar(150) NOT NULL,
    identificador varchar(100) NOT NULL,
    versao varchar(30) NOT NULL,
    finalidade text,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_agente_identificador_versao UNIQUE (identificador,versao),
    CONSTRAINT ck_agente_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE interacao_ia (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    agente_id uuid NOT NULL,
    usuario_id uuid,
    paciente_id uuid,
    conversa_id uuid,
    data_inicio timestamptz NOT NULL DEFAULT now(),
    data_fim timestamptz,
    status varchar(30) NOT NULL DEFAULT 'INICIADA',
    modelo varchar(100),
    versao_agente varchar(30),
    FOREIGN KEY (agente_id) REFERENCES agente(id) ON DELETE RESTRICT,
    FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE RESTRICT,
    FOREIGN KEY (paciente_id) REFERENCES paciente(id) ON DELETE RESTRICT,
    FOREIGN KEY (conversa_id) REFERENCES conversa(id) ON DELETE RESTRICT,
    CONSTRAINT ck_interacao_ia_status CHECK (status IN ('INICIADA','EM_ANDAMENTO','FINALIZADA','ERRO','CANCELADA')),
    CONSTRAINT ck_interacao_ia_periodo CHECK (data_fim IS NULL OR data_fim >= data_inicio)
);

CREATE TABLE ferramenta_ia (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome varchar(150) NOT NULL,
    identificador varchar(100) NOT NULL UNIQUE,
    descricao text,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_ferramenta_ia_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE execucao_ferramenta (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    interacao_ia_id uuid NOT NULL,
    ferramenta_id uuid NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'INICIADA',
    inicio timestamptz NOT NULL DEFAULT now(),
    fim timestamptz,
    erro text,
    FOREIGN KEY (interacao_ia_id) REFERENCES interacao_ia(id) ON DELETE RESTRICT,
    FOREIGN KEY (ferramenta_id) REFERENCES ferramenta_ia(id) ON DELETE RESTRICT,
    CONSTRAINT ck_execucao_ferramenta_status CHECK (status IN ('INICIADA','CONCLUIDA','ERRO','CANCELADA')),
    CONSTRAINT ck_execucao_ferramenta_periodo CHECK (fim IS NULL OR fim >= inicio)
);

-- ================= 011_create_pagamento_avaliacao.sql =================
CREATE TABLE metodo_pagamento (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome varchar(50) NOT NULL UNIQUE,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    data_criacao timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_metodo_pagamento_status CHECK (status IN ('ATIVO','INATIVO'))
);

CREATE TABLE pagamento (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    agendamento_id uuid NOT NULL,
    metodo_pagamento_id uuid NOT NULL,
    valor numeric(12,2) NOT NULL CHECK (valor >= 0),
    status varchar(30) NOT NULL DEFAULT 'PENDENTE',
    data_pagamento timestamptz,
    transacao_externa_id varchar(255),
    data_criacao timestamptz NOT NULL DEFAULT now(),
    data_atualizacao timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (agendamento_id) REFERENCES agendamento(id) ON DELETE RESTRICT,
    FOREIGN KEY (metodo_pagamento_id) REFERENCES metodo_pagamento(id) ON DELETE RESTRICT,
    CONSTRAINT ck_pagamento_status CHECK (status IN ('PENDENTE','PAGO','REEMBOLSADO','FALHOU'))
);

CREATE INDEX ix_pagamento_agendamento ON pagamento(agendamento_id);
CREATE INDEX ix_pagamento_status ON pagamento(status);

CREATE TABLE avaliacao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    agendamento_id uuid NOT NULL UNIQUE,
    nota integer NOT NULL CHECK (nota BETWEEN 1 AND 5),
    comentario text,
    data_criacao timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (agendamento_id) REFERENCES agendamento(id) ON DELETE RESTRICT
);

-- ================= 012_create_seguranca_auditoria.sql =================
CREATE TABLE permissao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome varchar(150) NOT NULL,
    identificador varchar(150) NOT NULL UNIQUE,
    descricao text,
    data_criacao timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE perfil_permissao (
    perfil_id uuid NOT NULL,
    permissao_id uuid NOT NULL,
    PRIMARY KEY (perfil_id,permissao_id),
    FOREIGN KEY (perfil_id) REFERENCES perfil(id) ON DELETE RESTRICT,
    FOREIGN KEY (permissao_id) REFERENCES permissao(id) ON DELETE RESTRICT
);

CREATE TABLE auditoria (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid,
    acao varchar(100) NOT NULL,
    entidade varchar(100) NOT NULL,
    entidade_id uuid,
    data_evento timestamptz NOT NULL DEFAULT now(),
    ip inet,
    sucesso boolean NOT NULL DEFAULT true,
    FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE RESTRICT
);

CREATE INDEX ix_auditoria_entidade ON auditoria(entidade,entidade_id);
CREATE INDEX ix_auditoria_data ON auditoria(data_evento);

-- ================= 013_create_updated_at.sql =================
CREATE OR REPLACE FUNCTION fn_atualizar_data_atualizacao()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.data_atualizacao = now();
    RETURN NEW;
END;
$$;

-- Triggers explícitas apenas nas entidades com data_atualizacao.
DO $$
DECLARE
    tabela text;
BEGIN
    FOREACH tabela IN ARRAY ARRAY['usuario','paciente','profissional','especialidade','organizacao','empresa','servico','agendamento','consulta','triagem','prontuario','exame','arquivo','conversa','agente','interacao_ia','ferramenta_ia','pagamento']
    LOOP
        EXECUTE format('CREATE TRIGGER trg_%I_updated_at BEFORE UPDATE ON %I FOR EACH ROW EXECUTE FUNCTION fn_atualizar_data_atualizacao()', tabela, tabela);
    END LOOP;
END $$;

-- ================= 014_seed_inicial.sql =================
INSERT INTO perfil (nome,descricao) VALUES
('ADMIN','Acesso administrativo da plataforma'),
('PROFISSIONAL','Acesso destinado a profissionais de saúde'),
('PACIENTE','Acesso destinado a pacientes')
ON CONFLICT (nome) DO NOTHING;

INSERT INTO metodo_pagamento (nome) VALUES
('PIX'),('CARTAO_CREDITO'),('CARTAO_DEBITO'),('BOLETO'),('DINHEIRO')
ON CONFLICT (nome) DO NOTHING;

COMMIT;
