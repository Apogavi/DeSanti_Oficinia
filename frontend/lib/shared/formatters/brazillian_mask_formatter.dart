import 'package:flutter/services.dart';

class BrazilianMaskFormatter extends TextInputFormatter {
  BrazilianMaskFormatter.cpf() : this._('###.###.###-##', RegExp(r'[0-9]'));

  BrazilianMaskFormatter.phone() : this._('(##) #####-####', RegExp(r'[0-9]'));

  BrazilianMaskFormatter.plate()
    : this._('###-####', RegExp(r'[A-Za-z0-9]'), uppercase: true);

  BrazilianMaskFormatter._(this.mask, this.allowed, {this.uppercase = false});

  final String mask;
  final RegExp allowed;
  final bool uppercase;

  int get _maxLength => '#'.allMatches(mask).length;

  String _raw(String text) {
    final chars = text.split('').where(allowed.hasMatch).join();
    final normalized = uppercase ? chars.toUpperCase() : chars;
    return normalized.length > _maxLength
        ? normalized.substring(0, _maxLength)
        : normalized;
  }

  String _applyMask(String raw) {
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
