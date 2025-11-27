# README – Pedido Management (API + Worker + Frontend + Infra Completa)


Este projeto implementa um sistema de gestão de pedidos com:

- Backend em .NET 7 + Entity Framework + PostgreSQL

- Mensageria via Azure Service Bus

- Worker com idempotência + processamento assíncrono

- Outbox Pattern

- SignalR para atualização em tempo real do frontend

- Frontend em React + TailwindCSS

- Infraestrutura com Docker Compose

- Testes unitários e de integração (Testcontainers)

## 1. Requisitos Obrigatórios

- Docker + Docker Compose
- .NET 7+
- Node.js 18+
- Conta Azure com Service Bus (opcional em dev)

Obs: Se não tiver, o Outbox armazenará eventos sem publicar — o sistema continuará funcionando.

## 2. Configuração de Variáveis de Ambiente

Copie .env.example para .env e preencha as variáveis.

## 3. Subindo com Docker Compose

```bash
docker compose up -d --build
```

## 4. Acessar o frontend

http://localhost:3000

## 5. Testar endpoints

Criar pedido:
```bash
POST /orders
{
  "cliente": "Jhon Due",
  "produto": "Notebook",
  "valor": 3500.00
}
```