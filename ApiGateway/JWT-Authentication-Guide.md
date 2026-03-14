# JWT Authentication Guide cho API Gateway

## Tổng quan
Hệ thống đã được cấu hình với JWT Authentication bao gồm:
- **AuthService** (port 5060): Service riêng để generate JWT tokens
- **API Gateway** (port 5050): Validate JWT tokens và route requests
- **Microservices**: Được bảo vệ bởi JWT authentication thông qua API Gateway

## Kiến trúc

```
Client → API Gateway (JWT Validation) → Microservices
         ↓
    AuthService (JWT Generation)
```

## Cách chạy hệ thống

### 1. Chạy bằng Docker Compose
```bash
docker-compose up --build
```

### 2. Chạy từng service riêng (Development)
```bash
# Terminal 1: AuthService
cd AuthService
dotnet run

# Terminal 2: API Gateway  
cd ApiGateway
dotnet run

# Terminal 3: PricingService
cd PricingService
dotnet run

# Terminal 4: PolicyService
cd PolicyService
dotnet run
```

## Cách test JWT Authentication

### Bước 1: Lấy JWT Token

#### Option 1: Generate token cho testing
```http
POST http://localhost:5050/auth/generate-token
Content-Type: application/json

{
  "username": "testuser"
}
```

#### Option 2: Login với credentials
```http
POST http://localhost:5050/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password"
}
```

**Response sẽ có dạng:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "testuser",
  "expiresAt": "2024-03-13T10:30:00Z"
}
```

### Bước 2: Test endpoint không có token (sẽ lỗi 401)
```http
GET http://localhost:5050/pricing
```

**Expected Response:** `401 Unauthorized`

### Bước 3: Test endpoint có token
```http
GET http://localhost:5050/pricing
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

**Expected Response:** `200 OK` với data từ PricingService

### Bước 4: Test các endpoint khác
```http
# GET by ID
GET http://localhost:5050/pricing/123
Authorization: Bearer YOUR_JWT_TOKEN_HERE

# POST
POST http://localhost:5050/pricing
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "basePrice": 1000,
  "quantity": 5,
  "taxRate": 0.1
}

# PUT
PUT http://localhost:5050/pricing/123
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "oldPrice": 1500,
  "newPrice": 1800
}

# DELETE
DELETE http://localhost:5050/pricing/123
Authorization: Bearer YOUR_JWT_TOKEN_HERE

# Policy Service
GET http://localhost:5050/policy
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## Cấu hình JWT

### Secret Key
- **Key**: `MySecretKeyForJWTAuthentication123456789`
- **Issuer**: `AuthService`
- **Audience**: `ApiGatewayUsers`
- **Expiration**: 60 phút

### Routes được bảo vệ
Tất cả routes đến microservices đều yêu cầu JWT token:
- `/pricing` (GET, POST)
- `/pricing/{id}` (GET, PUT, DELETE)
- `/policy` (GET)

### Routes công khai
- `/auth/login` - Login với credentials
- `/auth/generate-token` - Generate token cho testing

## Troubleshooting

### 1. Lỗi 401 Unauthorized
- Kiểm tra token có được gửi trong header `Authorization: Bearer <token>`
- Kiểm tra token chưa hết hạn (60 phút)
- Kiểm tra format token đúng

### 2. Lỗi 404 Not Found
- Kiểm tra AuthService đang chạy trên port 5060
- Kiểm tra API Gateway đang chạy trên port 5050
- Kiểm tra routes trong ocelot.json

### 3. Lỗi 500 Internal Server Error
- Kiểm tra logs của các services
- Kiểm tra Eureka Server đang chạy
- Kiểm tra network connectivity giữa các services

## Test với curl

```bash
# 1. Lấy token
TOKEN=$(curl -s -X POST http://localhost:5050/auth/generate-token \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser"}' | jq -r '.token')

# 2. Test endpoint với token
curl -H "Authorization: Bearer $TOKEN" http://localhost:5050/pricing

# 3. Test POST với token
curl -X POST http://localhost:5050/pricing \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"basePrice":1000,"quantity":5,"taxRate":0.1}'
```

## Ports Summary
- **AuthService**: http://localhost:5060
- **API Gateway**: http://localhost:5050  
- **PricingService**: http://localhost:5001
- **PolicyService**: http://localhost:5002
- **Eureka Server**: http://localhost:8761