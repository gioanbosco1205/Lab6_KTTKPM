# Hướng dẫn Test PaymentService - Chi tiết từng bước

## Cách chạy

```bash
# 1. Khởi động service
cd PaymentService
dotnet run

# Service chạy trên: http://localhost:5160
```

## Hướng dẫn test từng bước cụ thể

### BƯỚC 1: POST – Tạo dữ liệu để test LINQ

**Mục đích:** Tạo 5 accounts với dữ liệu đa dạng để test các LINQ queries

**Method:** `POST http://localhost:5160/api/account`  
**Content-Type:** `application/json`

**1.1** Tạo account đầu tiên:
```json
{
    "policyNumber": "POL001",
    "policyAccountNumber": "ACC001", 
    "ownerName": "Nguyen Van A",
    "balance": 1500.50
}
```

**1.2** Tạo account thứ hai:
```json
{
    "policyNumber": "POL002",
    "policyAccountNumber": "ACC002",
    "ownerName": "Tran Thi B", 
    "balance": 6000.75
}
```

**1.3** Tạo account thứ ba:
```json
{
    "policyNumber": "POL003",
    "policyAccountNumber": "ACC003",
    "ownerName": "Nguyen Van C",
    "balance": 2500.25
}
```

**1.4** Tạo account thứ tư:
```json
{
    "policyNumber": "POL004", 
    "policyAccountNumber": "ACC004",
    "ownerName": "Le Thi D",
    "balance": 500.00
}
```

**1.5** Tạo account thứ năm:
```json
{
    "policyNumber": "POL005",
    "policyAccountNumber": "ACC005",
    "ownerName": "Nguyen Van E", 
    "balance": 8000.00
}
```

**Kết quả mong đợi:** Status 200 OK, trả về account đã tạo với Id được generate

---

### BƯỚC 2: Query theo PolicyNumber (LINQ cơ bản)

**Mục đích:** Test LINQ `FirstOrDefaultAsync()` để tìm account theo PolicyNumber

**Method:** `GET http://localhost:5160/api/account/{policyNumber}`

**2.1** Tìm account POL001:
```
GET http://localhost:5160/api/account/POL001
```

**2.2** Tìm account POL003:
```
GET http://localhost:5160/api/account/POL003
```

**2.3** Tìm account POL005:
```
GET http://localhost:5160/api/account/POL005
```

**Kết quả mong đợi:** Status 200 OK, trả về account tương ứng với PolicyNumber

---

### BƯỚC 3: Query không tồn tại

**Mục đích:** Test error handling khi không tìm thấy dữ liệu

**3.1** Tìm PolicyNumber không tồn tại:
```
GET http://localhost:5160/api/account/POL999
```
**Kết quả mong đợi:** Status 404 Not Found

**3.2** Tìm OwnerName không tồn tại:
```
GET http://localhost:5160/api/account/owner/Khong Ton Tai
```
**Kết quả mong đợi:** Status 200 OK, trả về array rỗng []

---

### BƯỚC 4: Query LINQ nâng cao (QUAN TRỌNG NHẤT)

**4.1** Lấy tất cả accounts (`ToListAsync()`):
```
GET http://localhost:5160/api/account
```
**LINQ:** `_session.Query<PolicyAccount>().ToListAsync()`  
**Kết quả:** Array chứa 5 accounts

**4.2** Filter balance > 2000 (`Where()` + `OrderByDescending()`):
```
GET http://localhost:5160/api/account/balance/greater-than/2000
```
**LINQ:** `Where(x => x.Balance > 2000).OrderByDescending(x => x.Balance)`  
**Kết quả:** 3 accounts (POL005: 8000, POL002: 6000, POL003: 2500)

**4.3** Filter balance trong khoảng (`Where()` với AND):
```
GET http://localhost:5160/api/account/balance/between/1000/5000
```
**LINQ:** `Where(x => x.Balance >= 1000 && x.Balance <= 5000)`  
**Kết quả:** 2 accounts (POL001: 1500, POL003: 2500)

