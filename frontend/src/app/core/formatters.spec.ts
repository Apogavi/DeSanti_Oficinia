import assert from 'node:assert/strict';
import test from 'node:test';
import {
  formatarCentavos,
  formatarCpfOuCnpj,
  formatarMoedaInput,
  formatarPlacaInput,
  formatarTelefone,
  normalizarPlaca,
  textoParaCentavos,
} from './formatters.ts';

test('formata CPF (11 dígitos) e CNPJ (14 dígitos) dinamicamente', () => {
  assert.equal(formatarCpfOuCnpj('12345678901'), '123.456.789-01');
  assert.equal(formatarCpfOuCnpj('12345678000199'), '12.345.678/0001-99');
  assert.equal(formatarCpfOuCnpj(''), '');
});

test('formata telefone fixo (10 dígitos) e celular (11 dígitos)', () => {
  assert.equal(formatarTelefone('1134567890'), '(11) 3456-7890');
  assert.equal(formatarTelefone('11987654321'), '(11) 98765-4321');
});

test('formata e normaliza placa brasileira (antiga e Mercosul)', () => {
  assert.equal(formatarPlacaInput('aaa0a00'), 'AAA-0A00');
  assert.equal(normalizarPlaca('aaa-0a00'), 'AAA0A00');
});

test('converte e formata centavos inteiros sem float/double', () => {
  assert.equal(textoParaCentavos('120,00'), 12000);
  assert.equal(textoParaCentavos('0'), 0);
  assert.equal(textoParaCentavos(''), null);
  assert.equal(formatarCentavos(12000), '120,00');
  assert.equal(formatarCentavos(123456789n), '1.234.567,89');
  assert.equal(formatarCentavos(-5050), '-50,50');
  assert.equal(formatarMoedaInput('12000'), '120,00');
});
