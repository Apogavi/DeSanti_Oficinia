import 'dart:async';

import 'package:desanti_automotiva/core/network/api_service.dart';
import 'package:desanti_automotiva/features/ordens_servico/models/criar_ordem_servico_response.dart';
import 'package:desanti_automotiva/features/ordens_servico/models/peca_item.dart';
import 'package:desanti_automotiva/features/ordens_servico/view_models/nova_ordem_view_model.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fakes.dart';

void main() {
  for (final custo in <int>[0, 70]) {
    test('envia custo $custo preservando centavos e total', () async {
      final repo = OrdemServicoRepositoryFake();
      final vm = viewModelValido(repo);
      addTearDown(vm.dispose);
      await vm.carregarClientes();
      vm.adicionarPeca(
        PecaItem(
          nome: 'Peça teste',
          quantidade: 3,
          valorUnitarioCentavos: 101,
          custoUnitarioCentavos: custo,
        ),
      );
      expect(vm.totalCentavos, BigInt.from(12303));
      await vm.salvar();
      expect(
        repo.ultimoRequest!.toJson()['pecas'][0]['custoUnitarioCentavos'],
        custo,
      );
    });
  }
  test('custo negativo impede envio', () async {
    final repo = OrdemServicoRepositoryFake();
    final vm = viewModelValido(repo);
    addTearDown(vm.dispose);
    await vm.carregarClientes();
    vm.adicionarPeca(
      const PecaItem(
        nome: 'Peça teste',
        quantidade: 1,
        valorUnitarioCentavos: 101,
        custoUnitarioCentavos: -1,
      ),
    );
    await vm.salvar();
    expect(vm.erros, contains('pecas[0].custoUnitarioCentavos'));
    expect(repo.chamadas, 0);
  });

  test(
    'envia clienteId e placa normalizada; aceita o total oficial da API',
    () async {
      final repo = OrdemServicoRepositoryFake()
        ..responder = () async =>
            const CriarOrdemServicoResponse(id: 42, totalCentavos: 12303);
      final vm = viewModelValido(repo);
      addTearDown(vm.dispose);
      await vm.carregarClientes();
      vm.adicionarPeca(
        const PecaItem(
          nome: 'Peça teste',
          quantidade: 3,
          valorUnitarioCentavos: 101,
          custoUnitarioCentavos: 70,
        ),
      );
      expect(vm.totalCentavos, BigInt.from(12303));
      await vm.salvar();
      final json = repo.ultimoRequest!.toJson();
      expect(json['clienteId'], 1);
      expect(json['placa'], 'AAA0A00');
      expect(json['pecas'][0]['custoUnitarioCentavos'], 70);
      expect(json, isNot(contains('nomeCliente')));
      expect(json, isNot(contains('totalCentavos')));
      expect(vm.criada!.totalCentavos, 12303);
      expect(vm.criada!.id, 42);
      await vm.salvar();
      expect(repo.chamadas, 1);
    },
  );

  test(
    'falha de validation mantém rascunho, peças e errors do servidor',
    () async {
      final repo = OrdemServicoRepositoryFake()
        ..responder = () async => throw const ApiException(
          'Confira os campos informados.',
          errors: {
            'marca': ['Marca inválida'],
          },
        );
      final vm = viewModelValido(repo);
      addTearDown(vm.dispose);
      await vm.carregarClientes();
      vm.adicionarPeca(
        const PecaItem(
          nome: 'Peça teste',
          quantidade: 1,
          valorUnitarioCentavos: 100,
          custoUnitarioCentavos: 50,
        ),
      );
      await vm.salvar();
      expect(vm.rascunho.descricaoServico, 'Serviço de teste');
      expect(vm.rascunho.clienteId, 1);
      expect(vm.pecas, hasLength(1));
      expect(vm.erros['marca'], ['Marca inválida']);
      expect(vm.enviando, isFalse);
      expect(vm.bloqueado, isFalse);
      expect(vm.criada, isNull);
    },
  );

  test(
    'bloqueia envio simultâneo e resultado incerto sem repetir POST',
    () async {
      final pending = Completer<CriarOrdemServicoResponse>();
      final repo = OrdemServicoRepositoryFake()
        ..responder = () => pending.future;
      final vm = viewModelValido(repo);
      addTearDown(vm.dispose);
      await vm.carregarClientes();
      final envio = vm.salvar();
      expect(vm.enviando, isTrue);
      await vm.salvar();
      expect(repo.chamadas, 1);
      pending.completeError(
        const ApiException('Timeout', resultadoIncerto: true),
      );
      await envio;
      expect(vm.resultadoIncerto, isTrue);
      expect(vm.rascunho.marca, 'Marca Teste');
      await vm.salvar();
      expect(repo.chamadas, 1);
    },
  );

  test('validation impede envio sem cliente e com retorno sem prazo', () async {
    final repo = OrdemServicoRepositoryFake();
    final vm = viewModelValido(repo);
    addTearDown(vm.dispose);
    await vm.carregarClientes();
    vm.atualizar((r) {
      r.clienteId = null;
      r.necessitaRetorno = true;
    });
    await vm.salvar();
    expect(vm.erros.keys, containsAll(['clienteId', 'prazoRetornoDias']));
    expect(repo.chamadas, 0);
  });

  test(
    'desmarcar retorno envia prazo nulo; ano vazio permanece nulo',
    () async {
      final repo = OrdemServicoRepositoryFake();
      final vm = viewModelValido(repo);
      addTearDown(vm.dispose);
      await vm.carregarClientes();
      vm.atualizar((r) {
        r.necessitaRetorno = false;
        r.prazoRetornoDias = '30';
      });
      await vm.salvar();
      expect(repo.ultimoRequest!.prazoRetornoDias, isNull);
      expect(repo.ultimoRequest!.ano, isNull);
    },
  );

  test(
    'detecta overflow na prévia com BigInt sem enviar total inválido',
    () async {
      final repo = OrdemServicoRepositoryFake();
      final vm = viewModelValido(repo);
      addTearDown(vm.dispose);
      await vm.carregarClientes();
      vm.adicionarPeca(
        const PecaItem(
          nome: 'Peça teste',
          quantidade: 2147483647,
          valorUnitarioCentavos: 999999999999,
          custoUnitarioCentavos: 0,
        ),
      );
      expect(vm.totalCentavos, greaterThan(NovaOrdemViewModel.maxCentavos));
      await vm.salvar();
      expect(vm.erros, contains('totalCentavos'));
      expect(repo.chamadas, 0);
    },
  );

  test(
    'carregamento pode terminar depois do dispose sem notificar a View',
    () async {
      final pending = Completer<List<Never>>();
      final clientes = ClienteRepositoryFake()..carregar = () => pending.future;
      final vm = NovaOrdemViewModel(
        clientes: clientes,
        ordensServico: OrdemServicoRepositoryFake(),
      );
      final carregamento = vm.carregarClientes();
      vm.dispose();
      pending.complete([]);
      await carregamento;
    },
  );
}
