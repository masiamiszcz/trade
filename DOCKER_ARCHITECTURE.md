# Trading Platform - Docker Architecture

## ✅ Aktualna Architektura (Produkcyjna)

### Topologia Sieci

```
┌─────────────────────────────────────────────────────────────┐
│                         INTERNET (Port 80)                   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
        ┌────────────────────────────┐
        │    Nginx (Reverse Proxy)   │
        │  - Port 80 EXPOSED         │
        │  - All traffic routing     │
        └────┬──────────────┬────────┘
             │              │
      ┌──────▼──────┐  ┌───▼─────────┐
      │             │  │             │
  (backend-nginx)  (frontend-nginx)
      │             │  │             │
      ▼             │  ▼             │
  ┌─────────────┐   │ ┌──────────┐   │
  │  Backend    │   │ │ Frontend │   │
  │  .NET API   │   │ │  React   │   │
  │  5001/tcp   │   │ │ 80/tcp   │   │
  └──────┬──────┘   │ └──────────┘   │
         │          └────────────────┘
    (backend-sql)
         │
         ▼
    ┌─────────────┐
    │  SQL Server │
    │  2022       │
    │  1433/tcp   │
    │  (internal) │
    └─────────────┘
```

### Trzy Niezależne Sieci (Security Isolation)

| Sieć | Kontenery | Cel | Subnet |
|------|-----------|-----|--------|
| **frontend-nginx** | frontend, nginx | Frontend ↔ Nginx | 172.20.0.0/16 |
| **backend-nginx** | backend, nginx | Backend ↔ Nginx | 172.21.0.0/16 |
| **backend-sql** | backend, sqlserver | Backend ↔ Database | 172.22.0.0/16 |

### Bezpieczeństwo (Isolation Rules)

```
FRONTEND ─── (frontend-nginx network) ─── NGINX
            ❌ No direct access to backend
            ❌ No direct access to SQL

BACKEND ─── (backend-nginx network) ─── NGINX
         ─── (backend-sql network) ─── SQL SERVER
         ❌ No direct access to frontend
         ❌ Frontend cannot reach backend directly

SQL SERVER ─── (backend-sql network only) ─── BACKEND
            ❌ No exposure to frontend
            ❌ No exposure to external network
            ❌ Internal-only access
```

## Routing w Nginx

### Konfiguracja `/api/*`
```nginx
location /api/ {
    proxy_pass http://backend_api;
    # Headers dla client info (X-Real-IP, X-Forwarded-For)
    # WebSocket support
    # Rate limiting: 10 r/s
}
```

### Konfiguracja `/health`
```nginx
location /health {
    proxy_pass http://backend_api/health;
    # No logging
    # 5s timeout
}
```

### Konfiguracja `/` (Frontend SPA)
```nginx
location / {
    proxy_pass http://frontend_app;
    # React Router support (try_files /index.html)
    # Rate limiting: 30 r/s
}
```

### Konfiguracja Static Assets
```nginx
location ~* \.(js|css|png|jpg|...)$ {
    proxy_pass http://frontend_app;
    expires 7d;
    Cache-Control: public, immutable
}
```

## Ports Exposure

| Port | Protokół | Dostęp | Serwis |
|------|----------|--------|--------|
| **80** | HTTP | 🌐 PUBLIC | Nginx |
| 5001 | HTTP | 🔒 Internal | Backend (backend-nginx) |
| 1433 | MSSQL | 🔒 Internal | SQL Server (backend-sql) |
| 80 | HTTP | 🔒 Internal | Frontend (frontend-nginx) |

## Docker Compose Files

### `docker-compose.yaml`
- **Główna konfiguracja dla development/production**
- 4 serwisy: sqlserver, backend, frontend, nginx
- 3 niezależne sieci
- Health checks dla bazy danych
- Logging JSON z limitami

### `docker-compose.prod.yml`
- **Produkcyjne overriday**
- Zwiększone limity logów (10MB max)
- Backup volumes dla SQL
- SSL certificates support
- Nginx cache volume

## Migracje i Inicjalizacja

### Przy starcie Backend:
1. Łączy się z SQL Server (czeka na health check)
2. Automatycznie stosuje migracje EF Core
3. Inicjalizuje nowe bazy danych
4. Uruchamia aplikację

### Migracje:
```bash
dotnet ef migrations add <MigrationName> \
  --project TradingPlatform.Data \
  --startup-project TradingPlatform.Api
```

## Testowanie Architekury

### ✅ Frontend dostępny:
```powershell
Invoke-WebRequest http://localhost/ -UseBasicParsing
# Odpowiedź: HTML strony React
```

### ✅ API dostępny:
```powershell
Invoke-WebRequest http://localhost/health -UseBasicParsing
# Odpowiedź: 200 OK - "Healthy"
```

### ✅ Sieciowa izolacja:
```bash
# Frontend NIE ma dostępu do backend
docker exec trading-frontend curl http://trading-backend:5001/health
# ❌ Błąd - brak sieciowej ścieżki

# Backend może się łączyć z SQL
docker exec trading-backend sqlcmd -S trading-sql -U sa -P YourStrong!Passw0rd
# ✅ Pracuje
```

## Production Deployment

### Uruchomienie:
```bash
cd docker
docker compose -f docker-compose.yaml -f docker-compose.prod.yml up -d
```

### Environment Variables:
```bash
# .env file
ASPNETCORE_ENVIRONMENT=Production
Jwt__Key=<SecureKeyMin64Chars>
ConnectionStrings__DefaultConnection=Server=sqlserver;...
```

## Monitorowanie

### Logs:
```bash
docker compose logs -f trading-backend
docker compose logs -f trading-nginx
```

### Status:
```bash
docker compose ps
```

### Networking:
```bash
docker network ls
docker network inspect docker_backend-nginx
```

## Security Headers (Nginx)

```
X-Frame-Options: SAMEORIGIN
X-XSS-Protection: 1; mode=block
X-Content-Type-Options: nosniff
Strict-Transport-Security: max-age=31536000
Content-Security-Policy: default-src 'self'
Referrer-Policy: strict-origin-when-cross-origin
```

## Performance Optimizations

- **Gzip compression** dla text/json/javascript
- **Connection pooling** dla SQL Server
- **Rate limiting**: API (10 r/s), General (30 r/s)
- **Static asset caching**: 7 dni
- **Request buffering** w nginx
- **Health checks** z retry logic

## ⚠️ Ważne Uwagi

1. **Brak portów ekspozycji**: Tylko port 80 (HTTP) jest dostępny publicznie
2. **Sieci izolowane**: Frontend nie ma dostępu do SQL ani Backend
3. **Automatyczne migracje**: Nie potrzeba ręcznego `dotnet ef database update`
4. **Health checks**: SQL Server czeka na health check, Backend czeka na SQL
5. **Logging**: JSON driver z limitami 10MB/3 pliki

## Future Enhancements

- [ ] SSL/TLS certificates (docker-compose.prod.yml support)
- [ ] Failover redis cache
- [ ] Message queue (RabbitMQ)
- [ ] Monitoring stack (Prometheus + Grafana)
- [ ] Application load balancing
