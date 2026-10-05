import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../shared/formatters/money_input_formatter.dart';

class PecaItem {
  const PecaItem({
    required this.nome,
    required this.quantidade,
    required this.valorUnitarioCentavos,
  });

  final String nome;
  final int quantidade;
  final int valorUnitarioCentavos;

  int get subtotalCentavos => quantidade * valorUnitarioCentavos;
}

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
  // {
  //   final texto = (valor ?? '').trim().replaceAll('.', ',');
  //   if (!RegExp(r'^\d+(,\d{1,2})?$').hasMatch(texto)) return null;

  //   final partes = texto.split(',');
  //   final reais = int.tryParse(partes[0]);
  //   if (reais == null) return null;
  //   final centavos = partes.length == 2
  //       ? int.parse(partes[1].padRight(2, '0'))
  //       : 0;
  //   return reais * 100 + centavos;
  // }

  String _reais(int centavos) {
    final inteiros = (centavos ~/ 100).toString();
    final agrupado = StringBuffer();
    for (var i = 0; i < inteiros.length; i++) {
      if (i > 0 && (inteiros.length - i) % 3 == 0) agrupado.write('.');
      agrupado.write(inteiros[i]);
    }
    final decimais = (centavos % 100).toString().padLeft(2, '0');
    return 'R\$ ${agrupado.toString()},$decimais';
  }

  Future<void> _abrirCadastro(BuildContext context) async {
    final chave = GlobalKey<FormState>();
    var nome = '';
    var quantidade = 1;
    var valorUnitario = 0;

    final item = await showDialog<PecaItem>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Adicionar peça'),
        content: SizedBox(
          width: 420,
          child: Form(
            key: chave,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  decoration: const InputDecoration(labelText: 'Peça'),
                  validator: (valor) => valor == null || valor.trim().isEmpty
                      ? 'Informe a peça'
                      : null,
                  onSaved: (valor) => nome = valor!.trim(),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  initialValue: '1',
                  decoration: const InputDecoration(labelText: 'Quantidade'),
                  keyboardType: TextInputType.number,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  validator: (valor) {
                    final numero = int.tryParse(valor ?? '');
                    return numero != null && numero > 0
                        ? null
                        : 'Informe quantidade maior que zero';
                  },
                  onSaved: (valor) => quantidade = int.parse(valor!),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  decoration: const InputDecoration(
                    labelText: 'Valor unitário (R\$)',
                    hintText: 'Digite o valor da peça...',
                  ),
                  keyboardType: TextInputType.number,
                  inputFormatters: [const MoneyInputFormatter()],
                  validator: (valor) => _centavos(valor) == null
                      ? 'Informe um valor como 120,50'
                      : null,
                  onSaved: (valor) => valorUnitario = _centavos(valor)!,
                ),
              ],
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
    final total = pecas.fold<int>(
      0,
      (soma, item) => soma + item.subtotalCentavos,
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Peças', style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 12),
        if (pecas.isEmpty) const Text('Nenhuma peça adicionada.'),
        ...List.generate(pecas.length, (indice) {
          final item = pecas[indice];
          return Card(
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(item.nome),
                        Text(
                          '${item.quantidade} × ${_reais(item.valorUnitarioCentavos)}',
                        ),
                      ],
                    ),
                  ),
                  Text(_reais(item.subtotalCentavos)),
                  IconButton(
                    tooltip: 'Remover peça',
                    onPressed: () => onRemover(indice),
                    icon: const Icon(Icons.delete_outline),
                  ),
                ],
              ),
            ),
          );
        }),
        const SizedBox(height: 12),
        OutlinedButton.icon(
          onPressed: () => _abrirCadastro(context),
          icon: const Icon(Icons.add),
          label: const Text('Adicionar peça'),
        ),
        const SizedBox(height: 12),
        Text('Total de peças: ${_reais(total)}'),
      ],
    );
  }
}
