export function somenteDigitos(valor: string | null | undefined): string {
  return (valor ?? '').replace(/[^0-9]/g, '');
}

export function formatarCpfOuCnpj(valor: string | null | undefined): string {
  const d = somenteDigitos(valor).slice(0, 14);
  if (!d) return '';
  if (d.length <= 11) {
    return d
      .replace(/^(\d{3})(\d)/, '$1.$2')
      .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
      .replace(/\.(\d{3})(\d)/, '.$1-$2');
  }
  return d
    .replace(/^(\d{2})(\d)/, '$1.$2')
    .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1/$2')
    .replace(/(\d{4})(\d)/, '$1-$2');
}

export function formatarTelefone(valor: string | null | undefined): string {
  const d = somenteDigitos(valor).slice(0, 11);
  if (!d) return '';
  if (d.length <= 2) return `(${d}`;
  if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
  if (d.length <= 10) {
    return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  }
  return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
}

export function formatarPlacaInput(valor: string | null | undefined): string {
  const limpo = (valor ?? '').replace(/[^A-Za-z0-9]/g, '').toUpperCase().slice(0, 7);
  if (limpo.length <= 3) return limpo;
  return `${limpo.slice(0, 3)}-${limpo.slice(3)}`;
}

export function normalizarPlaca(valor: string | null | undefined): string {
  return (valor ?? '').replace(/[\s-]/g, '').toUpperCase();
}

/**
 * Converte digitação monetária em centavos inteiros (sem float/double).
 * Ex.: "12000" -> 12000 centavos -> "120,00"
 */
export function textoParaCentavos(valor: string | null | undefined): number | null {
  const digitos = somenteDigitos(valor);
  if (!digitos) return null;
  const centavos = Number.parseInt(digitos, 10);
  if (!Number.isSafeInteger(centavos) || centavos < 0) return null;
  return centavos;
}

export function formatarCentavos(centavos: number | bigint): string {
  const b = typeof centavos === 'bigint' ? centavos : BigInt(Math.trunc(centavos));
  const negativo = b < 0n;
  const abs = negativo ? -b : b;
  const reais = abs / 100n;
  const resto = (abs % 100n).toString().padStart(2, '0');
  const reaisFormatado = reais.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  return `${negativo ? '-' : ''}${reaisFormatado},${resto}`;
}

export function formatarMoedaInput(valor: string | null | undefined): string {
  const centavos = textoParaCentavos(valor);
  if (centavos === null) return '';
  return formatarCentavos(centavos);
}
