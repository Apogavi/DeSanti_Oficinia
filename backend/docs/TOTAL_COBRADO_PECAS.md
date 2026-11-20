# Cobrança total por item — 10/11/2026

O request de peça aceita valorTotalCentavos (Int64?, não negativo). Quando
informado, esse é o valor cobrado pelo item inteiro, independentemente da
quantidade. custoUnitarioCentavos continua unitário. Para compatibilidade,
valorUnitarioCentavos permanece no contrato; novas telas enviam zero nesse
campo legado e o total explícito. Na ausência de total, permanece o cálculo
histórico quantidade × valorUnitarioCentavos.

A coluna ValorTotalCentavos bigint NULL foi adicionada no MySQL local desanti_dev
com autorização de Diogo; registros antigos permanecem NULL. O SQL em
sql/2026-11-10_total_cobrado_pecas.proposta.sql registra a operação e não deve
ser reaplicado sem verificar metadata. Nenhum índice unique de cliente foi
aplicado nesta etapa.

Verificação: 53 tests backend passaram e build Angular passou. Teste real via
POST com três peças e total 10000 centavos preservou o valor exato no banco;
histórico e rollback também passaram. Registros fictícios removidos.
