import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

class MoneyInputFormatter extends TextInputFormatter {
  const MoneyInputFormatter();

  static final RegExp _notDigit = RegExp(r'\D');
  static const int maxDigits = 12;

  static int? cents(String? value) {
    final digits = (value ?? '').replaceAll(_notDigit, '');
    if (digits.isEmpty || digits.length > maxDigits) return null;
    return int.tryParse(digits);
  }

  static String formatCents(int cents) {
    if (cents < 0) throw ArgumentError.value(cents, 'cents');

    final digits = cents.toString().padLeft(3, '0');
    final whole = digits.substring(0, digits.length - 2);
    final decimal = digits.substring(digits.length - 2);
    final grouped = StringBuffer();

    for (var i = 0; i < whole.length; i++) {
      if (i > 0 && (whole.length - i) % 3 == 0) grouped.write('.');
      grouped.write(whole[i]);
    }

    return '${grouped.toString()},$decimal';
  }

  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    if (!newValue.composing.isCollapsed) return newValue;

    final digits = newValue.text.replaceAll(_notDigit, '');

    if (oldValue.text == '0,00' && newValue.text == '0,0') {
      return const TextEditingValue(
        text: '',
        selection: TextSelection.collapsed(offset: 0),
      );
    }

    if (digits.length > maxDigits) return oldValue;

    if (digits.isEmpty) {
      return const TextEditingValue(
        text: '',
        selection: TextSelection.collapsed(offset: 0),
      );
    }

    final formatted = formatCents(int.parse(digits));
    return TextEditingValue(
      text: formatted,
      selection: TextSelection.collapsed(offset: formatted.length),
    );
  }
}
