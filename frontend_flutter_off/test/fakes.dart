import 'package:desanti_automotiva/features/clientes/criar_cliente_request.dart';
import 'package:desanti_automotiva/features/clientes/models/cliente.dart';
import 'package:desanti_automotiva/features/clientes/repositories/cliente_repository.dart';
import 'package:desanti_automotiva/features/ordens_servico/criar_ordem_servico_request.dart';
import 'package:desanti_automotiva/features/ordens_servico/models/criar_ordem_servico_response.dart';
import 'package:desanti_automotiva/features/ordens_servico/repositories/ordem_servico_repository.dart';
import 'package:desanti_automotiva/features/ordens_servico/view_models/nova_ordem_view_model.dart';
import 'package:desanti_automotiva/shared/validation/ordem_servico_validation.dart';

const clienteTeste = Cliente(
  id: 1,
  nome: 'Cliente Teste',
  documento: '00000000000',
  telefone: '00000000000',
  ativo: true,
);

class ClienteRepositoryFake implements ClienteRepository {
  Future<List<Cliente>> Function()? carregar;
  Future<Cliente> Function(CriarClienteRequest)? aoCriar;
  CriarClienteRequest? ultimoCriado;
  int chamadasCriar = 0;

  @override
  Future<List<Cliente>> listar() async =>
      carregar == null ? [clienteTeste] : carregar!();

  @override
  Future<Cliente> criar(CriarClienteRequest pedido) async {
    chamadasCriar++;
    ultimoCriado = pedido;
    if (aoCriar != null) return aoCriar!(pedido);
    return Cliente(
      id: 99,
      nome: pedido.nome.trim(),
      documento: OrdemServicoValidation.normalizarDigitos(pedido.documento),
      telefone: OrdemServicoValidation.normalizarDigitos(pedido.telefone),
      ativo: true,
    );
  }
}

class OrdemServicoRepositoryFake implements OrdemServicoRepository {
  int chamadas = 0;
  CriarOrdemServicoRequest? ultimoRequest;
  Future<CriarOrdemServicoResponse> Function()? responder;

  @override
  Future<CriarOrdemServicoResponse> criar(
    CriarOrdemServicoRequest request,
  ) async {
    chamadas++;
    ultimoRequest = request;
    return responder == null
        ? const CriarOrdemServicoResponse(id: 42, totalCentavos: 12000)
        : responder!();
  }
}

NovaOrdemViewModel viewModelValido(OrdemServicoRepositoryFake ordens) {
  final vm = NovaOrdemViewModel(
    clientes: ClienteRepositoryFake(),
    ordensServico: ordens,
  );
  vm.atualizar((r) {
    r.clienteId = 1;
    r.placa = 'aaa-0a00';
    r.marca = 'Marca Teste';
    r.modelo = 'Modelo Teste';
    r.cor = 'Azul';
    r.descricaoServico = 'Serviço de teste';
    r.maoDeObra = '120,00';
  });
  return vm;
}
