import 'package:flutter/material.dart';

import '../../core/network/api_service.dart';
import '../../shared/formatters/brazillian_mask_formatter.dart';
import '../../shared/validation/ordem_servico_validation.dart';
import '../../theme/app_theme.dart';
import 'criar_cliente_request.dart';
import 'models/cliente.dart';
import 'repositories/cliente_repository.dart';

class ClientesView extends StatefulWidget {
  const ClientesView({
    super.key,
    required this.clientesRepository,
    this.aoAbrirOsComCliente,
  });

  final ClienteRepository clientesRepository;
  final ValueChanged<Cliente>? aoAbrirOsComCliente;

  @override
  State<ClientesView> createState() => _ClientesViewState();
}

class _ClientesViewState extends State<ClientesView> {
  final _formKey = GlobalKey<FormState>();
  final _nome = TextEditingController();
  final _documento = TextEditingController();
  final _telefone = TextEditingController();
  final _busca = TextEditingController();
  final _focoNome = FocusNode();
  final _focoDocumento = FocusNode();
  final _focoTelefone = FocusNode();

  List<Cliente> _clientes = const [];
  bool _carregandoLista = false;
  String? _erroLista;

  bool _salvando = false;
  String? _erroCadastro;
  Map<String, List<String>> _errosCampo = const {};
  Cliente? _ultimoCriado;
  String _termoBusca = '';

  @override
  void initState() {
    super.initState();
    _carregarClientes();
  }

  @override
  void dispose() {
    _nome.dispose();
    _documento.dispose();
    _telefone.dispose();
    _busca.dispose();
    _focoNome.dispose();
    _focoDocumento.dispose();
    _focoTelefone.dispose();
    super.dispose();
  }

