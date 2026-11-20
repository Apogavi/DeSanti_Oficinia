import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

/// Erro compreensível pela aplicação, independente de widgets ou BuildContext.
class ApiException implements Exception {
  const ApiException(
    this.message, {
    this.errors = const {},
    this.resultadoIncerto = false,
  });

  final String message;
  final Map<String, List<String>> errors;
  final bool resultadoIncerto;
}

/// Centraliza HTTP/JSON. O Client é injetado e encerrado pelo composition root.
class ApiService {
  ApiService({
    required this.client,
    required String baseUrl,
    this.timeout = const Duration(seconds: 20),
  }) : _baseUri = Uri.parse(baseUrl.endsWith('/') ? baseUrl : '$baseUrl/') {
    if (!['http', 'https'].contains(_baseUri.scheme) || _baseUri.host.isEmpty) {
      throw ArgumentError(
        'API_BASE_URL deve ser uma URL HTTP ou HTTPS válida.',
      );
    }
  }

  final http.Client client;
  final Uri _baseUri;
  final Duration timeout;

  Future<dynamic> get(String path) => _send(path);

  Future<dynamic> post(String path, Map<String, dynamic> body) =>
      _send(path, body: body);

  Future<dynamic> _send(String path, {Map<String, dynamic>? body}) async {
    final criando = body != null;
    try {
      final uri = _baseUri.resolve(path);
      const headers = {'Accept': 'application/json'};
      // Não usar RetryClient: repetir um POST pode criar uma segunda OS.
      final response =
          await (criando
                  ? client.post(
                      uri,
                      headers: {...headers, 'Content-Type': 'application/json'},
                      body: jsonEncode(body),
                    )
                  : client.get(uri, headers: headers))
              .timeout(timeout);

      dynamic data;
      try {
        data = response.bodyBytes.isEmpty
            ? null
            : jsonDecode(utf8.decode(response.bodyBytes));
      } on FormatException {
        throw ApiException(
          'A API retornou uma resposta inválida.',
          resultadoIncerto: criando,
        );
      }

      if ((criando && response.statusCode != 201) ||
          (!criando && response.statusCode != 200)) {
        final errors = <String, List<String>>{};
        if (data is Map<String, dynamic> && data['errors'] is Map) {
          for (final entry in (data['errors'] as Map).entries) {
            if (entry.value is List) {
              errors[entry.key.toString()] = (entry.value as List)
                  .whereType<String>()
                  .toList();
            }
          }
        }
        final message = switch (response.statusCode) {
          400 => 'Confira os campos informados.',
          404 => 'O cliente selecionado não foi encontrado.',
          409 => 'O cliente selecionado está inativo.',
          _ => 'Não foi possível concluir a operação na API.',
        };
        throw ApiException(
          message,
          errors: errors,
          resultadoIncerto:
              criando && ![400, 404, 409].contains(response.statusCode),
        );
      }
      return data;
    } on TimeoutException {
      throw ApiException(
        'A API demorou para responder.',
        resultadoIncerto: criando,
      );
    } on http.ClientException {
      throw ApiException(
        'Não foi possível conectar à API.',
        resultadoIncerto: criando,
      );
    }
  }
}
