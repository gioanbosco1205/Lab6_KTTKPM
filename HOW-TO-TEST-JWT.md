# Hướng dẫn Test JWT Authentication

## Cách 1: Test bằng HTTP file (Postman/VS Code REST Client)

### Bước 1: Mở file `JWT-Authentication-Test.http`
File này chứa tất cả các test cases đã được chuẩn bị sẵn.

### Bước 2: Chạy từng test theo thứ tự

1. **Test AuthService trực tiếp** (Bước 1)
   - Chạy `POST http://localhost:5060/api/auth/generate-token`
   - Kết quả mong đợi: Trả về token

2. **Test qua API Gateway** (Bước 2)
   - Chạy `POST http://localhost:5050/auth/generate-token`
   - **Copy token từ response** để dùng cho các test tiếp theo

3. **Test không có token** (Bước 3)
   - Chạy `GET http://localhost:5050/pricing`
   - Kết quả mong đợi: `401 Unauthorized`

4. **Test có token** (Bước 4)
   - **Thay `YOUR_JWT_TOKEN` bằng token từ bước 2**
   - Chạy `GET http://localhost:5050/pricing` với Authorization header
   - Kết quả mong đợi: `200 OK` với data từ PricingService

## Cách 2: Test bằng Script tự động

### Chạy script test:
```bash
./test-jwt-flow.sh
```

Script sẽ tự động:
- Test AuthService
- Lấy token qua API Gateway
- Test các endpoint với và không có token
- Hiển thị kết quả PASS/FAIL

## Cách 3: Test bằng curl thủ công

### Bước 1: Lấy token
```bash
curl -X POST http://localhost:5050/auth/generate-token \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser"}'
```

### Bước 2: Copy token từ response
Response sẽ có dạng:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "testuser",
  "expiresAt": "2024-03-13T10:30:00Z"
}
```

### Bước 3: Test endpoint với token
```bash
# Thay YOUR_TOKEN bằng token thực
curl -H "Authorization: Bearer YOUR_TOKEN" http://localhost:5050/pricing
```

## Kết quả mong đợi

### ✅ Test thành công khi:
- AuthService trả về token (200 OK)
- API Gateway route đúng đến AuthService
- Endpoint không có token trả về 401 Unauthorized
- Endpoint có token hợp lệ trả về 200 OK với data
- Endpoint có token không hợp lệ trả về 401 Unauthorized

### ❌ Test thất bại khi:
- AuthService không chạy (Connection refused)
- API Gateway không route được đến AuthService (404/500)
- Token không được validate đúng
- Microservices không trả về data

## Troubleshooting

### Lỗi thường gặp:

1. **Connection refused**
   - Kiểm tra services có chạy không
   - Kiểm tra ports: AuthService (5060), API Gateway (5050)

2. **404 Not Found**
   - Kiểm tra routes trong ocelot.json
   - Kiểm tra URL đúng format

3. **401 Unauthorized**
   - Kiểm tra token có được gửi trong header không
   - Kiểm tra token chưa hết hạn (60 phút)
   - Kiểm tra format: `Authorization: Bearer <token>`

4. **Rate limiting error**
   - Kiểm tra `EnableRateLimiting: false` trong ocelot.json
   - Restart API Gateway sau khi thay đổi config

## Ports Summary
- **AuthService**: http://localhost:5060
- **API Gateway**: http://localhost:5050
- **PricingService**: http://localhost:5001 (qua Eureka)
- **PolicyService**: http://localhost:5002 (qua Eureka)