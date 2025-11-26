# Decisões Técnicas do Projeto de Gestão de Pedidos

Este documento descreve as principais decisões técnicas adotadas no desenvolvimento do sistema completo de **Gestão de Pedidos com Processamento Assíncrono**, seguindo os requisitos do desafio e boas práticas de arquitetura, escalabilidade e confiabilidade.

---

## 1. Arquitetura Geral
A arquitetura do sistema foi projetada para suportar **processamento assíncrono**, **escala horizontal**, **observabilidade** e **baixa acoplagem** entre componentes.

### Decisões tomadas:
- A API foi construída em **.NET 7**, com camadas simples e isoladas.
- O processamento assíncrono foi delegado a um **Worker Service** isolado.
- O transporte de eventos foi feito via **Azure Service Bus**.
- O frontend usa **React + TailwindCSS**.
- A infraestrutura é orquestrada via **Docker Compose**.

---

## 2. Persistência & Mapeamento
### Banco de dados:
- Escolhido o **PostgreSQL**, combinando estabilidade, aderência ao EF Core e facilidade de containerização.

### Decisões de modelagem:
- Entidade `Order` modelada com estados controlados: `Pendente → Processando → Finalizado`.
- A relação de histórico de status foi adicionada como recurso opcional de auditoria.
- Implementado `OutboxMessage` como tabela transacional para mensageria.

---

## 3. Mensageria & Outbox Pattern
### Problema resolvido:
Evitar perda de mensagens e inconsistência entre “escrevi no banco mas não publiquei a mensagem”.

### Decisões:
- Implementado **Outbox Pattern** com polling pelo `OutboxDispatcher`.
- O Worker sempre processa eventos idempotentemente.
- Eventos usam `CorrelationId = OrderId`.
- `EventType = OrderCreated` foi padronizado.
- Mensagens são movidas para DLQ se excederem a política de retry.

---

## 4. Processamento Assíncrono (Worker)
### Decisões principais:
- Worker implementado com `BackgroundService`.
- Após consumir a mensagem:
  1. Status muda para **Processando**.
  2. Aguardar 5 segundos.
  3. Mudar para **Finalizado**.
- O Worker notifica a API (via endpoint interno) para atualizar o frontend.

---

## 5. Comunicação Tempo Real (SignalR)
### Motivo:
Permitir que o frontend veja o status do pedido mudando sem refresh.

### Decisões:
- Criado `OrdersHub` na API.
- Worker envia atualizações internas para o hub via endpoint protegido.
- Frontend usa **@microsoft/signalr** para escutar eventos.
- Fallback para polling está disponível caso WebSockets não esteja disponível.

---

## 6. Testes Automatizados
### Decisões:
- Criados **Testes de Integração** com **Testcontainers**.
- Os testes sobem containers reais de PostgreSQL.
- A API é testada com `WebApplicationFactory` + banco limpo a cada método.
- Cobriram criação de pedidos e transições de status via Worker simulado.

---

## 8. Infraestrutura & Deploy
### Docker Compose:
- Contém: API, Worker, Frontend, PostgreSQL, PgAdmin.
- Healthchecks foram configurados para API, banco e Mensageria.

### Outras decisões:
- `.env` controla strings sensíveis.
- Migrations rodadas automaticamente no startup.
- SQL fallback (`init_schema.sql`) adicionado para ambientes sem EF CLI.

---

## 9. CORS & Segurança
### Decisões:
- CORS configurado para permitir apenas a origem do FRONTEND declarada em `.env`.
- Endpoint interno usado pelo Worker para notificação é protegido por token.
- Variáveis sensíveis são injetadas via env, não hardcoded.

---

## 10. Observabilidade & Health Checks
### Decisões:
- Health checks implementados para:
  - API
  - Banco de dados
  - Service Bus
  - Worker (via compose healthcheck)
- Endpoint `/health` expõe resultados.
- Útil para Kubernetes, Docker, pipelines CI.

---

## 11. Critérios de Qualidade Aplicados
- Adoção de padrões DDD light.
- Baixo acoplamento API ↔ Worker.
- SOLID aplicado aos serviços principais.
- Tratamento de erros centralizado.
- Logging semântico estruturado.
- Regras de negócio encapsuladas no serviço de domínio.

---
