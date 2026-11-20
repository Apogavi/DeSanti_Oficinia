# DeSanti Automotiva — Plano de entregas

Elaborado em **10/11/2026** `[Codex / GPT-6]` a pedido de Diogo. Este é um plano de trabalho, não uma autorização para implementar cada etapa, alterar o banco ou publicar o sistema. Consulte também [AGENTS.md](AGENTS.md), [ROADMAP_E_REPASSE_CODEX.md](ROADMAP_E_REPASSE_CODEX.md) e os arquivos atuais antes de cada mudança. Propostas de API contract e interface abaixo precisam ser alinhadas antes da implementação; o código continua sendo a referência para o que já existe.

## 1. Ponto de partida

- O frontend principal está em `frontend/` e usa Angular + TypeScript; o Flutter anterior está em `frontend_flutter_off/`. O `package.json` declara Angular `^22.2.0`; alguns repasses históricos mencionam Angular 21.
- A tela de Clientes permite cadastro e consulta. A abertura de OS seleciona um cliente existente, recebe veículo, serviço, peças, mão de obra e retorno.
- O backend expõe a criação de OS em `POST /api/v1/ordens-servico`. O controller atual não expõe listagem, detalhes nem finalização de OS. O total oficial é calculado no backend.
- Cada peça de uma OS nova exige custo conhecido, inclusive quando zero. A coluna histórica de custo continua nullable; não transformar custo desconhecido em zero.
- Após salvar, o Angular já volta à área de Ordens de serviço e mostra uma mensagem com ID e total; ainda não apresenta uma lista de OS existentes.
- Em **10/11/2026** `[Codex / GPT-6]`, um teste com dados fictícios percorreu Angular → API → MySQL local `desanti_dev`: cliente e OS foram criados, e quantidade, preço, custo e total foram conferidos no banco. Um teste separado verificou histórico e rollback da OS/peças. Os registros fictícios foram removidos. Isso não valida produção nem todas as situações de uso.

## 2. Próxima entrega — tela geral e detalhes das OS

### 2.1 Objetivo e fluxo desejado

Diogo descreveu a tela **Ordens de serviço** como a área geral de consulta:

1. O usuário abre a área e encontra o botão **+ Nova ordem de serviço** e a lista das OS salvas.
2. Ao criar e salvar uma OS com sucesso, o sistema retorna à tela geral e apresenta a ordem recém-criada na lista, além de confirmar a operação.
3. Ao selecionar uma OS, abre-se um modal ou apresentação semelhante com seus detalhes.
4. O usuário fecha os detalhes e volta à lista, mantendo o contexto de consulta quando possível.

Busca por placa ou cliente e filtro por status são propostas do repasse para facilitar a consulta. A apresentação exata (modal ou painel), os campos da lista e os filtros devem ser alinhados no planejamento da implementação. Não depender de dados mantidos apenas na memória: a lista precisa refletir o backend e continuar disponível ao recarregar.

### 2.2 Backend — consulta

- Definir e confirmar o API contract de leitura: endpoints, parâmetros de busca/filtro, paginação ou limite, ordenação, formato da resposta, `404` e errors. O repasse **propõe** `GET /api/v1/ordens-servico` e `GET /api/v1/ordens-servico/{id}`, ainda não implementados.
- Criar DTOs de resumo e detalhes, expondo somente os campos necessários à tela e sem retornar automaticamente o grafo de entities. Avaliar separadamente a exposição do custo interno nas telas destinadas à oficina.
- Projetar leitura no Repository e coordená-la pelo Service, mantendo Domain, Application, Infrastructure e API nas boundaries existentes. Utilizar consultas de leitura adequadas ao EF Core e evitar consultas desnecessárias por item.
- Preservar o snapshot histórico do cliente armazenado na OS, em vez de substituir nome/documento/telefone por valores atuais do cadastro ao mostrar o histórico.
- Tratar OS sem peças, ano ou retorno; peças antigas podem ter custo `null`. Preservar centavos inteiros e datas UTC, com apresentação de data apropriada ao usuário.
- Verificar se os filtros e a ordenação funcionam com a quantidade de dados prevista; decidir paginação antes de assumir que uma listagem ilimitada é suficiente.

### 2.3 Frontend — área geral

