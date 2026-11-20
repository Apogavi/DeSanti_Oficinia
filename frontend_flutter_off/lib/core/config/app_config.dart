/// Endereço público da API. Credenciais do MySQL pertencem somente ao backend.
abstract final class AppConfig {
  static const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5085',
  );
}
