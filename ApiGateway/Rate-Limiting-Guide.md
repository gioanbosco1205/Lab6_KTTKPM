# Rate Limiting Configuration Guide

## Tổng quan
API Gateway đã được cấu hình với rate limiting khác nhau cho từng route để bảo vệ hệ thống khỏi abuse và đảm bảo fair usage.

## Rate Limiting Rules

### 1. Authentication Routes
- **Endpoint**: `/auth/*`
- **Rate Limit**: 10 requests/phút
- **Lý do**: Giới hạn việc tạo token để tránh abuse

### 2. Pricing Service Routes

#### GET /pricing
- **Rate Limit**: 5 requests/giây
- **Lý do**: Endpoint được truy cập nhiều, cho phép tần suất cao

#### GET /pricing/{id}
- **Rate Limit**: 3 requests/giây  
- **Lý do**: Ít được sử dụng hơn, giới hạn thấp hơn

#### POST /pricing
- **Rate Limit**: 20 requests/phút
- **Lý do**: Tạo pricing mới, cho phép nhiều requests

#### PUT /pricing/{id}
- **Rate Limit**: 15 requests/phút
- **Lý do**: Cập nhật pricing, ít hơn POST

#### DELETE /pricing/{id}
- **Rate Limit**: 5 requests/phút
- **Lý do**: Xóa pricing, rất nghiêm ngặt

### 3. Policy Service Routes

#### GET /policy
- **Rate Limit**: 2 requests/giây
- **Lý do**: Endpoint ít được sử dụng, giới hạn thấp

## Configuration Details

### Client Identification
Rate limiting sử dụng `ClientId` header để identify clients:
```http
ClientId: your-client-id
```

### Response khi vượt limit
- **Status Code**: 429 Too Many Requests
- **Message**: "Rate limit exceeded. Please try again later."
- **Headers**: Bao gồm rate limit information

### Rate Limit Headers
API Gateway trả về các headers:
- `X-RateLimit-Limit`: Số requests tối đa
- `X-RateLimit-Remaining`: Số requests còn lại
- `X-RateLimit-Reset`: Thời gian reset (Unix timestamp)

## Testing Rate Limiting

### Cách 1: Sử dụng HTTP file
1. Mở file `test-rate-limiting.http`
2. Lấy JWT token từ bước 1.1
3. Thay `YOUR_JWT_TOKEN` trong các requests
4. Chạy các requests liên tiếp để test rate limiting

### Cách 2: Sử dụng script tự động
```bash
./test-rate-limiting.sh
```

### Cách 3: Test thủ công với curl
```bash
# Lấy token
TOKEN=$(curl -s -X POST http://localhost:5050/auth/generate-token \
  -H "Content-Type: application/json" \
  -H "ClientId: test-client" \
  -d '{"username":"testuser"}' | jq -r '.token')

# Test rate limiting - chạy 6 lần liên tiếp (limit là 5/giây)
for i in {1..6}; do
  echo "Request $i:"
  curl -s -w "Status: %{http_code}\n" \
    -H "Authorization: Bearer $TOKEN" \
    -H "ClientId: test-client" \
    http://localhost:5050/pricing
  sleep 0.1
done
```

## Rate Limiting Strategy

### Tiêu chí thiết kế rate limits:

1. **Authentication endpoints**: Thấp để tránh brute force
2. **Read operations**: Cao hơn write operations
3. **Critical operations** (DELETE): Rất thấp
4. **Bulk operations**: Trung bình
5. **Public endpoints**: Thấp hơn authenticated endpoints

### Best Practices:

1. **Sử dụng ClientId** để identify clients
2. **Monitor rate limit usage** qua logs
3. **Adjust limits** dựa trên usage patterns
4. **Implement retry logic** ở client side
5. **Cache responses** để giảm API calls

## Troubleshooting

### Lỗi 429 Too Many Requests
```json
{
  "message": "Rate limit exceeded. Please try again later."
}
```

**Giải pháp**:
- Đợi cho đến khi rate limit reset
- Implement exponential backoff
- Sử dụng ClientId khác nhau cho các clients
- Cache responses để giảm API calls

### Rate limiting không hoạt động
1. Kiểm tra `EnableRateLimiting: true`
2. Kiểm tra `ClientId` header có được gửi không
3. Kiểm tra CacheManager đã được cấu hình
4. Restart API Gateway sau khi thay đổi config

### Headers không xuất hiện
Kiểm tra `DisableRateLimitHeaders: false` trong GlobalConfiguration

## Configuration Example

```json
{
  "RateLimitOptions": {
    "EnableRateLimiting": true,
    "Period": "1s",
    "Limit": 5,
    "PeriodTimespan": 1
  }
}
```

### Parameters:
- **EnableRateLimiting**: Bật/tắt rate limiting
- **Period**: Khoảng thời gian (1s, 1m, 1h)
- **Limit**: Số requests tối đa trong period
- **PeriodTimespan**: Thời gian tính bằng giây

## Monitoring

### Logs to watch:
- Rate limit exceeded events
- Client identification failures
- Configuration errors

### Metrics to track:
- Requests per endpoint
- Rate limit hit rate
- Client usage patterns
- Response times under load

## Production Considerations

1. **Database storage**: Sử dụng Redis cho distributed rate limiting
2. **Client identification**: Sử dụng API keys thay vì simple ClientId
3. **Dynamic limits**: Adjust limits based on client tier
4. **Monitoring**: Set up alerts for high rate limit usage
5. **Documentation**: Provide clear rate limit info to API consumers