class PecaRequest {
  const PecaRequest({
    required this.nome,
    required this.quantidade,
    required this.valorUnitarioCentavos,
  });

  final String nome;
  final int quantidade;
  final int valorUnitarioCentavos;

  Map<String, dynamic> toJson() => {
    'nome': nome,
    'quantidade': quantidade,
    'valorUnitarioCentavos': valorUnitarioCentavos,
  };
}

class CriarOrdemServicoRequest {
  const CriarOrdemServicoRequest({
    required this.nomeCliente,
    required this.cpf,
    required this.telefone,
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

  final String nomeCliente;
  final String cpf;
  final String telefone;
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
    'nomeCliente': nomeCliente,
    'cpf': cpf,
    'telefone': telefone,
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
