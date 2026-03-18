# Compiled Queries với Marten - Hướng dẫn chi tiết

## Compiled Query là gì?

**Compiled Query** là tính năng của Marten cho phép pre-compile LINQ expressions thành SQL queries, giúp:
- ✅ **Tăng performance** - Query được compile 1 lần, tái sử dụng nhiều lần
- ✅ **Giảm overhead** - Không cần parse LINQ expression mỗi lần
- ✅ **Type safety** - Compile-time checking
- ✅ **Caching** - Query plan được cache

## Cấu trúc Compiled Queries

### 1. Định nghĩa Compiled Queries (`PolicyAccountQueries.cs`)

```csharp
// Single result query
public static readonly ICompiledQuery<PolicyAccount, PolicyAccount?> FindByPolicyNumber =
    QuerySession.Query<PolicyAccount>()
        .Where(x => x.PolicyNumber == "")  // "" sẽ được thay bằng parameter
        .Compile<PolicyAccount?>();

// List result query  
public static readonly ICompiledListQuery<PolicyAccount> FindByOwnerName =
    QuerySession.Query<PolicyAccount>()
        .Where(x => x.OwnerName == "")
        .OrderBy(x => x.PolicyNumber)
        .Compile();

// Aggregation query
public static readonly ICompiledQuery<PolicyAccount, int> CountByOwner =
    QuerySession.Query<PolicyAccount>()
        .Where(x => x.OwnerName == "")
        .Count()
        .Compile();
```

### 2. Sử dụng Compiled Queries (`CompiledPolicyAccountRepository.cs`)

```csharp
// Single result
public async Task<PolicyAccount?> FindByPolicyNumberCompiled(string policyNumber)
{
    return await _session.QueryAsync(PolicyAccountQueries.FindByPolicyNumber, policyNumber);
}

// List result
public async Task<IReadOnlyList<PolicyAccount>> FindByOwnerNameCompiled(string ownerName)
{
    return await _session.QueryAsync(PolicyAccountQueries.FindByOwnerName, ownerName);
}

// Aggregation
public async Task<int> CountByOwnerCompiled(string ownerName)
{
    return await _session.QueryAsync(PolicyAccountQueries.CountByOwner, ownerName);
}
```

## Compiled Queries đã implement

### Basic Queries
1. **FindByPolicyNumber** - Tìm theo PolicyNumber
2. **FindByAccountNumber** - Tìm theo PolicyAccountNumber
3. **FindByOwnerName** - Tìm theo OwnerName (exact match)

### Filter Queries
4. **FindByBalanceGreaterThan** - Balance > amount
5. **FindByBalanceRange** - Balance trong khoảng
6. **SearchByOwnerNameContains** - OwnerName chứa text

### Aggregation Queries
7. **CountByOwner** - Đếm accounts theo owner
8. **SumBalanceByOwner** - Tổng balance theo owner

### Sorting & Limiting
9. **GetTopAccountsByBalance** - Top N accounts
10. **GetAllOrderedByBalance** - Tất cả accounts sắp xếp
11. **GetAccountsPaged** - Pagination với Skip/Take

## API Endpoints cho Compiled Queries

| Method | Endpoint | Compiled Query |
|--------|----------|----------------|
| GET | `/api/compiled/policy/{policyNumber}` | FindByPolicyNumber |
| GET | `/api/compiled/owner/{ownerName}` | FindByOwnerName |
| GET | `/api/compiled/balance/greater-than/{min}` | FindByBalanceGreaterThan |
| GET | `/api/compiled/balance/range/{min}/{max}` | FindByBalanceRange |
| GET | `/api/compiled/search/owner/{term}` | SearchByOwnerNameContains |
| GET | `/api/compiled/account-number/{number}` | FindByAccountNumber |
| GET | `/api/compiled/count/owner/{owner}` | CountByOwner |
| GET | `/api/compiled/sum-balance/owner/{owner}` | SumBalanceByOwner |
| GET | `/api/compiled/top/{count}` | GetTopAccountsByBalance |
| GET | `/api/compiled/all-ordered` | GetAllOrderedByBalance |
| GET | `/api/compiled/paged/{page}/{size}` | GetAccountsPaged |

## Performance Comparison

### Endpoint đặc biệt:
```
GET /api/compiled/performance-comparison/{policyNumber}
```

So sánh thời gian thực thi giữa:
- **Regular LINQ Query** - Parse expression mỗi lần
- **Compiled Query** - Pre-compiled, tái sử dụng

## Cách test

### 1. Chạy service:
```bash
cd PaymentService
dotnet run
# Service: http://localhost:5160
```

### 2. Test compiled queries với logs:
```bash
# Demo logs để chứng minh compiled query
curl "http://localhost:5160/api/compiled/demo-logs/POL001"

# Kiểm tra console output để thấy:
# 🔥 CALLING REGULAR LINQ QUERY:
# >>> Running Regular LINQ Query: FindByNumber
# ⚡ CALLING COMPILED QUERY:  
# >>> Running Compiled Query: FindByPolicyNumberCompiled
```

### 3. So sánh performance:
```bash
# Performance comparison với logs
curl "http://localhost:5160/api/compiled/performance-comparison/POL001"
```

## Lợi ích của Compiled Queries

### 1. Performance
- Query được compile 1 lần, cache và tái sử dụng
- Giảm overhead của LINQ expression parsing
- Faster execution time

### 2. Type Safety
- Compile-time checking
- IntelliSense support
- Refactoring safety

### 3. Maintainability
- Centralized query definitions
- Reusable across repositories
- Easy to modify and test

## Khi nào sử dụng Compiled Queries?

✅ **Nên sử dụng khi:**
- Query được gọi thường xuyên
- Performance là ưu tiên
- Query pattern cố định
- Cần type safety

❌ **Không nên sử dụng khi:**
- Query động, thay đổi structure
- Query chỉ gọi 1 lần
- Logic phức tạp, nhiều điều kiện động

## Kết quả mong đợi

Compiled queries sẽ cho response với format:
```json
{
    "message": "Found using COMPILED QUERY",
    "account": { ... }
}
```

Để phân biệt với regular queries!