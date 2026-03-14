# Hướng dẫn Test JWT từng bước

## Trước khi test, đảm bảo các services đang chạy:

### Kiểm tra services:
```bash
# AuthService (port 5060)
curl http://localhost:5060/api/auth/generate-token

# API Gateway (port 5050)  
curl http://localhost:5050/auth/generate-token

# PricingService (qua API Gateway)
curl http://localhost:5050/pricing
```

## BƯỚC 1: Test AuthService trực tiếp

### 1.1. Mở Terminal và chạy:
```bash
curl -X POST http://localhost:5060/api/auth/generate-token \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser"}'
```

**Kết quả mong đợi:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "testuser",
  "expiresAt": "2026-03-14T03:04:21Z"
}
```

### 1.2. Test login:
```bash
curl -X POST http://localhost:5060/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password"}'
```

## BƯỚC 2: Test qua API Gateway

### 2.1. Generate token qua Gateway:
```bash
curl -X POST http://localhost:5050/auth/generate-token \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser"}'
```

### 2.2. Login qua Gateway:
```bash
curl -X POST http://localhost:5050/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password"}'
```

## BƯỚC 3: Test endpoint không có token

```bash
curl -v http://localhost:5050/pricing
```

**Kết quả mong đợi:** `401 Unauthorized`

## BƯỚC 4: Test endpoint có token

### 4.1. Lấy token từ bước 2.1, sau đó:
```bash
# Thay YOUR_TOKEN bằng token thực
curl -H "Authorization: Bearer YOUR_TOKEN" http://localhost:5050/pricing
```

**Kết quả mong đợi:** 
- Nếu có Eureka: `200 OK` với data
- Nếu không có Eureka: `404 Not Found` (nhưng không phải 401)

## BƯỚC 5: Test với Postman/VS Code

### Nếu dùng VS Code REST Client:

1. Mở file `SIMPLE-TEST.http`
2. Click vào "Send Request" ở trên mỗi request
3. Xem kết quả ở panel bên phải

### Nếu dùng Postman:

1. **POST** `http://localhost:5060/api/auth/generate-token`
   - Headers: `Content-Type: application/json`
   - Body (raw JSON): `{"username":"testuser"}`

2. **GET** `http://localhost:5050/pricing`
   - Không có headers → Expect 401

3. **GET** `http://localhost:5050/pricing`  
   - Headers: `Authorization: Bearer <token_from_step_1>`

## Troubleshooting

### Lỗi "Connection refused":
```bash
# Kiểm tra service có chạy không
ps aux | grep dotnet

# Start AuthService
cd AuthService && dotnet run

# Start API Gateway  
cd ApiGateway && dotnet run
```

### Lỗi "404 Not Found" khi test AuthService:
- Kiểm tra URL đúng: `http://localhost:5060/api/auth/generate-token`
- Kiểm tra AuthService có chạy trên port 5060

### Lỗi "401 Unauthorized" khi có token:
- Kiểm tra token có đúng format không
- Kiểm tra header: `Authorization: Bearer <token>`
- Kiểm tra token chưa hết hạn (60 phút)

### Test nhanh bằng một lệnh:
```bash
# Lấy token và test luôn
TOKEN=$(curl -s -X POST http://localhost:5050/auth/generate-token \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

echo "Token: $TOKEN"

curl -H "Authorization: Bearer $TOKEN" http://localhost:5050/pricing
```