import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../shared/formatters/money_input_formatter.dart';
import '../../shared/validation/ordem_servico_validation.dart';
import '../../theme/app_theme.dart';
import 'models/peca_item.dart';

class PecasSection extends StatelessWidget {
  const PecasSection({
    super.key,
    required this.pecas,
    required this.onAdicionar,
    required this.onRemover,
  });

  final List<PecaItem> pecas;
  final ValueChanged<PecaItem> onAdicionar;
  final ValueChanged<int> onRemover;

  int? _centavos(String? valor) => MoneyInputFormatter.cents(valor);
  String _reais(BigInt centavos) =>
      'R\$ ${MoneyInputFormatter.formatBigIntCents(centavos)}';

  Future<void> _abrirCadastro(BuildContext context) async {
    final chave = GlobalKey<FormState>();
    var nome = '';
    var quantidade = 1;
    var valorUnitario = 0;
    var custoUnitario = 0;

    final item = await showDialog<PecaItem>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Adicionar peça'),
        content: SizedBox(
          width: 420,
          child: Form(
            key: chave,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  TextFormField(
                    decoration: const InputDecoration(labelText: 'Peça *'),
                    validator: (valor) => OrdemServicoValidation.obrigatorio(
                      valor,
                      maxLength: 200,
                    ),
                    onSaved: (valor) => nome = valor!.trim(),
                  ),
                  const SizedBox(height: 14),
                  TextFormField(
                    initialValue: '1',
                    decoration: const InputDecoration(
                      labelText: 'Quantidade *',
                    ),
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    validator: OrdemServicoValidation.inteiroPositivo,
                    onSaved: (valor) => quantidade = int.parse(valor!),
                  ),
                  const SizedBox(height: 14),
                  // Custo obrigatório desde 10/11/2026: vazio é erro, "0,00" é custo zero.
                  TextFormField(
                    decoration: const InputDecoration(
                      labelText: 'Custo da peça (R\$) *',
                      helperText: 'Valor pago ao fornecedor, por unidade',
                    ),
                    keyboardType: TextInputType.number,
                    inputFormatters: [const MoneyInputFormatter()],
                    validator: (valor) => _centavos(valor) == null
                        ? 'Informe o custo da peça'
                        : null,
                    onSaved: (valor) => custoUnitario = _centavos(valor)!,
                  ),
                  const SizedBox(height: 14),
                  TextFormField(
                    decoration: const InputDecoration(
                      labelText: 'Valor cobrado do cliente (R\$) *',
                      helperText: 'Valor por unidade',
                    ),
                    keyboardType: TextInputType.number,
                    inputFormatters: [const MoneyInputFormatter()],
                    validator: (valor) => _centavos(valor) == null
                        ? 'Informe o valor cobrado'
                        : null,
                    onSaved: (valor) => valorUnitario = _centavos(valor)!,
                  ),
                ],
              ),
            ),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () {
              if (!chave.currentState!.validate()) return;
              chave.currentState!.save();
              Navigator.of(dialogContext).pop(
                PecaItem(
                  nome: nome,
                  quantidade: quantidade,
                  valorUnitarioCentavos: valorUnitario,
                  custoUnitarioCentavos: custoUnitario,
                ),
              );
            },
            child: const Text('Adicionar'),
          ),
        ],
      ),
    );

    if (item != null && context.mounted) onAdicionar(item);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final total = pecas.fold<BigInt>(
      BigInt.zero,
      (soma, item) => soma + item.subtotalCentavos,
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Peças',
          style: theme.textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 10),
        if (pecas.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 6),
            child: Text(
              'Nenhuma peça adicionada.',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: scheme.onSurfaceVariant,
              ),
            ),
          ),
        ...List.generate(pecas.length, (indice) {
          final item = pecas[indice];
          return Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              decoration: BoxDecoration(
                color: scheme.surfaceContainerLowest,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: scheme.outlineVariant),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                      Expanded(
                        child: Text(
                          item.nome,
                          style: theme.textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      IconButton(
                        tooltip: 'Remover peça',
                        onPressed: () => onRemover(indice),
                        icon: Icon(
                          Icons.delete_outline,
                          size: 20,
                          color: scheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                  Text(
                    'Cobrado: ${item.quantidade} × ${_reais(BigInt.from(item.valorUnitarioCentavos))}',
                    style: theme.textTheme.bodyMedium?.copyWith(
                      fontFeatures: AppTheme.tabularFigures,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    'Custo por unidade: ${_reais(BigInt.from(item.custoUnitarioCentavos))}',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: scheme.onSurfaceVariant,
                      fontFeatures: AppTheme.tabularFigures,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    _reais(item.subtotalCentavos),
                    textAlign: TextAlign.end,
                    style: theme.textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.w700,
                      fontFeatures: AppTheme.tabularFigures,
                    ),
                  ),
                ],
              ),
            ),
          );
        }),
        const SizedBox(height: 12),
        LayoutBuilder(
          builder: (context, constraints) {
            final botao = OutlinedButton.icon(
              onPressed: () => _abrirCadastro(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('Adicionar peça'),
            );
            final resumoTotal = Text(
              'Total de peças: ${_reais(total)}',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: scheme.onSurfaceVariant,
                fontWeight: FontWeight.w600,
                fontFeatures: AppTheme.tabularFigures,
              ),
            );

            if (constraints.maxWidth < 520) {
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [botao, const SizedBox(height: 10), resumoTotal],
              );
            }
            return Row(
              children: [
                botao,
                const SizedBox(width: 16),
                Expanded(
                  child: Align(
                    alignment: Alignment.centerRight,
                    child: resumoTotal,
                  ),
                ),
              ],
            );
          },
        ),
      ],
    );
  }
}
