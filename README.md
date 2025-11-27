# TingoAI Payment Gateway

A high-performance, globally distributed payment gateway built with .NET 9 and Domain-Driven Design (DDD) Clean Architecture principles. Integrates with GlobalPay (Zenith) API to process payments across multiple currencies (NGN, USD, EUR, GBP).

## 🏗️ Architecture

The solution follows **Domain-Driven Design (DDD) Clean Architecture** with clear separation of concerns:

```
├── Domain Layer          - Core business entities and domain logic (no dependencies)
├── Application Layer     - Use cases, interfaces, DTOs (depends only on Domain)
├── Infrastructure Layer  - External concerns (DB, APIs, Redis, Security)
└── API Layer            - Controllers, Middleware (depends on Application & Infrastructure)
```

## ✨ Features

- ⚡ **Lightning-fast responses** (< 100ms) with asynchronous transaction recording
- 🌍 **Multi-region deployment** (UK, US, NG) with AWS Application Load Balancer
- 🛡️ **IP-based rate limiting** with Redis (configurable, default 100 req/min)
- 💾 **PostgreSQL database** with optimized indexing and connection pooling
- 🚀 **No user registration required** - instant checkout
- 📊 **Real-time transaction tracking** and analytics
- 🔒 **AES webhook decryption** for secure GlobalPay notifications
- 📝 **Structured logging** with Serilog and correlation IDs
- 🐳 **Docker containerization** for easy deployment
- ☁️ **AWS-ready** with health checks and auto-scaling support

## 🚀 Quick Start

### Prerequisites

- .NET 9 SDK
- PostgreSQL 16+
- Redis 7+
- Docker (optional)

### Local Development

1. **Clone the repository**
   ```bash
   cd c:\Users\Josiah.Obaje\.gemini\antigravity\playground\tensor-kuiper
   ```

2. **Update configuration**
   
   Edit `src/TingoAI.PaymentGateway.API/appsettings.json`:
   ```json
   {
     "GlobalPay": {
       "PublicKey": "YOUR_GLOBALPAY_PUBLIC_KEY",
       "SecretKey": "YOUR_GLOBALPAY_SECRET_KEY"
     },
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=TingoAIPayments;Username=postgres;Password=yourpassword"
     }
   }
   ```

3. **Run with Docker Compose** (Recommended)
   ```bash
   docker-compose up -d
   ```
   
   The API will be available at `http://localhost:5000`

4. **Or run manually**
   ```bash
   # Start PostgreSQL and Redis
   # Then run:
   dotnet run --project src/TingoAI.PaymentGateway.API/TingoAI.PaymentGateway.API.csproj
   ```

5. **Access Swagger UI**
   
   Navigate to `http://localhost:5000/swagger`

## 📡 API Endpoints

### Payment Endpoints

- **POST** `/api/payment/initiate` - Generate payment link
  ```json
  {
    "amount": 1000.00,
    "currency": "NGN",
    "customerFirstName": "John",
    "customerLastName": "Doe",
    "customerEmail": "john@example.com",
    "customerPhone": "08012345678",
    "customerAddress": "123 Main St"
  }
  ```

- **GET** `/api/payment/verify/{reference}` - Verify transaction status
- **POST** `/api/payment/webhook` - Handle GlobalPay webhooks

### Transaction Endpoints

- **GET** `/api/transaction` - Get all transactions (paginated)
- **GET** `/api/transaction/{id}` - Get transaction by ID
- **GET** `/api/transaction/summary` - Get transaction analytics

### Health Check Endpoints

- **GET** `/health` - Health check for load balancer
- **GET** `/health/ready` - Readiness probe
- **GET** `/health/live` - Liveness probe

## 🌍 Multi-Region Deployment

The application supports deployment across multiple AWS regions:

### Region Configuration

Each region has its own configuration file:
- `appsettings.UK.json` - UK region
- `appsettings.US.json` - US region
- `appsettings.NG.json` - NG region

### AWS Deployment

1. **Build Docker image**
   ```bash
   docker build -t tingoai-payment-gateway .
   ```

2. **Push to AWS ECR**
   ```bash
   aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
   docker tag tingoai-payment-gateway:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/tingoai-payment-gateway:latest
   docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/tingoai-payment-gateway:latest
   ```

3. **Deploy to ECS**
   - Create ECS clusters in UK, US, and NG regions
   - Configure Application Load Balancer
   - Set up Auto Scaling groups
   - Configure RDS PostgreSQL with read replicas
   - Set up ElastiCache for Redis

## 🔧 Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Environment (Development, Staging, Production)
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `Redis__ConnectionString` - Redis connection string
- `GlobalPay__PublicKey` - GlobalPay public API key
- `GlobalPay__SecretKey` - GlobalPay secret key
- `RateLimit__RequestsPerMinute` - Rate limit threshold (default: 100)
- `RateLimit__EnableRateLimiting` - Enable/disable rate limiting (default: true)

## 📊 Database Migrations

The application automatically runs migrations on startup. To create new migrations:

```bash
dotnet ef migrations add MigrationName --project src/TingoAI.PaymentGateway.Infrastructure --startup-project src/TingoAI.PaymentGateway.API
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 📝 Logging

Logs are written to:
- Console (structured JSON in production)
- File: `logs/tingoai-payment-gateway-{Date}.txt`
- AWS CloudWatch (when deployed to AWS)

Each request includes a correlation ID for tracing.

## 🔒 Security

- **Rate Limiting**: IP-based with Redis (distributed across instances)
- **HTTPS**: Enforced in production
- **Webhook Encryption**: AES decryption for GlobalPay webhooks
- **Input Validation**: Model validation on all endpoints
- **Error Handling**: Global exception handler with safe error messages

## 🚦 Rate Limiting

Default: 100 requests per minute per IP address

To whitelist IPs, add to `appsettings.json`:
```json
{
  "RateLimit": {
    "WhitelistedIPs": ["192.168.1.1", "10.0.0.1"]
  }
}
```

## 📈 Performance

- **Response Time**: < 100ms for payment initiation
- **Throughput**: 1000+ concurrent requests
- **Database**: Optimized indexes on all query fields
- **Caching**: Redis for rate limiting and future query caching
- **Connection Pooling**: Configured for PostgreSQL

## 🛠️ Technology Stack

- **.NET 9** - Framework
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM
- **PostgreSQL** - Database
- **Redis** - Caching & Rate Limiting
- **Serilog** - Logging
- **Docker** - Containerization
- **AWS ECS** - Container Orchestration
- **AWS RDS** - Managed PostgreSQL
- **AWS ElastiCache** - Managed Redis
- **AWS ALB** - Load Balancing

## 📄 License

Proprietary - TingoAI

## 👥 Support

For support, contact the TingoAI development team.
