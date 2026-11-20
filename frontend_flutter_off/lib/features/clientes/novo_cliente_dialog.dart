import 'package:flutter/material.dart';

import '../../core/network/api_service.dart';
import '../../shared/formatters/brazillian_mask_formatter.dart';
import '../../shared/validation/ordem_servico_validation.dart';
import '../../theme/app_theme.dart';
import 'criar_cliente_request.dart';
import 'models/cliente.dart';
import 'repositories/cliente_repository.dart';

Future<Cliente?> mostrarNovoClienteDialog(
  BuildContext context, {
  required ClienteRepository clientes,
  String? nomeInicial,
  String? documentoInicial,
}) {
  return showDialog<Cliente>(
    context: context,
    builder: (context) => NovoClienteDialog(
      clientes: clientes,
      nomeInicial: nomeInicial,
      documentoInicial: documentoInicial,
    ),
  );
}

class NovoClienteDialog extends StatefulWidget {
  const NovoClienteDialog({
    super.key,
    required this.clientes,
    this.nomeInicial,
    this.documentoInicial,
  });

  final ClienteRepository clientes;
  final String? nomeInicial;
  final String? documentoInicial;

  @override
  State<NovoClienteDialog> createState() => _NovoClienteDialogState();
}

class _NovoClienteDialogState extends State<NovoClienteDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _nome;
  late final TextEditingController _documento;
  final _telefone = TextEditingController();

  bool _salvando = false;
  String? _erroGeral;
  Map<String, List<String>> _errosCampo = const {};

  @override
  void initState() {
    super.initState();
    _nome = TextEditingController(text: widget.nomeInicial?.trim() ?? '');
    _documento = TextEditingController(
      text: BrazilianMaskFormatter.formatarCpfOuCnpj(
        widget.documentoInicial ?? '',
      ),
    );
  }

  @override
  void dispose() {
    _nome.dispose();
    _documento.dispose();
    _telefone.dispose();
    super.dispose();
  }

  String? _erroServidor(String campo) => _errosCampo[campo]?.join('\n');

  void _limparErro(String campo) {
    if (_erroGeral == null && !_errosCampo.containsKey(campo)) return;
    setState(() {
      _erroGeral = null;
      _errosCampo = {
        for (final entry in _errosCampo.entries)
          if (entry.key != campo) entry.key: entry.value,
      };
    });
  }

  Future<void> _salvar() async {
    setState(() {
      _erroGeral = null;
      _errosCampo = const {};
    });
    if (!_formKey.currentState!.validate()) return;

    setState(() => _salvando = true);
    try {
      final criado = await widget.clientes.criar(
        CriarClienteRequest(
          nome: _nome.text,
          documento: _documento.text,
          telefone: _telefone.text,
        ),
      );
      if (!mounted) return;
      Navigator.of(context).pop(criado);
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _salvando = false;
        _erroGeral = e.message;
        _errosCampo = e.errors;
      });
      _formKey.currentState!.validate();
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return AlertDialog(
      title: const Text('Novo cliente'),
      content: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 440),
        child: Form(
          key: _formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Cadastre o nome, CPF ou CNPJ e telefone do cliente.',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: scheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: 16),
                if (_erroGeral case final erro?) ...[
                  Container(
                    padding: const EdgeInsets.all(12),
                    margin: const EdgeInsets.only(bottom: 16),
                    decoration: BoxDecoration(
                      color: scheme.errorContainer,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: scheme.error.withValues(alpha: 0.4),
                      ),
                    ),
                    child: Text(
                      erro,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: scheme.onErrorContainer,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
                TextFormField(
                  controller: _nome,
                  autofocus: (widget.nomeInicial ?? '').isEmpty,
                  enabled: !_salvando,
                  textCapitalization: TextCapitalization.words,
                  decoration: const InputDecoration(
                    labelText: 'Nome completo *',
                  ),
                  onChanged: (_) => _limparErro('nome'),
                  validator: (valor) =>
                      OrdemServicoValidation.obrigatorio(
                        valor,
                        maxLength: 200,
                      ) ??
                      _erroServidor('nome'),
                ),
                const SizedBox(height: 14),
                TextFormField(
                  controller: _documento,
                  enabled: !_salvando,
                  keyboardType: TextInputType.number,
                  style: const TextStyle(
                    fontFeatures: AppTheme.tabularFigures,
                  ),
                  decoration: const InputDecoration(
                    labelText: 'CPF ou CNPJ *',
                    helperText: 'CPF (11 dígitos) ou CNPJ (14 dígitos)',
                  ),
                  inputFormatters: [BrazilianMaskFormatter.cpfOrCnpj()],
                  onChanged: (_) => _limparErro('documento'),
                  validator: (valor) =>
                      OrdemServicoValidation.cpfOuCnpj(valor) ??
                      _erroServidor('documento'),
                ),
                const SizedBox(height: 14),
                TextFormField(
                  controller: _telefone,
                  enabled: !_salvando,
                  keyboardType: TextInputType.phone,
                  style: const TextStyle(
                    fontFeatures: AppTheme.tabularFigures,
                  ),
                  decoration: const InputDecoration(
                    labelText: 'Telefone / WhatsApp *',
                    helperText: 'Com DDD — ex.: (11) 98765-4321',
                  ),
                  inputFormatters: [BrazilianMaskFormatter.phone()],
                  onChanged: (_) => _limparErro('telefone'),
                  validator: (valor) =>
                      OrdemServicoValidation.telefone(valor) ??
                      _erroServidor('telefone'),
                ),
              ],
            ),
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _salvando ? null : () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: _salvando ? null : _salvar,
          child: Text(_salvando ? 'Salvando...' : 'Salvar cliente'),
        ),
      ],
    );
  }
}
