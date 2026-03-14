# HƯỚNG DẪN TEST PAYMENTSERVICE

## 🚀 CÁCH CHẠY SERVICE

### 1. Chạy PaymentService:
```bash
cd PaymentService
dotnet run
```

Service sẽ chạy tại: `http://localhost:5160`

## 🧪 CÁCH TEST API

### 2. Sử dụng file HTTP (Khuyến nghị):
Mở file `PaymentService/test-phan-11.http` trong VS Code và chạy từng request.

### 3. Hoặc sử dụng curl:

#### Tạo Policy Account:
```bash
curl -X POST http://localhost:5160/api/account \
  -H "Content-Type: application/json" \
  -d '{"policyNumber":"POL001","policyAccountNumber":"ACC001","balance":1000}'
```

#### Lấy Policy Account:
```bash
curl http://localhost:5160/api/account/POL001
```

#### Kiểm tra database connection:
```bash
curl http://localhost:5160/test-db
```

#### Kiểm tra tables đã được tạo:
```bash
curl http://localhost:5160/check-tables
```

## 🗄️ KIỂM TRA POSTGRESQL

### 4. Kết nối database:
```bash
psql -h localhost -U postgres -d lab_netmicro_payments
```

### 5. Xem tables:
```sql
\dt
```

### 6. Xem dữ liệu:
```sql
SELECT 
    data->>'policyNumber' as policy_number,
    data->>'policyAccountNumber' as account_number,
    (data->>'balance')::decimal as balance
FROM mt_doc_policyaccount;
```

## ⚠️ LƯU Ý QUAN TRỌNG

- **Tables chỉ được tạo sau khi có dữ liệu đầu tiên**
- **Table name**: `mt_doc_policyaccount` (Marten tự động thêm prefix)
- **Dữ liệu lưu dạng JSON** trong column `data`

## 📁 FILES QUAN TRỌNG

- `PaymentService/test-phan-11.http` - File test HTTP
- `PaymentService/PHAN-11-TEST-GUIDE.md` - Hướng dẫn chi tiết
- `PaymentService/KHAC-PHUC-TABLES.md` - Khắc phục vấn đề tables
- `GIAI-PHAP-TABLES-HOAN-CHINH.md` - Giải pháp hoàn chỉnh

## 🎯 CHECKLIST TEST

- [ ] Service chạy thành công
- [ ] Tạo được account qua API
- [ ] Lấy được account theo policy number
- [ ] Tables xuất hiện trong PostgreSQL
- [ ] Dữ liệu được lưu đúng

## 🚨 TROUBLESHOOTING

Nếu gặp vấn đề "Did not find any tables":
1. Tạo ít nhất 1 account qua API
2. Kiểm tra lại bằng `\dt` trong PostgreSQL
3. Xem file `GIAI-PHAP-TABLES-HOAN-CHINH.md` để biết chi tiết