import '../../../core/network/api_service.dart';
import '../criar_cliente_request.dart';
import '../models/cliente.dart';

abstract interface class ClienteRepository {
  Future<List<Cliente>> listar();
  Future<Cliente> criar(CriarClienteRequest pedido);
}

class ApiClienteRepository implements ClienteRepository {
  ApiClienteRepository(this._api);

  final ApiService _api;

  @override
  Future<List<Cliente>> listar() async {
    final data = await _api.get('api/v1/clientes');
    try {
      return (data as List)
          .map((item) => Cliente.fromJson(item as Map<String, dynamic>))
          .toList();
    } on Object {
      throw const ApiException(
        'A API retornou uma lista de clientes inválida.',
      );
    }
  }

  @override
  Future<Cliente> criar(CriarClienteRequest pedido) async {
    final data = await _api.post('api/v1/clientes', pedido.toJson());
    try {
      return Cliente.fromJson(data as Map<String, dynamic>);
    } on Object {
      throw const ApiException(
        'A API retornou uma resposta de cliente inválida.',
      );
    }
  }
}
