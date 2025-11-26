# Migrations (Entity Framework Core)

## Comandos úteis

- Criar uma migration:
  ```bash
  dotnet ef migrations add InitialCreate -p Order.Api -s Order.Api --output-dir Migrations
  ```

- Aplicar migrations (local/containers):
  ```bash
  dotnet ef database update -p Order.Api -s Order.Api
  ```

- Forçar migrações no container (exec):
  ```bash
  docker-compose exec api dotnet ef database update -p Order.Api -s Order.Api
  ```