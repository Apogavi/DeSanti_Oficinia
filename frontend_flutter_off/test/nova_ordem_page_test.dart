import 'package:desanti_automotiva/core/network/api_service.dart';
import 'package:desanti_automotiva/features/ordens_servico/nova_ordem_page.dart';
import 'package:desanti_automotiva/features/ordens_servico/models/peca_item.dart';
import 'package:desanti_automotiva/features/ordens_servico/view_models/nova_ordem_view_model.dart';
import 'package:desanti_automotiva/main.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fakes.dart';

void main() {
  testWidgets('valida cliente no topo ao salvar no fim da página', (
    tester,
  ) async {
    final repo = OrdemServicoRepositoryFake();
    final vm = NovaOrdemViewModel(
      clientes: ClienteRepositoryFake(),
      ordensServico: repo,
    );
    await tester.pumpWidget(MaterialApp(home: NovaOrdemPage(viewModel: vm)));
    await tester.pumpAndSettle();
    final salvar = find.text('Salvar ordem de serviço');
    await tester.ensureVisible(salvar);
    await tester.tap(salvar);
    await tester.pumpAndSettle();
    expect(find.text('Selecione um cliente'), findsOneWidget);
    expect(repo.chamadas, 0);
    expect(tester.takeException(), isNull);
  });

  testWidgets('erro mantém controllers e seleção do cliente', (tester) async {
    final repo = OrdemServicoRepositoryFake()
      ..responder = () async =>
          throw const ApiException('Confira os campos informados.');
    final vm = viewModelValido(repo);
    await tester.pumpWidget(MaterialApp(home: NovaOrdemPage(viewModel: vm)));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Salvar ordem de serviço'));
    await tester.tap(find.text('Salvar ordem de serviço'));
    await tester.pumpAndSettle();
    expect(repo.chamadas, 1);
    expect(find.text('Confira os campos informados.'), findsOneWidget);
    expect(find.text('Marca Teste'), findsOneWidget);
    expect(find.text('Cliente Teste'), findsOneWidget);
    expect(find.text('Serviço de teste'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'conclui fluxo e mostra confirmação com total retornado pela API',
    (tester) async {
      final repo = OrdemServicoRepositoryFake();
      await tester.pumpWidget(
        MaterialApp(
          home: InicioPage(
            clientes: ClienteRepositoryFake(),
            ordensServico: repo,
          ),
        ),
      );
      await tester.tap(find.text('Nova ordem de serviço'));
      await tester.pumpAndSettle();
      await tester.tap(find.byType(DropdownButtonFormField<int>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cliente Teste').last);
      await tester.pumpAndSettle();

      Future<void> preencher(String label, String value) async {
        // Localiza o TextField interno do FormField pelo label da UI.
        final input = find.byWidgetPredicate(
          (widget) =>
              widget is TextField && widget.decoration?.labelText == label,
        );
        await tester.ensureVisible(input);
        await tester.enterText(input, value);
        await tester.pumpAndSettle();
      }

      await preencher('Placa *', 'AAA0A00');
      await preencher('Marca *', 'Marca Teste');
      await preencher('Modelo *', 'Modelo Teste');
      await preencher('Cor *', 'Azul');
      await preencher('Descrição do serviço *', 'Serviço de teste');
      await preencher('Mão de obra (R\$) *', '12000');
      tester.testTextInput.hide();
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Salvar ordem de serviço'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Salvar ordem de serviço'));
      await tester.pumpAndSettle();
      expect(repo.ultimoRequest!.clienteId, 1);
      expect(repo.ultimoRequest!.maoDeObraCentavos, 12000);
      expect(
        find.text('Ordem de serviço #42 salva com sucesso.'),
        findsOneWidget,
      );
      expect(find.text('Total confirmado: R\$ 120,00'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('formulário funciona em janela estreita', (tester) async {
    tester.view.physicalSize = const Size(380, 760);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final vm = viewModelValido(OrdemServicoRepositoryFake());
    vm.adicionarPeca(
      const PecaItem(
        nome: 'Peça de teste com nome extenso',
        quantidade: 2147483647,
        valorUnitarioCentavos: 999999999999,
        custoUnitarioCentavos: 0,
      ),
    );
    await tester.pumpWidget(MaterialApp(home: NovaOrdemPage(viewModel: vm)));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Adicionar peça'));
    await tester.tap(find.text('Adicionar peça'));
    await tester.pumpAndSettle();
    expect(find.text('Adicionar peça'), findsNWidgets(2));
    expect(tester.takeException(), isNull);
    await tester.tap(find.text('Cancelar'));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Salvar ordem de serviço'));
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'alerta quando CPF/CNPJ ou nome não é encontrado e cadastra selecionando o novo cliente',
    (tester) async {
      final clientes = ClienteRepositoryFake();
      final ordens = OrdemServicoRepositoryFake();
      final vm = NovaOrdemViewModel(clientes: clientes, ordensServico: ordens);
      await tester.pumpWidget(MaterialApp(home: NovaOrdemPage(viewModel: vm)));
      await tester.pumpAndSettle();

      final campoBusca = find.byWidgetPredicate(
        (widget) =>
            widget is TextField &&
            widget.decoration?.labelText == 'Buscar por nome ou CPF/CNPJ',
      );
      await tester.enterText(campoBusca, '12345678901');
      await tester.pumpAndSettle();

      expect(
        find.text('Cliente não encontrado para "12345678901".'),
        findsOneWidget,
      );
      expect(
        find.text('Cadastrar cliente antes de prosseguir'),
        findsOneWidget,
      );

      await tester.tap(find.text('Cadastrar cliente antes de prosseguir'));
      await tester.pumpAndSettle();

      // O CPF digitado na busca já vem pré-preenchido e formatado no modal.
      expect(find.text('123.456.789-01'), findsOneWidget);

      await tester.enterText(
        find.byWidgetPredicate(
          (w) => w is TextField && w.decoration?.labelText == 'Nome completo *',
        ),
        'Carlos Mecânica',
      );
      await tester.enterText(
        find.byWidgetPredicate(
          (w) =>
              w is TextField &&
              w.decoration?.labelText == 'Telefone / WhatsApp *',
        ),
        '11987654321',
      );
      await tester.ensureVisible(find.text('Salvar cliente'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Salvar cliente'));
      await tester.pumpAndSettle();

      expect(clientes.chamadasCriar, 1);
      expect(vm.rascunho.clienteId, 99);
      expect(find.text('Nome: Carlos Mecânica'), findsOneWidget);
      expect(find.text('Documento: 123.456.789-01'), findsOneWidget);
      expect(find.text('Telefone: (11) 98765-4321'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'menu suspenso Liquid Glass expande na horizontal e abre aba Clientes (cadastro e consulta)',
    (tester) async {
      final clientes = ClienteRepositoryFake();
      await tester.pumpWidget(
        MaterialApp(
          home: InicioPage(
            clientes: clientes,
            ordensServico: OrdemServicoRepositoryFake(),
          ),
        ),
      );

      // Menu inicia recolhido (opção Clientes oculta)
      expect(find.text('Clientes'), findsNothing);

      // Clica no botão sanduíche (3 traços) estilo Apple Liquid Glass
      await tester.tap(find.byKey(const ValueKey('menu_sanduiche_apple')));
      await tester.pumpAndSettle();

      // Expande na horizontal exibindo as opções
      expect(find.text('Clientes'), findsOneWidget);
      await tester.tap(find.text('Clientes'));
      await tester.pumpAndSettle();

      // Estamos na aba dedicada de Clientes (Cadastro + Consulta)
      expect(find.text('Cadastrar cliente'), findsOneWidget);
      expect(find.text('Consulta de clientes'), findsOneWidget);
      expect(find.text('Cliente Teste'), findsOneWidget);

      final campoNome = find.byWidgetPredicate(
        (w) => w is TextField && w.decoration?.labelText == 'Nome completo *',
      );
      final campoDocumento = find.byWidgetPredicate(
        (w) => w is TextField && w.decoration?.labelText == 'CPF ou CNPJ *',
      );
      final campoTelefone = find.byWidgetPredicate(
        (w) =>
            w is TextField &&
            w.decoration?.labelText == 'Telefone / WhatsApp *',
      );

      // Foca no Nome e verifica que TAB avança para CPF/CNPJ e depois Telefone (sem pular para Consulta)
      await tester.tap(campoNome);
      await tester.pumpAndSettle();
      await tester.enterText(campoNome, 'Transportadora ABC');

      await tester.sendKeyEvent(LogicalKeyboardKey.tab);
      await tester.pumpAndSettle();
      expect(
        tester.widget<TextField>(campoDocumento).focusNode?.hasFocus,
        isTrue,
      );
      await tester.enterText(campoDocumento, '12345678000199');

      await tester.sendKeyEvent(LogicalKeyboardKey.tab);
      await tester.pumpAndSettle();
      expect(
        tester.widget<TextField>(campoTelefone).focusNode?.hasFocus,
        isTrue,
      );
      await tester.enterText(campoTelefone, '1134567890');
      await tester.ensureVisible(find.text('Salvar cliente'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Salvar cliente'));
      await tester.pumpAndSettle();

      expect(clientes.chamadasCriar, 1);
      expect(clientes.ultimoCriado!.documento, '12.345.678/0001-99');
      expect(
        find.text('Cliente Transportadora ABC cadastrado com sucesso.'),
        findsOneWidget,
      );
      expect(find.text('Transportadora ABC'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}