- Criar tipos TypeScript coerentes com o contract de leitura e métodos no `ApiService`; não presumir a existência de endpoints antes de implementados.
- Exibir lista legível de OS, com informações úteis como número, cliente, veículo/placa, data, status e total, conforme definição visual acordada.
- Tratar `loading`, lista vazia, falha de comunicação e opção de tentar novamente. Filtro sem resultados deve ser distinguível de ausência de OS cadastradas.
- Atualizar a lista depois do `201` de criação. Se a atualização falhar após a criação ter sido confirmada, informar isso sem sugerir que a OS não foi salva nem reenviar a criação automaticamente.
- Preservar a proteção existente para resultado incerto da criação e o rascunho em caso de falha; não introduzir retry automático sem estratégia de idempotency acordada.
- Considerar busca/filtros por teclado, labels acessíveis, layout responsivo e recuperação de contexto ao sair dos detalhes.

### 2.4 Frontend — detalhes

- Buscar os dados completos da OS selecionada; tratar carregamento, falha e OS não encontrada.
- Apresentar snapshot do cliente, veículo, descrição, peças com quantidade e preço, mão de obra, total e informações de retorno. Exibir custos internos somente de acordo com a finalidade e as decisões do sistema.
- Tratar ausência de peças ou de custo histórico sem inventar valores. Formatar moedas em centavos e deixar clara a ausência de conclusão ou previsão de retorno.
- Se escolhido modal, oferecer fechamento explícito, navegação por teclado, foco adequado e retorno do foco ao item selecionado. Uma alternativa de painel deve oferecer ergonomia equivalente.
- Usar `design-studio` e as skills apropriadas, preservando a identidade existente e testando o custo visual no computador modesto da oficina.

### 2.5 Critérios de aceite e verificações

- [ ] OS criada anteriormente aparece após recarregar a página; lista e detalhes usam dados persistidos.
- [ ] Nova OS salva retorna à área geral e aparece na lista, com o total oficial retornado pelo backend.
- [ ] Abrir e fechar detalhes funciona com mouse e teclado; dados apresentados correspondem à OS selecionada.
- [ ] Busca e filtro, se incluídos no escopo aprovado, retornam resultados coerentes e tratam resultado vazio.
- [ ] Loading, lista vazia, erro, OS inexistente e falha de atualização após salvar têm mensagens e ações adequadas.
- [ ] OS sem peças e peça histórica com custo desconhecido não causam erro nem apresentam custo desconhecido como zero.
- [ ] Unit Tests e Integration Tests adequados cobrem o contract, snapshot histórico e estados críticos; validar o fluxo completo com dados fictícios no ambiente local quando autorizado para a execução.
- [ ] Testar layout, acessibilidade e desempenho no equipamento alvo; registrar resultados reais e limitações.

## 3. Finalização de OS e retorno

Após a consulta/detalhes, implementar a transição de OS aberta para concluída em Domain, Application, Infrastructure, API e frontend. A regra confirmada é registrar conclusão e calcular a previsão a partir da data de conclusão quando houver retorno; ambas ficam vazias antes da finalização.

- Definir o contract e a política para tentativa de finalizar uma OS já concluída, concorrência e eventuais falhas de comunicação. O endpoint e status descritos no repasse são propostas, não contrato implementado.
- Validar a transição no Domain e persistir o resultado de forma consistente; atualizar a lista e os detalhes após confirmação do backend.
- Testar OS com e sem retorno, prazo aplicável, repetição/conflito e datas exibidas corretamente. Não confundir conclusão de OS com criação.

## 4. Edição de clientes — opcional na fase 1

- Avaliar com Diogo a prioridade e quais campos poderão ser editados. Existe `PUT /api/v1/clientes`, mas seu contract, validação e efeito precisam ser conferidos antes de conectá-lo à interface.
- Preservar o histórico já copiado nas OS existentes; editar o cadastro não deve reescrever esse snapshot.
- Tratar validation, sucesso e erro, e testar que a alteração não modifica dados históricos das ordens.

## 5. Relatórios e resumo financeiro — fase 2

Antes de implementar, alinhar: quais OS entram (todas ou concluídas), qual data define o período, semana e timezone, agregação por quantidade, tratamento de OS sem peças e tratamento explícito de custos históricos desconhecidos. Confirmar também como alterações futuras no preço do fornecedor serão refletidas, sem modificar indevidamente o histórico.

Regras de negócio já registradas:

