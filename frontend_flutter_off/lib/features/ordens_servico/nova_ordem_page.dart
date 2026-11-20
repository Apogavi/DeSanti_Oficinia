import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../shared/formatters/brazillian_mask_formatter.dart';
import '../../shared/formatters/money_input_formatter.dart';
import '../../shared/validation/ordem_servico_validation.dart';
import '../../theme/app_theme.dart';
import '../clientes/novo_cliente_dialog.dart';
import 'pecas_section.dart';
import 'view_models/nova_ordem_view_model.dart';

class NovaOrdemPage extends StatefulWidget {
  /// A rota transfere a propriedade do ViewModel para esta View, que o descarta.
  const NovaOrdemPage({super.key, required this.viewModel});

  final NovaOrdemViewModel viewModel;

  @override
  State<NovaOrdemPage> createState() => _NovaOrdemPageState();
}

class _NovaOrdemPageState extends State<NovaOrdemPage> {
  final _formKey = GlobalKey<FormState>();
  final _buscaCliente = TextEditingController();
  final _placa = TextEditingController();
  final _marca = TextEditingController();
  final _modelo = TextEditingController();
  final _cor = TextEditingController();
  final _ano = TextEditingController();
  final _descricao = TextEditingController();
  final _maoDeObra = TextEditingController();
  final _diasRetorno = TextEditingController();
  NovaOrdemViewModel get _vm => widget.viewModel;

  @override
  void initState() {
    super.initState();
    final r = _vm.rascunho;
    _placa.text = r.placa;
    _marca.text = r.marca;
    _modelo.text = r.modelo;
    _cor.text = r.cor;
    _ano.text = r.ano;
    _descricao.text = r.descricaoServico;
    _maoDeObra.text = r.maoDeObra;
    _diasRetorno.text = r.prazoRetornoDias;
    _vm.carregarClientes();
  }

