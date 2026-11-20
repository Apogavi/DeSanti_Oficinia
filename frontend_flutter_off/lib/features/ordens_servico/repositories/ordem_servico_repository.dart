import '../../../core/network/api_service.dart';
import '../criar_ordem_servico_request.dart';
import '../models/criar_ordem_servico_response.dart';

abstract interface class OrdemServicoRepository {
  Future<CriarOrdemServicoResponse> criar(CriarOrdemServicoRequest request);
}

class ApiOrdemServicoRepository implements OrdemServicoRepository {
  ApiOrdemServicoRepository(this._api);

  final ApiService _api;

  @override
  Future<CriarOrdemServicoResponse> criar(
    CriarOrdemServicoRequest request,
  ) async {
    final data = await _api.post('api/v1/ordens-servico', request.toJson());
    try {
      return CriarOrdemServicoResponse.fromJson(data as Map<String, dynamic>);
    } on Object {
      // Um 201 incompatível também pode ter gravado a OS; não reenviar.
      throw const ApiException(
        'A API não confirmou o ID e o total da OS.',
        resultadoIncerto: true,
      );
    }
  }
}
