import 'dart:async';
import 'dart:convert';

import 'package:desanti_automotiva/core/network/api_service.dart';
import 'package:desanti_automotiva/features/clientes/criar_cliente_request.dart';
import 'package:desanti_automotiva/features/clientes/repositories/cliente_repository.dart';
import 'package:desanti_automotiva/features/ordens_servico/criar_ordem_servico_request.dart';
import 'package:desanti_automotiva/features/ordens_servico/repositories/ordem_servico_repository.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

const request = CriarOrdemServicoRequest(
  clienteId: 1,
  placa: 'AAA0A00',
  marca: 'Marca Teste',
  modelo: 'Modelo Teste',
  cor: 'Azul',
  ano: null,
  descricaoServico: 'Serviço de teste',
  pecas: [
    PecaRequest(
      nome: 'Peça teste',
      quantidade: 3,
      valorUnitarioCentavos: 101,
      custoUnitarioCentavos: 70,
    ),
  ],
  maoDeObraCentavos: 12000,
  necessitaRetorno: false,
  prazoRetornoDias: null,
);

void main() {
  test('GET de clientes respeita JSON camelCase da API', () async {
    final client = MockClient((r) async {
      expect(r.method, 'GET');
      expect(r.url.toString(), 'http://localhost:5085/api/v1/clientes');
      return http.Response(
        jsonEncode([
          {
            'id': 1,
            'nome': 'Cliente Teste',
            'documento': '00000000000',
            'telefone': '00000000000',
            'ativo': true,
          },
        ]),
        200,
        headers: {'content-type': 'application/json; charset=utf-8'},
      );
    });
    addTearDown(client.close);
    final clientes = await ApiClienteRepository(
      ApiService(client: client, baseUrl: 'http://localhost:5085'),
    ).listar();
    expect(clientes.single.nome, 'Cliente Teste');
    expect(clientes.single.ativo, isTrue);
  });

  test(
    'POST de cliente envia apenas dígitos em documento/telefone e email vazio',
    () async {
      final client = MockClient((r) async {
        expect(r.method, 'POST');
        expect(r.url.toString(), 'http://localhost:5085/api/v1/clientes');
        final json = jsonDecode(r.body) as Map<String, dynamic>;
        expect(json['nome'], 'Oficina Parceira');
        expect(json['documento'], '12345678000199');
        expect(json['telefone'], '11987654321');
        expect(json['email'], '');
        return http.Response(
          jsonEncode({
            'id': 7,
            'nome': 'Oficina Parceira',
            'documento': '12345678000199',
            'telefone': '11987654321',
            'ativo': true,
          }),
          201,
          headers: {'content-type': 'application/json; charset=utf-8'},
        );
      });
      addTearDown(client.close);
      final criado = await ApiClienteRepository(
        ApiService(client: client, baseUrl: 'http://localhost:5085'),
      ).criar(
        const CriarClienteRequest(
          nome: '  Oficina Parceira  ',
          documento: '12.345.678/0001-99',
          telefone: '(11) 98765-4321',
        ),
      );
      expect(criado.id, 7);
      expect(criado.documento, '12345678000199');
    },
  );

  test(
    'POST envia centavos inteiros e reconhece apenas 201 com id e total',
    () async {
      final client = MockClient((r) async {
        expect(r.method, 'POST');
        expect(r.url.path, '/api/v1/ordens-servico');
        final json = jsonDecode(r.body) as Map<String, dynamic>;
        expect(json['clienteId'], 1);
        expect(json['maoDeObraCentavos'], isA<int>());
        expect(json['pecas'][0]['valorUnitarioCentavos'], 101);
        expect(json['pecas'][0]['custoUnitarioCentavos'], 70);
        expect(json, isNot(contains('cpf')));
        return http.Response('{"id":42,"totalCentavos":12303}', 201);
      });
      addTearDown(client.close);
      final result = await ApiOrdemServicoRepository(
        ApiService(client: client, baseUrl: 'http://localhost:5085'),
      ).criar(request);
      expect(result.id, 42);
      expect(result.totalCentavos, 12303);
    },
  );

  test('400 mantém errors por campo e permite correção', () async {
    final client = MockClient(
      (_) async => http.Response(
        '{"errors":{"clienteId":["Selecione um cliente."]}}',
        400,
      ),
    );
    addTearDown(client.close);
    final repo = ApiOrdemServicoRepository(
      ApiService(client: client, baseUrl: 'http://localhost:5085'),
    );
    await expectLater(
      repo.criar(request),
      throwsA(
        isA<ApiException>()
            .having((e) => e.errors['clienteId'], 'errors', [
              'Selecione um cliente.',
            ])
            .having((e) => e.resultadoIncerto, 'resultadoIncerto', false),
      ),
    );
  });

  test('timeout faz uma tentativa e marca criação como incerta', () async {
    var chamadas = 0;
    final client = MockClient((_) async {
      chamadas++;
      throw TimeoutException('simulado');
    });
    addTearDown(client.close);
    final repo = ApiOrdemServicoRepository(
      ApiService(client: client, baseUrl: 'http://localhost:5085'),
    );
    await expectLater(
      repo.criar(request),
      throwsA(
        isA<ApiException>().having(
          (e) => e.resultadoIncerto,
          'resultadoIncerto',
          true,
        ),
      ),
    );
    expect(chamadas, 1);
  });

  for (final status in [200, 201, 500]) {
    test('resposta incompatível $status não autoriza reenviar a OS', () async {
      final client = MockClient((_) async => http.Response('{}', status));
      addTearDown(client.close);
      final repo = ApiOrdemServicoRepository(
        ApiService(client: client, baseUrl: 'http://localhost:5085'),
      );
      await expectLater(
        repo.criar(request),
        throwsA(
          isA<ApiException>().having(
            (e) => e.resultadoIncerto,
            'resultadoIncerto',
            true,
          ),
        ),
      );
    });
  }
}
