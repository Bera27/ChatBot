# ChatBot

Respostas automáticas por palavra-chave no WhatsApp, com escalonamento para um atendente humano.

Backend em ASP.NET Core que recebe as mensagens pelo Twilio, procura palavras-chave cadastradas e responde na hora. Quando não entende a mensagem, avisa um atendente por WhatsApp e deixa o bot quieto naquela conversa por um tempo, para não atropelar o atendimento humano.

## Funcionalidades

- **Gatilhos com várias palavras-chave.** Cada gatilho (por exemplo, "Horário") tem uma lista de chaves ("horario", "horas") e uma única resposta.
- **Comparação tolerante.** Ignora maiúsculas e acentos e procura a chave dentro da frase, então "qual o HORARIO de voces?" encontra o gatilho de horário.
- **Escalonamento.** Se nenhuma chave bate, o cliente recebe uma resposta de espera e o atendente recebe um aviso por WhatsApp. A falha no envio do aviso é registrada em log e nunca impede a resposta ao cliente.
- **Pausa do bot.** Depois de uma escalação, o bot fica quieto para aquele número por um tempo configurável (padrão: 2 horas) ou até o registro ser marcado como atendido. As mensagens recebidas durante a pausa não são respondidas nem gravadas.
- **Histórico.** Toda mensagem processada é registrada com o número, o texto, o gatilho encontrado (ou nenhum) e, quando houver, o momento do atendimento.
- **Dados iniciais por migration.** Os gatilhos de exemplo entram via `HasData()`, sem nenhum endpoint aberto para popular o banco.

## Stack

- .NET 10 e ASP.NET Core (Controllers)
- Entity Framework Core com SQLite
- Twilio (WhatsApp): `Twilio.AspNet.Core` para validar a assinatura das requisições e o SDK `Twilio` para enviar o aviso ao atendente
- xUnit para testes unitários
- Docker, GitHub Actions e GitHub Container Registry
- Railway para hospedagem

## Endpoints

| Método | Rota | Proteção | Função |
|--------|------|----------|--------|
| POST | `/api/whatsapp/webhook` | Assinatura do Twilio (`X-Twilio-Signature`) | Recebe a mensagem e devolve a resposta em TwiML |
| PATCH | `/api/whatsapp/historico/{id}/atender` | Cabeçalho `X-Api-Key` | Marca o registro como atendido e libera o bot para aquele cliente |

Requisições ao webhook sem assinatura válida recebem `403`. O endpoint de atendimento recusa qualquer chamada quando a `ApiKey` não está configurada.

## Decisões de projeto

- **Um projeto só, com as camadas em pastas.** O domínio tem quatro entidades e um fluxo curto, então separar em vários projetos acrescentaria manutenção sem ganho. A separação de responsabilidades continua nas pastas e nas dependências.
- **SQLite.** É um arquivo, sem servidor separado, o que mantém a hospedagem mais barata. O volume persistente é obrigatório em produção.
- **Regras de negócio puras.** `MatchingService` e `EscalationPauseService` não acessam banco nem rede e recebem tudo por parâmetro (inclusive a hora atual), então os testes não precisam de mock nem dependem do relógio.
- **Interface só onde há I/O.** O notificador do Twilio fica atrás de `IWhatsAppNotifier`, para os testes não enviarem mensagens de verdade.
- **Segredos fora do código.** Tokens e números vêm de variáveis de ambiente ou do User Secrets, nunca do repositório.

## Testes

```bash
dotnet test Chatbot.Tests
```

Os testes cobrem a comparação de texto (acentos, maiúsculas e frase com texto ao redor) e a regra de pausa do bot (escalação aberta, antiga, já atendida, com gatilho e lista vazia). Rodar `dotnet test` na raiz do repositório não executa os testes, porque a raiz tem só o projeto da API.