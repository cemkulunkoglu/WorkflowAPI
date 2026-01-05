# WorkflowAPI

WorkflowAPI; akış (workflow) tasarımı, akış node/edge yönetimi ve ilgili süreç/izin/rol gibi işlevleri sağlayan .NET Web API servisidir.

## Teknolojiler
- .NET (Web API)
- Entity Framework Core
- MySQL
- RabbitMQ / SignalR / JWT

## Gereksinimler
- .NET SDK 8
- MySQL Server
- Docker Desktop

## Kurulum

### 1) Projeyi klonla
```bash
git clone <repo-url>
cd WorkflowAPI
```

## Workflow.MessagesService

### Docker ile çalıştırma
```bash
docker compose up -d
```

Servisler:
- Messages API: http://localhost:8085
- Mailpit UI: http://localhost:8025

### Örnek curl istekleri
```bash
curl -X POST http://localhost:8085/api/messages/send \
  -H "Content-Type: application/json" \
  -d '{
    "flowDesignsId": 1,
    "flowNodesId": 10,
    "employeeToId": 100,
    "employeeFromId": 200,
    "emailTo": "to@example.com",
    "emailFrom": "from@example.com",
    "subject": "Merhaba"
  }'
```

```bash
curl "http://localhost:8085/api/messages/outbox?employeeId=200"
```

```bash
curl "http://localhost:8085/api/messages/inbox?employeeId=100"
```
