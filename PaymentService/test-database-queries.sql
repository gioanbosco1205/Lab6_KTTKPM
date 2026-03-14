-- PHẦN 11 - KIỂM TRA DỮ LIỆU TRONG POSTGRESQL

-- 1. Kết nối database
-- psql -h localhost -U postgres -d lab_netmicro_payments

-- 2. Xem tất cả tables được tạo bởi Marten
\dt

-- 3. Xem cấu trúc table PolicyAccount
\d mt_doc_policyaccount

-- 4. Xem tất cả dữ liệu PolicyAccount
SELECT * FROM mt_doc_policyaccount;

-- 5. Xem dữ liệu JSON đẹp hơn
SELECT 
    id,
    data->>'policyNumber' as policy_number,
    data->>'policyAccountNumber' as account_number,
    (data->>'balance')::decimal as balance,
    data
FROM mt_doc_policyaccount;

-- 6. Tìm account theo PolicyNumber
SELECT * FROM mt_doc_policyaccount 
WHERE data->>'policyNumber' = 'POL001';

-- 7. Đếm tổng số accounts
SELECT COUNT(*) as total_accounts FROM mt_doc_policyaccount;

-- 8. Tính tổng balance của tất cả accounts
SELECT SUM((data->>'balance')::decimal) as total_balance 
FROM mt_doc_policyaccount;

-- 9. Xem accounts có balance > 1000
SELECT 
    data->>'policyNumber' as policy_number,
    (data->>'balance')::decimal as balance
FROM mt_doc_policyaccount 
WHERE (data->>'balance')::decimal > 1000;

-- 10. Xóa tất cả test data (nếu cần)
-- DELETE FROM mt_doc_policyaccount;

-- 11. Xem version của Marten tables
SELECT * FROM mt_doc_policyaccount_transform;

-- 12. Kiểm tra indexes
\di mt_doc_policyaccount*