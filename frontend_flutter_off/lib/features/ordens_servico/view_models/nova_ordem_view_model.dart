import 'package:flutter/foundation.dart';

import '../../../core/network/api_service.dart';
import '../../../shared/formatters/money_input_formatter.dart';
import '../../../shared/validation/ordem_servico_validation.dart';
import '../../clientes/models/cliente.dart';
import '../../clientes/repositories/cliente_repository.dart';
import '../criar_ordem_servico_request.dart';
import '../models/criar_ordem_servico_response.dart';
import '../models/peca_item.dart';
import '../repositories/ordem_servico_repository.dart';

/// Rascunho da aplicação; TextEditingControllers apenas refletem a edição na View.
class NovaOrdemRascunho {
  int? clienteId;
  String placa = '';
  String marca = '';
  String modelo = '';
  String cor = '';
  String ano = '';
  String descricaoServico = '';
  String maoDeObra = '';
  bool necessitaRetorno = false;
  String prazoRetornoDias = '';
}

/// Coordena estado e use cases sem BuildContext, navegação ou SnackBar.
class NovaOrdemViewModel extends ChangeNotifier {
  NovaOrdemViewModel({
    required ClienteRepository clientes,
    required OrdemServicoRepository ordensServico,
  }) : _clientesRepository = clientes,
       _ordensServicoRepository = ordensServico;

  final ClienteRepository _clientesRepository;
  final OrdemServicoRepository _ordensServicoRepository;
  final rascunho = NovaOrdemRascunho();
  final List<PecaItem> _pecas = [];
  List<Cliente> _clientes = [];
  String _filtroCliente = '';
  ClienteRepository get clientesRepository => _clientesRepository;
  List<PecaItem> get pecas => List.unmodifiable(_pecas);
  List<Cliente> get clientes => List.unmodifiable(_clientes);
  String get filtroCliente => _filtroCliente;
  bool carregandoClientes = false;
  bool enviando = false;
  bool resultadoIncerto = false;
  bool _disposed = false;
  String? erroClientes;
  String? mensagem;
  Map<String, List<String>> erros = {};
  CriarOrdemServicoResponse? criada;

  bool get bloqueado => enviando || resultadoIncerto || criada != null;
  int get maoDeObraCentavos =>
      MoneyInputFormatter.cents(rascunho.maoDeObra) ?? 0;
  BigInt get totalPecasCentavos =>
      _pecas.fold(BigInt.zero, (total, item) => total + item.subtotalCentavos);
  BigInt get totalCentavos =>
      totalPecasCentavos + BigInt.from(maoDeObraCentavos);
  static final maxCentavos = BigInt.parse('9223372036854775807');

  List<Cliente> get clientesFiltrados {
    final termo = _filtroCliente.trim().toLowerCase();
    if (termo.isEmpty) return clientes;
    final digitosBusca = OrdemServicoValidation.normalizarDigitos(termo);
    return _clientes
        .where((cliente) {
          final nomeMatch = cliente.nome.toLowerCase().contains(termo);
          final docBruto = OrdemServicoValidation.normalizarDigitos(
            cliente.documento,
          );
          final docMatch =
              (digitosBusca.isNotEmpty && docBruto.contains(digitosBusca)) ||
              cliente.documento.toLowerCase().contains(termo);
          return nomeMatch || docMatch;
        })
        .toList(growable: false);
  }

  bool get buscaClienteSemResultado =>
      !carregandoClientes &&
      erroClientes == null &&
      _filtroCliente.trim().isNotEmpty &&
      clientesFiltrados.isEmpty;

  Cliente? get clienteSelecionado {
    for (final cliente in _clientes) {
      if (cliente.id == rascunho.clienteId) return cliente;
    }
    return null;
  }

  void atualizarFiltroCliente(String valor) {
    if (bloqueado) return;
    _filtroCliente = valor;
    final digitos = OrdemServicoValidation.normalizarDigitos(valor);
    if (digitos.length == 11 || digitos.length == 14) {
      final exatos = _clientes
          .where(
            (c) =>
                OrdemServicoValidation.normalizarDigitos(c.documento) ==
                digitos,
          )
          .toList();
      if (exatos.length == 1) {
        rascunho.clienteId = exatos.first.id;
        erros.remove('clienteId');
      }
    }
    notifyListeners();
  }

  void registrarClienteCriado(Cliente cliente) {
    if (bloqueado) return;
    if (cliente.ativo) {
      _clientes = [
        for (final existente in _clientes)
          if (existente.id != cliente.id) existente,
        cliente,
      ];
      rascunho.clienteId = cliente.id;
    }
    _filtroCliente = '';
    erros.remove('clienteId');
    mensagem = null;
    notifyListeners();
  }

