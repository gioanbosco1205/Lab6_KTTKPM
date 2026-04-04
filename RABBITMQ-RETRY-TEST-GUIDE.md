# Hướng dẫn Kiểm tra (Test) Cơ chế Retry RabbitMQ

Tài liệu này hướng dẫn cách kiểm tra cơ chế Retry và Tự động kết nối lại (Reconnection) vừa được triển khai trong `PolicyService` và `ChatService`.

## 1. Chuẩn bị (Prerequisites)
- Đảm bảo RabbitMQ đang chạy (hoặc có thể tắt đi để test).
- Mở Terminal để theo dõi log của các service.

## 2. Kịch bản 1: RabbitMQ chưa chạy khi khởi động Service
Kịch bản này kiểm tra khả năng "đợi" RabbitMQ của các service.

1. **Dừng Docker RabbitMQ**:
   ```bash
   docker stop <container_id_rabbitmq>
   ```
2. **Khởi động ChatService**:
   ```bash
   cd ChatService
   dotnet run
   ```
3. **Quan sát log**:
   - Bạn sẽ thấy log cảnh báo: `Retry X failed to connect to RabbitMQ. Waiting X before next retry.`
   - Service sẽ không bị crash mà tiếp tục thử lại với thời gian giãn cách tăng dần (Exponential Backoff).
4. **Khởi động lại RabbitMQ**:
   ```bash
   docker start <container_id_rabbitmq>
   ```
5. **Quan sát log**:
   - Sau vài giây, log sẽ hiện: `Connected to RabbitMQ successfully` và `PolicyEventSubscriber connected and consumers setup successfully`.

## 3. Kịch bản 2: RabbitMQ bị sập khi các Service đang chạy
Kịch bản này kiểm tra khả năng tự phục hồi khi mất kết nối đột ngột.

1. **Đảm bảo các Service đang chạy bình thường**.
2. **Dừng Docker RabbitMQ**:
   ```bash
   docker stop <container_id_rabbitmq>
   ```
3. **Quan sát log của ChatService**:
   - Bạn sẽ thấy log: `RabbitMQ connection lost. Reason: ... Attempting to reconnect...`
   - Tiếp tục thấy các log retry.
4. **Gửi tin nhắn từ PolicyService (Kiểm tra Publisher)**:
   - Gọi API tạo Policy (khi RabbitMQ vẫn đang tắt).
   - Publisher sẽ thử kết nối lại 5 lần trước khi báo lỗi.
5. **Khởi động lại RabbitMQ**:
   - Sau khi kết nối lại thành công, hãy thử gửi lại sự kiện từ `PolicyService`.
   - Kiểm tra `ChatService` có nhận được sự kiện và gửi thông báo SignalR như bình thường không.

## 4. Kiểm tra mã nguồn (Code Verification)
Hãy đảm bảo các thuộc tính sau đã được cấu hình trong `Program.cs`:
- `AutomaticRecoveryEnabled = true`
- `NetworkRecoveryInterval = TimeSpan.FromSeconds(10)`

## 5. Lưu ý
- **PolicyService (Publisher)**: Sử dụng Singleton để tái sử dụng kết nối, tránh tạo quá nhiều kết nối rác.
- **ChatService (Subscriber)**: Sử dụng Polly với `WaitAndRetryForever` để đảm bảo subscriber luôn sẵn sàng ngay khi RabbitMQ online.
