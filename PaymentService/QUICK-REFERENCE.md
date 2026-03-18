# Quick Reference - Test Steps

## Bước 1: POST - Tạo dữ liệu
```
POST http://localhost:5160/api/account
Content-Type: application/json

Body: 5 accounts với POL001-POL005
```

## Bước 2: GET - Query cơ bản
```
GET http://localhost:5160/api/account/POL001
GET http://localhost:5160/api/account/POL003  
GET http://localhost:5160/api/account/POL005
```

## Bước 3: GET - Query không tồn tại
```
GET http://localhost:5160/api/account/POL999
GET http://localhost:5160/api/account/owner/Khong Ton Tai
```

## Bước 4: GET - LINQ nâng cao (14 APIs)
```
GET http://localhost:5160/api/account                                    # ToListAsync
GET http://localhost:5160/api/account/balance/greater-than/2000          # Where + OrderBy
GET http://localhost:5160/api/account/balance/between/1000/5000          # Where AND
GET http://localhost:5160/api/account/search/owner/Nguyen                # Contains
GET http://localhost:5160/api/account/top/3                              # Take
GET http://localhost:5160/api/account/ordered-by-balance?descending=false # OrderBy
GET http://localhost:5160/api/account/ordered-by-balance?descending=true  # OrderByDesc
GET http://localhost:5160/api/query-demo/complex-where                   # Complex WHERE
GET http://localhost:5160/api/query-demo/group-by-owner                  # GroupBy
GET http://localhost:5160/api/query-demo/select-projection               # Select
GET http://localhost:5160/api/query-demo/any-all-queries                 # Any/All
GET http://localhost:5160/api/query-demo/skip-take-paging/1/3            # Skip/Take
GET http://localhost:5160/api/query-demo/distinct-owners                 # Distinct
GET http://localhost:5160/api/query-demo/statistics                      # Aggregations
```

## Bước 5: GET - Bonus OwnerName
```
GET http://localhost:5160/api/account/owner/Nguyen Van A                 # Exact match
GET http://localhost:5160/api/account/owner/Tran Thi B                   # Exact match
GET http://localhost:5160/api/account/count/owner/Nguyen Van A           # CountAsync
GET http://localhost:5160/api/account/total-balance/owner/Nguyen Van A   # SumAsync
GET http://localhost:5160/api/account/account-number/ACC002              # FirstOrDefault
```

**Chạy theo thứ tự: 1 → 2 → 3 → 4 → 5**