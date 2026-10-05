import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'pecas_section.dart';

import 'criar_ordem_servico_request.dart';
import '../../shared/formatters/money_input_formatter.dart';
import '../../shared/formatters/brazillian_mask_formatter.dart';

class NovaOrdemPage extends StatefulWidget {
  const NovaOrdemPage({super.key});

  @override
  State<NovaOrdemPage> createState() => _NovaOrdemPageState();
}

class _NovaOrdemPageState extends State<NovaOrdemPage> {
  final _formKey = GlobalKey<FormState>();

  final _nome = TextEditingController();
  final _cpf = TextEditingController();
  final _telefone = TextEditingController();
  final _placa = TextEditingController();
  final _marca = TextEditingController();
  final _modelo = TextEditingController();
  final _cor = TextEditingController();
  final _ano = TextEditingController();
  final _descricao = TextEditingController();
  final _pecas = <PecaItem>[];
  final _maoDeObra = TextEditingController();
  final _diasRetorno = TextEditingController();
  bool _necessitaRetorno = false;

  @override
  void dispose() {
    for (final controller in [
      _nome,
      _cpf,
      _telefone,
      _placa,
      _marca,
      _modelo,
      _cor,
      _ano,
      _descricao,
      _maoDeObra,
      _diasRetorno,
    ]) {
      controller.dispose();
    }
    super.dispose();
  }

  String? _obrigatorio(String? valor) {
    return valor == null || valor.trim().isEmpty ? 'Campo obrigatório' : null;
  }

  bool _cpfValido(String valor) {
    final cpf = valor.replaceAll(RegExp(r'\D'), '');

    if (cpf.length != 11 || cpf.split('').every((digito) => digito == cpf[0])) {
      return false;
    }

    for (var posicao = 9; posicao <= 10; posicao++) {
      var soma = 0;

      for (var i = 0; i < posicao; i++) {
        soma += int.parse(cpf[i]) * (posicao + 1 - i);
      }

      var verificador = (soma * 10) % 11;
      if (verificador == 10) verificador = 0;

      if (verificador != int.parse(cpf[posicao])) return false;
    }

    return true;
  }

  Widget _campo(
    TextEditingController controller,
    String rotulo, {
    TextInputType? teclado,
    int linhas = 1,
    String? Function(String?)? validar,
    List<TextInputFormatter>? formatadores,
    ValueChanged<String>? aoMudar,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: TextFormField(
        controller: controller,
        keyboardType: teclado,
        maxLines: linhas,
        decoration: InputDecoration(labelText: rotulo),
        validator: validar ?? _obrigatorio,
        inputFormatters: formatadores,
        onChanged: aoMudar,
      ),
    );
  }

  int get _totalPecasCentavos =>
      _pecas.fold<int>(0, (total, peca) => total + peca.subtotalCentavos);

  int get _maoDeObraCentavos => MoneyInputFormatter.cents(_maoDeObra.text) ?? 0;

  int get _totalGeralCentavos => _totalPecasCentavos + _maoDeObraCentavos;

  String _reais(int centavos) =>
      'R\$ ${MoneyInputFormatter.formatCents(centavos)}';

  CriarOrdemServicoRequest _montarRequest() {
    return CriarOrdemServicoRequest(
      nomeCliente: _nome.text.trim(),
      cpf: _cpf.text.replaceAll(RegExp(r'\D'), ''),
      telefone: _telefone.text.replaceAll(RegExp(r'\D'), ''),
      placa: _placa.text.replaceAll(RegExp(r'[\s-]'), '').toUpperCase(),
      marca: _marca.text.trim(),
      modelo: _modelo.text.trim(),
      cor: _cor.text.trim(),
      ano: _ano.text.isEmpty ? null : int.parse(_ano.text),
      descricaoServico: _descricao.text.trim(),
      pecas: _pecas
          .map(
            (peca) => PecaRequest(
              nome: peca.nome,
              quantidade: peca.quantidade,
              valorUnitarioCentavos: peca.valorUnitarioCentavos,
            ),
          )
          .toList(),
      maoDeObraCentavos: MoneyInputFormatter.cents(_maoDeObra.text)!,
      necessitaRetorno: _necessitaRetorno,
      prazoRetornoDias: _necessitaRetorno ? int.parse(_diasRetorno.text) : null,
    );
  }