- O preço da peça é o valor cobrado do cliente; o custo é o valor pago ao fornecedor. O líquido da peça é a diferença entre eles.
- A mão de obra é separada e entra integralmente no líquido operacional da OS, somada ao líquido das peças. Discriminá-la do resultado das peças mesmo quando houver total conjunto.
- O líquido operacional não desconta todas as despesas da oficina e não deve ser apresentado como lucro líquido total do negócio.
- O repasse propõe multiplicar valores unitários por quantidade; fechar as regras de agregação com Diogo antes de implementar.

Depois de definidos os critérios, planejar consultas, contract, tela, estados de ausência/erro e tests com dados de períodos e custos diversos. Não inventar custo de registros antigos nem alterar schema sem autorização específica.

## 6. PDF e compartilhamento por WhatsApp

Recurso desejado para etapa posterior; o repasse sugere fase 3, mas essa posição não está confirmada. Definir conteúdo, visual de impressão, dados exibidos, como o arquivo será gerado e a experiência de compartilhamento. O custo interno da oficina não deve aparecer na versão destinada ao cliente. Validar disponibilidade do compartilhamento no ambiente final e obter autorização para integrações ou dependencies necessárias.

## 7. Consulta de veículo pela placa — futuro / talvez

Avaliar primeiro se há necessidade real. Se aprovado, a API poderá consultar a Falcon após placa válida e sugerir dados editáveis, preservando entrada manual e evitando sobrescrever edições com response atrasada. Confirmar se o campo ano corresponde à fabricação ou ao modelo. Manter o token apenas no backend e fora do Git.

## 8. Ambiente, qualidade e operação

- **Ambientes:** `desanti_dev` é o banco local de desenvolvimento. Para produção, definir instalação/hosting, banco e acesso; `desanti_prod` é um nome sugerido, não um ambiente já criado. Dois schemas no mesmo servidor não isolam falhas do servidor. Planejar usuários e permissões distintos, backups e recuperação.
- **Schema e migrations:** estabelecer uma estratégia incremental revisável para atualizações futuras. O bootstrap do Compose vale para volume novo e não atualiza bancos existentes. Não executar migrations nem mudanças de dados/schema sem solicitação explícita.
- **Configuração:** a URL da API Angular está fixa em `http://localhost:5085`; planejar configuração por ambiente antes de uso em outras máquinas. Não distribuir credenciais do banco no frontend.
- **Windows e acesso:** validar a implantação real no Windows e definir rede, HTTPS e authentication/authorization antes de acesso remoto ou hosting público.
- **Proteção de criação:** discutir idempotency e recuperação de rascunho persistente. O bloqueio atual de reenvio após resultado incerto é restrito à tela atual.
- **Documentação:** atualizar READMEs e guias históricos quando houver pedido de edição, distinguindo o frontend Angular ativo do Flutter e a regra atual de custo obrigatório. Registrar checks efetivamente realizados, não apenas resultados herdados dos repasses.

## 9. Ordem sugerida

1. Planejar e implementar consulta/listagem e detalhes da OS, incluindo o retorno à lista após salvar.
2. Implementar finalização e previsão de retorno.
3. Avaliar edição de clientes como item opcional da fase 1.
4. Definir regras e implementar relatórios financeiros.
5. Planejar PDF e WhatsApp, em fase a confirmar.
6. Avaliar integração Falcon apenas quando houver necessidade.

Configuração de ambientes, testes, documentação, acessibilidade e desempenho acompanham as entregas conforme a necessidade; não são confirmação de deployment ou alteração de banco.

## Implementação parcial — 10/11/2026 [Codex / GPT-6]

Implementados GET de lista (pagina, 20 registros, ordem decrescente de criação/ID) e detalhes,
com Repository de consulta na Infrastructure e mapping de DTOs na Application.
Angular recebeu lista paginada, modal nativo e atualização após salvar.
Build backend e Angular passaram; 48 tests backend passaram, incluindo dois novos
tests de snapshot e custo histórico desconhecido. Não houve migration.

Ainda faltam filtros, Integration Tests HTTP das consultas, validação visual e E2E
desta nova entrega, finalização, relatórios e demais etapas. A API em execução
precisa ser reiniciada para disponibilizar os novos endpoints. Não considerar
o roadmap concluído. O DTO da lista atualmente reutiliza o de detalhe (peças
vazias na listagem); pode ser separado na evolução do contract.
