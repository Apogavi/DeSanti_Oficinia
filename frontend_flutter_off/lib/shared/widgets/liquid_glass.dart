import 'dart:ui';

import 'package:flutter/material.dart';

/// Aba ativa na navegação principal suspensa estilo Apple.
enum AbaPrincipal { ordensServico, clientes }

/// Backdrop ambiental sutil (Regra 1 do Liquid Glass: o vidro precisa de um
/// fundo com variações suaves de luz para refletir e refratar).
class LiquidGlassBackdrop extends StatelessWidget {
  const LiquidGlassBackdrop({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final bgColor = isDark ? const Color(0xFF0A0F16) : const Color(0xFFECEFF4);

    return DecoratedBox(
      decoration: BoxDecoration(color: bgColor),
      child: Stack(
        fit: StackFit.expand,
        children: [
          Positioned.fill(
            child: IgnorePointer(
              child: CustomPaint(
                painter: _AmbientMeshPainter(isDark: isDark),
              ),
            ),
          ),
          child,
        ],
      ),
    );
  }
}

class _AmbientMeshPainter extends CustomPainter {
  const _AmbientMeshPainter({required this.isDark});

  final bool isDark;

  @override
  void paint(Canvas canvas, Size size) {
    void drawGlow(Offset center, double radius, Color color) {
      final rect = Rect.fromCircle(center: center, radius: radius);
      final paint = Paint()
        ..shader = RadialGradient(
          colors: [color, color.withValues(alpha: 0)],
          stops: const [0.0, 1.0],
        ).createShader(rect);
      canvas.drawCircle(center, radius, paint);
    }

    final shortest = size.shortestSide;
    // Glows sutis inspirados na paleta DeSanti + Apple Liquid Glass mesh
    drawGlow(
      Offset(size.width * 0.25, size.height * 0.08),
      shortest * 0.55,
      isDark
          ? const Color(0xFF1E4E79).withValues(alpha: 0.30)
          : const Color(0xFF3B82F6).withValues(alpha: 0.12),
    );
    drawGlow(
      Offset(size.width * 0.72, size.height * 0.14),
      shortest * 0.45,
      isDark
          ? const Color(0xFFF2B927).withValues(alpha: 0.14)
          : const Color(0xFFF2B927).withValues(alpha: 0.12),
    );
    drawGlow(
      Offset(size.width * 0.50, size.height * 0.75),
      shortest * 0.60,
      isDark
          ? const Color(0xFF0E7490).withValues(alpha: 0.16)
          : const Color(0xFF06B6D4).withValues(alpha: 0.09),
    );
  }

  @override
  bool shouldRepaint(covariant _AmbientMeshPainter oldDelegate) =>
      oldDelegate.isDark != isDark;
}

/// Superfície Liquid Glass (Tier Menu/Floating Bar) com Specular Rim
/// (borda polida iluminada no topo e reflexo inferior).
class LiquidGlassSurface extends StatelessWidget {
  const LiquidGlassSurface({
    super.key,
    required this.child,
    this.borderRadius = const BorderRadius.all(Radius.circular(999)),
    this.padding = EdgeInsets.zero,
    this.blurSigma = 12.0,
  });

  final Widget child;
  final BorderRadius borderRadius;
  final EdgeInsetsGeometry padding;
  final double blurSigma;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    // Wash translúcido + tint incolor de topo/base conforme liquid-glass.css
    final topTint = isDark
        ? const Color(0xFF1A2332).withValues(alpha: 0.72)
        : const Color(0xFFFDFEFF).withValues(alpha: 0.76);
    final bottomTint = isDark
        ? const Color(0xFF111823).withValues(alpha: 0.68)
        : const Color(0xFFF1F5F9).withValues(alpha: 0.70);

