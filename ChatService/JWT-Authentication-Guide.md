# JWT Authentication Guide - ChatService

## Tổng quan
ChatService sử dụng JWT (JSON Web Token) để xác thực người dùng khi kết nối SignalR và gọi API.

## Cấu hình

### 1. Cài đặt packages
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 2. Cấu hình trong Program.cs
```csharp
// Cấu hình JWT Authentication
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
    
    // Cấu hình để SignalR có thể sử dụng JWT từ query string
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
```

### 3. Cấu hình appsettings.json
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-here-make-it-long-enough-for-security-purposes",
    "Issuer": "ChatService",
    "Audience": "ChatServiceUsers",
    "ExpiryInMinutes": 60
  }
}
```

## Sử dụng

### 1. Đăng nhập để lấy token
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "testuser",
  "password": "testpass"
}
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "guid-here",
  "username": "testuser",
  "expiresAt": "2024-01-01T12:00:00Z"
}
```

### 2. Sử dụng token trong API calls
```http
GET /api/protected-endpoint
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 3. Sử dụng token với SignalR
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub", {
        accessTokenFactory: () => {
            return localStorage.getItem("jwt_token");
        }
    })
    .build();
```

## Bảo mật

### Lưu ý quan trọng:
1. **Secret Key**: Phải đủ dài và phức tạp (ít nhất 32 ký tự)
2. **HTTPS**: Luôn sử dụng HTTPS trong production
3. **Token Storage**: Lưu token an toàn ở client (tránh localStorage nếu có thể)
4. **Token Expiry**: Đặt thời gian hết hạn hợp lý
5. **Refresh Token**: Implement refresh token mechanism cho production

### Middleware Order:
```csharp
app.UseAuthentication();  // Phải đặt trước UseAuthorization
app.UseAuthorization();
```

## Testing
Sử dụng file `test-jwt-auth.http` để test các endpoint authentication.