  Future<void> carregarClientes() async {
    if (carregandoClientes || bloqueado) return;
    carregandoClientes = true;
    erroClientes = null;
    notifyListeners();
    try {
      final clientes = await _clientesRepository.listar();
      if (_disposed) return;
      _clientes = clientes.where((cliente) => cliente.ativo).toList();
      if (!_clientes.any((cliente) => cliente.id == rascunho.clienteId)) {
        rascunho.clienteId = null;
      }
    } on ApiException catch (error) {
      if (!_disposed) erroClientes = error.message;
    } finally {
      if (!_disposed) {
        carregandoClientes = false;
        notifyListeners();
      }
    }
  }

  void atualizar(void Function(NovaOrdemRascunho) editar) {
    if (bloqueado) return;
    editar(rascunho);
    erros = {};
    mensagem = null;
    notifyListeners();
  }

  void adicionarPeca(PecaItem peca) {
    if (bloqueado) return;
    _pecas.add(peca);
    erros = {};
    mensagem = null;
    notifyListeners();
  }

  void removerPeca(int indice) {
    if (bloqueado) return;
    _pecas.removeAt(indice);
    erros = {};
    mensagem = null;
    notifyListeners();
  }

  Map<String, List<String>> _validar() {
    final result = <String, List<String>>{};
    void campo(String nome, String? erro) {
      if (erro != null) result[nome] = [erro];
    }

    campo(
      'clienteId',
      clienteSelecionado == null ? 'Selecione um cliente' : null,
    );
    campo('placa', OrdemServicoValidation.placa(rascunho.placa));
    campo(
      'marca',
      OrdemServicoValidation.obrigatorio(rascunho.marca, maxLength: 100),
    );
    campo(
      'modelo',
      OrdemServicoValidation.obrigatorio(rascunho.modelo, maxLength: 100),
    );
    campo(
      'cor',
      OrdemServicoValidation.obrigatorio(rascunho.cor, maxLength: 50),
    );
    campo('ano', OrdemServicoValidation.ano(rascunho.ano));
    campo(
      'descricaoServico',
      OrdemServicoValidation.obrigatorio(rascunho.descricaoServico),
    );
    campo(
      'maoDeObraCentavos',
      MoneyInputFormatter.cents(rascunho.maoDeObra) == null
          ? 'Informe o valor da mão de obra'
          : null,
    );
    if (rascunho.necessitaRetorno) {
      campo(
        'prazoRetornoDias',
        OrdemServicoValidation.inteiroPositivo(rascunho.prazoRetornoDias),
      );
    }
    for (var i = 0; i < _pecas.length; i++) {
      final peca = _pecas[i];
      campo(
        'pecas[$i].custoUnitarioCentavos',
        peca.custoUnitarioCentavos < 0
            ? 'O custo da peça não pode ser negativo'
            : null,
      );
      campo(
        'pecas[$i].nome',
        OrdemServicoValidation.obrigatorio(peca.nome, maxLength: 200),
      );
      campo(
        'pecas[$i].quantidade',
        OrdemServicoValidation.inteiroPositivo(peca.quantidade.toString()),
      );
      campo(
        'pecas[$i].valorUnitarioCentavos',
        peca.valorUnitarioCentavos < 0 ? 'Valor inválido' : null,
      );
    }
    campo(
      'totalCentavos',
      totalCentavos > maxCentavos
          ? 'O total excede o limite de um inteiro de 64 bits.'
          : null,
    );
    return result;
  }

  CriarOrdemServicoRequest _montarRequest() => CriarOrdemServicoRequest(
    clienteId: rascunho.clienteId!,
    placa: OrdemServicoValidation.normalizarPlaca(rascunho.placa),
    marca: rascunho.marca.trim(),
    modelo: rascunho.modelo.trim(),
    cor: rascunho.cor.trim(),
    ano: int.tryParse(rascunho.ano),
    descricaoServico: rascunho.descricaoServico.trim(),
    pecas: _pecas
        .map(
          (p) => PecaRequest(
            nome: p.nome,
            quantidade: p.quantidade,
            valorUnitarioCentavos: p.valorUnitarioCentavos,
            custoUnitarioCentavos: p.custoUnitarioCentavos,
          ),
        )
        .toList(),
    maoDeObraCentavos: maoDeObraCentavos,
    necessitaRetorno: rascunho.necessitaRetorno,
    prazoRetornoDias: rascunho.necessitaRetorno
        ? int.parse(rascunho.prazoRetornoDias)
        : null,
  );

  Future<void> salvar() async {
    if (bloqueado) return;
    erros = _validar();
    mensagem = null;
    if (erros.isNotEmpty) {
      mensagem = 'Confira os campos informados.';
      notifyListeners();
      return;
    }
    final request = _montarRequest();
    enviando = true;
    notifyListeners();
    try {
      final response = await _ordensServicoRepository.criar(request);
      if (!_disposed) criada = response;
    } on ApiException catch (error) {
      if (!_disposed) {
        mensagem = error.message;
        erros = error.errors;
        resultadoIncerto = error.resultadoIncerto;
      }
    } finally {
      // Falha nunca limpa o rascunho. POST com resultado incerto não é reenviado.
      if (!_disposed) {
        enviando = false;
        notifyListeners();
      }
    }
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}
