# PHẦN 11 - TEST HƯỚNG DẪN CHI TIẾT

## 🚀 BƯỚC 1: CHẠY SERVICE

### Cách 1: Chạy từ terminal
```bash
cd PaymentService
dotnet run
```

### Cách 2: Chạy với hot reload
```bash
cd PaymentService
dotnet watch run
```

**Kết quả mong đợi:**
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5160
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

## 🧪 BƯỚC 2: TEST API

### Test 1: Tạo Policy Account
**Method:** POST  
**URL:** `http://localhost:5160/api/account`  
**Headers:** `Content-Type: application/json`  
**Body:**
```json
{
  "policyNumber": "POL001",
  "policyAccountNumber": "ACC001", 
  "balance": 1000
}
```

**Kết quả mong đợi:**
```json
{
  "id": "guid-tự-động-tạo",
  "policyNumber": "POL001",
  "policyAccountNumber": "ACC001",
  "balance": 1000
}
```

### Test 2: Lấy Policy Account
**Method:** GET  
**URL:** `http://localhost:5160/api/account/POL001`

**Kết quả mong đợi:**
```json
{
  "id": "guid-giống-như-trên",
  "policyNumber": "POL001", 
  "policyAccountNumber": "ACC001",
  "balance": 1000
}
```

### Test 3: Test Not Found
**Method:** GET  
**URL:** `http://localhost:5160/api/account/POL999`

**Kết quả mong đợi:**
```
Account with policy number POL999 not found
```

## 🗄️ BƯỚC 3: KIỂM TRA DỮ LIỆU TRONG POSTGRESQL

### Cách 1: Sử dụng psql command line
```bash
# Kết nối database
psql -h localhost -U postgres -d lab_netmicro_payments

# Xem tất cả tables
\dt

# Xem dữ liệu trong table mt_doc_policyaccount
SELECT * FROM mt_doc_policyaccount;

# Xem dữ liệu dạng JSON đẹp
SELECT data FROM mt_doc_policyaccount;

# Thoát
\q
```

### Cách 2: Sử dụng pgAdmin hoặc GUI tool
1. Mở pgAdmin
2. Kết nối đến server localhost:5432
3. Mở database `lab_netmicro_payments`
4. Xem table `mt_doc_policyaccount`

### Cách 3: Test endpoint để xem database
**Method:** GET  
**URL:** `http://localhost:5160/test-db`

## 📋 CHECKLIST TEST

- [ ] Service chạy thành công trên port 5160
- [ ] POST /api/account tạo được account mới
- [ ] GET /api/account/{number} lấy được account
- [ ] GET /api/account/{number} trả về 404 khi không tìm thấy
- [ ] Dữ liệu được lưu vào PostgreSQL
- [ ] Swagger UI hoạt động tại http://localhost:5160/swagger

## 🔧 TROUBLESHOOTING

### Lỗi thường gặp:

1. **Service không chạy được:**
   - Kiểm tra PostgreSQL đã chạy chưa
   - Kiểm tra connection string trong appsettings.json
   - Chạy `dotnet build` để kiểm tra lỗi compile

2. **API trả về 500 error:**
   - Kiểm tra database connection
   - Xem log trong console
   - Kiểm tra format JSON request

3. **Không tìm thấy dữ liệu:**
   - Kiểm tra table name trong PostgreSQL
   - Marten tự động tạo table với prefix `mt_doc_`
   - Kiểm tra PolicyNumber có đúng không

## 📊 KẾT QUẢ MONG ĐỢI

Sau khi test thành công, bạn sẽ thấy:
- Service chạy ổn định
- API endpoints hoạt động đúng
- Dữ liệu được lưu vào PostgreSQL
- Có thể query dữ liệu từ database