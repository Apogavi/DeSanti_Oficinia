class PecaRequest {
  const PecaRequest({
    required this.nome,
    required this.quantidade,
    required this.valorUnitarioCentavos,
    required this.custoUnitarioCentavos,
  });

  final String nome;
  final int quantidade;
  final int valorUnitarioCentavos;

  /// Custo unitário obrigatório; o backend responde 400 quando ausente.
  final int custoUnitarioCentavos;

  Map<String, dynamic> toJson() => {
    'nome': nome,
    'quantidade': quantidade,
    'valorUnitarioCentavos': valorUnitarioCentavos,
    'custoUnitarioCentavos': custoUnitarioCentavos,
  };
}

/// Payload de criação. Não envia total nem cópia dos dados do cliente.
class CriarOrdemServicoRequest {
  const CriarOrdemServicoRequest({
    required this.clienteId,
    required this.placa,
    required this.marca,
    required this.modelo,
    required this.cor,
    required this.ano,
    required this.descricaoServico,
    required this.pecas,
    required this.maoDeObraCentavos,
    required this.necessitaRetorno,
    required this.prazoRetornoDias,
  });

  final int clienteId;
  final String placa;
  final String marca;
  final String modelo;
  final String cor;
  final int? ano;
  final String descricaoServico;
  final List<PecaRequest> pecas;
  final int maoDeObraCentavos;
  final bool necessitaRetorno;
  final int? prazoRetornoDias;

  Map<String, dynamic> toJson() => {
    'clienteId': clienteId,
    'placa': placa,
    'marca': marca,
    'modelo': modelo,
    'cor': cor,
    'ano': ano,
    'descricaoServico': descricaoServico,
    'pecas': pecas.map((peca) => peca.toJson()).toList(),
    'maoDeObraCentavos': maoDeObraCentavos,
    'necessitaRetorno': necessitaRetorno,
    'prazoRetornoDias': prazoRetornoDias,
  };
}
