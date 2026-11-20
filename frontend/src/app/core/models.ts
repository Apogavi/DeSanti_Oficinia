export interface Cliente {
  id: number;
  nome: string;
  documento: string;
  telefone: string;
  ativo: boolean;
}

export interface CriarClienteRequest {
  nome: string;
  documento: string;
  telefone: string;
  email: string;
}

export interface UpdateClienteRequest {
  id: number;
  nome: string;
  documento: string;
  telefone: string;
  email: string;
  ativo: boolean;
}

export interface PecaItem {
  nome: string;
  quantidade: number;
  valorTotalCentavos?: number;
  valorUnitarioCentavos: number;
  custoUnitarioCentavos: number;
}

export type StatusOrdem = 'orcamento' | 'aberta' | 'concluida';
export type UserRole = 'Admin' | 'Operador';

export interface UsuarioPerfil {
  id: number;
  userName: string;
  name: string;
  email: string | null;
  role: UserRole | string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface LoginRequest {
  userName: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  user: UsuarioPerfil;
}

export interface AtualizarMeuPerfilRequest {
  userName: string;
  name: string;
  email: string | null;
}

export interface AlterarSenhaRequest {
  currentPassword: string;
  newPassword: string;
}

export interface CriarUsuarioRequest {
  userName: string;
  name: string;
  email: string | null;
  password: string;
  role: UserRole | string;
}

export interface AtualizarUsuarioRequest {
  userName?: string;
  name: string;
  email: string | null;
  role: UserRole | string;
  isActive: boolean;
}

export interface RedefinirSenhaRequest {
  newPassword: string;
}

export interface CriarOrdemServicoRequest {
  clienteId: number;
  placa: string;
  marca: string;
  modelo: string;
  cor: string;
  ano: number | null;
  descricaoServico: string;
  pecas: PecaItem[];
  maoDeObraCentavos: number;
  necessitaRetorno: boolean;
  prazoRetornoDias: number | null;
  ehOrcamento?: boolean;
}

export interface CriarOrdemServicoResponse {
  id: number;
  totalCentavos: number;
}

export interface AprovarOrdemServicoResponse {
  id: number;
  status: string;
}

export interface FinalizarOrdemServicoResponse {
  id: number;
  dataConclusaoUtc: string;
  dataPrevistaRetornoUtc: string | null;
}

export class ApiException extends Error {
  constructor(
    message: string,
    public readonly errors: Record<string, string[]> = {},
    public readonly resultadoIncerto = false,
  ) {
    super(message);
  }
}

export interface OrdemConsulta {
  id: number;
  nomeCliente: string;
  documentoCliente: string;
  telefoneCliente: string;
  placa: string;
  marca: string;
  modelo: string;
  cor: string;
  ano: number | null;
  descricaoServico: string;
  maoDeObraCentavos: number;
  totalCentavos: number;
  dataCriacaoUtc: string;
  dataConclusaoUtc: string | null;
  dataPrevistaRetornoUtc: string | null;
  necessitaRetorno: boolean;
  prazoRetornoDias: number | null;
  status?: StatusOrdem;
  pecas: {
    valorTotalCentavos?: number | null;
    nome: string;
    quantidade: number;
    valorUnitarioCentavos: number;
    custoUnitarioCentavos: number | null;
  }[];
}

export interface RelatorioFinanceiro {
  inicioUtc: string;
  fimUtc: string;
  criterio: 'conclusao' | 'abertura';
  quantidadeOrdens: number;
  quantidadeOrdensComPecas: number;
  quantidadeOrdensSomenteMaoDeObra: number;
  quantidadeOrcamentosPendentes: number;
  brutoPecasCentavos: number;
  custoPecasCentavos: number;
  liquidoPecasCentavos: number;
  maoDeObraCentavos: number;
  liquidoOperacionalCentavos: number;
  totalCobradoCentavos: number;
  ticketMedioCentavos: number;
  quantidadeOrdensComCustoDesconhecido: number;
  quantidadePecasComCustoDesconhecido: number;
  brutoPecasSemCustoCentavos: number;
}

export interface PaginaOrdens {
  itens: OrdemConsulta[];
  temProxima: boolean;
}

export interface RetornoOrdem {
  id: number;
  nomeCliente: string;
  telefoneCliente: string;
  placa: string;
  marca: string;
  modelo: string;
  dataConclusaoUtc: string;
  dataPrevistaRetornoUtc: string;
  prazoRetornoDias: number | null;
  descricaoServico: string;
}

export interface HistoricoVeiculo {
  placa: string;
  marca: string;
  modelo: string;
  cor: string;
  ano: number | null;
  ultimoClienteId: number;
  ultimoNomeCliente: string;
  ultimasOrdens: OrdemConsulta[];
}

export type PecaRascunho = PecaItem & { maoDeObraUnitariaCentavos: number };
