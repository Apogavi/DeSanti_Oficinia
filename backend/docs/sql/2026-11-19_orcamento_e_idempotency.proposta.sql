-- Proposta incremental — 19/11/2026 [Antigravity / Gemini 4 Neon]
-- Adiciona suporte a Status (0=Orcamento, 1=Aberta, 2=Concluida) e IdempotencyKey em OrdensServico.
-- NÃO executar automaticamente. Conferir ambiente, backup e metadata antes de aplicar.

-- ============================================================================
-- OPÇÃO A — MySQL 8.x (desanti_dev local ou Oracle Cloud Free VPS com MySQL)
-- ============================================================================
ALTER TABLE `OrdensServico`
    ADD COLUMN `Status` int NOT NULL DEFAULT 1,
    ADD COLUMN `IdempotencyKey` varchar(64) NULL;

-- Sincroniza registros já concluídos anteriormente para Status = 2 (Concluida).
UPDATE `OrdensServico`
SET `Status` = 2
WHERE `DataConclusaoUtc` IS NOT NULL AND `Status` = 1;

CREATE INDEX `IX_OrdensServico_Status` ON `OrdensServico` (`Status`);
CREATE UNIQUE INDEX `idx_ordens_servico_idempotency_key` ON `OrdensServico` (`IdempotencyKey`);


-- ============================================================================
-- OPÇÃO B — PostgreSQL / Supabase (caso decidam migrar o banco para Supabase)
-- ============================================================================
-- ALTER TABLE "OrdensServico"
--     ADD COLUMN "Status" integer NOT NULL DEFAULT 1,
--     ADD COLUMN "IdempotencyKey" varchar(64) NULL;
--
-- UPDATE "OrdensServico"
-- SET "Status" = 2
-- WHERE "DataConclusaoUtc" IS NOT NULL AND "Status" = 1;
--
-- CREATE INDEX "IX_OrdensServico_Status" ON "OrdensServico" ("Status");
-- CREATE UNIQUE INDEX "idx_ordens_servico_idempotency_key" ON "OrdensServico" ("IdempotencyKey");
