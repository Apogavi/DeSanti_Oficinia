-- Bootstrap local gerado do modelo EF Core em 29/10/2026.
-- Atualizado em 04/11/2026 para incluir custo unitário opcional das peças.
-- A imagem MySQL o executa somente na primeira inicialização de um volume vazio.
-- Não é migration incremental. Comparar com o schema existente antes de qualquer aplicação.
-- SQL do modelo com ENGINE=InnoDB explícito para preservar transactions e foreign keys.

CREATE TABLE `Clientes` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Nome` varchar(200) NOT NULL,
    `Email` varchar(200) NOT NULL,
    `Telefone` varchar(20) NOT NULL,
    `Documento` varchar(20) NULL,
    `DataCriacao` datetime(6) NOT NULL,
    `DataAlteracao` datetime(6) NULL,
    `Ativo` tinyint(1) NOT NULL DEFAULT TRUE,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB;


CREATE TABLE `OrdensServico` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ClienteId` bigint NOT NULL,
    `NomeCliente` varchar(200) NOT NULL,
    `DocumentoCliente` varchar(20) NOT NULL,
    `TelefoneCliente` varchar(20) NOT NULL,
    `Placa` varchar(7) NOT NULL,
    `Marca` varchar(100) NOT NULL,
    `Modelo` varchar(100) NOT NULL,
    `Cor` varchar(50) NOT NULL,
    `Ano` int NULL,
    `DescricaoServico` text NOT NULL,
    `MaoDeObraCentavos` bigint NOT NULL,
    `TotalCentavos` bigint NOT NULL,
    `NecessitaRetorno` tinyint(1) NOT NULL,
    `PrazoRetornoDias` int NULL,
    `DataCriacaoUtc` datetime(6) NOT NULL,
    `DataConclusaoUtc` datetime(6) NULL,
    `DataPrevistaRetornoUtc` datetime(6) NULL,
    `Status` int NOT NULL DEFAULT 1,
    `IdempotencyKey` varchar(64) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OrdensServico_Clientes_ClienteId` FOREIGN KEY (`ClienteId`) REFERENCES `Clientes` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB;


CREATE TABLE `PecasOrdemServico` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `OrdemServicoId` bigint NOT NULL,
    `Nome` varchar(200) NOT NULL,
    `Quantidade` int NOT NULL,
    `ValorUnitarioCentavos` bigint NOT NULL,
    `ValorTotalCentavos` bigint NULL,
    `CustoUnitarioCentavos` bigint NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PecasOrdemServico_OrdensServico_OrdemServicoId` FOREIGN KEY (`OrdemServicoId`) REFERENCES `OrdensServico` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB;


CREATE UNIQUE INDEX `idx_clientes_documento` ON `Clientes` (`Documento`);


CREATE INDEX `idx_clientes_nome` ON `Clientes` (`Nome`);


CREATE INDEX `IX_OrdensServico_ClienteId` ON `OrdensServico` (`ClienteId`);


CREATE INDEX `IX_OrdensServico_Placa` ON `OrdensServico` (`Placa`);


CREATE INDEX `IX_OrdensServico_Status` ON `OrdensServico` (`Status`);


CREATE UNIQUE INDEX `idx_ordens_servico_idempotency_key` ON `OrdensServico` (`IdempotencyKey`);


CREATE INDEX `IX_PecasOrdemServico_OrdemServicoId` ON `PecasOrdemServico` (`OrdemServicoId`);


CREATE TABLE `Usuarios` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `NomeUsuario` varchar(64) NOT NULL,
    `Nome` varchar(120) NOT NULL,
    `Email` varchar(200) NULL,
    `PasswordHash` varchar(256) NOT NULL,
    `Role` varchar(32) NOT NULL,
    `Ativo` tinyint(1) NOT NULL DEFAULT TRUE,
    `DataCriacaoUtc` datetime(6) NOT NULL,
    `DataAlteracaoUtc` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `idx_usuarios_nome_usuario` (`NomeUsuario`)
) ENGINE=InnoDB;


