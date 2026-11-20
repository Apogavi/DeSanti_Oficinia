import { formatarCentavos, formatarPlacaInput, somenteDigitos } from './formatters';
import type { OrdemConsulta, RetornoOrdem } from './models';

function formatarDataBr(isoUtc: string | null | undefined): string | null {
  if (!isoUtc) return null;
  const data = new Date(isoUtc);
  if (Number.isNaN(data.getTime())) return null;
  return data.toLocaleDateString('pt-BR');
}

function normalizarTelefoneWhatsapp(telefone: string | null | undefined): string | null {
  const digitos = somenteDigitos(telefone);
  if (digitos.length !== 10 && digitos.length !== 11) return null;
  return `55${digitos}`;
}

/**
 * Monta link do WhatsApp com resumo da OS destinado ao cliente.
 * Nunca inclui custos internos da oficina (custoUnitarioCentavos).
 */
export function criarLinkWhatsappOs(os: OrdemConsulta): string | null {
  const fone = normalizarTelefoneWhatsapp(os.telefoneCliente);
  if (!fone) return null;

  const ehOrcamento = os.status === 'orcamento';
  const titulo = ehOrcamento
    ? `*DeSanti Automotiva — Orçamento #${os.id}*`
    : `*DeSanti Automotiva — Ordem de Serviço #${os.id}*`;
  const statusTexto = ehOrcamento
    ? 'Orçamento (aguardando aprovação)'
    : os.dataConclusaoUtc
      ? 'Concluída'
      : 'Em aberto';

  const linhas: string[] = [
    titulo,
    `Cliente: ${os.nomeCliente}`,
    `Veículo: ${os.marca} ${os.modelo}${os.cor ? ` (${os.cor})` : ''} · Placa ${formatarPlacaInput(os.placa)}`,
    `Status: ${statusTexto}`,
    '',
    `*Serviço:* ${os.descricaoServico}`,
  ];

  if (os.pecas.length > 0) {
    linhas.push('', '*Peças:*');
    for (const p of os.pecas) {
      const totalItem =
        p.valorTotalCentavos != null
          ? BigInt(p.valorTotalCentavos)
          : BigInt(p.quantidade) * BigInt(p.valorUnitarioCentavos);
      linhas.push(`• ${p.nome} (${p.quantidade} un.) — R$ ${formatarCentavos(totalItem)}`);
    }
  }

  linhas.push(
    '',
    `Mão de obra: R$ ${formatarCentavos(os.maoDeObraCentavos)}`,
    `*Total: R$ ${formatarCentavos(os.totalCentavos)}*`,
  );

  const retorno = formatarDataBr(os.dataPrevistaRetornoUtc);
  if (retorno) {
    linhas.push(`Previsão de retorno: ${retorno}`);
  }

  return `https://wa.me/${fone}?text=${encodeURIComponent(linhas.join('\n'))}`;
}

/**
 * Monta link do WhatsApp para lembrete de retorno/revisão do veículo.
 */
export function criarLinkWhatsappRetorno(retorno: RetornoOrdem): string | null {
  const fone = normalizarTelefoneWhatsapp(retorno.telefoneCliente);
  if (!fone) return null;

  const dataStr = formatarDataBr(retorno.dataPrevistaRetornoUtc) ?? 'em breve';
  const texto =
    `Olá ${retorno.nomeCliente}, tudo bem? Aqui é da DeSanti Automotiva! ` +
    `O retorno/revisão do seu ${retorno.marca} ${retorno.modelo} (placa ${formatarPlacaInput(retorno.placa)}, OS #${retorno.id}) ` +
    `está previsto para ${dataStr}. Gostaria de agendar um horário?`;

  return `https://wa.me/${fone}?text=${encodeURIComponent(texto)}`;
}
