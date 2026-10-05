import 'package:flutter/material.dart';

import 'features/ordens_servico/nova_ordem_page.dart';
import 'theme/app_theme.dart';

void main() {
  runApp(const OficinaApp());
}

class OficinaApp extends StatelessWidget {
  const OficinaApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'DeSanti Automotiva',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: ThemeMode.system,
      home: const InicioPage(),
    );
  }
}

class InicioPage extends StatelessWidget {
  const InicioPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('DeSanti Automotiva')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Ordens de serviço',
              style: Theme.of(context).textTheme.headlineMedium,
            ),
            const SizedBox(height: 16),
            const Text('Nenhuma ordem de serviço cadastrada.'),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (context) => const NovaOrdemPage(),
                  ),
                );
              },
              icon: const Icon(Icons.add),
              label: const Text('Nova ordem de serviço'),
            ),
          ],
        ),
      ),
    );
  }
}
