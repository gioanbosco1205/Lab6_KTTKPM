# KHẮC PHỤC VẤN ĐỀ "DID NOT FIND ANY TABLES"

## 🔍 NGUYÊN NHÂN:
Marten chỉ tạo tables khi có dữ liệu đầu tiên được lưu vào database.

## ✅ GIẢI PHÁP:

### BƯỚC 1: Chạy service
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

### BƯỚC 3: Kiểm tra lại PostgreSQL
```bash
psql -h localhost -U postgres -d lab_netmicro_payments
\dt
```

**Bây giờ bạn sẽ thấy table:** `mt_doc_policyaccount`

### BƯỚC 4: Xem dữ liệu
```sql
SELECT * FROM mt_doc_policyaccount;

-- Hoặc xem đẹp hơn:
SELECT 
    data->>'policyNumber' as policy_number,
    data->>'policyAccountNumber' as account_number,
    (data->>'balance')::decimal as balance
FROM mt_doc_policyaccount;
```

## 🎯 TẠI SAO XẢY RA VẤN ĐỀ NÀY?

1. **Marten lazy loading**: Marten chỉ tạo tables khi cần thiết
2. **Document Database**: Marten hoạt động như document database, không tạo schema trước
3. **Performance**: Tránh tạo tables không cần thiết

## 🚀 CÁCH KIỂM TRA NHANH:

### Script tự động:
```bash
# Chạy script này để tự động tạo table
./fix-database-tables.sh
```

### Hoặc thủ công:
1. Chạy service: `dotnet run`
2. Tạo 1 account qua API
3. Kiểm tra PostgreSQL: `\dt`

## 📋 CHECKLIST:

- [ ] Service chạy thành công
- [ ] Tạo được account qua API
- [ ] Table `mt_doc_policyaccount` xuất hiện
- [ ] Có thể query dữ liệu

## ⚠️ LƯU Ý:

- **Table name**: Marten tự động thêm prefix `mt_doc_` trước tên class
- **Schema**: Marten tạo schema tự động khi lưu dữ liệu đầu tiên
- **JSON Storage**: Dữ liệu được lưu dạng JSON trong column `data`

## 🎉 KẾT QUẢ MONG ĐỢI:

Sau khi làm theo hướng dẫn, bạn sẽ thấy:
```
lab_netmicro_payments=# \dt
                    List of relations
 Schema |         Name          | Type  |  Owner   
--------+-----------------------+-------+----------
 public | mt_doc_policyaccount  | table | postgres
(1 row)
```