    return Container(
      decoration: BoxDecoration(
        borderRadius: borderRadius,
        boxShadow: [
          BoxShadow(
            color: isDark
                ? Colors.black.withValues(alpha: 0.42)
                : const Color(0xFF14161E).withValues(alpha: 0.12),
            blurRadius: 24,
            offset: const Offset(0, 8),
          ),
          BoxShadow(
            color: isDark
                ? Colors.black.withValues(alpha: 0.25)
                : const Color(0xFF14161E).withValues(alpha: 0.05),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: borderRadius,
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: blurSigma, sigmaY: blurSigma),
          child: CustomPaint(
            foregroundPainter: _SpecularRimPainter(
              borderRadius: borderRadius,
              isDark: isDark,
            ),
            child: DecoratedBox(
              decoration: BoxDecoration(
                borderRadius: borderRadius,
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [topTint, bottomTint],
                ),
              ),
              child: Material(
                type: MaterialType.transparency,
                child: Padding(padding: padding, child: child),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _SpecularRimPainter extends CustomPainter {
  const _SpecularRimPainter({
    required this.borderRadius,
    required this.isDark,
  });

  final BorderRadius borderRadius;
  final bool isDark;

  @override
  void paint(Canvas canvas, Size size) {
    final rect = Offset.zero & size;
    final rrect = borderRadius.toRRect(rect.deflate(0.5));

    // Hairline externa
    final outerBorder = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.0
      ..color = isDark
          ? Colors.white.withValues(alpha: 0.12)
          : const Color(0xFF1E212D).withValues(alpha: 0.11);
    canvas.drawRRect(rrect, outerBorder);

    // Specular rim interno (brilho branco polido no topo e suave na base)
    final innerRRect = borderRadius.toRRect(rect.deflate(1.2));
    final specularPaint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.0
      ..shader = LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [
          Colors.white.withValues(alpha: isDark ? 0.48 : 0.72),
          Colors.white.withValues(alpha: isDark ? 0.08 : 0.16),
          Colors.white.withValues(alpha: isDark ? 0.14 : 0.25),
        ],
        stops: const [0.0, 0.55, 1.0],
      ).createShader(rect);
    canvas.drawRRect(innerRRect, specularPaint);
  }

  @override
  bool shouldRepaint(covariant _SpecularRimPainter oldDelegate) =>
      oldDelegate.isDark != isDark || oldDelegate.borderRadius != borderRadius;
}

/// Menu suspenso no topo estilo Apple Liquid Glass:
/// Inicia recolhido exibindo apenas o botão sanduíche (3 traços) e, ao clicar,
/// expande na horizontal revelando as opções de navegação.
class AppleExpandingTopMenu extends StatefulWidget {
  const AppleExpandingTopMenu({
    super.key,
    required this.abaAtiva,
    required this.aoSelecionarAba,
  });

  final AbaPrincipal abaAtiva;
  final ValueChanged<AbaPrincipal> aoSelecionarAba;

  @override
  State<AppleExpandingTopMenu> createState() => _AppleExpandingTopMenuState();
}

class _AppleExpandingTopMenuState extends State<AppleExpandingTopMenu> {
  bool _expandido = false;

  void _alternarMenu() {
    setState(() => _expandido = !_expandido);
  }

  void _escolher(AbaPrincipal aba) {
    widget.aoSelecionarAba(aba);
    setState(() => _expandido = false);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    return Semantics(
      container: true,
      label: 'Menu principal de navegação',
      child: LiquidGlassSurface(
        borderRadius: BorderRadius.circular(999),
        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 6),
        child: AnimatedSize(
          duration: const Duration(milliseconds: 220),
          curve: const Cubic(0.16, 1, 0.3, 1),
          alignment: Alignment.centerLeft,
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Botão sanduíche (3 traços) estilo Apple
              Tooltip(
                message: _expandido ? 'Recolher menu' : 'Abrir menu de navegação',
                child: InkWell(
                  key: const ValueKey('menu_sanduiche_apple'),
                  borderRadius: BorderRadius.circular(999),
                  onTap: _alternarMenu,
                  child: Container(
                    width: 40,
                    height: 40,
                    alignment: Alignment.center,
                    child: AnimatedSwitcher(
                      duration: const Duration(milliseconds: 160),
                      child: Icon(
                        _expandido ? Icons.close_rounded : Icons.menu_rounded,
                        key: ValueKey(_expandido),
                        size: 22,
                        color: scheme.onSurface,
                      ),
                    ),
                  ),
                ),
              ),
              if (_expandido) ...[
                const SizedBox(width: 6),
                Flexible(
                  child: SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        _ItemMenuHorizontal(
                          rotulo: 'Ordens de serviço',
                          icone: Icons.assignment_outlined,
                          ativo: widget.abaAtiva == AbaPrincipal.ordensServico,
                          isDark: isDark,
                          aoClicar: () => _escolher(AbaPrincipal.ordensServico),
                        ),
                        const SizedBox(width: 4),
                        _ItemMenuHorizontal(
                          rotulo: 'Clientes',
                          icone: Icons.people_outline_rounded,
                          ativo: widget.abaAtiva == AbaPrincipal.clientes,
                          isDark: isDark,
                          aoClicar: () => _escolher(AbaPrincipal.clientes),
                        ),
                        const SizedBox(width: 4),
                      ],
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

/// Regra 3 do Liquid Glass: os filhos dentro de um painel de vidro não pintam
/// borda opaca própria; apenas o item ativo recebe uma cápsula translúcida sutil.
class _ItemMenuHorizontal extends StatelessWidget {
  const _ItemMenuHorizontal({
    required this.rotulo,
    required this.icone,
    required this.ativo,
    required this.isDark,
    required this.aoClicar,
  });

  final String rotulo;
  final IconData icone;
  final bool ativo;
  final bool isDark;
  final VoidCallback aoClicar;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    final corTexto = ativo
        ? (isDark ? Colors.white : const Color(0xFF0F172A))
        : scheme.onSurfaceVariant;

    final corCapsulaAtiva = ativo
        ? (isDark
              ? Colors.white.withValues(alpha: 0.14)
              : const Color(0xFF102B42).withValues(alpha: 0.10))
        : Colors.transparent;

    return InkWell(
      borderRadius: BorderRadius.circular(999),
      onTap: aoClicar,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 160),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 9),
        decoration: BoxDecoration(
          color: corCapsulaAtiva,
          borderRadius: BorderRadius.circular(999),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icone, size: 18, color: corTexto),
            const SizedBox(width: 8),
            Text(
              rotulo,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: corTexto,
                fontWeight: ativo ? FontWeight.w700 : FontWeight.w500,
                letterSpacing: -0.2,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
