import assert from 'node:assert/strict';
import test from 'node:test';
import { gerarChaveIdempotencia, rascunhoTemConteudo } from './rascunho-os.ts';
import type { RascunhoNovaOs } from './rascunho-os.ts';

test('gerarChaveIdempotencia produz chaves únicas e dentro do limite de 64 chars', () => {
  const a = gerarChaveIdempotencia();
  const b = gerarChaveIdempotencia();
  assert.notEqual(a, b);
  assert.ok(a.length > 0 && a.length <= 64);
});

test('rascunhoTemConteudo distingue rascunho vazio de preenchido', () => {
  const vazio: RascunhoNovaOs = {
    clienteId: null,
    buscaCliente: '',
    placa: '',
    marca: '',
    modelo: '',
    cor: '',
    ano: '',
    descricao: '',
    maoDeObraAvulsa: '',
    necessitaRetorno: false,
    prazoRetornoDias: '',
    ehOrcamento: false,
    idempotencyKey: 'k1',
    pecas: [],
  };

  assert.equal(rascunhoTemConteudo(vazio), false);
  assert.equal(rascunhoTemConteudo({ ...vazio, placa: 'AAA-0A00' }), true);
  assert.equal(rascunhoTemConteudo({ ...vazio, ehOrcamento: true }), true);
});
