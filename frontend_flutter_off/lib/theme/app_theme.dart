import 'package:flutter/material.dart';

/// Cores e tokens semânticos inspirados na identidade visual da DeSanti Automotiva.
///
/// Projetado com a diretriz Design Studio (Material 3 + Apple HIG) priorizando
/// alto contraste para operação em oficina e leveza gráfica (zero blur em GPU)
/// para computadores modestos.
abstract final class AppTheme {
  static const Color navy = Color(0xFF102B42);
  static const Color deepNavy = Color(0xFF081C2E);
  static const Color gold = Color(0xFFF2B927);
  static const Color lightBackground = Color(0xFFF6F8FA);
  static const Color lightText = Color(0xFFF5F7FA);

  static const List<FontFeature> tabularFigures = [
    FontFeature.tabularFigures(),
  ];

  static ThemeData get light => _build(Brightness.light);
  static ThemeData get dark => _build(Brightness.dark);

  static ThemeData _build(Brightness brightness) {
    final isDark = brightness == Brightness.dark;
    final scheme =
        ColorScheme.fromSeed(
          seedColor: isDark ? gold : navy,
          brightness: brightness,
        ).copyWith(
          primary: isDark ? gold : navy,
          onPrimary: isDark ? deepNavy : Colors.white,
          primaryContainer: isDark
              ? const Color(0xFF1C3F60)
              : const Color(0xFFDCE7F2),
          onPrimaryContainer: isDark ? lightText : deepNavy,
          secondary: gold,
          onSecondary: deepNavy,
          secondaryContainer: isDark
              ? const Color(0xFF3A3014)
              : const Color(0xFFFEF3D1),
          onSecondaryContainer: isDark ? const Color(0xFFFCE39C) : deepNavy,
          surface: isDark ? deepNavy : lightBackground,
          onSurface: isDark ? lightText : deepNavy,
          onSurfaceVariant: isDark
              ? const Color(0xFFB8C7D6)
              : const Color(0xFF475868),
          surfaceContainerLowest: isDark
              ? const Color(0xFF0B2236)
              : Colors.white,
          surfaceContainerLow: isDark ? navy : Colors.white,
          surfaceContainer: isDark
              ? const Color(0xFF153550)
              : const Color(0xFFEEF2F6),
          surfaceContainerHigh: isDark
              ? const Color(0xFF1D4160)
              : const Color(0xFFE3E9F0),
          outline: isDark ? const Color(0xFF91A3B1) : const Color(0xFF6F7F8D),
          outlineVariant: isDark
              ? const Color(0xFF24435F)
              : const Color(0xFFD5DEE7),
          error: isDark ? const Color(0xFFFFB4AB) : const Color(0xFFB3261E),
          onError: isDark ? const Color(0xFF690005) : Colors.white,
          errorContainer: isDark
              ? const Color(0xFF4E1513)
              : const Color(0xFFFCEEEE),
          onErrorContainer: isDark
              ? const Color(0xFFFFDAD6)
              : const Color(0xFF601410),
        );

    final baseTextTheme = isDark
        ? Typography.material2021().white
        : Typography.material2021().black;

    final textTheme = baseTextTheme.copyWith(
      headlineMedium: TextStyle(
        fontSize: 26,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.5,
        height: 1.2,
        color: scheme.onSurface,
      ),
      headlineSmall: TextStyle(
        fontSize: 22,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.4,
        height: 1.25,
        color: scheme.onSurface,
      ),
      titleLarge: TextStyle(
        fontSize: 19,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.3,
        height: 1.3,
        color: scheme.onSurface,
      ),
      titleMedium: TextStyle(
        fontSize: 16,
        fontWeight: FontWeight.w600,
        letterSpacing: -0.15,
        height: 1.35,
        color: scheme.onSurface,
      ),
      titleSmall: TextStyle(
        fontSize: 14,
        fontWeight: FontWeight.w600,
        letterSpacing: -0.1,
        height: 1.35,
        color: scheme.onSurface,
      ),
      bodyLarge: TextStyle(
        fontSize: 16,
        fontWeight: FontWeight.w400,
        height: 1.45,
        color: scheme.onSurface,
      ),
      bodyMedium: TextStyle(
        fontSize: 14,
        fontWeight: FontWeight.w400,
        height: 1.45,
        color: scheme.onSurface,
      ),
      bodySmall: TextStyle(
        fontSize: 12.5,
        fontWeight: FontWeight.w500,
        height: 1.4,
        color: scheme.onSurfaceVariant,
      ),
      labelLarge: TextStyle(
        fontSize: 15,
        fontWeight: FontWeight.w600,
        letterSpacing: 0.1,
        color: scheme.onSurface,
      ),
    );

    final fieldBorder = OutlineInputBorder(
      borderRadius: BorderRadius.circular(10),
      borderSide: BorderSide(color: scheme.outlineVariant, width: 1),
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: scheme,
      textTheme: textTheme,
      scaffoldBackgroundColor: scheme.surface,
      appBarTheme: AppBarTheme(
        backgroundColor: isDark ? deepNavy : navy,
        foregroundColor: Colors.white,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: const TextStyle(
          fontSize: 17,
          fontWeight: FontWeight.w600,
          letterSpacing: -0.25,
          color: Colors.white,
        ),
        shape: Border(
          bottom: BorderSide(
            color: isDark ? const Color(0xFF1A334B) : const Color(0xFF1D3F5E),
          ),
        ),
      ),
      cardTheme: CardThemeData(
        color: scheme.surfaceContainerLow,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(color: scheme.outlineVariant),
        ),
      ),
      dividerTheme: DividerThemeData(
        color: scheme.outlineVariant,
        thickness: 1,
        space: 1,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: scheme.surfaceContainerLowest,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 14,
          vertical: 14,
        ),
        labelStyle: TextStyle(
          color: scheme.onSurfaceVariant,
          fontWeight: FontWeight.w500,
          fontSize: 14.5,
        ),
        floatingLabelStyle: TextStyle(
          color: scheme.primary,
          fontWeight: FontWeight.w600,
        ),
        helperStyle: TextStyle(color: scheme.onSurfaceVariant, fontSize: 12),
        errorStyle: TextStyle(
          color: scheme.error,
          fontWeight: FontWeight.w500,
          fontSize: 12,
        ),
        border: fieldBorder,
        enabledBorder: fieldBorder.copyWith(
          borderSide: BorderSide(color: scheme.outline.withValues(alpha: 0.55)),
        ),
        disabledBorder: fieldBorder.copyWith(
          borderSide: BorderSide(color: scheme.outlineVariant),
        ),
        focusedBorder: fieldBorder.copyWith(
          borderSide: BorderSide(color: scheme.primary, width: 1.8),
        ),
        errorBorder: fieldBorder.copyWith(
          borderSide: BorderSide(color: scheme.error, width: 1.4),
        ),
        focusedErrorBorder: fieldBorder.copyWith(
          borderSide: BorderSide(color: scheme.error, width: 1.8),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: scheme.primary,
          foregroundColor: scheme.onPrimary,
          disabledBackgroundColor: scheme.surfaceContainerHigh,
          disabledForegroundColor: scheme.onSurfaceVariant,
          elevation: 0,
          minimumSize: const Size(64, 48),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          textStyle: const TextStyle(
            fontSize: 14.5,
            fontWeight: FontWeight.w600,
            letterSpacing: -0.1,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: scheme.onSurface,
          side: BorderSide(color: scheme.outline.withValues(alpha: 0.7)),
          minimumSize: const Size(64, 44),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: scheme.primary,
          minimumSize: const Size(48, 42),
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
      ),
      checkboxTheme: CheckboxThemeData(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: scheme.surfaceContainerLow,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: BorderSide(color: scheme.outlineVariant),
        ),
        titleTextStyle: textTheme.titleLarge,
      ),
    );
  }
}
