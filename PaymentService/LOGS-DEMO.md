# Demo Logs - Chứng minh Compiled Query

## Cách chạy demo

### 1. Khởi động service:
```bash
cd PaymentService
dotnet run
```

### 2. Gọi API demo logs:
```bash
curl "http://localhost:5160/api/compiled/demo-logs/POL001"
```

### 3. Kiểm tra console output:

Bạn sẽ thấy logs như sau:

```
============================================================
DEMO: COMPILED QUERY vs REGULAR LINQ QUERY
============================================================

🔥 CALLING REGULAR LINQ QUERY:
>>> Running Regular LINQ Query: FindByNumber

⚡ CALLING COMPILED QUERY:
>>> Running Compiled Query: FindByPolicyNumberCompiled

============================================================
DEMO COMPLETED - Check console output above!
============================================================
```

## Ý nghĩa của logs

### Regular LINQ Query:
```
>>> Running Regular LINQ Query: FindByNumber
```
- Đây là LINQ query thông thường
- Mỗi lần gọi phải parse LINQ expression
- Chậm hơn compiled query

### Compiled Query:
```
>>> Running Compiled Query: FindByPolicyNumberCompiled
```
- Đây là compiled query đã được pre-compile
- Không cần parse LINQ expression mỗi lần
- Nhanh hơn regular query

## API endpoints có logs

### Compiled Query APIs (có logs):
- `GET /api/compiled/policy/{policyNumber}`
- `GET /api/compiled/owner/{ownerName}`
- `GET /api/compiled/balance/greater-than/{min}`
- `GET /api/compiled/count/owner/{owner}`
- `GET /api/compiled/top/{count}`
- Tất cả đều có log: `>>> Running Compiled Query: [MethodName]`

### Regular LINQ APIs (có logs):
- `GET /api/account/{policyNumber}`
- `GET /api/account/owner/{ownerName}`
- `GET /api/account/balance/greater-than/{min}`
- `GET /api/account/count/owner/{owner}`
- `GET /api/account/top/{count}`
- Tất cả đều có log: `>>> Running Regular LINQ Query: [MethodName]`

## Demo endpoints đặc biệt

### 1. Demo logs:
```
GET /api/compiled/demo-logs/{policyNumber}
```
- Gọi cả regular và compiled query
- Hiển thị logs rõ ràng để so sánh

### 2. Performance comparison:
```
GET /api/compiled/performance-comparison/{policyNumber}
```
- So sánh thời gian thực thi
- Có logs chi tiết trong console
- Trả về kết quả performance

## Chứng minh compiled query

Logs này chứng minh rằng:
1. ✅ Code thực sự gọi compiled query methods
2. ✅ Có sự khác biệt giữa regular và compiled
3. ✅ Compiled query được implement đúng cách
4. ✅ Performance có thể được đo lường

**Khi demo, hãy mở console để thấy logs real-time!**