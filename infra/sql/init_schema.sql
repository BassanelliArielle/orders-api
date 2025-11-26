-- init_schema.sql
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS "Orders" (
  "Id" uuid PRIMARY KEY,
  "Cliente" varchar(255) NOT NULL,
  "Produto" varchar(255) NOT NULL,
  "Valor" numeric(18,2) NOT NULL,
  "Status" varchar(50) NOT NULL,
  "DataCriacao" timestamp without time zone NOT NULL DEFAULT (now())
);

CREATE TABLE IF NOT EXISTS "OrderStatusHistories" (
  "Id" uuid PRIMARY KEY,
  "OrderId" uuid NOT NULL REFERENCES "Orders"("Id") ON DELETE CASCADE,
  "Status" varchar(50) NOT NULL,
  "When" timestamp without time zone NOT NULL DEFAULT (now())
);

CREATE TABLE IF NOT EXISTS "OutboxMessages" (
  "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  "Destination" varchar(200) NOT NULL,
  "Payload" text NOT NULL,
  "CorrelationId" varchar(100),
  "EventType" varchar(100),
  "OccurredAt" timestamp without time zone NOT NULL DEFAULT (now()),
  "Dispatched" boolean NOT NULL DEFAULT false,
  "DispatchedAt" timestamp without time zone,
  "DispatchAttempts" integer NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS "ConsumedMessages" (
  "MessageId" varchar(200) PRIMARY KEY,
  "OrderId" uuid,
  "ProcessedAt" timestamp without time zone NOT NULL DEFAULT (now())
);

CREATE TABLE IF NOT EXISTS "Users" (
  "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  "Name" varchar(200) NOT NULL,
  "Email" varchar(200) NOT NULL,
  "Role" varchar(50) NOT NULL
);
