import 'package:flutter/services.dart';

class BrazilianMaskFormatter extends TextInputFormatter {
  BrazilianMaskFormatter.cpf() : this._(['###.###.###-##'], RegExp(r'[0-9]'));

  BrazilianMaskFormatter.cpfOrCnpj()
    : this._(['###.###.###-##', '##.###.###/####-##'], RegExp(r'[0-9]'));

  BrazilianMaskFormatter.phone()
    : this._(['(##) ####-####', '(##) #####-####'], RegExp(r'[0-9]'));

  BrazilianMaskFormatter.plate()
    : this._(['###-####'], RegExp(r'[A-Za-z0-9]'), uppercase: true);

  BrazilianMaskFormatter._(
    this.masks,
    this.allowed, {
    this.uppercase = false,
  }) : assert(masks.isNotEmpty);

  final List<String> masks;
  final RegExp allowed;
  final bool uppercase;

  static String somenteDigitos(String value) =>
      value.replaceAll(RegExp(r'[^0-9]'), '');

  static String formatarCpfOuCnpj(String value) {
    final digitos = somenteDigitos(value);
    if (digitos.isEmpty) return '';
    final formatter = BrazilianMaskFormatter.cpfOrCnpj();
    return formatter._applyMask(formatter._raw(digitos));
  }

  static String formatarTelefone(String value) {
    final digitos = somenteDigitos(value);
    if (digitos.isEmpty) return '';
    final formatter = BrazilianMaskFormatter.phone();
    return formatter._applyMask(formatter._raw(digitos));
  }

  int _maskCapacity(String mask) => '#'.allMatches(mask).length;

  int get _maxLength =>
      masks.map(_maskCapacity).reduce((a, b) => a > b ? a : b);

  String _activeMask(int rawLength) {
    for (final mask in masks) {
      if (rawLength <= _maskCapacity(mask)) return mask;
    }
    return masks.last;
  }

  String _raw(String text) {
    final chars = text.split('').where(allowed.hasMatch).join();
    final normalized = uppercase ? chars.toUpperCase() : chars;
    return normalized.length > _maxLength
        ? normalized.substring(0, _maxLength)
        : normalized;
  }

  String _applyMask(String raw) {
    final mask = _activeMask(raw.length);
    final result = StringBuffer();
    var index = 0;

    for (final char in mask.split('')) {
      if (index >= raw.length) break;
      if (char == '#') {
        result.write(raw[index]);
        index++;
      } else {
        result.write(char);
      }
    }
    return result.toString();
  }

  int _cursorOffset(String formatted, int rawCount) {
    if (rawCount <= 0) return 0;
    var seen = 0;
    for (var i = 0; i < formatted.length; i++) {
      if (allowed.hasMatch(formatted[i])) seen++;
      if (seen == rawCount) return i + 1;
    }
    return formatted.length;
  }

  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    if (!newValue.composing.isCollapsed) return newValue;

    final oldRaw = _raw(oldValue.text);
    var newRaw = _raw(newValue.text);
    final cursor = newValue.selection.isValid
        ? newValue.selection.extentOffset
        : newValue.text.length;
    final safeCursor = cursor.clamp(0, newValue.text.length);
    var rawBeforeCursor = _raw(newValue.text.substring(0, safeCursor)).length;
    if (rawBeforeCursor > newRaw.length) rawBeforeCursor = newRaw.length;

    if (newRaw == oldRaw &&
        newValue.text.length == oldValue.text.length - 1 &&
        oldValue.selection.isCollapsed) {
      final backspace =
          newValue.selection.extentOffset < oldValue.selection.extentOffset;
      final index = backspace ? rawBeforeCursor - 1 : rawBeforeCursor;
      if (index >= 0 && index < newRaw.length) {
        newRaw = newRaw.replaceRange(index, index + 1, '');
        if (backspace) rawBeforeCursor--;
      }
    }

    final formatted = _applyMask(newRaw);
    return TextEditingValue(
      text: formatted,
      selection: TextSelection.collapsed(
        offset: _cursorOffset(formatted, rawBeforeCursor),
      ),
    );
  }
}
