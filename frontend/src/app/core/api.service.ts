import { Injectable } from '@angular/core';
import { obterTokenAuth, salvarTokenAuth } from './auth-storage';
import {
  AlterarSenhaRequest,
  ApiException,
  AprovarOrdemServicoResponse,
  AtualizarMeuPerfilRequest,
  AtualizarUsuarioRequest,
  Cliente,
  CriarClienteRequest,
  CriarOrdemServicoRequest,
  CriarOrdemServicoResponse,
  CriarUsuarioRequest,
  FinalizarOrdemServicoResponse,
  HistoricoVeiculo,
  LoginRequest,
  LoginResponse,
  OrdemConsulta,
  PaginaOrdens,
  RedefinirSenhaRequest,
  RelatorioFinanceiro,
  RetornoOrdem,
  UpdateClienteRequest,
  UsuarioPerfil,
} from './models';

declare global {
  interface Window {
    __DESANTI_CONFIG__?: {
      apiBaseUrl?: string;
    };
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT';
  body?: unknown;
  expectedStatus?: number;
  resultadoIncertoEmErro?: boolean;
  extraHeaders?: Record<string, string>;
}

interface SendResult {
  data: unknown;
  headers: Headers;
}

function resolverBaseUrl(): string {
  const configured =
    typeof window !== 'undefined' ? window.__DESANTI_CONFIG__?.apiBaseUrl?.trim() : undefined;
  return (configured || 'http://localhost:5085').replace(/\/+$/, '');
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = resolverBaseUrl();
  private onUnauthorized: (() => void) | null = null;

  registrarHandlerNaoAutorizado(handler: (() => void) | null): void {
    this.onUnauthorized = handler;
  }

  async login(pedido: LoginRequest): Promise<LoginResponse> {
    const data = await this.send('api/v1/auth/login', {
      method: 'POST',
      body: pedido,
      expectedStatus: 200,
      resultadoIncertoEmErro: false,
    });
    return data as LoginResponse;
  }

  async obterMeuPerfil(): Promise<UsuarioPerfil> {
    return (await this.send('api/v1/auth/me')) as UsuarioPerfil;
  }

  async atualizarMeuPerfil(pedido: AtualizarMeuPerfilRequest): Promise<UsuarioPerfil> {
    return (await this.send('api/v1/auth/me', {
      method: 'PUT',
      body: pedido,
      expectedStatus: 200,
      resultadoIncertoEmErro: false,
    })) as UsuarioPerfil;
  }

  async alterarMinhaSenha(pedido: AlterarSenhaRequest): Promise<void> {
    await this.send('api/v1/auth/change-password', {
      method: 'POST',
      body: pedido,
      expectedStatus: 204,
      resultadoIncertoEmErro: false,
    });
  }

  async listarUsuarios(): Promise<UsuarioPerfil[]> {
    const data = await this.send('api/v1/usuarios');
    return Array.isArray(data) ? (data as UsuarioPerfil[]) : [];
  }

  async criarUsuario(pedido: CriarUsuarioRequest): Promise<UsuarioPerfil> {
    return (await this.send('api/v1/usuarios', {
      method: 'POST',
      body: pedido,
      expectedStatus: 201,
      resultadoIncertoEmErro: false,
    })) as UsuarioPerfil;
  }

  async atualizarUsuario(id: number, pedido: AtualizarUsuarioRequest): Promise<UsuarioPerfil> {
    return (await this.send(`api/v1/usuarios/${id}`, {
      method: 'PUT',
      body: pedido,
      expectedStatus: 200,
      resultadoIncertoEmErro: false,
    })) as UsuarioPerfil;
  }

  async redefinirSenhaUsuario(id: number, pedido: RedefinirSenhaRequest): Promise<void> {
    await this.send(`api/v1/usuarios/${id}/reset-password`, {
      method: 'POST',
      body: pedido,
      expectedStatus: 204,
      resultadoIncertoEmErro: false,
    });
  }

  async listarOrdens(pagina: number, busca: string, status: string): Promise<PaginaOrdens> {
    const params = new URLSearchParams({ pagina: String(pagina) });
    if (busca) params.set('busca', busca);
    if (status) params.set('status', status);
    const { data, headers } = await this.sendRaw(`api/v1/ordens-servico?${params}`);
    const itens = Array.isArray(data) ? (data as OrdemConsulta[]) : [];
    const headerProxima = headers.get('X-Has-Next-Page');
    const temProxima =
      headerProxima !== null ? headerProxima.toLowerCase() === 'true' : itens.length >= 20;
    return { itens, temProxima };
  }

  async listarRetornos(dias = 14): Promise<RetornoOrdem[]> {
    const data = await this.send(`api/v1/ordens-servico/retornos?dias=${dias}`);
    return Array.isArray(data) ? (data as RetornoOrdem[]) : [];
  }

  async obterHistoricoVeiculo(placa: string): Promise<HistoricoVeiculo | null> {
    try {
      return (await this.send(
        `api/v1/ordens-servico/veiculo/${encodeURIComponent(placa)}`,
      )) as HistoricoVeiculo;
    } catch (e) {
      if (e instanceof ApiException && e.message === 'Veículo sem histórico.') {
        return null;
      }
      throw e;
    }
  }

  async obterOrdem(id: number): Promise<OrdemConsulta> {
    return (await this.send(`api/v1/ordens-servico/${id}`)) as OrdemConsulta;
  }

  async aprovarOrdem(id: number): Promise<AprovarOrdemServicoResponse> {
    return (await this.send(`api/v1/ordens-servico/${id}/aprovar`, {
      method: 'POST',
      expectedStatus: 200,
      resultadoIncertoEmErro: true,
    })) as AprovarOrdemServicoResponse;
  }

  async obterRelatorioFinanceiro(
    inicioUtc: string,
    fimUtc: string,
    criterio: 'conclusao' | 'abertura',
  ): Promise<RelatorioFinanceiro> {
    const params = new URLSearchParams({ inicioUtc, fimUtc, criterio });
    return (await this.send(`api/v1/relatorios/financeiro?${params}`)) as RelatorioFinanceiro;
  }

  async finalizarOrdem(id: number): Promise<FinalizarOrdemServicoResponse> {
    const data = await this.send(`api/v1/ordens-servico/${id}/finalizar`, {
      method: 'POST',
      expectedStatus: 200,
      resultadoIncertoEmErro: true,
    });
    if (
      !data ||
      typeof data !== 'object' ||
      typeof (data as any).id !== 'number' ||
      typeof (data as any).dataConclusaoUtc !== 'string'
    ) {
      throw new ApiException(
        'A API retornou uma resposta inválida ao finalizar a ordem de serviço.',
        {},
        true,
      );
    }
    return data as FinalizarOrdemServicoResponse;
  }

  async listarClientes(): Promise<Cliente[]> {
    const data = await this.send('api/v1/clientes');
    if (!Array.isArray(data)) {
      throw new ApiException('A API retornou uma lista de clientes inválida.');
    }
    return data as Cliente[];
  }

  async criarCliente(pedido: CriarClienteRequest): Promise<Cliente> {
    const data = await this.send('api/v1/clientes', {
      method: 'POST',
      body: pedido,
      expectedStatus: 201,
      resultadoIncertoEmErro: true,
    });
    if (!data || typeof data !== 'object' || typeof (data as any).id !== 'number') {
      throw new ApiException('A API retornou uma resposta de cliente inválida.');
    }
    return data as Cliente;
  }

  async atualizarCliente(pedido: UpdateClienteRequest): Promise<void> {
    await this.send('api/v1/clientes', {
      method: 'PUT',
      body: pedido,
      expectedStatus: 204,
      resultadoIncertoEmErro: true,
    });
  }

  async criarOrdemServico(
    pedido: CriarOrdemServicoRequest,
    idempotencyKey?: string,
  ): Promise<CriarOrdemServicoResponse> {
    const data = await this.send('api/v1/ordens-servico', {
      method: 'POST',
      body: pedido,
      expectedStatus: 201,
      resultadoIncertoEmErro: true,
      extraHeaders: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined,
    });
    if (
      !data ||
      typeof data !== 'object' ||
      typeof (data as any).id !== 'number' ||
      typeof (data as any).totalCentavos !== 'number'
    ) {
      throw new ApiException(
        'A API retornou uma resposta inválida para a ordem de serviço.',
        {},
        true,
      );
    }
    return data as CriarOrdemServicoResponse;
  }

  private async send(path: string, options: RequestOptions = {}): Promise<unknown> {
    return (await this.sendRaw(path, options)).data;
  }

  private async sendRaw(path: string, options: RequestOptions = {}): Promise<SendResult> {
    const method = options.method ?? 'GET';
    const expectedStatus = options.expectedStatus ?? (method === 'POST' ? 201 : 200);
    const hasBody = options.body !== undefined;
    const resultadoIncertoEmErro = options.resultadoIncertoEmErro ?? method === 'POST';

    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 20000);

    try {
      const headers: Record<string, string> = hasBody
        ? { Accept: 'application/json', 'Content-Type': 'application/json' }
        : { Accept: 'application/json' };
      const token = obterTokenAuth();
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }
      if (options.extraHeaders) {
        Object.assign(headers, options.extraHeaders);
      }

      const response = await fetch(`${this.baseUrl}/${path}`, {
        method,
        headers,
        body: hasBody ? JSON.stringify(options.body) : undefined,
        signal: controller.signal,
      });

      const text = await response.text();
      let data: any = null;
      if (text) {
        try {
          data = JSON.parse(text);
        } catch {
          throw new ApiException(
            'A API retornou uma resposta inválida.',
            {},
            resultadoIncertoEmErro,
          );
        }
      }

      if (response.status !== expectedStatus) {
        const ehLogin = path === 'api/v1/auth/login';
        if (response.status === 401 && !ehLogin) {
          salvarTokenAuth(null);
          this.onUnauthorized?.();
          throw new ApiException('Sua sessão expirou. Faça login novamente.', {}, false);
        }

        const errors: Record<string, string[]> = {};
        if (data && typeof data === 'object' && data.errors && typeof data.errors === 'object') {
          for (const [k, v] of Object.entries(data.errors)) {
            if (Array.isArray(v)) {
              errors[k] = v.filter((item): item is string => typeof item === 'string');
            }
          }
        }
        const serverMessage =
          data && typeof data === 'object' && typeof data.message === 'string' && data.message.trim()
            ? data.message.trim()
            : null;
        const serverTitle =
          data && typeof data === 'object' && typeof data.title === 'string' && data.title.trim()
            ? data.title.trim()
            : serverMessage;
        const ehFinalizar = path.endsWith('/finalizar');
        const ehHistoricoVeiculo = path.startsWith('api/v1/ordens-servico/veiculo/');
        const message =
          response.status === 401
            ? (serverTitle ?? 'Usuário ou senha inválidos.')
            : response.status === 403
              ? 'Você não tem permissão para esta operação.'
              : response.status === 400
                ? (serverMessage ?? 'Confira os campos informados.')
                : response.status === 404
                  ? ehHistoricoVeiculo
                    ? 'Veículo sem histórico.'
                    : (serverTitle ??
                      (ehFinalizar
                        ? 'Ordem de serviço não encontrada.'
                        : path === 'api/v1/clientes'
                          ? 'Cliente não encontrado.'
                          : 'O cliente selecionado não foi encontrado.'))
                  : response.status === 409
                    ? (serverTitle ??
                      (ehFinalizar
                        ? 'A ordem de serviço já foi finalizada.'
                        : path === 'api/v1/clientes'
                          ? 'Já existe um cliente com este CPF/CNPJ.'
                          : 'O cliente selecionado está inativo.'))
                    : 'Não foi possível concluir a operação na API.';

        throw new ApiException(
          message,
          errors,
          resultadoIncertoEmErro && ![400, 401, 403, 404, 409].includes(response.status),
        );
      }

      return { data, headers: response.headers };
    } catch (err) {
      if (err instanceof ApiException) throw err;
      if ((err as any)?.name === 'AbortError') {
        throw new ApiException('A API demorou para responder.', {}, resultadoIncertoEmErro);
      }
      throw new ApiException('Não foi possível conectar à API.', {}, resultadoIncertoEmErro);
    } finally {
      clearTimeout(timer);
    }
  }
}
