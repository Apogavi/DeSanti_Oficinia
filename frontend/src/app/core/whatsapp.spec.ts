import assert from 'node:assert/strict';
import test from 'node:test';
import { criarLinkWhatsappOs, criarLinkWhatsappRetorno } from './whatsapp.ts';
import type { OrdemConsulta, RetornoOrdem } from './models.ts';

test('link do WhatsApp da OS inclui valores cobrados e nunca expõe custo interno', () => {
  const os: OrdemConsulta = {
    id: 42,
    nomeCliente: 'Cliente Exemplo',
    documentoCliente: '00000000000',
    telefoneCliente: '11987654321',
    placa: 'AAA0A00',
    marca: 'Fiat',
    modelo: 'Uno',
    cor: 'Prata',
    ano: 2020,
    descricaoServico: 'Troca da bomba d agua',
    maoDeObraCentavos: 15000,
    totalCentavos: 27000,
    dataCriacaoUtc: '2026-11-19T12:00:00Z',
    dataConclusaoUtc: null,
    dataPrevistaRetornoUtc: null,
    necessitaRetorno: false,
    prazoRetornoDias: null,
    status: 'aberta',
    pecas: [
      {
        nome: 'Bomba d agua',
        quantidade: 1,
        valorUnitarioCentavos: 0,
        valorTotalCentavos: 12000,
        custoUnitarioCentavos: 4321,
      },
    ],
  };

  const url = criarLinkWhatsappOs(os);
  assert.ok(url);
  assert.ok(url.startsWith('https://wa.me/5511987654321?text='));
  const texto = decodeURIComponent(url.split('?text=')[1]);
  assert.ok(texto.includes('Ordem de Serviço #42'));
  assert.ok(texto.includes('R$ 120,00'));
  assert.ok(texto.includes('R$ 270,00'));
  assert.ok(!texto.includes('43,21'));
  assert.ok(!texto.includes('custo'));
});

test('link do WhatsApp identifica orçamento quando status for orcamento', () => {
  const os: OrdemConsulta = {
    id: 99,
    nomeCliente: 'Cliente Exemplo',
    documentoCliente: '00000000000',
    telefoneCliente: '11987654321',
    placa: 'AAA0A00',
    marca: 'VW',
    modelo: 'Gol',
    cor: 'Preto',
    ano: 2019,
    descricaoServico: 'Revisão de freios',
    maoDeObraCentavos: 10000,
    totalCentavos: 10000,
    dataCriacaoUtc: '2026-11-19T12:00:00Z',
    dataConclusaoUtc: null,
    dataPrevistaRetornoUtc: null,
    necessitaRetorno: false,
    prazoRetornoDias: null,
    status: 'orcamento',
    pecas: [],
  };

  const url = criarLinkWhatsappOs(os);
  assert.ok(url);
  const texto = decodeURIComponent(url.split('?text=')[1]);
  assert.ok(texto.includes('Orçamento #99'));
});

test('link de retorno do WhatsApp formata placa e dados do veículo', () => {
  const retorno: RetornoOrdem = {
    id: 10,
    nomeCliente: 'Cliente Retorno',
    telefoneCliente: '(11) 91234-5678',
    placa: 'ABC1D23',
    marca: 'Ford',
    modelo: 'Ka',
    dataConclusaoUtc: '2026-11-01T12:00:00Z',
    dataPrevistaRetornoUtc: '2026-11-20T12:00:00Z',
    prazoRetornoDias: 19,
    descricaoServico: 'Troca de óleo',
  };

  const url = criarLinkWhatsappRetorno(retorno);
  assert.ok(url);
  assert.ok(url.startsWith('https://wa.me/5511912345678?text='));
  const texto = decodeURIComponent(url.split('?text=')[1]);
  assert.ok(texto.includes('ABC-1D23'));
  assert.ok(texto.includes('OS #10'));
});
