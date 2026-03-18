# PaymentService - LINQ Queries với Marten

## Tính năng đã implement

✅ **API tìm account theo PolicyNumber**  
✅ **Field OwnerName trong PolicyAccount model**  
✅ **LINQ queries đầy đủ với Marten**  
✅ **Advanced LINQ operations**  
✅ **Compiled Queries với Marten** ← **MỚI**  

## Cách chạy

```bash
cd PaymentService
dotnet run
```

Service chạy trên: **http://localhost:5160**

## Test

**File test chính:** `FINAL-TEST.http` - Regular LINQ queries  
**File test compiled:** `COMPILED-QUERY-TEST.http` - Compiled queries  
**Hướng dẫn chi tiết:** `TEST-GUIDE.md` ← **ĐỌC FILE NÀY**  
**Hướng dẫn compiled:** `COMPILED-QUERY-GUIDE.md` ← **MỚI**

### Các bước test chi tiết:

1. **POST** – Tạo dữ liệu để test LINQ (5 accounts)
   - Method: `POST /api/account` với JSON body
   - 5 requests tạo accounts với dữ liệu đa dạng

2. **Query theo PolicyNumber** (LINQ cơ bản)
   - Method: `GET /api/account/{policyNumber}`
   - Test `FirstOrDefaultAsync()` với 3 PolicyNumbers

3. **Query không tồn tại** (Error handling)
   - Method: `GET` với dữ liệu không tồn tại
   - Kiểm tra 404 Not Found và empty array

4. **Query LINQ nâng cao** (QUAN TRỌNG NHẤT)
   - 14 API calls khác nhau
   - Covering tất cả LINQ operations: Where, OrderBy, Take, GroupBy, etc.

5. **(Bonus) Query theo OwnerName**
   - Method: `GET` với các endpoints khác nhau
   - Test CountAsync, SumAsync, exact match

**→ Xem `TEST-GUIDE.md` để biết method HTTP và payload cụ thể cho từng bước!**

### LINQ Methods được demo:

- `ToListAsync()`, `FirstOrDefaultAsync()`
- `Where()`, `Contains()`, `OrderBy()`, `Take()`, `Skip()`
- `CountAsync()`, `SumAsync()`, `Average()`, `Max()`, `Min()`
- `GroupBy()`, `Select()`, `Any()`, `All()`, `Distinct()`
- Complex WHERE với AND/OR logic
- Pagination, Statistics, Projections
- **Compiled Queries** cho performance tối ưu

## Files quan trọng

- `FINAL-TEST.http` - File test regular LINQ queries
- `COMPILED-QUERY-TEST.http` - File test compiled queries
- `TEST-GUIDE.md` - Hướng dẫn chi tiết regular queries
- `COMPILED-QUERY-GUIDE.md` - Hướng dẫn compiled queries
- `Controllers/AccountController.cs` - Basic LINQ APIs
- `Controllers/QueryDemoController.cs` - Advanced LINQ demos
- `Controllers/CompiledQueryController.cs` - Compiled query APIs
- `Repositories/PolicyAccountRepository.cs` - Regular LINQ implementations
- `Repositories/CompiledPolicyAccountRepository.cs` - Compiled query implementations
- `CompiledQueries/PolicyAccountQueries.cs` - Compiled query definitions

## Model

```csharp
public class PolicyAccount
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; }
    public string PolicyAccountNumber { get; set; }
    public string OwnerName { get; set; }  // Field mới
    public decimal Balance { get; set; }
}
```

**Sẵn sàng để test và chụp màn hình!**
 docker compose down -v   
 docker compose up --build