  Future<void> _carregarClientes() async {
    if (_carregandoLista) return;
    setState(() {
      _carregandoLista = true;
      _erroLista = null;
    });
    try {
      final lista = await widget.clientesRepository.listar();
      if (!mounted) return;
      setState(() {
        _clientes = lista.where((c) => c.ativo).toList();
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() => _erroLista = e.message);
    } finally {
      if (mounted) {
        setState(() => _carregandoLista = false);
      }
    }
  }

  List<Cliente> get _clientesFiltrados {
    final termo = _termoBusca.trim().toLowerCase();
    if (termo.isEmpty) return _clientes;
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

  String? _erroServidor(String campo) => _errosCampo[campo]?.join('\n');

  void _limparErroCampo(String campo) {
    if (_erroCadastro == null && !_errosCampo.containsKey(campo)) return;
    setState(() {
      _erroCadastro = null;
      _errosCampo = {
        for (final entry in _errosCampo.entries)
          if (entry.key != campo) entry.key: entry.value,
      };
    });
  }

  Future<void> _salvarCliente() async {
    setState(() {
      _erroCadastro = null;
      _errosCampo = const {};
      _ultimoCriado = null;
    });
    if (!_formKey.currentState!.validate()) return;

    setState(() => _salvando = true);
    try {
      final criado = await widget.clientesRepository.criar(
        CriarClienteRequest(
          nome: _nome.text,
          documento: _documento.text,
          telefone: _telefone.text,
        ),
      );
      if (!mounted) return;
      _nome.clear();
      _documento.clear();
      _telefone.clear();
      setState(() {
        _salvando = false;
        _ultimoCriado = criado;
        _clientes = [
          criado,
          for (final existente in _clientes)
            if (existente.id != criado.id) existente,
        ];
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _salvando = false;
        _erroCadastro = e.message;
        _errosCampo = e.errors;
      });
      _formKey.currentState!.validate();
    }
  }

  Widget _secaoCadastro(ThemeData theme, ColorScheme scheme, bool isDark) {
    final corSucesso = isDark
        ? const Color(0xFF4ADE80)
        : const Color(0xFF15803D);

    return FocusTraversalGroup(
      policy: OrderedTraversalPolicy(),
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Semantics(
                  header: true,
                  child: Text(
                    'Cadastrar cliente',
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  'Preencha nome, CPF ou CNPJ e telefone com DDD.',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: scheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: 20),
                if (_erroCadastro case final erro?) ...[
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
                if (_ultimoCriado case final cliente?) ...[
                  Container(
                    padding: const EdgeInsets.all(12),
                    margin: const EdgeInsets.only(bottom: 16),
                    decoration: BoxDecoration(
                      color: corSucesso.withValues(alpha: 0.10),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: corSucesso.withValues(alpha: 0.35),
                      ),
                    ),
                    child: Row(
                      children: [
                        Icon(Icons.check_circle, size: 18, color: corSucesso),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            'Cliente ${cliente.nome} cadastrado com sucesso.',
                            style: theme.textTheme.bodyMedium?.copyWith(
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                FocusTraversalOrder(
                  order: const NumericFocusOrder(1),
                  child: TextFormField(
                    controller: _nome,
                    focusNode: _focoNome,
                    enabled: !_salvando,
                    textInputAction: TextInputAction.next,
                    textCapitalization: TextCapitalization.words,
                    decoration: const InputDecoration(
                      labelText: 'Nome completo *',
                    ),
                    onChanged: (_) => _limparErroCampo('nome'),
                    onFieldSubmitted: (_) => _focoDocumento.requestFocus(),
                    validator: (valor) =>
                        OrdemServicoValidation.obrigatorio(
                          valor,
                          maxLength: 200,
                        ) ??
                        _erroServidor('nome'),
                  ),
                ),
                const SizedBox(height: 16),
                FocusTraversalOrder(
                  order: const NumericFocusOrder(2),
                  child: TextFormField(
                    controller: _documento,
                    focusNode: _focoDocumento,
                    enabled: !_salvando,
                    textInputAction: TextInputAction.next,
                    keyboardType: TextInputType.number,
                    style: const TextStyle(
                      fontFeatures: AppTheme.tabularFigures,
                    ),
                    decoration: const InputDecoration(
                      labelText: 'CPF ou CNPJ *',
                      helperText: 'CPF (11 dígitos) ou CNPJ (14 dígitos)',
                    ),
                    inputFormatters: [BrazilianMaskFormatter.cpfOrCnpj()],
                    onChanged: (_) => _limparErroCampo('documento'),
                    onFieldSubmitted: (_) => _focoTelefone.requestFocus(),
                    validator: (valor) =>
                        OrdemServicoValidation.cpfOuCnpj(valor) ??
                        _erroServidor('documento'),
                  ),
                ),
                const SizedBox(height: 16),
                FocusTraversalOrder(
                  order: const NumericFocusOrder(3),
                  child: TextFormField(
                    controller: _telefone,
                    focusNode: _focoTelefone,
                    enabled: !_salvando,
                    textInputAction: TextInputAction.done,
                    keyboardType: TextInputType.phone,
                    style: const TextStyle(
                      fontFeatures: AppTheme.tabularFigures,
                    ),
                    decoration: const InputDecoration(
                      labelText: 'Telefone / WhatsApp *',
                      helperText: 'Com DDD — ex.: (11) 98765-4321',
                    ),
                    inputFormatters: [BrazilianMaskFormatter.phone()],
                    onChanged: (_) => _limparErroCampo('telefone'),
                    onFieldSubmitted: (_) {
                      if (!_salvando) _salvarCliente();
                    },
                    validator: (valor) =>
                        OrdemServicoValidation.telefone(valor) ??
                        _erroServidor('telefone'),
                  ),
                ),
                const SizedBox(height: 20),
                FocusTraversalOrder(
                  order: const NumericFocusOrder(4),
                  child: FilledButton.icon(
                    onPressed: _salvando ? null : _salvarCliente,
                    icon: const Icon(Icons.person_add_alt_1, size: 18),
                    label: Text(_salvando ? 'Salvando...' : 'Salvar cliente'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _secaoConsulta(ThemeData theme, ColorScheme scheme) {
    final filtrados = _clientesFiltrados;

    return FocusTraversalGroup(
      policy: OrderedTraversalPolicy(),
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Flexible(
                    child: Semantics(
                      header: true,
                      child: Text(
                        'Consulta de clientes',
                        style: theme.textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ),
                  FocusTraversalOrder(
                    order: const NumericFocusOrder(2),
                    child: IconButton(
                      tooltip: 'Atualizar lista de clientes',
                      onPressed: _carregandoLista ? null : _carregarClientes,
                      icon: const Icon(Icons.refresh_rounded, size: 20),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                '${_clientes.length} cliente(s) ativo(s) cadastrado(s)',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: scheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 16),
              FocusTraversalOrder(
                order: const NumericFocusOrder(1),
                child: TextField(
                  controller: _busca,
                  decoration: InputDecoration(
                    labelText: 'Consultar por nome, CPF ou CNPJ',
                    hintText: 'Digite para filtrar os clientes',
                    suffixIcon: _termoBusca.isEmpty
                        ? null
                        : IconButton(
                            tooltip: 'Limpar filtro',
                            onPressed: () {
                              _busca.clear();
                              setState(() => _termoBusca = '');
                            },
                            icon: const Icon(Icons.close, size: 18),
                          ),
                  ),
                  onChanged: (valor) => setState(() => _termoBusca = valor),
                ),
              ),
            const SizedBox(height: 16),
            if (_carregandoLista) ...[
              const LinearProgressIndicator(),
              const SizedBox(height: 16),
            ],
            if (_erroLista case final erro?) ...[
              Container(
                padding: const EdgeInsets.all(14),
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
                  style: TextStyle(
                    color: scheme.onErrorContainer,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
            if (!_carregandoLista && _erroLista == null && filtrados.isEmpty)
              Container(
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: scheme.surfaceContainerLowest,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: scheme.outlineVariant),
                ),
                child: Text(
                  _termoBusca.trim().isEmpty
                      ? 'Nenhum cliente cadastrado ainda. Use o formulário para cadastrar o primeiro cliente.'
                      : 'Nenhum cliente encontrado para "${_termoBusca.trim()}".',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: scheme.onSurfaceVariant,
                  ),
                ),
              )
            else
              Column(
                children: [
                  for (var i = 0; i < filtrados.length; i++) ...[
                    if (i > 0) const SizedBox(height: 10),
                    _ItemClienteConsulta(
                      cliente: filtrados[i],
                      aoAbrirOs: widget.aoAbrirOsComCliente == null
                          ? null
                          : () => widget.aoAbrirOsComCliente!(filtrados[i]),
                    ),
                  ],
                ],
              ),
          ],
        ),
      ),
    ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= 820;

        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Clientes', style: theme.textTheme.headlineMedium),
            const SizedBox(height: 6),
            Text(
              'Cadastre novos clientes e consulte os registros por nome, CPF ou CNPJ.',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: scheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 24),
            const Divider(height: 1),
            const SizedBox(height: 24),
            if (isWide)
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    flex: 5,
                    child: _secaoCadastro(theme, scheme, isDark),
                  ),
                  const SizedBox(width: 24),
                  Expanded(flex: 6, child: _secaoConsulta(theme, scheme)),
                ],
              )
            else ...[
              _secaoCadastro(theme, scheme, isDark),
              const SizedBox(height: 20),
              _secaoConsulta(theme, scheme),
            ],
          ],
        );
      },
    );
  }
}

class _ItemClienteConsulta extends StatelessWidget {
  const _ItemClienteConsulta({required this.cliente, this.aoAbrirOs});

  final Cliente cliente;
  final VoidCallback? aoAbrirOs;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final doc = cliente.documento.isEmpty
        ? 'Não informado'
        : BrazilianMaskFormatter.formatarCpfOuCnpj(cliente.documento);
    final tel = cliente.telefone.isEmpty
        ? 'Não informado'
        : BrazilianMaskFormatter.formatarTelefone(cliente.telefone);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      decoration: BoxDecoration(
        color: scheme.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: scheme.outlineVariant),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  cliente.nome,
                  style: theme.textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'CPF/CNPJ: $doc • Tel.: $tel',
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: scheme.onSurfaceVariant,
                    fontFeatures: AppTheme.tabularFigures,
                  ),
                ),
              ],
            ),
          ),
          if (aoAbrirOs != null) ...[
            const SizedBox(width: 12),
            TextButton.icon(
              onPressed: aoAbrirOs,
              icon: const Icon(Icons.add_circle_outline, size: 18),
              label: const Text('Abrir OS'),
            ),
          ],
        ],
      ),
    );
  }
}
