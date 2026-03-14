# GIẢI PHÁP HOÀN CHỈNH CHO VẤN ĐỀ "DID NOT FIND ANY TABLES"

## 🎯 VẤN ĐỀ:
Khi chạy `\dt` trong PostgreSQL, bạn thấy "Did not find any tables" mặc dù API hoạt động bình thường.

## ✅ NGUYÊN NHÂN:
**Marten sử dụng lazy loading** - chỉ tạo tables khi có dữ liệu đầu tiên được lưu.

## 🚀 GIẢI PHÁP ĐƠN GIẢN:

### BƯỚC 1: Chạy PaymentService
```bash
cd PaymentService
dotnet run
```

### BƯỚC 2: Tạo ít nhất 1 account để force tạo table
```bash
curl -X POST http://localhost:5160/api/account \
  -H "Content-Type: application/json" \
  -d '{"policyNumber":"POL001","policyAccountNumber":"ACC001","balance":1000}'
```

### BƯỚC 3: Kiểm tra table đã được tạo
```bash
curl http://localhost:5160/check-tables
```

**Kết quả mong đợi:**
```json
{
  "status": "Success",
  "message": "Table mt_doc_policyaccount exists and accessible",
  "totalRecords": 1,
  "timestamp": "2026-03-14T07:14:39.076545Z"
}
```

### BƯỚC 4: Bây giờ kiểm tra PostgreSQL
```bash
psql -h localhost -U postgres -d lab_netmicro_payments
\dt
```

**Bây giờ bạn sẽ thấy:**
```
                    List of relations
 Schema |         Name          | Type  |  Owner   
--------+-----------------------+-------+----------
 public | mt_doc_policyaccount  | table | postgres
(1 row)
```

### BƯỚC 5: Xem dữ liệu
```sql
SELECT * FROM mt_doc_policyaccount;

-- Hoặc xem đẹp hơn:
SELECT 
    data->>'policyNumber' as policy_number,
    data->>'policyAccountNumber' as account_number,
    (data->>'balance')::decimal as balance
FROM mt_doc_policyaccount;
```

## 🔧 SCRIPTS HỖ TRỢ:

### 1. Script kiểm tra tự động:
```bash
./check-tables.sh
```

### 2. Script khắc phục hoàn chỉnh:
```bash
./fix-database-tables.sh
```

### 3. File test HTTP:
Sử dụng `PaymentService/test-phan-11.http` để test từng bước.

## 📋 API ENDPOINTS HỖ TRỢ:

### Kiểm tra database connection:
```
GET http://localhost:5160/test-db
```

### Kiểm tra tables:
```
GET http://localhost:5160/check-tables
```

### Tạo account:
```
POST http://localhost:5160/api/account
Content-Type: application/json

{
  "policyNumber": "POL001",
  "policyAccountNumber": "ACC001",
  "balance": 1000
}
```

### Lấy account:
```
GET http://localhost:5160/api/account/POL001
```

## ⚠️ LƯU Ý QUAN TRỌNG:

1. **Table name**: Marten tự động thêm prefix `mt_doc_` trước tên class
   - Class: `PolicyAccount` → Table: `mt_doc_policyaccount`

2. **Lazy loading**: Tables chỉ được tạo khi có dữ liệu đầu tiên

3. **JSON storage**: Dữ liệu được lưu dạng JSON trong column `data`

4. **Schema tự động**: Marten tự động tạo schema khi cần thiết

## 🎉 KẾT QUẢ CUỐI CÙNG:

Sau khi làm theo hướng dẫn:
- ✅ Tables được tạo trong PostgreSQL
- ✅ API hoạt động hoàn hảo
- ✅ Dữ liệu được lưu và truy vấn đúng
- ✅ Có thể xem dữ liệu qua SQL

## 🚨 NẾU VẪN KHÔNG HOẠT ĐỘNG:

1. **Kiểm tra PostgreSQL có chạy:**
   ```bash
   brew services list | grep postgresql
   # hoặc
   sudo systemctl status postgresql
   ```

2. **Kiểm tra database có tồn tại:**
   ```bash
   psql -h localhost -U postgres -l | grep lab_netmicro_payments
   ```

3. **Tạo database nếu chưa có:**
   ```bash
   createdb -h localhost -U postgres lab_netmicro_payments
   ```

4. **Kiểm tra connection string trong appsettings.json**

## 📞 SUPPORT:

Nếu vẫn gặp vấn đề, hãy:
1. Chạy `./check-tables.sh` và gửi kết quả
2. Chạy `curl http://localhost:5160/test-db` và gửi response
3. Kiểm tra log của dotnet run

**🎯 Tóm lại: Chỉ cần tạo 1 account qua API, tables sẽ tự động xuất hiện trong PostgreSQL!**