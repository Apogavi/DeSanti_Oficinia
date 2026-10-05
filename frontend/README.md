# DeSanti Automotiva

Aplicativo Flutter para a oficina. O desenvolvimento ocorre no Omarchy;
o aplicativo final será usado no Windows.

## Tema

`lib/theme/app_theme.dart` define as cores da marca e os temas claro e escuro.
`lib/main.dart` seleciona o tema conforme a configuração do sistema.

## Ordem de serviço

O formulário coleta nome, CPF e telefone do cliente; placa, marca, modelo, cor
e ano opcional do veículo; e descrição do serviço. CPF, telefone e placa recebem
máscaras durante a digitação; o ano aceita até quatro dígitos. É possível
adicionar e remover peças com nome, quantidade e valor unitário. O valor é
formatado enquanto se digita (`12000` → `120,00`); subtotais e total de peças
são calculados em centavos. A mão de obra usa o mesmo formato e compõe o total
geral. Ao marcar "Necessário retorno", o prazo em dias passa a ser obrigatório.
O botão "Conferir dados" valida os campos, mas não salva a OS.

Após a validação, a página monta um `CriarOrdemServicoRequest` definido em
`lib/features/ordens_servico/criar_ordem_servico_request.dart`. Nesse objeto,
CPF e telefone ficam sem máscara, e a placa é normalizada sem hífen. O
formulário usa `SingleChildScrollView` com `Column` para manter todos os campos
montados durante a validação; isso garante que Nome também seja verificado
quando o botão é acionado no fim da página. Ainda não há cliente HTTP nem
conexão com a API.

Próximas etapas: ligar o formulário à criação da OS pela API ASP.NET, depois de
implementar a persistência MySQL no backend, e consultar placa pela API para
sugerir marca, modelo, cor e ano. A integração prevista é com a Falcon Data Hub;
os campos preenchidos automaticamente continuarão editáveis.

Regra planejada para retorno: o prazo em dias pode ser informado durante a OS.
A data prevista será calculada somente quando o serviço for finalizado, usando
a data de conclusão como início da contagem. O formulário atual ainda não
finaliza serviços nem agenda lembretes.

## Acesso futuro pelo celular

O frontend poderá ganhar uma versão Flutter Web adaptada a telas menores. O
endereço da API deverá ser configurável para desenvolvimento local e eventual
hospedagem; o navegador e o aplicativo acessarão os dados somente pela API.
