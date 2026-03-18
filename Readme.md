# KTTKPM - Microservices Project

## 📁 Cấu trúc dự án:

- **ApiGateway** - API Gateway với JWT, Rate Limiting, Redis Caching
- **AuthService** - Service xác thực JWT
- **PaymentService** - Service thanh toán với PostgreSQL + Marten
- **PricingService** - Service tính giá
- **PolicyService** - Service chính sách

## 🚀 Cách chạy PaymentService:

### 1. Chạy service:
```bash
cd PaymentService
dotnet run
```

### 2. Test API:
Sử dụng file `PaymentService/test-phan-11.http` hoặc:
```bash
curl -X POST http://localhost:5160/api/account \
  -H "Content-Type: application/json" \
  -d '{"policyNumber":"POL001","policyAccountNumber":"ACC001","balance":1000}'
```

### 3. Kiểm tra PostgreSQL:
```bash
psql -h localhost -U postgres -d lab_netmicro_payments
\dt
SELECT * FROM mt_doc_policyaccount;
```

## 📋 Hướng dẫn chi tiết:

- **PaymentService**: Xem `HOW-TO-TEST-PAYMENT-SERVICE.md`
- **Database Issues**: Xem `GIAI-PHAP-TABLES-HOAN-CHINH.md`
- **JWT Testing**: Xem `SIMPLE-JWT-TEST.md`

## 🧪 Test Scripts còn lại:

- `start-redis.sh` - Khởi động Redis
- `test-jwt-flow.sh` - Test JWT flow
- `test-rate-limiting.sh` - Test rate limiting
- `test-redis-caching.sh` - Test Redis caching

## 🐳 Docker Services:

Sau khi eureka chạy ta bắt buộc phải restart cả 2 để nó gọi:
```bash
docker restart pricing-service policy-service api-gateway
```
docker compose down -v
docker compose up --build