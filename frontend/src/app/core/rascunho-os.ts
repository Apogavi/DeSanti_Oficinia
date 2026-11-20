import type { PecaRascunho } from './models';

const STORAGE_KEY = 'desanti-os-rascunho-v1';

export interface RascunhoNovaOs {
  clienteId: number | null;
  buscaCliente: string;
  placa: string;
  marca: string;
  modelo: string;
  cor: string;
  ano: string;
  descricao: string;
  maoDeObraAvulsa: string;
  necessitaRetorno: boolean;
  prazoRetornoDias: string;
  ehOrcamento: boolean;
  idempotencyKey: string;
  pecas: PecaRascunho[];
}

export function gerarChaveIdempotencia(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return `os-${Date.now()}-${Math.random().toString(36).slice(2, 12)}`;
}

export function rascunhoTemConteudo(r: RascunhoNovaOs): boolean {
  return Boolean(
    r.clienteId !== null ||
      r.placa.trim() ||
      r.marca.trim() ||
      r.modelo.trim() ||
      r.cor.trim() ||
      r.ano.trim() ||
      r.descricao.trim() ||
      r.maoDeObraAvulsa.trim() ||
      r.necessitaRetorno ||
      r.prazoRetornoDias.trim() ||
      r.ehOrcamento ||
      r.pecas.length > 0,
  );
}

export function salvarRascunhoOs(rascunho: RascunhoNovaOs): void {
  try {
    if (!rascunhoTemConteudo(rascunho)) {
      localStorage.removeItem(STORAGE_KEY);
      return;
    }
    localStorage.setItem(STORAGE_KEY, JSON.stringify(rascunho));
  } catch {
    // Ignora falhas de quota/privacidade do navegador sem interromper o formulário.
  }
}

export function carregarRascunhoOs(): RascunhoNovaOs | null {
  try {
    const bruto = localStorage.getItem(STORAGE_KEY);
    if (!bruto) return null;
    const parsed = JSON.parse(bruto) as Partial<RascunhoNovaOs>;
    if (!parsed || typeof parsed !== 'object') return null;

    const rascunho: RascunhoNovaOs = {
      clienteId: typeof parsed.clienteId === 'number' ? parsed.clienteId : null,
      buscaCliente: typeof parsed.buscaCliente === 'string' ? parsed.buscaCliente : '',
      placa: typeof parsed.placa === 'string' ? parsed.placa : '',
      marca: typeof parsed.marca === 'string' ? parsed.marca : '',
      modelo: typeof parsed.modelo === 'string' ? parsed.modelo : '',
      cor: typeof parsed.cor === 'string' ? parsed.cor : '',
      ano: typeof parsed.ano === 'string' ? parsed.ano : '',
      descricao: typeof parsed.descricao === 'string' ? parsed.descricao : '',
      maoDeObraAvulsa: typeof parsed.maoDeObraAvulsa === 'string' ? parsed.maoDeObraAvulsa : '',
      necessitaRetorno: Boolean(parsed.necessitaRetorno),
      prazoRetornoDias: typeof parsed.prazoRetornoDias === 'string' ? parsed.prazoRetornoDias : '',
      ehOrcamento: Boolean(parsed.ehOrcamento),
      idempotencyKey:
        typeof parsed.idempotencyKey === 'string' && parsed.idempotencyKey.trim()
          ? parsed.idempotencyKey.trim()
          : gerarChaveIdempotencia(),
      pecas: Array.isArray(parsed.pecas) ? parsed.pecas : [],
    };
    return rascunhoTemConteudo(rascunho) ? rascunho : null;
  } catch {
    return null;
  }
}

export function limparRascunhoOs(): void {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Ignora falhas de acesso ao storage.
  }
}
