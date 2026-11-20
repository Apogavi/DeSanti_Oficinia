-- Aplicada com autorização em 04/11/2026 somente no MySQL local desanti_dev.
-- Outros ambientes exigem revisão e autorização. Não reaplicar no banco local.
-- Confirmar database selecionado e ausência da coluna antes de aplicar.
-- MySQL: ALTER TABLE faz commit implícito. Não reaplicar nem remover o volume.
-- Linhas existentes recebem NULL (custo desconhecido), preservando os preços.
ALTER TABLE `PecasOrdemServico`
    ADD COLUMN `CustoUnitarioCentavos` bigint NULL;