  @override
  void dispose() {
    for (final controller in [
      _buscaCliente,
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
    _vm.dispose();
    super.dispose();
  }

  String? _erroCampo(String campo) => _vm.erros[campo]?.join('\n');

  Widget _campo(
    TextEditingController controller,
    String rotulo,
    String campo,
    ValueChanged<String> aoMudar, {
    TextInputType? teclado,
    int linhas = 1,
    double espacoInferior = 16,
    String? Function(String?)? validar,
    List<TextInputFormatter>? formatadores,
  }) => Padding(
    padding: EdgeInsets.only(bottom: espacoInferior),
    child: TextFormField(
      controller: controller,
      keyboardType: teclado,
      maxLines: linhas,
      decoration: InputDecoration(
        labelText: rotulo,
        alignLabelWithHint: linhas > 1,
      ),
      validator: (valor) =>
          (validar ?? OrdemServicoValidation.obrigatorio)(valor) ??
          _erroCampo(campo),
      inputFormatters: formatadores,
      onChanged: aoMudar,
    ),
  );

  String _reais(BigInt centavos) =>
      'R\$ ${MoneyInputFormatter.formatBigIntCents(centavos)}';

  Widget _tituloSecao(String titulo) => Semantics(
    header: true,
    child: Text(
      titulo,
      style: Theme.of(context).textTheme.titleMedium
          ?.copyWith(fontWeight: FontWeight.w700),
    ),
  );

  Widget _linhaTotal(String rotulo, BigInt centavos, {bool destaque = false}) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final estiloRotulo = destaque
        ? theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)
        : theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant);
    final estiloValor = destaque
        ? theme.textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.w700,
            color: scheme.onSurface,
            fontFeatures: AppTheme.tabularFigures,
          )
        : theme.textTheme.bodyLarge?.copyWith(
            fontWeight: FontWeight.w500,
            color: scheme.onSurface,
            fontFeatures: AppTheme.tabularFigures,
          );

    return Padding(
      padding: EdgeInsets.symmetric(vertical: destaque ? 6 : 4),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final label = Text(rotulo, style: estiloRotulo);
          final valor = Text(
            _reais(centavos),
            textAlign: TextAlign.end,
            style: estiloValor,
          );
          if (constraints.maxWidth < 420) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [label, const SizedBox(height: 2), valor],
            );
          }
          return Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Flexible(child: label),
              const SizedBox(width: 16),
              Flexible(child: valor),
            ],
          );
        },
      ),
    );
  }

  Future<void> _salvar() async {
    // Todos os campos continuam montados pelo SingleChildScrollView.
    if (!_formKey.currentState!.validate()) return;
    await _vm.salvar();
    if (!mounted) return;
    if (_vm.criada case final criada?) {
      Navigator.of(context).pop(criada);
    } else {
      _formKey.currentState!.validate();
    }
  }

  Future<void> _abrirCadastroCliente({String? termoInicial}) async {
    if (_vm.bloqueado) return;
    final texto = (termoInicial ?? '').trim();
    final temLetra = RegExp(r'[A-Za-zÀ-ÿ]').hasMatch(texto);
    final digitos = OrdemServicoValidation.normalizarDigitos(texto);

    final criado = await mostrarNovoClienteDialog(
      context,
      clientes: _vm.clientesRepository,
      nomeInicial: temLetra ? texto : null,
      documentoInicial: !temLetra && digitos.isNotEmpty ? digitos : null,
    );
    if (!mounted || criado == null) return;
    _buscaCliente.clear();
    _vm.registrarClienteCriado(criado);
  }

  Widget _clientes() {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;
    final cliente = _vm.clienteSelecionado;
    final filtrados = _vm.clientesFiltrados;
    final opcoesDropdown = [
      ...filtrados,
      if (cliente != null && !filtrados.any((c) => c.id == cliente.id)) cliente,
    ];

    final docFormatado = cliente == null || cliente.documento.isEmpty
        ? 'Não informado'
        : BrazilianMaskFormatter.formatarCpfOuCnpj(cliente.documento);
    final telFormatado = cliente == null || cliente.telefone.isEmpty
        ? 'Não informado'
        : BrazilianMaskFormatter.formatarTelefone(cliente.telefone);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _tituloSecao('Cliente'),
            const SizedBox(height: 16),
            if (_vm.carregandoClientes) ...[
              const LinearProgressIndicator(),
              const SizedBox(height: 16),
            ],
            if (_vm.erroClientes case final erro?) ...[
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
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      erro,
                      style: TextStyle(
                        color: scheme.onErrorContainer,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 8),
                    TextButton.icon(
                      onPressed: _vm.carregarClientes,
                      icon: const Icon(Icons.refresh, size: 18),
                      label: const Text('Tentar carregar clientes novamente'),
                    ),
                  ],
                ),
              ),
            ],
            TextField(
              controller: _buscaCliente,
              enabled: !_vm.carregandoClientes && !_vm.bloqueado,
              decoration: InputDecoration(
                labelText: 'Buscar por nome ou CPF/CNPJ',
                hintText: 'Digite o nome, CPF ou CNPJ para localizar',
                suffixIcon: _vm.filtroCliente.isEmpty
                    ? null
                    : IconButton(
                        tooltip: 'Limpar busca',
                        onPressed: () {
                          _buscaCliente.clear();
                          _vm.atualizarFiltroCliente('');
                        },
                        icon: const Icon(Icons.close, size: 18),
                      ),
              ),
              onChanged: _vm.atualizarFiltroCliente,
            ),
            const SizedBox(height: 16),
            if (_vm.buscaClienteSemResultado) ...[
              Container(
                padding: const EdgeInsets.all(14),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                  color: isDark
                      ? const Color(0xFF2B210B)
                      : const Color(0xFFFEF9E7),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(
                    color: isDark
                        ? const Color(0xFFF2B927).withValues(alpha: 0.45)
                        : const Color(0xFFD99B00).withValues(alpha: 0.5),
                  ),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(
                          Icons.warning_amber_rounded,
                          size: 20,
                          color: isDark
                              ? const Color(0xFFF2B927)
                              : const Color(0xFF9A6700),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Cliente não encontrado para "${_vm.filtroCliente.trim()}".',
                                style: theme.textTheme.bodyMedium?.copyWith(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                'Cadastre o cliente antes de prosseguir com a abertura da ordem de serviço.',
                                style: theme.textTheme.bodySmall?.copyWith(
                                  color: scheme.onSurfaceVariant,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    FilledButton.tonalIcon(
                      onPressed: () => _abrirCadastroCliente(
                        termoInicial: _vm.filtroCliente,
                      ),
                      icon: const Icon(Icons.person_add_alt_1, size: 18),
                      label: const Text('Cadastrar cliente antes de prosseguir'),
                    ),
                  ],
                ),
              ),
            ] else if (!_vm.carregandoClientes &&
                _vm.erroClientes == null &&
                _vm.clientes.isEmpty) ...[
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: Text(
                  'Nenhum cliente ativo disponível. Cadastre um cliente antes de abrir a OS.',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: scheme.onSurfaceVariant,
                  ),
                ),
              ),
            ],
            DropdownButtonFormField<int>(
              key: ValueKey(_vm.rascunho.clienteId),
              initialValue: _vm.rascunho.clienteId,
              isExpanded: true,
              decoration: const InputDecoration(
                labelText: 'Cliente cadastrado *',
              ),
              items: opcoesDropdown
                  .map(
                    (c) => DropdownMenuItem(
                      value: c.id,
                      child: Text(c.nome, overflow: TextOverflow.ellipsis),
                    ),
                  )
                  .toList(),
              onChanged: _vm.carregandoClientes || _vm.erroClientes != null
                  ? null
                  : (id) => _vm.atualizar((r) => r.clienteId = id),
              validator: (id) =>
                  id == null ? 'Selecione um cliente' : _erroCampo('clienteId'),
            ),
            if (cliente != null) ...[
              const SizedBox(height: 14),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 14,
                  vertical: 12,
                ),
                decoration: BoxDecoration(
                  color: scheme.surfaceContainerLowest,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: scheme.outlineVariant),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      'Nome: ${cliente.nome}',
                      style: theme.textTheme.bodyMedium?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Documento: $docFormatado',
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: scheme.onSurfaceVariant,
                        fontFeatures: AppTheme.tabularFigures,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      'Telefone: $telFormatado',
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: scheme.onSurfaceVariant,
                        fontFeatures: AppTheme.tabularFigures,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _veiculo() {
    return FocusTraversalGroup(
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _tituloSecao('Veículo'),
              const SizedBox(height: 16),
            LayoutBuilder(
              builder: (context, constraints) {
                final campoPlaca = _campo(
                  _placa,
                  'Placa *',
                  'placa',
                  (v) => _vm.atualizar((r) => r.placa = v),
                  validar: OrdemServicoValidation.placa,
                  formatadores: [BrazilianMaskFormatter.plate()],
                );
                final campoMarca = _campo(
                  _marca,
                  'Marca *',
                  'marca',
                  (v) => _vm.atualizar((r) => r.marca = v),
                  validar: (v) =>
                      OrdemServicoValidation.obrigatorio(v, maxLength: 100),
                );
                final campoModelo = _campo(
                  _modelo,
                  'Modelo *',
                  'modelo',
                  (v) => _vm.atualizar((r) => r.modelo = v),
                  espacoInferior: constraints.maxWidth >= 580 ? 0 : 16,
                  validar: (v) =>
                      OrdemServicoValidation.obrigatorio(v, maxLength: 100),
                );
                final campoCor = _campo(
                  _cor,
                  'Cor *',
                  'cor',
                  (v) => _vm.atualizar((r) => r.cor = v),
                  espacoInferior: constraints.maxWidth >= 580 ? 0 : 16,
                  validar: (v) =>
                      OrdemServicoValidation.obrigatorio(v, maxLength: 50),
                );
                final campoAno = _campo(
                  _ano,
                  'Ano',
                  'ano',
                  (v) => _vm.atualizar((r) => r.ano = v),
                  teclado: TextInputType.number,
                  espacoInferior: 0,
                  validar: OrdemServicoValidation.ano,
                  formatadores: [
                    FilteringTextInputFormatter.digitsOnly,
                    LengthLimitingTextInputFormatter(4),
                  ],
                );

                if (constraints.maxWidth < 580) {
                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      campoPlaca,
                      campoMarca,
                      campoModelo,
                      campoCor,
                      campoAno,
                    ],
                  );
                }

                return Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(flex: 2, child: campoPlaca),
                        const SizedBox(width: 16),
                        Expanded(flex: 3, child: campoMarca),
                      ],
                    ),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(flex: 3, child: campoModelo),
                        const SizedBox(width: 16),
                        Expanded(flex: 2, child: campoCor),
                        const SizedBox(width: 16),
                        Expanded(flex: 2, child: campoAno),
                      ],
                    ),
                  ],
                );
              },
            ),
          ],
        ),
      ),
    ),
    );
  }

  Widget _servico() {
    final theme = Theme.of(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _tituloSecao('Serviço *'),
            const SizedBox(height: 16),
            PecasSection(
              pecas: _vm.pecas,
              onAdicionar: _vm.adicionarPeca,
              onRemover: _vm.removerPeca,
            ),
            const SizedBox(height: 20),
            const Divider(height: 1),
            const SizedBox(height: 20),
            _campo(
              _descricao,
              'Descrição do serviço *',
              'descricaoServico',
              (v) => _vm.atualizar((r) => r.descricaoServico = v),
              linhas: 3,
            ),
            _campo(
              _maoDeObra,
              'Mão de obra (R\$) *',
              'maoDeObraCentavos',
              (v) => _vm.atualizar((r) => r.maoDeObra = v),
              teclado: TextInputType.number,
              espacoInferior: 8,
              formatadores: [const MoneyInputFormatter()],
              validar: (v) => MoneyInputFormatter.cents(v) == null
                  ? 'Informe o valor da mão de obra'
                  : null,
            ),
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              controlAffinity: ListTileControlAffinity.leading,
              title: Text(
                'Necessário retorno',
                style: theme.textTheme.bodyMedium?.copyWith(
                  fontWeight: FontWeight.w500,
                ),
              ),
              value: _vm.rascunho.necessitaRetorno,
              onChanged: (valor) {
                _vm.atualizar((r) {
                  r.necessitaRetorno = valor ?? false;
                  if (!r.necessitaRetorno) {
                    r.prazoRetornoDias = '';
                  }
                });
                if (!(valor ?? false)) _diasRetorno.clear();
              },
            ),
            if (_vm.rascunho.necessitaRetorno) ...[
              const SizedBox(height: 8),
              _campo(
                _diasRetorno,
                'Retorno em quantos dias?',
                'prazoRetornoDias',
                (v) => _vm.atualizar((r) => r.prazoRetornoDias = v),
                teclado: TextInputType.number,
                espacoInferior: 0,
                formatadores: [FilteringTextInputFormatter.digitsOnly],
                validar: OrdemServicoValidation.inteiroPositivo,
              ),
            ],
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return ListenableBuilder(
      listenable: _vm,
      builder: (context, _) => PopScope(
        canPop: !_vm.enviando,
        child: Scaffold(
          appBar: AppBar(title: const Text('Nova ordem de serviço')),
          body: LayoutBuilder(
            builder: (context, constraints) {
              final isWide = constraints.maxWidth >= 640;
              final horizontalPadding = isWide ? 28.0 : 16.0;

              return Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 820),
                  child: Form(
                    key: _formKey,
                    child: SingleChildScrollView(
                      padding: EdgeInsets.symmetric(
                        horizontal: horizontalPadding,
                        vertical: 24,
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          AbsorbPointer(
                            absorbing: _vm.bloqueado,
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                _clientes(),
                                const SizedBox(height: 16),
                                _veiculo(),
                                const SizedBox(height: 16),
                                _servico(),
                              ],
                            ),
                          ),
                          const SizedBox(height: 16),
                          Card(
                            child: Padding(
                              padding: const EdgeInsets.all(20),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.stretch,
                                children: [
                                  _linhaTotal('Peças', _vm.totalPecasCentavos),
                                  _linhaTotal(
                                    'Mão de obra',
                                    BigInt.from(_vm.maoDeObraCentavos),
                                  ),
                                  const Padding(
                                    padding: EdgeInsets.symmetric(vertical: 10),
                                    child: Divider(height: 1),
                                  ),
                                  _linhaTotal(
                                    'Total geral',
                                    _vm.totalCentavos,
                                    destaque: true,
                                  ),
                                ],
                              ),
                            ),
                          ),
                          if (_vm.mensagem case final mensagem?) ...[
                            const SizedBox(height: 16),
                            Container(
                              padding: const EdgeInsets.all(14),
                              decoration: BoxDecoration(
                                color: scheme.errorContainer,
                                borderRadius: BorderRadius.circular(10),
                                border: Border.all(
                                  color: scheme.error.withValues(alpha: 0.4),
                                ),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    mensagem,
                                    style: TextStyle(
                                      color: scheme.onErrorContainer,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                  for (final mensagens in _vm.erros.values) ...[
                                    const SizedBox(height: 4),
                                    Text(
                                      mensagens.join('\n'),
                                      style: TextStyle(
                                        color: scheme.onErrorContainer,
                                      ),
                                    ),
                                  ],
                                ],
                              ),
                            ),
                          ],
                          if (_vm.resultadoIncerto) ...[
                            const SizedBox(height: 16),
                            Container(
                              padding: const EdgeInsets.all(14),
                              decoration: BoxDecoration(
                                color: scheme.secondaryContainer,
                                borderRadius: BorderRadius.circular(10),
                                border: Border.all(
                                  color: scheme.outlineVariant,
                                ),
                              ),
                              child: Text(
                                'A OS pode ter sido salva. Confira o registro antes de iniciar outra OS. O rascunho foi preservado e o reenvio está bloqueado.',
                                style: TextStyle(
                                  color: scheme.onSecondaryContainer,
                                  fontWeight: FontWeight.w500,
                                ),
                              ),
                            ),
                          ],
                          const SizedBox(height: 20),
                          if (isWide)
                            Align(
                              alignment: Alignment.centerRight,
                              child: FilledButton(
                                onPressed:
                                    _vm.bloqueado || _vm.carregandoClientes
                                    ? null
                                    : _salvar,
                                child: Text(
                                  _vm.enviando
                                      ? 'Salvando...'
                                      : 'Salvar ordem de serviço',
                                ),
                              ),
                            )
                          else
                            FilledButton(
                              onPressed: _vm.bloqueado || _vm.carregandoClientes
                                  ? null
                                  : _salvar,
                              child: Text(
                                _vm.enviando
                                    ? 'Salvando...'
                                    : 'Salvar ordem de serviço',
                              ),
                            ),
                          if (_vm.enviando) ...[
                            const SizedBox(height: 8),
                            const LinearProgressIndicator(),
                          ],
                        ],
                      ),
                    ),
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}
