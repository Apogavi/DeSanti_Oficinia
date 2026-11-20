# CPF/CNPJ único — 10/11/2026 [Codex / GPT-6]

A Application rejeita documento normalizado já cadastrado, inclusive de cliente
inativo, na criação e na alteração para outro documento. A API retorna 409 com
errors.documento. Telefone não é único. O mapping e bootstrap de bancos novos
usam índice unique; a Infrastructure traduz violação MySQL 1062 para o conflito.

O banco existente NÃO foi alterado: há duplicidades observadas na interface.
É necessário decidir quais cadastros preservar e como tratar suas OS antes de
aplicar o índice. Não excluir nem mesclar automaticamente. Até aplicar o índice,
a consulta prévia não elimina a possibilidade de corrida entre requests.
Nenhuma migration foi executada. A verificação atual consulta todos os clientes;
substituir por consulta específica no Repository na evolução de desempenho.

Verificação: 50 tests backend passaram e build Angular passou. Dois novos cenários
HTTP verificam 409 com documento simples e mascarado. Não foi realizado teste de
concorrência contra banco real. API em execução precisa carregar o código novo.

## Aplicação no banco local — 10/11/2026

Após a limpeza autorizada do desanti_dev, a TASK-14 substituiu o índice local
por UNIQUE em Clientes.Documento. O MySQL confirmou NON_UNIQUE=0 e rejeitou
a duplicata fictícia com erro 1062. O rollback removeu o registro de teste.
