import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from './core/api.service';
import { obterTokenAuth, salvarTokenAuth, usuarioEhAdmin } from './core/auth-storage';
import {
  formatarCentavos,
  formatarCpfOuCnpj,
  formatarMoedaInput,
  formatarPlacaInput,
  formatarTelefone,
  normalizarPlaca,
  somenteDigitos,
  textoParaCentavos,
} from './core/formatters';
import {
  ApiException,
  Cliente,
  CriarOrdemServicoResponse,
  FinalizarOrdemServicoResponse,
  HistoricoVeiculo,
  OrdemConsulta,
  PecaItem,
  PecaRascunho,
  RelatorioFinanceiro,
  RetornoOrdem,
  StatusOrdem,
  UsuarioPerfil,
} from './core/models';
import {
  carregarRascunhoOs,
  gerarChaveIdempotencia,
  limparRascunhoOs,
  salvarRascunhoOs,
} from './core/rascunho-os';
import { criarLinkWhatsappOs, criarLinkWhatsappRetorno } from './core/whatsapp';
import { LiquidGlassDirective } from './shared/liquid-glass.directive';

type AbaPrincipal = 'ordens' | 'clientes' | 'financeiro' | 'conta';
type PeriodoFinanceiro = 'semana' | 'mes' | 'mes_anterior' | 'personalizado';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, LiquidGlassDirective],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly api = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);

  // Estado de Autenticação e Sessão
  readonly usuarioAtual = signal<UsuarioPerfil | null>(null);
  readonly verificandoSessao = signal(Boolean(obterTokenAuth()));
  readonly sessaoExpiradaMsg = signal<string | null>(null);
  readonly ehAdmin = computed(() => usuarioEhAdmin(this.usuarioAtual()));

  // Tela de Login
  readonly loginUsuario = signal('');
  readonly loginSenha = signal('');
  readonly loginMostrarSenha = signal(false);
  readonly loginEnviando = signal(false);
  readonly loginErro = signal<string | null>(null);

  // Aba Conta & Gestão de Usuários
  readonly perfilUserName = signal('');
  readonly perfilNome = signal('');
  readonly perfilEmail = signal('');
  readonly perfilSalvando = signal(false);
  readonly perfilMensagem = signal<string | null>(null);
  readonly perfilErro = signal<string | null>(null);

  readonly senhaAtual = signal('');
  readonly senhaNova = signal('');
  readonly senhaSalvando = signal(false);
  readonly senhaMensagem = signal<string | null>(null);
  readonly senhaErro = signal<string | null>(null);

  readonly usuariosLista = signal<UsuarioPerfil[]>([]);
  readonly carregandoUsuarios = signal(false);
  readonly novoUsuUserName = signal('');
  readonly novoUsuNome = signal('');
  readonly novoUsuEmail = signal('');
  readonly novoUsuSenha = signal('');
  readonly novoUsuRole = signal<'Admin' | 'Operador'>('Operador');
  readonly novoUsuSalvando = signal(false);
  readonly novoUsuMensagem = signal<string | null>(null);
  readonly novoUsuErro = signal<string | null>(null);

  readonly ordens = signal<OrdemConsulta[]>([]);
  readonly temProximaPaginaOs = signal(false);
  readonly paginaOs = signal(1);
  readonly buscaOsLista = signal('');
  readonly statusOsLista = signal('');
  private filtrosAplicados = { busca: '', status: '' };

  // Radar de retornos próximos / vencidos
  readonly retornos = signal<RetornoOrdem[]>([]);
  readonly carregandoRetornos = signal(false);
  readonly radarExpandido = signal(true);

  buscarOrdens(): void {
    this.filtrosAplicados = { busca: this.buscaOsLista().trim(), status: this.statusOsLista() };
    this.paginaOs.set(1);
    void this.carregarOrdens(1);
  }

  limparFiltrosOrdens(): void {
    this.buscaOsLista.set('');
    this.statusOsLista.set('');
    this.buscarOrdens();
  }
  readonly consultaOcupada = signal(false);
  readonly consultaErro = signal('');
  readonly detalheErro = signal('');
  private sequenciaDetalhe = 0;
  readonly detalhe = signal<OrdemConsulta | null>(null);
  readonly finalizandoOs = signal(false);
  readonly aprovandoOs = signal(false);
  readonly confirmandoFinalizacao = signal(false);
  readonly finalizacaoErro = signal<string | null>(null);
  readonly finalizacaoSucesso = signal<FinalizarOrdemServicoResponse | null>(null);
  readonly aprovacaoSucesso = signal(false);

  // Fase 2 — Financeiro / Relatórios (Semanal e Mensal)
  readonly finPeriodo = signal<PeriodoFinanceiro>('mes');
  readonly finCriterio = signal<'conclusao' | 'abertura'>('conclusao');
  readonly finInicio = signal('');
  readonly finFim = signal('');
  readonly finCarregando = signal(false);
  readonly finErro = signal<string | null>(null);
  readonly finRelatorio = signal<RelatorioFinanceiro | null>(null);

  constructor() {
    this.aplicarPresetFinanceiro('mes', false);

    // Auto-save reativo do rascunho da Nova OS no localStorage enquanto o formulário está aberto.
    effect(() => {
      if (!this.abrindoNovaOs()) return;
      salvarRascunhoOs({
        clienteId: this.osClienteId(),
        buscaCliente: this.osBuscaCliente(),
        placa: this.osPlaca(),
        marca: this.osMarca(),
        modelo: this.osModelo(),
        cor: this.osCor(),
        ano: this.osAno(),
        descricao: this.osDescricao(),
        maoDeObraAvulsa: this.osMaoDeObraAvulsa(),
        necessitaRetorno: this.osNecessitaRetorno(),
        prazoRetornoDias: this.osPrazoRetornoDias(),
        ehOrcamento: this.osEhOrcamento(),
        idempotencyKey: this.osIdempotencyKey(),
        pecas: this.osPecas(),
      });
    });
  }

  async carregarOrdens(pagina = this.paginaOs()): Promise<void> {
    if (this.consultaOcupada()) return;
    this.consultaOcupada.set(true);
    this.consultaErro.set('');
    try {
      const resultado = await this.api.listarOrdens(
        pagina,
        this.filtrosAplicados.busca,
        this.filtrosAplicados.status,
      );
      this.ordens.set(resultado.itens);
      this.temProximaPaginaOs.set(resultado.temProxima);
      this.paginaOs.set(pagina);
    } catch {
      this.ordens.set([]);
      this.temProximaPaginaOs.set(false);
      this.consultaErro.set('Não foi possível atualizar as ordens. Tente novamente.');
    } finally {
      this.consultaOcupada.set(false);
    }
  }

  async carregarRetornos(): Promise<void> {
    if (this.carregandoRetornos()) return;
    this.carregandoRetornos.set(true);
    try {
      this.retornos.set(await this.api.listarRetornos(14));
    } catch {
      this.retornos.set([]);
    } finally {
      this.carregandoRetornos.set(false);
    }
  }

  retornoVencido(dataIsoUtc: string): boolean {
    const data = new Date(dataIsoUtc);
    const hoje = new Date();
    hoje.setHours(0, 0, 0, 0);
    return !Number.isNaN(data.getTime()) && data.getTime() < hoje.getTime();
  }

  linkWhatsappRetorno(retorno: RetornoOrdem): string | null {
    return criarLinkWhatsappRetorno(retorno);
  }

  linkWhatsappDetalhe(): string | null {
    const atual = this.detalhe();
    return atual ? criarLinkWhatsappOs(atual) : null;
  }

  imprimirOsCliente(): void {
    if (!this.detalhe()) return;
    window.print();
  }
  async abrirDetalhe(id: number, dialog: HTMLDialogElement): Promise<void> {
    const sequencia = ++this.sequenciaDetalhe;
    this.detalhe.set(null);
    this.detalheErro.set('');
    this.confirmandoFinalizacao.set(false);
    this.finalizacaoErro.set(null);
    this.finalizacaoSucesso.set(null);
    this.aprovacaoSucesso.set(false);
    dialog.showModal();
    try {
      const ordem = await this.api.obterOrdem(id);
      if (sequencia === this.sequenciaDetalhe) this.detalhe.set(ordem);
    } catch {
      if (sequencia === this.sequenciaDetalhe) {
        this.detalheErro.set('Não foi possível consultar esta ordem. Feche e tente novamente.');
      }
    }
  }

  aoFecharDetalhe(): void {
    this.confirmandoFinalizacao.set(false);
    this.finalizacaoErro.set(null);
    this.finalizacaoSucesso.set(null);
    this.aprovacaoSucesso.set(false);
  }

  aoCancelarDetalhe(event: Event): void {
    if (this.finalizandoOs() || this.aprovandoOs()) {
      event.preventDefault();
    }
  }

  statusDaOrdem(os: OrdemConsulta): StatusOrdem {
    if (os.status === 'orcamento') return 'orcamento';
    if (os.status === 'concluida' || os.dataConclusaoUtc) return 'concluida';
    return 'aberta';
  }

  rotuloStatusOrdem(os: OrdemConsulta): string {
    const st = this.statusDaOrdem(os);
    return st === 'orcamento' ? 'Orçamento' : st === 'concluida' ? 'Concluída' : 'Em aberto';
  }

  async aprovarOrcamentoOs(): Promise<void> {
    if (this.aprovandoOs()) return;
    const atual = this.detalhe();
    if (!atual || this.statusDaOrdem(atual) !== 'orcamento') return;

    this.aprovandoOs.set(true);
    this.finalizacaoErro.set(null);
    this.aprovacaoSucesso.set(false);

    try {
      await this.api.aprovarOrdem(atual.id);
      this.detalhe.update((os) => (os && os.id === atual.id ? { ...os, status: 'aberta' } : os));
      this.aprovacaoSucesso.set(true);
      if (this.filtrosAplicados.status === 'orcamento') {
        void this.carregarOrdens(this.paginaOs());
      } else {
        this.ordens.update((lista) =>
          lista.map((os) => (os.id === atual.id ? { ...os, status: 'aberta' } : os)),
        );
      }
    } catch (e) {
      this.finalizacaoErro.set(
        e instanceof ApiException ? e.message : 'Não foi possível aprovar este orçamento.',
      );
    } finally {
      this.aprovandoOs.set(false);
    }
  }

  aplicarPresetFinanceiro(preset: PeriodoFinanceiro, carregar = true): void {
    this.finPeriodo.set(preset);
    const hoje = new Date();
    const fmt = (d: Date) =>
      [d.getFullYear(), String(d.getMonth() + 1).padStart(2, '0'), String(d.getDate()).padStart(2, '0')].join('-');

    if (preset === 'semana') {
      const inicio = new Date(hoje);
      const diaSemana = (hoje.getDay() + 6) % 7; // segunda-feira = 0
      inicio.setDate(hoje.getDate() - diaSemana);
      this.finInicio.set(fmt(inicio));
      this.finFim.set(fmt(hoje));
    } else if (preset === 'mes') {
      const inicio = new Date(hoje.getFullYear(), hoje.getMonth(), 1);
      this.finInicio.set(fmt(inicio));
      this.finFim.set(fmt(hoje));
    } else if (preset === 'mes_anterior') {
      const inicio = new Date(hoje.getFullYear(), hoje.getMonth() - 1, 1);
      const fim = new Date(hoje.getFullYear(), hoje.getMonth(), 0);
      this.finInicio.set(fmt(inicio));
      this.finFim.set(fmt(fim));
    }

    if (carregar && preset !== 'personalizado') {
      void this.carregarRelatorioFinanceiro();
    }
  }

  alterarCriterioFinanceiro(criterio: 'conclusao' | 'abertura'): void {
    this.finCriterio.set(criterio);
    void this.carregarRelatorioFinanceiro();
  }

  async carregarRelatorioFinanceiro(): Promise<void> {
    if (this.finCarregando()) return;
    const iniStr = this.finInicio().trim();
    const fimStr = this.finFim().trim();
    if (!iniStr || !fimStr) {
      this.finErro.set('Informe a data inicial e a data final.');
      return;
    }

    const [ia, im, id] = iniStr.split('-').map(Number);
    const [fa, fm, fd] = fimStr.split('-').map(Number);
    const inicioLocal = new Date(ia, im - 1, id, 0, 0, 0, 0);
    const fimLocal = new Date(fa, fm - 1, fd, 23, 59, 59, 999);
    if (inicioLocal > fimLocal) {
      this.finErro.set('A data inicial deve ser anterior ou igual à data final.');
      return;
    }

    this.finCarregando.set(true);
    this.finErro.set(null);
    try {
      const dados = await this.api.obterRelatorioFinanceiro(
        inicioLocal.toISOString(),
        fimLocal.toISOString(),
        this.finCriterio(),
      );
      this.finRelatorio.set(dados);
    } catch (e) {
      this.finRelatorio.set(null);
      this.finErro.set(
        e instanceof ApiException
          ? e.message
          : 'Não foi possível carregar o relatório financeiro.',
      );
    } finally {
      this.finCarregando.set(false);
    }
  }

  solicitarFinalizacao(): void {
    const atual = this.detalhe();
    if (!atual || atual.dataConclusaoUtc || this.finalizandoOs()) return;
    this.finalizacaoErro.set(null);
    this.finalizacaoSucesso.set(null);
    this.confirmandoFinalizacao.set(true);
  }

  cancelarFinalizacao(): void {
    if (this.finalizandoOs()) return;
    this.confirmandoFinalizacao.set(false);
    this.finalizacaoErro.set(null);
  }

  async confirmarFinalizacaoOs(): Promise<void> {
    if (this.finalizandoOs()) return;
    const atual = this.detalhe();
    if (!atual || atual.dataConclusaoUtc) return;

    this.finalizandoOs.set(true);
    this.finalizacaoErro.set(null);
    this.finalizacaoSucesso.set(null);

    try {
      const resposta = await this.api.finalizarOrdem(atual.id);
      this.detalhe.update((os) =>
        os && os.id === resposta.id
          ? {
              ...os,
              dataConclusaoUtc: resposta.dataConclusaoUtc,
              dataPrevistaRetornoUtc: resposta.dataPrevistaRetornoUtc,
            }
          : os,
      );
      this.confirmandoFinalizacao.set(false);
      this.finalizacaoSucesso.set(resposta);
      void this.carregarRetornos();

      if (this.filtrosAplicados.status === 'aberta') {
        void this.carregarOrdens(this.paginaOs());
      } else {
        this.ordens.update((lista) =>
          lista.map((os) =>
            os.id === resposta.id
              ? {
                  ...os,
                  dataConclusaoUtc: resposta.dataConclusaoUtc,
                  dataPrevistaRetornoUtc: resposta.dataPrevistaRetornoUtc,
                }
              : os,
          ),
        );
      }
    } catch (e) {
      const mensagem =
        e instanceof ApiException
          ? e.message
          : 'Não foi possível finalizar esta ordem de serviço.';
      this.finalizacaoErro.set(mensagem);

      // Se outra sessão já tiver finalizado a OS (409), sincroniza os detalhes atuais.
      if (e instanceof ApiException && !e.resultadoIncerto) {
        try {
          const atualizada = await this.api.obterOrdem(atual.id);
          if (atualizada.dataConclusaoUtc) {
            this.detalhe.set(atualizada);
            this.confirmandoFinalizacao.set(false);
            void this.carregarOrdens(this.paginaOs());
          }
        } catch {
          // Mantém a mensagem de erro da finalização visível se o refresh falhar.
        }
      }
    } finally {
      this.finalizandoOs.set(false);
    }
  }

  dataHoje(): string {
    const d = new Date();
    return [d.getFullYear(), String(d.getMonth()+1).padStart(2,'0'), String(d.getDate()).padStart(2,'0')].join('-');
  }
  selecionarDataRetorno(valor: string): void {
    if (!valor || this.osBloqueada() || !this.osNecessitaRetorno()) return;
    const [a,m,d] = valor.split('-').map(Number);
    const hoje = new Date();
    const dias = Math.round((Date.UTC(a,m-1,d)-Date.UTC(hoje.getFullYear(),hoje.getMonth(),hoje.getDate()))/86400000);
    if (Number.isFinite(dias) && dias > 0) this.osPrazoRetornoDias.set(String(dias));
  }
  // Navegação, tema e desempenho (Modo Leve para PC modesto)
  readonly menuExpandido = signal(false);
  readonly abaAtiva = signal<AbaPrincipal>('ordens');
  readonly abrindoNovaOs = signal(false);
  readonly temaEscuro = signal(
    document.documentElement.dataset['theme'] === 'dark',
  );
  readonly modoLeve = signal(
    document.documentElement.dataset['perf'] === 'lite',
  );

  // Rascunho e Prontuário de Veículo na Nova OS
  readonly rascunhoRecuperado = signal(false);
  readonly historicoVeiculo = signal<HistoricoVeiculo | null>(null);
  readonly buscandoHistoricoVeiculo = signal(false);
  private sequenciaPlaca = 0;
  private ultimaPlacaConsultada = '';

  // Estado compartilhado de Clientes
  readonly todosClientes = signal<Cliente[]>([]);
  readonly clientes = computed(() => this.todosClientes().filter((c) => c.ativo));
  readonly mostrarClientesInativos = signal(false);
  readonly carregandoClientes = signal(false);
  readonly erroListaClientes = signal<string | null>(null);

  // Aba Clientes: Cadastro + Consulta + Edição
  readonly cadNome = signal('');
  readonly cadDocumento = signal('');
  readonly cadTelefone = signal('');
  readonly salvandoCliente = signal(false);
  readonly erroCadastroCliente = signal<string | null>(null);
  readonly errosCampoCliente = signal<Record<string, string[]>>({});
  readonly ultimoClienteCriado = signal<Cliente | null>(null);
  readonly ultimoClienteEditado = signal<string | null>(null);
  readonly filtroConsultaClientes = signal('');

  // Modal de edição de cliente (TASK-23)
  readonly clienteEmEdicao = signal<Cliente | null>(null);
  readonly editCliNome = signal('');
  readonly editCliDoc = signal('');
  readonly editCliTel = signal('');
  readonly editCliAtivo = signal(true);
  readonly editCliErros = signal<Record<string, string[]>>({});
  readonly editCliErroGeral = signal<string | null>(null);
  readonly editCliSalvando = signal(false);

  // Modal rápido de cadastro de cliente (acionado pelo alerta na Nova OS)
  readonly modalNovoClienteAberto = signal(false);
  readonly modalCliNome = signal('');
  readonly modalCliDoc = signal('');
  readonly modalCliTel = signal('');
  readonly modalCliErros = signal<Record<string, string[]>>({});
  readonly modalCliErroGeral = signal<string | null>(null);
  readonly modalCliSalvando = signal(false);

  // Estado de Nova Ordem de Serviço
  readonly ultimaOsCriada = signal<CriarOrdemServicoResponse | null>(null);
  readonly osBuscaCliente = signal('');
  readonly osClienteId = signal<number | null>(null);
  readonly osPlaca = signal('');
  readonly osMarca = signal('');
  readonly osModelo = signal('');
  readonly osCor = signal('');
  readonly osAno = signal('');
  readonly osDescricao = signal('');
  readonly osMaoDeObraAvulsa = signal('');
  readonly osNecessitaRetorno = signal(false);
  readonly osPrazoRetornoDias = signal('');
  readonly osEhOrcamento = signal(false);
  readonly osIdempotencyKey = signal(gerarChaveIdempotencia());
  readonly osPecas = signal<PecaRascunho[]>([]);
  readonly osErros = signal<Record<string, string[]>>({});
  readonly osMensagemErro = signal<string | null>(null);
  readonly osEnviando = signal(false);
  readonly osResultadoIncerto = signal(false);

  // Modal de adicionar peça
  readonly modalPecaAberta = signal(false);
  readonly pecaNome = signal('');
  readonly pecaQuantidade = signal('1');
  readonly pecaCusto = signal('');
  readonly pecaMaoDeObra = signal('');
  readonly pecaValorCobrado = signal('');
  readonly pecaErros = signal<Record<string, string>>({});

  // Computed signals — Aba Clientes
  readonly clientesFiltradosConsulta = computed(() => {
    const termo = this.filtroConsultaClientes().trim().toLowerCase();
    const lista = this.mostrarClientesInativos()
      ? this.todosClientes()
      : this.clientes();
    if (!termo) return lista;
    const digitos = somenteDigitos(termo);
    return lista.filter((c) => {
      const nomeOk = c.nome.toLowerCase().includes(termo);
      const docBruto = somenteDigitos(c.documento);
      const docOk =
        (digitos.length > 0 && docBruto.includes(digitos)) ||
        c.documento.toLowerCase().includes(termo);
      return nomeOk || docOk;
    });
  });

  // Computed signals — Busca de Cliente na Nova OS
  readonly clientesFiltradosOs = computed(() => {
    const termo = this.osBuscaCliente().trim().toLowerCase();
    const lista = this.clientes();
    if (!termo) return lista;
    const digitos = somenteDigitos(termo);
    return lista.filter((c) => {
      const nomeOk = c.nome.toLowerCase().includes(termo);
      const docBruto = somenteDigitos(c.documento);
      const docOk =
        (digitos.length > 0 && docBruto.includes(digitos)) ||
        c.documento.toLowerCase().includes(termo);
      return nomeOk || docOk;
    });
  });

  readonly clienteSelecionadoOs = computed(() => {
    const id = this.osClienteId();
    if (id === null) return null;
    return this.clientes().find((c) => c.id === id) ?? null;
  });

  readonly buscaOsSemResultado = computed(
    () =>
      this.buscaClienteExecutada() &&
      this.resultadosCliente().length === 0 &&
      !this.carregandoClientes() &&
      !this.erroListaClientes() &&
      this.osBuscaCliente().trim().length > 0 &&
      this.osClienteId() === null,
  );

  // Computed signals — Totais financeiros em centavos (BigInt)
  readonly maoDeObraAvulsaCentavos = computed(
    () => BigInt(textoParaCentavos(this.osMaoDeObraAvulsa()) ?? 0),
  );

  readonly temMaoDeObraAvulsa = computed(
    () => this.maoDeObraAvulsaCentavos() > 0n,
  );

  readonly maoDeObraCentavos = computed(
    () =>
      this.maoDeObraAvulsaCentavos() +
      this.osPecas().reduce((total, peca) => total + this.maoDeObraDaPeca(peca), 0n),
  );

  readonly totalPecasCentavos = computed(() =>
    this.osPecas().reduce(
      (acc, item) =>
        acc + (item.valorTotalCentavos !== undefined ? BigInt(item.valorTotalCentavos) : BigInt(item.quantidade) * BigInt(item.valorUnitarioCentavos)),
      0n,
    ),
  );

  readonly totalGeralCentavos = computed(
    () => this.totalPecasCentavos() + this.maoDeObraCentavos(),
  );

  readonly osBloqueada = computed(
    () => this.osEnviando() || this.osResultadoIncerto(),
  );

  // Alerta preventivo (não bloqueante) se o valor cobrado da peça for menor que o custo total.
  readonly pecaAbaixoDoCusto = computed(() => {
    const qtd = Number.parseInt(somenteDigitos(this.pecaQuantidade()), 10);
    const custo = textoParaCentavos(this.pecaCusto());
    const cobrado = textoParaCentavos(this.pecaValorCobrado());
    if (!Number.isInteger(qtd) || qtd <= 0 || custo === null || cobrado === null) return null;
    const custoTotal = BigInt(qtd) * BigInt(custo);
    const cobradoBig = BigInt(cobrado);
    return cobradoBig < custoTotal ? { custoTotal, cobrado: cobradoBig } : null;
  });

  ngOnInit(): void {
    this.api.registrarHandlerNaoAutorizado(() => {
      this.usuarioAtual.set(null);
      this.verificandoSessao.set(false);
      this.sessaoExpiradaMsg.set('Sua sessão expirou. Faça login novamente para continuar.');
    });
    this.destroyRef.onDestroy(() => this.api.registrarHandlerNaoAutorizado(null));

    if (obterTokenAuth()) {
      void this.restaurarSessao();
    }

    const mq = window.matchMedia?.('(prefers-color-scheme: dark)');
    if (mq) {
      const aoAlterarSistema = (event: MediaQueryListEvent) => {
        if (localStorage.getItem('desanti-theme')) return;
        const valor = event.matches ? 'dark' : 'light';
        document.documentElement.dataset['theme'] = valor;
        this.temaEscuro.set(event.matches);
      };
      mq.addEventListener('change', aoAlterarSistema);
      this.destroyRef.onDestroy(() => mq.removeEventListener('change', aoAlterarSistema));
    }
  }

  private sincronizarCamposPerfil(u: UsuarioPerfil): void {
    this.perfilUserName.set(u.userName);
    this.perfilNome.set(u.name);
    this.perfilEmail.set(u.email ?? '');
  }

  private async restaurarSessao(): Promise<void> {
    this.verificandoSessao.set(true);
    try {
      const perfil = await this.api.obterMeuPerfil();
      this.usuarioAtual.set(perfil);
      this.sincronizarCamposPerfil(perfil);
      this.carregarDadosIniciaisAutenticado();
    } catch {
      salvarTokenAuth(null);
      this.usuarioAtual.set(null);
    } finally {
      this.verificandoSessao.set(false);
    }
  }

  private carregarDadosIniciaisAutenticado(): void {
    void this.carregarOrdens(1);
    void this.carregarRetornos();
    void this.carregarClientes();
  }

  async realizarLogin(): Promise<void> {
    if (this.loginEnviando()) return;
    this.sessaoExpiradaMsg.set(null);

    const user = this.loginUsuario().trim();
    const pass = this.loginSenha();
    if (!user || !pass) {
      this.loginErro.set('Informe seu usuário e senha para entrar.');
      return;
    }

    this.loginEnviando.set(true);
    this.loginErro.set(null);
    try {
      const resp = await this.api.login({ userName: user, password: pass });
      salvarTokenAuth(resp.token);
      this.usuarioAtual.set(resp.user);
      this.sincronizarCamposPerfil(resp.user);
      this.loginSenha.set('');
      this.abaAtiva.set('ordens');
      this.carregarDadosIniciaisAutenticado();
    } catch (e) {
      this.loginErro.set(
        e instanceof ApiException
          ? e.message
          : 'Não foi possível entrar. Verifique usuário, senha e a conexão com a API.',
      );
    } finally {
      this.loginEnviando.set(false);
    }
  }

  sair(): void {
    salvarTokenAuth(null);
    this.usuarioAtual.set(null);
    this.menuExpandido.set(false);
    this.abrindoNovaOs.set(false);
    this.loginSenha.set('');
    this.loginErro.set(null);
    this.sessaoExpiradaMsg.set(null);
  }

  async salvarMeuPerfil(): Promise<void> {
    if (this.perfilSalvando()) return;
    this.perfilMensagem.set(null);
    this.perfilErro.set(null);
    this.perfilSalvando.set(true);
    try {
      const atualizado = await this.api.atualizarMeuPerfil({
        userName: this.perfilUserName().trim(),
        name: this.perfilNome().trim(),
        email: this.perfilEmail().trim() || null,
      });
      this.usuarioAtual.set(atualizado);
      this.sincronizarCamposPerfil(atualizado);
      this.perfilMensagem.set('Seus dados foram atualizados com sucesso.');
    } catch (e) {
      this.perfilErro.set(e instanceof ApiException ? e.message : 'Erro ao atualizar perfil.');
    } finally {
      this.perfilSalvando.set(false);
    }
  }

  async salvarNovaSenhaPropria(): Promise<void> {
    if (this.senhaSalvando()) return;
    this.senhaMensagem.set(null);
    this.senhaErro.set(null);
    this.senhaSalvando.set(true);
    try {
      await this.api.alterarMinhaSenha({
        currentPassword: this.senhaAtual(),
        newPassword: this.senhaNova(),
      });
      this.senhaAtual.set('');
      this.senhaNova.set('');
      this.senhaMensagem.set('Senha alterada com sucesso.');
    } catch (e) {
      this.senhaErro.set(e instanceof ApiException ? e.message : 'Erro ao alterar a senha.');
    } finally {
      this.senhaSalvando.set(false);
    }
  }

  async carregarUsuarios(): Promise<void> {
    if (!this.ehAdmin() || this.carregandoUsuarios()) return;
    this.carregandoUsuarios.set(true);
    try {
      this.usuariosLista.set(await this.api.listarUsuarios());
    } catch {
      this.usuariosLista.set([]);
    } finally {
      this.carregandoUsuarios.set(false);
    }
  }

  async criarNovoUsuario(): Promise<void> {
    if (this.novoUsuSalvando() || !this.ehAdmin()) return;
    this.novoUsuMensagem.set(null);
    this.novoUsuErro.set(null);
    this.novoUsuSalvando.set(true);
    try {
      const criado = await this.api.criarUsuario({
        userName: this.novoUsuUserName().trim(),
        name: this.novoUsuNome().trim(),
        email: this.novoUsuEmail().trim() || null,
        password: this.novoUsuSenha(),
        role: this.novoUsuRole(),
      });
      this.novoUsuUserName.set('');
      this.novoUsuNome.set('');
      this.novoUsuEmail.set('');
      this.novoUsuSenha.set('');
      this.novoUsuMensagem.set(`Usuário '${criado.userName}' criado com sucesso.`);
      void this.carregarUsuarios();
    } catch (e) {
      this.novoUsuErro.set(e instanceof ApiException ? e.message : 'Erro ao criar usuário.');
    } finally {
      this.novoUsuSalvando.set(false);
    }
  }

  async alternarAtivoUsuario(u: UsuarioPerfil): Promise<void> {
    if (!this.ehAdmin()) return;
    this.novoUsuErro.set(null);
    this.novoUsuMensagem.set(null);
    try {
      const atualizado = await this.api.atualizarUsuario(u.id, {
        userName: u.userName,
        name: u.name,
        email: u.email,
        role: u.role,
        isActive: !u.isActive,
      });
      this.usuariosLista.update((lista) => lista.map((item) => (item.id === u.id ? atualizado : item)));
    } catch (e) {
      this.novoUsuErro.set(e instanceof ApiException ? e.message : 'Não foi possível alterar o status do usuário.');
    }
  }

  async alternarRoleUsuario(u: UsuarioPerfil): Promise<void> {
    if (!this.ehAdmin()) return;
    this.novoUsuErro.set(null);
    this.novoUsuMensagem.set(null);
    const novaRole = u.role === 'Admin' ? 'Operador' : 'Admin';
    try {
      const atualizado = await this.api.atualizarUsuario(u.id, {
        userName: u.userName,
        name: u.name,
        email: u.email,
        role: novaRole,
        isActive: u.isActive,
      });
      this.usuariosLista.update((lista) => lista.map((item) => (item.id === u.id ? atualizado : item)));
    } catch (e) {
      this.novoUsuErro.set(e instanceof ApiException ? e.message : 'Não foi possível alterar o perfil do usuário.');
    }
  }

  selecionarAba(aba: AbaPrincipal): void {
    if (aba === 'financeiro' && !this.ehAdmin()) return;
    this.abaAtiva.set(aba);
    this.abrindoNovaOs.set(false);
    this.menuExpandido.set(false);
    if (aba === 'clientes') {
      this.carregarClientes();
    } else if (aba === 'financeiro') {
      void this.carregarRelatorioFinanceiro();
    } else if (aba === 'conta') {
      const atual = this.usuarioAtual();
      if (atual) this.sincronizarCamposPerfil(atual);
      if (this.ehAdmin()) void this.carregarUsuarios();
    }
  }

  alternarMenu(): void {
    this.menuExpandido.update((v) => !v);
  }

  alternarTema(): void {
    const proximo = !this.temaEscuro();
    this.temaEscuro.set(proximo);
    const valor = proximo ? 'dark' : 'light';
    document.documentElement.dataset['theme'] = valor;
    localStorage.setItem('desanti-theme', valor);
  }

  alternarModoLeve(): void {
    const proximo = !this.modoLeve();
    this.modoLeve.set(proximo);
    if (proximo) {
      document.documentElement.dataset['perf'] = 'lite';
      localStorage.setItem('desanti-perf-mode', 'lite');
    } else {
      delete document.documentElement.dataset['perf'];
      localStorage.removeItem('desanti-perf-mode');
    }
    window.dispatchEvent(new Event('desanti-perf-change'));
  }

  async carregarClientes(): Promise<void> {
    if (this.carregandoClientes()) return;
    this.carregandoClientes.set(true);
    this.erroListaClientes.set(null);
    try {
      const lista = await this.api.listarClientes();
      this.todosClientes.set(lista);
    } catch (e) {
      this.erroListaClientes.set(
        e instanceof ApiException
          ? e.message
          : 'Não foi possível carregar os clientes.',
      );
    } finally {
      this.carregandoClientes.set(false);
    }
  }

  // Formatação de inputs
  aoDigitarDocumentoCadastro(valor: string): void {
    this.cadDocumento.set(formatarCpfOuCnpj(valor));
  }

  aoDigitarTelefoneCadastro(valor: string): void {
    this.cadTelefone.set(formatarTelefone(valor));
  }

  aoDigitarDocumentoModal(valor: string): void {
    this.modalCliDoc.set(formatarCpfOuCnpj(valor));
  }

  aoDigitarTelefoneModal(valor: string): void {
    this.modalCliTel.set(formatarTelefone(valor));
  }

  aoDigitarDocumentoEdicao(valor: string): void {
    this.editCliDoc.set(formatarCpfOuCnpj(valor));
  }

  aoDigitarTelefoneEdicao(valor: string): void {
    this.editCliTel.set(formatarTelefone(valor));
  }

  aoDigitarPlaca(valor: string): void {
    const formatada = formatarPlacaInput(valor);
    this.osPlaca.set(formatada);

    const norm = normalizarPlaca(formatada);
    if (/^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(norm)) {
      if (norm !== this.ultimaPlacaConsultada) {
        void this.buscarHistoricoPlaca(norm);
      }
    } else {
      this.ultimaPlacaConsultada = '';
      this.historicoVeiculo.set(null);
      this.buscandoHistoricoVeiculo.set(false);
    }
  }

  private async buscarHistoricoPlaca(placaNorm: string): Promise<void> {
    const seq = ++this.sequenciaPlaca;
    this.ultimaPlacaConsultada = placaNorm;
    this.buscandoHistoricoVeiculo.set(true);
    try {
      const historico = await this.api.obterHistoricoVeiculo(placaNorm);
      if (seq !== this.sequenciaPlaca) return;
      this.historicoVeiculo.set(historico);
      if (historico) {
        // Preenche apenas campos ainda vazios, sem sobrescrever edições manuais do usuário.
        if (!this.osMarca().trim()) this.osMarca.set(historico.marca);
        if (!this.osModelo().trim()) this.osModelo.set(historico.modelo);
        if (!this.osCor().trim()) this.osCor.set(historico.cor);
        if (!this.osAno().trim() && historico.ano !== null) {
          this.osAno.set(String(historico.ano));
        }
      }
    } catch {
      if (seq === this.sequenciaPlaca) {
        this.historicoVeiculo.set(null);
      }
    } finally {
      if (seq === this.sequenciaPlaca) {
        this.buscandoHistoricoVeiculo.set(false);
      }
    }
  }

  selecionarClienteDoHistorico(): void {
    const hist = this.historicoVeiculo();
    if (!hist || this.osBloqueada()) return;
    const encontrado = this.clientes().find((c) => c.id === hist.ultimoClienteId);
    if (encontrado) {
      this.selecionarClienteBusca(encontrado);
    }
  }

  aoDigitarMaoDeObraAvulsa(valor: string): void {
    this.osMaoDeObraAvulsa.set(formatarMoedaInput(valor));
  }

  aoDigitarMaoDeObraDaPeca(valor: string): void {
    this.pecaMaoDeObra.set(formatarMoedaInput(valor));
  }

  aoDigitarPecaCusto(valor: string): void {
    this.pecaCusto.set(formatarMoedaInput(valor));
  }

  aoDigitarPecaValorCobrado(valor: string): void {
    this.pecaValorCobrado.set(formatarMoedaInput(valor));
  }

  readonly buscaClienteExecutada = signal(false);
  readonly resultadosCliente = signal<Cliente[]>([]);

  aoBuscarClienteNaOs(valor: string): void {
    this.osBuscaCliente.set(valor);
    this.osClienteId.set(null);
    this.buscaClienteExecutada.set(false);
    this.resultadosCliente.set([]);
    this.limparErroCliente();
  }

  private limparErroCliente(): void {
    const erros = { ...this.osErros() };
    delete erros['clienteId'];
    this.osErros.set(erros);
  }

  selecionarClienteBusca(cliente: Cliente): void {
    this.osClienteId.set(cliente.id);
    this.osBuscaCliente.set(cliente.nome);
    this.resultadosCliente.set([]);
    this.buscaClienteExecutada.set(false);
    this.limparErroCliente();
  }

  buscarClienteOs(event?: Event): void {
    event?.preventDefault();
    if (this.osBloqueada() || this.carregandoClientes() || this.erroListaClientes()) return;
    this.buscaClienteExecutada.set(true);
    const termo = this.osBuscaCliente().trim();
    const digitos = somenteDigitos(termo);
    const documento = /^[\d.\/\-\s]+$/.test(termo);
    const resultados = !termo ? [] : documento
      ? this.clientes().filter(c => somenteDigitos(c.documento) === digitos && [11,14].includes(digitos.length))
      : this.clientesFiltradosOs();
    this.resultadosCliente.set(resultados);
    if (resultados.length === 1) this.selecionarClienteBusca(resultados[0]);
  }

  // Validação e Cadastro de Cliente na Aba Clientes
  private validarDadosCliente(
    nome: string,
    documento: string,
    telefone: string,
  ): Record<string, string[]> {
    const erros: Record<string, string[]> = {};
    const nomeLimpo = nome.trim();
    if (!nomeLimpo) erros['nome'] = ['Informe o nome do cliente.'];
    else if (nomeLimpo.length > 200)
      erros['nome'] = ['Use até 200 caracteres.'];

    const docDigitos = somenteDigitos(documento);
    if (docDigitos.length !== 11 && docDigitos.length !== 14) {
      erros['documento'] = ['Informe um CPF (11 dígitos) ou CNPJ (14 dígitos).'];
    }

    const telDigitos = somenteDigitos(telefone);
    if (telDigitos.length !== 10 && telDigitos.length !== 11) {
      erros['telefone'] = ['Informe um telefone válido com DDD.'];
    }
    return erros;
  }

  async salvarClienteNaAba(): Promise<void> {
    if (this.salvandoCliente()) return;
    this.erroCadastroCliente.set(null);
    this.ultimoClienteCriado.set(null);

    const erros = this.validarDadosCliente(
      this.cadNome(),
      this.cadDocumento(),
      this.cadTelefone(),
    );
    this.errosCampoCliente.set(erros);
    if (Object.keys(erros).length > 0) return;

    this.salvandoCliente.set(true);
    try {
      const criado = await this.api.criarCliente({
        nome: this.cadNome().trim(),
        documento: somenteDigitos(this.cadDocumento()),
        telefone: somenteDigitos(this.cadTelefone()),
        email: '',
      });
      this.cadNome.set('');
      this.cadDocumento.set('');
      this.cadTelefone.set('');
      this.errosCampoCliente.set({});
      this.ultimoClienteCriado.set(criado);
      this.ultimoClienteEditado.set(null);
      this.todosClientes.update((atuais) => [
        criado,
        ...atuais.filter((c) => c.id !== criado.id),
      ]);
    } catch (e) {
      if (e instanceof ApiException) {
        this.erroCadastroCliente.set(e.message);
        this.errosCampoCliente.set(e.errors);
      } else {
        this.erroCadastroCliente.set('Erro ao salvar o cliente.');
      }
    } finally {
      this.salvandoCliente.set(false);
    }
  }

  // Modal rápido de cliente dentro da Nova OS
  abrirModalClienteDaOs(termoInicial?: string): void {
    const texto = (termoInicial ?? '').trim();
    const temLetra = /[A-Za-zÀ-ÿ]/.test(texto);
    const digitos = somenteDigitos(texto);

    this.modalCliNome.set(temLetra ? texto : '');
    this.modalCliDoc.set(
      !temLetra && digitos.length > 0 ? formatarCpfOuCnpj(digitos) : '',
    );
    this.modalCliTel.set('');
    this.modalCliErros.set({});
    this.modalCliErroGeral.set(null);
    this.modalNovoClienteAberto.set(true);
  }

  async salvarClienteDoModal(): Promise<void> {
    if (this.modalCliSalvando()) return;
    this.modalCliErroGeral.set(null);
    const erros = this.validarDadosCliente(
      this.modalCliNome(),
      this.modalCliDoc(),
      this.modalCliTel(),
    );
    this.modalCliErros.set(erros);
    if (Object.keys(erros).length > 0) return;

    this.modalCliSalvando.set(true);
    try {
      const criado = await this.api.criarCliente({
        nome: this.modalCliNome().trim(),
        documento: somenteDigitos(this.modalCliDoc()),
        telefone: somenteDigitos(this.modalCliTel()),
        email: '',
      });
      this.todosClientes.update((atuais) => [
        criado,
        ...atuais.filter((c) => c.id !== criado.id),
      ]);
      this.osClienteId.set(criado.id);
      this.osBuscaCliente.set('');
    this.buscaClienteExecutada.set(false);
    this.resultadosCliente.set([]);
      this.modalNovoClienteAberto.set(false);
    } catch (e) {
      if (e instanceof ApiException) {
        this.modalCliErroGeral.set(e.message);
        this.modalCliErros.set(e.errors);
      } else {
        this.modalCliErroGeral.set('Não foi possível salvar o cliente.');
      }
    } finally {
      this.modalCliSalvando.set(false);
    }
  }

  abrirEdicaoCliente(cliente: Cliente, dialog: HTMLDialogElement): void {
    this.clienteEmEdicao.set(cliente);
    this.editCliNome.set(cliente.nome);
    this.editCliDoc.set(formatarCpfOuCnpj(cliente.documento));
    this.editCliTel.set(formatarTelefone(cliente.telefone));
    this.editCliAtivo.set(cliente.ativo);
    this.editCliErros.set({});
    this.editCliErroGeral.set(null);
    this.ultimoClienteEditado.set(null);
    dialog.showModal();
  }

  aoFecharEdicaoCliente(): void {
    this.clienteEmEdicao.set(null);
    this.editCliErros.set({});
    this.editCliErroGeral.set(null);
  }

  aoCancelarEdicaoCliente(event: Event): void {
    if (this.editCliSalvando()) {
      event.preventDefault();
    }
  }

  async salvarEdicaoCliente(dialog: HTMLDialogElement): Promise<void> {
    if (this.editCliSalvando()) return;
    const alvo = this.clienteEmEdicao();
    if (!alvo) return;

    this.editCliErroGeral.set(null);
    const erros = this.validarDadosCliente(
      this.editCliNome(),
      this.editCliDoc(),
      this.editCliTel(),
    );
    this.editCliErros.set(erros);
    if (Object.keys(erros).length > 0) return;

    const nomeLimpo = this.editCliNome().trim();
    const docDigitos = somenteDigitos(this.editCliDoc());
    const telDigitos = somenteDigitos(this.editCliTel());
    const ativo = this.editCliAtivo();

    this.editCliSalvando.set(true);
    try {
      await this.api.atualizarCliente({
        id: alvo.id,
        nome: nomeLimpo,
        documento: docDigitos,
        telefone: telDigitos,
        email: '',
        ativo,
      });

      this.todosClientes.update((atuais) =>
        atuais.map((c) =>
          c.id === alvo.id
            ? {
                ...c,
                nome: nomeLimpo,
                documento: docDigitos,
                telefone: telDigitos,
                ativo,
              }
            : c,
        ),
      );
      this.ultimoClienteEditado.set(nomeLimpo);
      dialog.close();
    } catch (e) {
      if (e instanceof ApiException) {
        this.editCliErroGeral.set(e.message);
        this.editCliErros.set(e.errors);
      } else {
        this.editCliErroGeral.set('Não foi possível atualizar o cliente.');
      }
    } finally {
      this.editCliSalvando.set(false);
    }
  }

  // Fluxo de Nova Ordem de Serviço
  iniciarNovaOrdem(clientePreSelecionado?: Cliente): void {
    const rascunho = !clientePreSelecionado ? carregarRascunhoOs() : null;
    this.rascunhoRecuperado.set(Boolean(rascunho));
    this.historicoVeiculo.set(null);
    this.buscandoHistoricoVeiculo.set(false);
    this.ultimaPlacaConsultada = '';

    if (rascunho) {
      this.osBuscaCliente.set(rascunho.buscaCliente);
      this.osClienteId.set(rascunho.clienteId);
      this.osPlaca.set(rascunho.placa);
      this.osMarca.set(rascunho.marca);
      this.osModelo.set(rascunho.modelo);
      this.osCor.set(rascunho.cor);
      this.osAno.set(rascunho.ano);
      this.osDescricao.set(rascunho.descricao);
      this.osMaoDeObraAvulsa.set(rascunho.maoDeObraAvulsa);
      this.osNecessitaRetorno.set(rascunho.necessitaRetorno);
      this.osPrazoRetornoDias.set(rascunho.prazoRetornoDias);
      this.osEhOrcamento.set(rascunho.ehOrcamento);
      this.osIdempotencyKey.set(rascunho.idempotencyKey);
      this.osPecas.set(rascunho.pecas);
    } else {
      this.osBuscaCliente.set('');
      this.osClienteId.set(clientePreSelecionado?.id ?? null);
      this.osPlaca.set('');
      this.osMarca.set('');
      this.osModelo.set('');
      this.osCor.set('');
      this.osAno.set('');
      this.osDescricao.set('');
      this.osMaoDeObraAvulsa.set('');
      this.osNecessitaRetorno.set(false);
      this.osPrazoRetornoDias.set('');
      this.osEhOrcamento.set(false);
      this.osIdempotencyKey.set(gerarChaveIdempotencia());
      this.osPecas.set([]);
    }

    this.buscaClienteExecutada.set(false);
    this.resultadosCliente.set([]);
    this.osErros.set({});
    this.osMensagemErro.set(null);
    this.osEnviando.set(false);
    this.osResultadoIncerto.set(false);
    this.abaAtiva.set('ordens');
    this.abrindoNovaOs.set(true);
    this.menuExpandido.set(false);
    this.carregarClientes();

    const placaNorm = normalizarPlaca(this.osPlaca());
    if (/^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(placaNorm)) {
      void this.buscarHistoricoPlaca(placaNorm);
    }
  }

  descartarRascunhoNovaOs(): void {
    if (this.osEnviando()) return;
    limparRascunhoOs();
    this.rascunhoRecuperado.set(false);
    this.historicoVeiculo.set(null);
    this.ultimaPlacaConsultada = '';
    this.osBuscaCliente.set('');
    this.osClienteId.set(null);
    this.osPlaca.set('');
    this.osMarca.set('');
    this.osModelo.set('');
    this.osCor.set('');
    this.osAno.set('');
    this.osDescricao.set('');
    this.osMaoDeObraAvulsa.set('');
    this.osNecessitaRetorno.set(false);
    this.osPrazoRetornoDias.set('');
    this.osEhOrcamento.set(false);
    this.osIdempotencyKey.set(gerarChaveIdempotencia());
    this.osPecas.set([]);
    this.osErros.set({});
    this.osMensagemErro.set(null);
    this.osResultadoIncerto.set(false);
  }

  voltarParaInicio(): void {
    if (this.osEnviando()) return;
    this.abrindoNovaOs.set(false);
  }

  // Modal de Peças
  abrirModalPeca(): void {
    if (this.osBloqueada()) return;
    this.pecaNome.set('');
    this.pecaQuantidade.set('1');
    this.pecaCusto.set('');
    this.pecaMaoDeObra.set('');
    this.pecaValorCobrado.set('');
    this.pecaErros.set({});
    this.modalPecaAberta.set(true);
  }

  adicionarPecaDoModal(): void {
    const erros: Record<string, string> = {};
    const nome = this.pecaNome().trim();
    if (!nome) erros['nome'] = 'Informe o nome da peça.';
    else if (nome.length > 200) erros['nome'] = 'Use até 200 caracteres.';

    const qtd = Number.parseInt(somenteDigitos(this.pecaQuantidade()), 10);
    if (!Number.isInteger(qtd) || qtd <= 0 || qtd > 2147483647) {
      erros['quantidade'] = 'Informe uma quantidade entre 1 e 2.147.483.647.';
    }

    const custo = textoParaCentavos(this.pecaCusto());
    if (custo === null || custo < 0) {
      erros['custo'] = 'Informe o custo da peça.';
    }

    const cobrado = textoParaCentavos(this.pecaValorCobrado());
    if (cobrado === null || cobrado < 0) {
      erros['valor'] = 'Informe o valor cobrado do cliente.';
    }

    const maoDeObra = textoParaCentavos(this.pecaMaoDeObra());
    if (maoDeObra === null) {
      erros['maoDeObra'] = 'Informe a mão de obra por unidade. Digite 0 se não houver cobrança.';
    }

    this.pecaErros.set(erros);
    if (Object.keys(erros).length > 0) return;

    this.osPecas.update((atuais) => [
      ...atuais,
      {
        nome,
        quantidade: qtd,
        custoUnitarioCentavos: custo!,
        maoDeObraUnitariaCentavos: maoDeObra!,
        valorUnitarioCentavos: 0,
        valorTotalCentavos: cobrado!,
      },
    ]);
    this.modalPecaAberta.set(false);
  }

  removerPeca(indice: number): void {
    if (this.osBloqueada()) return;
    this.osPecas.update((atuais) => atuais.filter((_, i) => i !== indice));
  }

  tentarReenviarComIdempotencia(): void {
    if (this.osEnviando() || !this.osResultadoIncerto()) return;
    this.osResultadoIncerto.set(false);
    void this.salvarOrdemServico();
  }

  async salvarOrdemServico(): Promise<void> {
    if (this.osBloqueada()) return;
    const erros: Record<string, string[]> = {};
    const addErro = (campo: string, msg: string) => (erros[campo] = [msg]);

    if (this.osClienteId() === null) {
      addErro('clienteId', 'Selecione um cliente.');
    }

    const placaNorm = normalizarPlaca(this.osPlaca());
    if (!/^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(placaNorm)) {
      addErro('placa', 'Placa inválida.');
    }

    if (!this.osMarca().trim()) addErro('marca', 'Informe a marca.');
    if (!this.osModelo().trim()) addErro('modelo', 'Informe o modelo.');
    if (!this.osCor().trim()) addErro('cor', 'Informe a cor.');

    const anoTxt = this.osAno().trim();
    let anoNum: number | null = null;
    if (anoTxt) {
      anoNum = Number.parseInt(anoTxt, 10);
      const maxAno = new Date().getFullYear() + 1;
      if (!Number.isInteger(anoNum) || anoNum < 1900 || anoNum > maxAno) {
        addErro('ano', 'Informe um ano válido.');
      }
    }

    if (!this.osDescricao().trim()) {
      addErro('descricaoServico', 'Descreva o serviço.');
    }

    const maoDeObraAvulsaTxt = this.osMaoDeObraAvulsa().trim();
    if (maoDeObraAvulsaTxt) {
      const avulsaCentavos = textoParaCentavos(maoDeObraAvulsaTxt);
      if (avulsaCentavos === null || avulsaCentavos < 0) {
        addErro('maoDeObraCentavos', 'Informe um valor válido de mão de obra.');
      }
    }

    if (this.totalGeralCentavos() > BigInt(Number.MAX_SAFE_INTEGER)) {
      addErro('totalCentavos', 'O total da OS excede o limite de valores em centavos.');
    }

    let prazoNum: number | null = null;
    if (this.osNecessitaRetorno()) {
      prazoNum = Number.parseInt(somenteDigitos(this.osPrazoRetornoDias()), 10);
      if (!Number.isInteger(prazoNum) || prazoNum <= 0) {
        addErro('prazoRetornoDias', 'Informe um prazo em dias maior que zero.');
      }
    }

    this.osErros.set(erros);
    if (Object.keys(erros).length > 0) {
      this.osMensagemErro.set('Confira os campos destacados abaixo.');
      return;
    }

    this.osEnviando.set(true);
    this.osMensagemErro.set(null);
    try {
      const criada = await this.api.criarOrdemServico(
        {
          clienteId: this.osClienteId()!,
          placa: placaNorm,
          marca: this.osMarca().trim(),
          modelo: this.osModelo().trim(),
          cor: this.osCor().trim(),
          ano: anoNum,
          descricaoServico: this.osDescricao().trim(),
          pecas: this.osPecas().map(
            ({ nome, quantidade, custoUnitarioCentavos, valorUnitarioCentavos, valorTotalCentavos }) => ({
              nome,
              quantidade,
              custoUnitarioCentavos,
              valorUnitarioCentavos,
              valorTotalCentavos,
            }),
          ),
          maoDeObraCentavos: Number(this.maoDeObraCentavos()),
          necessitaRetorno: this.osNecessitaRetorno(),
          prazoRetornoDias: this.osNecessitaRetorno() ? prazoNum : null,
          ehOrcamento: this.osEhOrcamento(),
        },
        this.osIdempotencyKey(),
      );
      this.ultimaOsCriada.set(criada);
      limparRascunhoOs();
      this.osIdempotencyKey.set(gerarChaveIdempotencia());
      this.rascunhoRecuperado.set(false);
      void this.carregarOrdens(1);
      this.abrindoNovaOs.set(false);
    } catch (e) {
      if (e instanceof ApiException) {
        this.osMensagemErro.set(e.message);
        this.osErros.set(e.errors);
        this.osResultadoIncerto.set(e.resultadoIncerto);
      } else {
        this.osMensagemErro.set('Falha inesperada ao salvar a ordem de serviço.');
      }
    } finally {
      this.osEnviando.set(false);
    }
  }

  // Helpers de exibição no template
  reais(centavos: number | bigint): string {
    return `R$ ${formatarCentavos(centavos)}`;
  }

  maoDeObraDaPeca(item: PecaRascunho): bigint {
    return BigInt(item.quantidade) * BigInt(item.maoDeObraUnitariaCentavos);
  }

  subtotalPeca(item: PecaItem): string {
    return this.reais(
      item.valorTotalCentavos != null
        ? BigInt(item.valorTotalCentavos)
        : BigInt(item.quantidade) * BigInt(item.valorUnitarioCentavos),
    );
  }

  pecaRascunhoAbaixoDoCusto(item: PecaItem): boolean {
    const cobrado =
      item.valorTotalCentavos != null
        ? BigInt(item.valorTotalCentavos)
        : BigInt(item.quantidade) * BigInt(item.valorUnitarioCentavos);
    const custoTotal = BigInt(item.quantidade) * BigInt(item.custoUnitarioCentavos);
    return cobrado < custoTotal;
  }

  placaFormatada(placa: string): string {
    return formatarPlacaInput(placa) || placa;
  }

  docFormatado(doc: string): string {
    return formatarCpfOuCnpj(doc) || 'Não informado';
  }

  telFormatado(tel: string): string {
    return formatarTelefone(tel) || 'Não informado';
  }

  erroCampo(mapa: Record<string, string[]>, campo: string): string | null {
    return mapa[campo]?.join(' ') ?? null;
  }
}
