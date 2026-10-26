# ChatBot

Respostas automáticas por palavra-chave no WhatsApp, com escalonamento para um atendente humano e um painel web para o dono da loja.

Backend em ASP.NET Core que recebe as mensagens pelo Twilio, procura palavras-chave cadastradas e responde na hora. Quando não entende a mensagem, avisa um atendente por WhatsApp e deixa o bot quieto naquela conversa por um tempo, para não atropelar o atendimento humano. As respostas e a fila de clientes esperando são gerenciadas por um painel em `/admin/`, servido pela própria API.

## Funcionalidades

- **Gatilhos com várias palavras-chave.** Cada gatilho (por exemplo, "Horário") tem uma lista de chaves ("horario", "horas") e uma única resposta.
- **Comparação tolerante.** Ignora maiúsculas e acentos e procura a chave dentro da frase, então "qual o HORARIO de voces?" encontra o gatilho de horário.
- **Escalonamento.** Se nenhuma chave bate, o cliente recebe uma resposta de espera e o atendente recebe um aviso por WhatsApp. A falha no envio do aviso é registrada em log e nunca impede a resposta ao cliente.
- **Pausa do bot.** Depois de uma escalação, o bot fica quieto para aquele número por um tempo configurável (padrão: 2 horas) ou até o registro ser marcado como atendido. As mensagens recebidas durante a pausa não são respondidas nem gravadas.
- **Painel de atendimento.** Em `/admin/`, protegido por chave de acesso: mostra a fila de clientes esperando (do mais antigo para o mais novo, com o tempo de espera), permite marcar como atendido e criar, editar e apagar as respostas automáticas, sem mexer em código nem no banco.
- **Histórico.** Toda mensagem processada é registrada com o número, o texto, o gatilho encontrado (ou nenhum) e, quando houver, o momento do atendimento.
- **Retenção do histórico.** Um serviço em segundo plano apaga, uma vez por dia, os registros mais antigos que o prazo configurado (padrão: 90 dias), para não guardar dados pessoais indefinidamente.
- **Dados iniciais por migration.** Os gatilhos de exemplo entram via `HasData()`, sem nenhum endpoint aberto para popular o banco.

## Stack

- .NET 10 e ASP.NET Core (Controllers)
- Entity Framework Core com SQLite
- Twilio (WhatsApp): `Twilio.AspNet.Core` para validar a assinatura das requisições e o SDK `Twilio` para enviar o aviso ao atendente
- Painel em HTML, CSS e JavaScript puros (sem framework e sem etapa de build)
- xUnit para testes unitários
- Docker, GitHub Actions e GitHub Container Registry
- Railway para hospedagem

## Endpoints

| Método | Rota | Proteção | Função |
|--------|------|----------|--------|
| POST | `/api/whatsapp/webhook` | Assinatura do Twilio (`X-Twilio-Signature`) | Recebe a mensagem e devolve a resposta em TwiML |
| GET | `/api/gatilhos` | `X-Api-Key` | Lista os gatilhos |
| GET | `/api/gatilhos/{id}` | `X-Api-Key` | Busca um gatilho |
| POST | `/api/gatilhos` | `X-Api-Key` | Cria um gatilho |
| PUT | `/api/gatilhos/{id}` | `X-Api-Key` | Substitui nome, chaves e resposta de um gatilho |
| DELETE | `/api/gatilhos/{id}` | `X-Api-Key` | Apaga um gatilho (e suas chaves e resposta) |
| GET | `/api/historico/pendentes` | `X-Api-Key` | Lista os clientes esperando atendimento, do mais antigo para o mais novo |
| PATCH | `/api/historico/{id}/atender` | `X-Api-Key` | Marca o registro como atendido e libera o bot para aquele cliente |

Corpo do `POST` e do `PUT` de gatilhos:

```json
{
  "nome": "Horário",
  "chaves": ["horario", "horas"],
  "resposta": "Funcionamos de segunda a sexta, das 8h às 18h."
}
```

Respostas de erro:

- `400`: dados inválidos (campo vazio ou acima dos limites de tamanho).
- `401`: chave ausente ou incorreta. Também é o resultado quando a `ApiKey` não está configurada no servidor, para qualquer rota do painel.
- `403`: webhook sem assinatura válida do Twilio.
- `404`: gatilho ou registro de histórico inexistente.
- `429`: limite de requisições excedido (veja a seção de segurança).

Exemplo:

```bash
curl https://SEU-DOMINIO/api/gatilhos -H "X-Api-Key: SUA_CHAVE"
```

## Painel de atendimento

Acesse `https://SEU-DOMINIO/admin/` e entre com a chave de acesso (o valor de `ApiKey`).

- **Na fila:** clientes que mandaram uma mensagem que o bot não entendeu e ainda não foram atendidos. A espera aparece em cores (mais longa, mais atrasada) e há um atalho para abrir a conversa no WhatsApp. A lista se atualiza sozinha a cada 30 segundos.
- **Respostas automáticas:** cria, edita e apaga gatilhos. O painel valida os campos antes de enviar, e o servidor valida de novo.

