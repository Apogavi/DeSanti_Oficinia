import '../../shared/validation/ordem_servico_validation.dart';

class CriarClienteRequest {
  const CriarClienteRequest({
    required this.nome,
    required this.documento,
    required this.telefone,
  });

  final String nome;
  final String documento;
  final String telefone;

  Map<String, dynamic> toJson() => {
    'nome': nome.trim(),
    'documento': OrdemServicoValidation.normalizarDigitos(documento),
    'telefone': OrdemServicoValidation.normalizarDigitos(telefone),
    // Envia email vazio para manter compatibilidade com a coluna NOT NULL sem migration.
    'email': '',
  };
}
