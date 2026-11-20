import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import 'core/config/app_config.dart';
import 'core/network/api_service.dart';
import 'features/clientes/clientes_page.dart';
import 'features/clientes/models/cliente.dart';
import 'features/clientes/repositories/cliente_repository.dart';
import 'features/ordens_servico/models/criar_ordem_servico_response.dart';
import 'features/ordens_servico/nova_ordem_page.dart';
import 'features/ordens_servico/repositories/ordem_servico_repository.dart';
import 'features/ordens_servico/view_models/nova_ordem_view_model.dart';
import 'shared/formatters/money_input_formatter.dart';
import 'shared/widgets/liquid_glass.dart';
import 'theme/app_theme.dart';

void main() {
  runApp(const OficinaApp());
}

class OficinaApp extends StatefulWidget {
  const OficinaApp({super.key});

  @override
  State<OficinaApp> createState() => _OficinaAppState();
}

class _OficinaAppState extends State<OficinaApp> {
  final _client = http.Client();
  late final _api = ApiService(client: _client, baseUrl: AppConfig.apiBaseUrl);
  late final _clientes = ApiClienteRepository(_api);
  late final _ordensServico = ApiOrdemServicoRepository(_api);

  @override
  void dispose() {
    _client.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'DeSanti Automotiva',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: ThemeMode.system,
      home: InicioPage(clientes: _clientes, ordensServico: _ordensServico),
    );
  }
}

class InicioPage extends StatefulWidget {
  const InicioPage({
    super.key,
    required this.clientes,
    required this.ordensServico,
  });

  final ClienteRepository clientes;
  final OrdemServicoRepository ordensServico;

  @override
  State<InicioPage> createState() => _InicioPageState();
}

class _InicioPageState extends State<InicioPage> {
  AbaPrincipal _abaAtiva = AbaPrincipal.ordensServico;
  CriarOrdemServicoResponse? _ultimaCriada;

  Future<void> _abrirNovaOrdem(
    BuildContext context, {
    Cliente? clientePreSelecionado,
  }) async {
    final vm = NovaOrdemViewModel(
      clientes: widget.clientes,
      ordensServico: widget.ordensServico,
    );
    if (clientePreSelecionado != null) {
      vm.atualizar((r) => r.clienteId = clientePreSelecionado.id);
    }
    final criada = await Navigator.of(context).push<CriarOrdemServicoResponse>(
      MaterialPageRoute(builder: (context) => NovaOrdemPage(viewModel: vm)),
    );
    if (mounted && criada != null) {
      setState(() {
        _abaAtiva = AbaPrincipal.ordensServico;
        _ultimaCriada = criada;
      });
    }
  }

  Widget _conteudoOrdensServico(
    BuildContext context,
    ThemeData theme,
    ColorScheme scheme,
    bool isDark,
    bool isWide,
  ) {
    final corSucesso = isDark
        ? const Color(0xFF4ADE80)
        : const Color(0xFF15803D);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (isWide)
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Ordens de serviço',
                      style: theme.textTheme.headlineMedium,
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Abra uma ordem de serviço para um cliente cadastrado.',
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: scheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 24),
              FilledButton.icon(
                onPressed: () => _abrirNovaOrdem(context),
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Nova ordem de serviço'),
              ),
            ],
          )
        else ...[
          Text('Ordens de serviço', style: theme.textTheme.headlineMedium),
          const SizedBox(height: 6),
          Text(
            'Abra uma ordem de serviço para um cliente cadastrado.',
            style: theme.textTheme.bodyMedium?.copyWith(
              color: scheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: 20),
          FilledButton.icon(
            onPressed: () => _abrirNovaOrdem(context),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Nova ordem de serviço'),
          ),
        ],
        const SizedBox(height: 24),
        const Divider(height: 1),
        if (_ultimaCriada case final ordem?) ...[
          const SizedBox(height: 24),
          Card(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 18),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Icon(Icons.check_circle, size: 20, color: corSucesso),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Ordem de serviço #${ordem.id} salva com sucesso.',
                          style: theme.textTheme.titleMedium,
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Total confirmado: R\$ ${MoneyInputFormatter.formatCents(ordem.totalCentavos)}',
                          style: theme.textTheme.bodyMedium?.copyWith(
                            color: scheme.onSurfaceVariant,
                            fontFeatures: AppTheme.tabularFigures,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      body: LiquidGlassBackdrop(
        child: SafeArea(
          child: LayoutBuilder(
            builder: (context, constraints) {
              final isWide = constraints.maxWidth >= 640;
              final horizontalPadding = isWide ? 32.0 : 16.0;

              return Stack(
                fit: StackFit.expand,
                children: [
                  // Camada 1 (Content Layer): conteúdo rola sob a barra Liquid Glass suspensa
                  Align(
                    alignment: Alignment.topCenter,
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 1040),
                      child: ListView(
                        padding: EdgeInsets.fromLTRB(
                          horizontalPadding,
                          88,
                          horizontalPadding,
                          36,
                        ),
                        children: [
                          if (_abaAtiva == AbaPrincipal.ordensServico)
                            _conteudoOrdensServico(
                              context,
                              theme,
                              scheme,
                              isDark,
                              isWide,
                            )
                          else
                            ClientesView(
                              clientesRepository: widget.clientes,
                              aoAbrirOsComCliente: (cliente) => _abrirNovaOrdem(
                                context,
                                clientePreSelecionado: cliente,
                              ),
                            ),
                        ],
                      ),
                    ),
                  ),
                  // Camada 2 (Functional Layer — Liquid Glass): menu suspenso no topo
                  // Somente o sanduíche (3 traços) que expande na horizontal ao clicar
                  Positioned(
                    top: 16,
                    left: horizontalPadding,
                    right: horizontalPadding,
                    child: Align(
                      alignment: Alignment.topCenter,
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 1040),
                        child: Align(
                          alignment: Alignment.centerLeft,
                          child: AppleExpandingTopMenu(
                            abaAtiva: _abaAtiva,
                            aoSelecionarAba: (aba) =>
                                setState(() => _abaAtiva = aba),
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}
