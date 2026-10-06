CREATE TABLE IF NOT EXISTS ordens_servico (
    id BIGINT NOT NULL AUTO_INCREMENT,
    nome_cliente VARCHAR(200) NOT NULL,
    cpf CHAR(11) NOT NULL,
    telefone CHAR(11) NOT NULL,
    placa CHAR(7) NOT NULL,
    marca VARCHAR(100) NOT NULL,
    modelo VARCHAR(100) NOT NULL,
    cor VARCHAR(50) NOT NULL,
    ano SMALLINT NULL,
    descricao_servico TEXT NOT NULL,
    mao_de_obra_centavos BIGINT NOT NULL,
    total_centavos BIGINT NOT NULL,
    necessita_retorno BOOLEAN NOT NULL,
    prazo_retorno_dias INT NULL,
    criada_em_utc DATETIME(6) NOT NULL,
    concluida_em_utc DATETIME(6) NULL,
    data_prevista_retorno DATE NULL,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS pecas_ordem_servico (
    id BIGINT NOT NULL AUTO_INCREMENT,
    ordem_servico_id BIGINT NOT NULL,
    nome VARCHAR(200) NOT NULL,
    quantidade INT NOT NULL,
    valor_unitario_centavos BIGINT NOT NULL,
    PRIMARY KEY (id),
    INDEX idx_pecas_ordem_servico (ordem_servico_id),
    CONSTRAINT fk_pecas_ordem_servico
        FOREIGN KEY (ordem_servico_id)
        REFERENCES ordens_servico (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;