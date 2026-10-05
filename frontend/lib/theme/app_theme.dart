import 'package:flutter/material.dart';

/// Cores inspiradas na identidade visual da DeSanti Automotiva.
abstract final class AppTheme {
  static const Color navy = Color(0xFF102B42);
  static const Color deepNavy = Color(0xFF081C2E);
  static const Color gold = Color(0xFFF2B927);
  static const Color lightBackground = Color(0xFFF6F8FA);
  static const Color lightText = Color(0xFFF5F7FA);

  static ThemeData get light => _build(Brightness.light);
  static ThemeData get dark => _build(Brightness.dark);

  static ThemeData _build(Brightness brightness) {
    final isDark = brightness == Brightness.dark;
    final scheme = ColorScheme.fromSeed(
      seedColor: isDark ? gold : navy,
      brightness: brightness,
    ).copyWith(
      primary: isDark ? gold : navy,
      onPrimary: isDark ? deepNavy : Colors.white,
      secondary: gold,
      onSecondary: deepNavy,
      surface: isDark ? deepNavy : lightBackground,
      onSurface: isDark ? lightText : deepNavy,
      surfaceContainerLow: isDark ? navy : Colors.white,
      outline: isDark ? const Color(0xFF91A3B1) : const Color(0xFF6F7F8D),
    );

    final fieldBorder = OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: BorderSide(color: scheme.outline),
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: scheme,
      scaffoldBackgroundColor: scheme.surface,
      appBarTheme: const AppBarTheme(
        backgroundColor: navy,
        foregroundColor: Colors.white,
        centerTitle: false,
      ),
      cardTheme: CardThemeData(
        color: scheme.surfaceContainerLow,
        elevation: 1,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: scheme.surfaceContainerLow,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: fieldBorder,
        enabledBorder: fieldBorder,
        focusedBorder: fieldBorder.copyWith(
          borderSide: BorderSide(color: scheme.primary, width: 2),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: scheme.primary,
          foregroundColor: scheme.onPrimary,
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: scheme.primary,
          side: BorderSide(color: scheme.primary),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
        ),
      ),
    );
  }
}
