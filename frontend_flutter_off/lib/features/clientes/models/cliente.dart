class Cliente {
  const Cliente({
    required this.id,
    required this.nome,
    required this.documento,
    required this.telefone,
    required this.ativo,
  });

  final int id;
  final String nome;
  final String documento;
  final String telefone;
  final bool ativo;

  factory Cliente.fromJson(Map<String, dynamic> json) => Cliente(
    id: json['id'] as int,
    nome: json['nome'] as String,
    documento: json['documento'] as String? ?? '',
    telefone: json['telefone'] as String? ?? '',
    ativo: json['ativo'] as bool,
  );
}
