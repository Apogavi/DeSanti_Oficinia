class CriarOrdemServicoResponse {
  const CriarOrdemServicoResponse({
    required this.id,
    required this.totalCentavos,
  });

  final int id;
  final int totalCentavos;

  factory CriarOrdemServicoResponse.fromJson(Map<String, dynamic> json) {
    final id = json['id'] as int;
    final total = json['totalCentavos'] as int;
    if (id <= 0 || total < 0) throw const FormatException('OS inválida.');
    return CriarOrdemServicoResponse(id: id, totalCentavos: total);
  }
}