  void _conferirDados() {
    if (!_formKey.currentState!.validate()) return;

    final request = _montarRequest();
    final totalCentavos =
        request.pecas.fold<int>(
          0,
          (soma, peca) => soma + peca.quantidade * peca.valorUnitarioCentavos,
        ) +
        request.maoDeObraCentavos;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('Dados conferidos. Total: ${_reais(totalCentavos)}'),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Nova ordem de serviço')),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 760),
          child: Form(
            key: _formKey,
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Cliente',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 16),
                  _campo(_nome, 'Nome *'),
                  _campo(
                    _cpf,
                    'CPF *',
                    teclado: TextInputType.number,
                    validar: (valor) =>
                        _cpfValido(valor ?? '') ? null : 'CPF inválido',
                    formatadores: [BrazilianMaskFormatter.cpf()],
                  ),
                  _campo(
                    _telefone,
                    'Telefone com DDD *',
                    teclado: TextInputType.number,
                    validar: (valor) {
                      final digitos = (valor ?? '').replaceAll(
                        RegExp(r'\D'),
                        '',
                      );
                      return digitos.length == 11
                          ? null
                          : 'Informe um telefone com DDD';
                    },
                    formatadores: [BrazilianMaskFormatter.phone()],
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Veículo',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 16),
                  _campo(
                    _placa,
                    'Placa *',
                    validar: (valor) {
                      final placa = (valor ?? '')
                          .replaceAll(RegExp(r'[\s-]'), '')
                          .toUpperCase();
                      return RegExp(r'^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$')
                              .hasMatch(placa)
                          ? null
                          : 'Placa inválida';
                    },
                    formatadores: [BrazilianMaskFormatter.plate()],
                  ),
                  _campo(_marca, 'Marca *'),
                  _campo(_modelo, 'Modelo *'),
                  _campo(_cor, 'Cor *'),
                  _campo(
                    _ano,
                    'Ano',
                    teclado: TextInputType.number,
                    validar: (valor) {
                      if (valor == null || valor.trim().isEmpty) return null;
                      final ano = int.tryParse(valor);
                      return ano != null &&
                              ano >= 1900 &&
                              ano <= DateTime.now().year + 1
                          ? null
                          : 'Informe um ano válido';
                    },
                    formatadores: [
                      FilteringTextInputFormatter.digitsOnly,
                      LengthLimitingTextInputFormatter(4),
                    ],
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Serviço *',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 16),
                  PecasSection(
                    pecas: _pecas,
                    onAdicionar: (peca) => setState(() => _pecas.add(peca)),
                    onRemover: (indice) =>
                        setState(() => _pecas.removeAt(indice)),
                  ),
                  const SizedBox(height: 16),
                  _campo(_descricao, 'Descrição do serviço *', linhas: 3),
                  const SizedBox(height: 16),
                  _campo(
                    _maoDeObra,
                    'Mão de obra (R\$) *',
                    teclado: TextInputType.number,
                    formatadores: [const MoneyInputFormatter()],
                    validar: (valor) => MoneyInputFormatter.cents(valor) == null
                        ? 'Informe o valor da mão de obra'
                        : null,
                    aoMudar: (_) => setState(() {}),
                  ),
                  CheckboxListTile(
                    contentPadding: EdgeInsets.zero,
                    controlAffinity: ListTileControlAffinity.leading,
                    title: const Text('Necessário retorno'),
                    value: _necessitaRetorno,
                    onChanged: (valor) {
                      setState(() {
                        _necessitaRetorno = valor ?? false;
                        if (!_necessitaRetorno) _diasRetorno.clear();
                      });
                    },
                  ),
                  if (_necessitaRetorno)
                    _campo(
                      _diasRetorno,
                      'Retorno em quantos dias?',
                      teclado: TextInputType.number,
                      formatadores: [FilteringTextInputFormatter.digitsOnly],
                      validar: (valor) {
                        final dias = int.tryParse(valor ?? '');
                        return dias != null && dias > 0
                            ? null
                            : 'Informe um número de dias maior que zero';
                      },
                    ),
                  Card(
                    child: Column(
                      children: [
                        ListTile(
                          title: const Text('Peças'),
                          trailing: Text(_reais(_totalPecasCentavos)),
                        ),
                        ListTile(
                          title: const Text('Mão de obra'),
                          trailing: Text(_reais(_maoDeObraCentavos)),
                        ),
                        const Divider(height: 1),
                        ListTile(
                          title: const Text(
                            'Total geral',
                            style: TextStyle(fontWeight: FontWeight.bold),
                          ),
                          trailing: Text(
                            _reais(_totalGeralCentavos),
                            style: const TextStyle(fontWeight: FontWeight.bold),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: _conferirDados,
                    child: const Text('Conferir dados'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