**4.4** Search text (`Contains()` + `OrderBy()`):
```
GET http://localhost:5160/api/account/search/owner/Nguyen
```
**LINQ:** `Where(x => x.OwnerName.Contains("Nguyen")).OrderBy(x => x.OwnerName)`  
**Kết quả:** 3 accounts có "Nguyen" trong tên

**4.5** Top N records (`OrderByDescending()` + `Take()`):
```
GET http://localhost:5160/api/account/top/3
```
**LINQ:** `OrderByDescending(x => x.Balance).Take(3)`  
**Kết quả:** 3 accounts có balance cao nhất

**4.6** Sắp xếp tăng dần (`OrderBy()`):
```
GET http://localhost:5160/api/account/ordered-by-balance?descending=false
```
**LINQ:** `OrderBy(x => x.Balance)`  
**Kết quả:** 5 accounts sắp xếp balance từ thấp đến cao

**4.7** Sắp xếp giảm dần (`OrderByDescending()`):
```
GET http://localhost:5160/api/account/ordered-by-balance?descending=true
```
**LINQ:** `OrderByDescending(x => x.Balance)`  
**Kết quả:** 5 accounts sắp xếp balance từ cao đến thấp

**4.8** Complex WHERE (AND/OR logic):
```
GET http://localhost:5160/api/query-demo/complex-where
```
**LINQ:** `Where(x => (x.Balance > 1000 && x.OwnerName.Contains("Nguyen")) || (x.Balance > 5000 && x.OwnerName.Contains("Tran")))`

**4.9** Group By với statistics (`GroupBy()` + aggregations):
```
GET http://localhost:5160/api/query-demo/group-by-owner
```
**LINQ:** `GroupBy(x => x.OwnerName).Select(g => new { Count, Sum, Average, Max, Min })`

**4.10** Select projection (`Select()` + switch expression):
```
GET http://localhost:5160/api/query-demo/select-projection
```
**LINQ:** `Select(x => new { formatted fields, categories })`

**4.11** Any/All operations:
```
GET http://localhost:5160/api/query-demo/any-all-queries
```
**LINQ:** `Any()`, `All()` conditions

**4.12** Pagination (`Skip()` + `Take()`):
```
GET http://localhost:5160/api/query-demo/skip-take-paging/1/3
```
**LINQ:** `Skip((page-1) * pageSize).Take(pageSize)`

**4.13** Distinct values (`Select()` + `Distinct()`):
```
GET http://localhost:5160/api/query-demo/distinct-owners
```
**LINQ:** `Select(x => x.OwnerName).Distinct().OrderBy(x => x)`

**4.14** Statistical operations:
```
GET http://localhost:5160/api/query-demo/statistics
```
**LINQ:** `Count()`, `Sum()`, `Average()`, `Max()`, `Min()`

---

### BƯỚC 5: (Bonus) Query theo OwnerName

**5.1** Tìm exact match (`Where()` + exact equality):
```
GET http://localhost:5160/api/account/owner/Nguyen Van A
```
**LINQ:** `Where(x => x.OwnerName == ownerName)`

**5.2** Tìm owner khác:
```
GET http://localhost:5160/api/account/owner/Tran Thi B
```

**5.3** Đếm accounts (`CountAsync()`):
```
GET http://localhost:5160/api/account/count/owner/Nguyen Van A
```
**LINQ:** `CountAsync(x => x.OwnerName == ownerName)`

**5.4** Tính tổng balance (`SumAsync()`):
```
GET http://localhost:5160/api/account/total-balance/owner/Nguyen Van A
```
**LINQ:** `Where(x => x.OwnerName == ownerName).SumAsync(x => x.Balance)`

**5.5** Tìm theo account number (`FirstOrDefaultAsync()`):
```
GET http://localhost:5160/api/account/account-number/ACC002
```
**LINQ:** `FirstOrDefaultAsync(x => x.PolicyAccountNumber == accountNumber)`

## Cách chạy test

1. Mở `FINAL-TEST.http` trong VS Code
2. Cài REST Client extension  
3. **QUAN TRỌNG:** Chạy theo đúng thứ tự từ bước 1 đến 5
4. Click "Send Request" cho từng API call

**Lưu ý:** Phải chạy Bước 1 trước để có dữ liệu test!