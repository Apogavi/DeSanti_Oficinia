/// Item do rascunho. A prévia usa BigInt para detectar overflow antes do envio.
class PecaItem {
  const PecaItem({
    required this.nome,
    required this.quantidade,
    required this.valorUnitarioCentavos,
    required this.custoUnitarioCentavos,
  });

  final String nome;
  final int quantidade;
  final int valorUnitarioCentavos;

  /// Custo unitário pago ao fornecedor, obrigatório desde 10/11/2026.
  /// Zero indica custo conhecido de zero; não existe custo desconhecido no rascunho.
  final int custoUnitarioCentavos;

  BigInt get subtotalCentavos =>
      BigInt.from(quantidade) * BigInt.from(valorUnitarioCentavos);
}
