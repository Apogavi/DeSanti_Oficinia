import 'package:desanti_automotiva/features/ordens_servico/models/peca_item.dart';
import 'package:desanti_automotiva/features/ordens_servico/pecas_section.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  Future<void> abrirDialogo(
    WidgetTester tester,
    ValueChanged<PecaItem> onAdicionar,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: PecasSection(
            pecas: const [],
            onAdicionar: onAdicionar,
            onRemover: (_) {},
          ),
        ),
      ),
    );
    await tester.tap(find.text('Adicionar peça'));
    await tester.pumpAndSettle();
  }

  for (final custo in <String, int>{'0': 0, '10000': 10000}.entries) {
    testWidgets('adiciona peça com custo ${custo.value} sem alterar cobrança', (
      tester,
    ) async {
      PecaItem? adicionada;
      await abrirDialogo(tester, (item) => adicionada = item);
      final campos = find.byType(TextFormField);
      await tester.enterText(campos.at(0), 'Peça teste');
      await tester.enterText(campos.at(1), '2');
      await tester.enterText(campos.at(2), custo.key);
      await tester.enterText(campos.at(3), '15000');
      await tester.tap(find.text('Adicionar'));
      await tester.pumpAndSettle();
      expect(adicionada!.custoUnitarioCentavos, custo.value);
      expect(adicionada!.valorUnitarioCentavos, 15000);
      expect(adicionada!.subtotalCentavos, BigInt.from(30000));
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('custo vazio bloqueia a peça e mostra erro no campo', (
    tester,
  ) async {
    PecaItem? adicionada;
    await abrirDialogo(tester, (item) => adicionada = item);
    final campos = find.byType(TextFormField);
    await tester.enterText(campos.at(0), 'Peça teste');
    await tester.enterText(campos.at(3), '15000');
    await tester.tap(find.text('Adicionar'));
    await tester.pumpAndSettle();
    expect(adicionada, isNull);
    expect(find.text('Informe o custo da peça'), findsOneWidget);
    // O diálogo continua aberto preservando o que já foi digitado.
    expect(find.text('Peça teste'), findsOneWidget);
  });

  testWidgets('apagar custo preenchido volta a exigir o custo', (tester) async {
    PecaItem? adicionada;
    await abrirDialogo(tester, (item) => adicionada = item);
    final campos = find.byType(TextFormField);
    await tester.enterText(campos.at(0), 'Peça teste');
    await tester.enterText(campos.at(2), '10000');
    await tester.enterText(campos.at(2), '');
    await tester.enterText(campos.at(3), '15000');
    await tester.tap(find.text('Adicionar'));
    await tester.pumpAndSettle();
    expect(adicionada, isNull);
    expect(find.text('Informe o custo da peça'), findsOneWidget);
  });

  testWidgets('rótulo indica custo obrigatório sem orientar a deixar vazio', (
    tester,
  ) async {
    await abrirDialogo(tester, (_) {});
    expect(find.text('Custo da peça (R\$) *'), findsOneWidget);
    expect(find.textContaining('Deixe vazio'), findsNothing);
  });

  testWidgets('card exibe o custo por unidade da peça', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: PecasSection(
            pecas: const [
              PecaItem(
                nome: 'Bomba d\'água',
                quantidade: 1,
                valorUnitarioCentavos: 12000,
                custoUnitarioCentavos: 5000,
              ),
            ],
            onAdicionar: (_) {},
            onRemover: (_) {},
          ),
        ),
      ),
    );
    expect(find.text('Custo por unidade: R\$ 50,00'), findsOneWidget);
    expect(find.textContaining('não informado'), findsNothing);
  });
}