A chave fica guardada só na sessão do navegador (some ao fechar a aba) e é enviada no cabeçalho `X-Api-Key`. Os arquivos ficam em `wwwroot/admin/`.

## Configuração

Valores sensíveis vêm de variáveis de ambiente (ou do User Secrets, em desenvolvimento), nunca do repositório.

| Variável | Obrigatória | Função |
|----------|-------------|--------|
| `ConnectionStrings__DefaultConnection` | Sim | Caminho do banco SQLite (em produção, aponte para o volume persistente, por exemplo `Data Source=/data/chatbot.db`) |
| `Twilio__AccountSid` | Sim | Identificador da conta Twilio |
| `Twilio__AuthToken` | Sim | Token do Twilio (também valida a assinatura do webhook) |
| `Twilio__WhatsAppNumber` | Sim | Número WhatsApp de onde o aviso ao atendente é enviado |
| `Notification__AgentNumber` | Sim | Número do atendente que recebe o aviso de escalação |
| `ApiKey` | Sim | Chave de acesso do painel e das rotas `/api/gatilhos` e `/api/historico`. Use um valor longo e aleatório |
| `Escalation__PauseHours` | Não | Horas em que o bot fica quieto após uma escalação (padrão: `2`) |
| `Historico__RetencaoDias` | Não | Dias de retenção do histórico (padrão: `90`; `0` desliga a limpeza) |

As migrations são aplicadas automaticamente quando o app inicia.

## Segurança

- **Webhook:** só aceita requisições assinadas pelo Twilio.
- **Chave de acesso:** comparada em tempo constante e nunca registrada em log. Sem `ApiKey` configurada, as rotas protegidas recusam tudo.
- **Limite de requisições:** 60 por minuto por IP nas rotas do painel (o webhook fica de fora, porque quem chama é o Twilio). Passou disso, a resposta é `429`.
- **Validação no servidor:** nome (80 caracteres), até 30 palavras-chave por resposta (50 caracteres cada) e resposta (1600 caracteres, o limite de uma mensagem de WhatsApp).
- **Saída segura:** o painel exibe tudo com `textContent` (sem `innerHTML`) e o texto da resposta é escapado ao montar o TwiML.
- **Cabeçalhos:** `Content-Security-Policy` restritiva, `X-Content-Type-Options`, `X-Frame-Options` e `Referrer-Policy` em todas as respostas.
- **Registro de acessos negados:** tentativas com chave ausente ou inválida geram um aviso com IP, método e caminho (nunca a chave digitada).
- **Dados pessoais:** número e texto das mensagens são apagados automaticamente após o prazo de retenção.

## Decisões de projeto

- **Um projeto só, com as camadas em pastas.** O domínio tem poucas entidades e um fluxo curto, então separar em vários projetos acrescentaria manutenção sem ganho. A separação de responsabilidades continua nas pastas e nas dependências.
- **SQLite.** É um arquivo, sem servidor separado, o que mantém a hospedagem mais barata. O volume persistente é obrigatório em produção.
- **Regras de negócio puras.** `MatchingService` e `EscalationPauseService` não acessam banco nem rede e recebem tudo por parâmetro (inclusive a hora atual), então os testes não precisam de mock nem dependem do relógio.
- **Interface só onde há I/O.** O notificador do Twilio fica atrás de `IWhatsAppNotifier`, para os testes não enviarem mensagens de verdade.
- **Painel sem framework, servido pela própria API.** Evita um segundo projeto, um processo de build e mais uma superfície para manter, e a chave de acesso nunca sai da mesma origem.
- **Exclusão de verdade.** Apagar um gatilho remove a linha (e suas chaves e resposta em cascata), e o histórico antigo é apagado de fato, em vez de marcado como inativo. O banco não cresce sem fim e não guarda dado pessoal além do necessário.
- **Limpeza testável.** A regra de retenção fica numa classe própria que recebe a data atual por parâmetro; um serviço em segundo plano só a chama uma vez por dia, com um escopo novo do `DbContext` a cada execução.
- **IP real atrás do proxy.** Em produção o app fica atrás do proxy do Railway, que monta a cadeia `X-Forwarded-For` como "cliente, servidor de entrada". O `ForwardedHeaders` usa `ForwardLimit = 2` para o limite de requisições e os logs enxergarem o IP do cliente, e não o do proxy.
- **Segredos fora do código.** Tokens e números vêm de variáveis de ambiente ou do User Secrets, nunca do repositório.

## Testes

```bash
dotnet test Chatbot.Tests
```

Os testes cobrem a comparação de texto (acentos, maiúsculas e frase com texto ao redor), a regra de pausa do bot (escalação aberta, antiga, já atendida, com gatilho e lista vazia) e a limpeza do histórico (apaga só o que passou do prazo, prazo zero desliga a limpeza e um registro no limite exato é mantido). A limpeza é testada contra um SQLite em memória. Rodar `dotnet test` na raiz do repositório não executa os testes, porque a raiz tem só o projeto da API.