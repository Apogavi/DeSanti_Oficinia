/// Validation imediata da View; o backend repete a validation e calcula o total oficial.
abstract final class OrdemServicoValidation {
  static String? obrigatorio(String? value, {int? maxLength}) {
    if (value == null || value.trim().isEmpty) return 'Campo obrigatório';
    if (maxLength != null && value.length > maxLength) {
      return 'Use até $maxLength caracteres';
    }
    return null;
  }

  static String normalizarDigitos(String? value) =>
      (value ?? '').replaceAll(RegExp(r'[^0-9]'), '');

  static String? cpfOuCnpj(String? value) {
    final digitos = normalizarDigitos(value);
    if (digitos.isEmpty) return 'Informe o CPF ou CNPJ';
    if (digitos.length != 11 && digitos.length != 14) {
      return 'Informe um CPF (11 dígitos) ou CNPJ (14 dígitos)';
    }
    return null;
  }

  static String? telefone(String? value) {
    final digitos = normalizarDigitos(value);
    if (digitos.isEmpty) return 'Informe o telefone';
    if (digitos.length != 10 && digitos.length != 11) {
      return 'Informe um telefone válido com DDD';
    }
    return null;
  }

  static String normalizarPlaca(String value) =>
      value.replaceAll(RegExp(r'[\s-]'), '').toUpperCase();

  static String? placa(String? value) =>
      RegExp(r'^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$')
          .hasMatch(normalizarPlaca(value ?? ''))
      ? null
      : 'Placa inválida';

  static String? ano(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final ano = int.tryParse(value);
    return ano != null && ano >= 1900 && ano <= DateTime.now().year + 1
        ? null
        : 'Informe um ano válido';
  }

  static String? inteiroPositivo(String? value) {
    final numero = int.tryParse(value ?? '');
    return numero != null && numero > 0 && numero <= 2147483647
        ? null
        : 'Informe um inteiro entre 1 e 2147483647';
  }
}
