# CORS Configuration Guide - ChatService

## Tổng quan
CORS (Cross-Origin Resource Sharing) được cấu hình để cho phép frontend từ các domain khác có thể gọi API và kết nối SignalR.

## Cấu hình

### 1. Cấu hình trong appsettings.json
```json
{
  "AppSettings": {
    "AllowedChatOrigins": ["http://localhost:8080"]
  }
}
```

### 2. Cấu hình trong Program.cs
```csharp
// Cấu hình CORS
var allowedOrigins = builder.Configuration.GetSection("AppSettings:AllowedChatOrigins").Get<string[]>() ?? new[] { "http://localhost:8080" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials()
               .WithOrigins(allowedOrigins);
    });
});
```

### 3. Sử dụng CORS Middleware
```csharp
app.UseCors("CorsPolicy");
```

## Thứ tự Middleware quan trọng
```csharp
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");        // Phải đặt trước Authentication
app.UseAuthentication();
app.UseAuthorization();
```

## Cấu hình cho Production

### Thêm nhiều origins:
```json
{
  "AppSettings": {
    "AllowedChatOrigins": [
      "http://localhost:8080",
      "https://yourdomain.com",
      "https://www.yourdomain.com"
    ]
  }
}
```

### Cấu hình nghiêm ngặt hơn:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithHeaders("Content-Type", "Authorization")  // Chỉ cho phép headers cụ thể
               .WithMethods("GET", "POST")                    // Chỉ cho phép methods cụ thể
               .AllowCredentials()
               .WithOrigins(allowedOrigins);
    });
});
```

## SignalR và CORS
SignalR tự động sử dụng CORS policy đã cấu hình, không cần cấu hình thêm.

## Lưu ý bảo mật
1. **Không sử dụng AllowAnyOrigin()** trong production
2. **Luôn specify origins cụ thể** thay vì wildcard
3. **AllowCredentials()** cần thiết cho JWT authentication
4. **HTTPS only** trong production environment