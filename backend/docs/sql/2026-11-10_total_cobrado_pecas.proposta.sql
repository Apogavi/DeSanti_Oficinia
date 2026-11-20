-- Aplicar somente no desanti_dev local após autorização.
-- Preserva o preço unitário histórico; NULL usa quantidade * preço unitário.
ALTER TABLE PecasOrdemServico ADD COLUMN ValorTotalCentavos bigint NULL;
