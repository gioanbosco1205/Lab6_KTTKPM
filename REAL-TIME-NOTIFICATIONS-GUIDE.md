# 🔔 REAL-TIME NOTIFICATIONS SYSTEM - TEST GUIDE

## 🎯 Tính năng đã nâng cấp:

### ✅ **Enhanced Toast Notifications**
- **Toast Popups**: Hiển thị ở góc phải màn hình
- **4 loại notifications**: Success (xanh), Warning (vàng), Error (đỏ), Info (xanh dương)
- **Auto-dismiss**: Tự động biến mất sau 5-7 giây
- **Progress Bar**: Thanh tiến trình countdown
- **Close Button**: Có thể đóng thủ công
- **Sound Alerts**: Âm thanh khác nhau cho từng loại

### ✅ **Visual Effects**
- **Slide-in Animation**: Toast trượt vào từ bên phải
- **Slide-out Animation**: Toast trượt ra khi đóng
- **Bounce Animation**: Badge notification nhảy lên xuống
- **Pulse Effect**: Button có hiệu ứng pulse khi có event
- **Progress Animation**: Thanh tiến trình chạy từ 100% → 0%

### ✅ **Notification Counter**
- **Badge Counter**: Hiển thị số notifications mới
- **Auto-update**: Tự động tăng khi có notification mới
- **Auto-hide**: Badge tự động biến mất sau 3 giây

### ✅ **Sound System**
- **Different Frequencies**: Mỗi loại notification có âm thanh khác nhau
  - Success: 800Hz (cao, vui)
  - Warning: 600Hz (trung bình)
  - Error: 400Hz (thấp, nghiêm trọng)
  - Info: 1000Hz (rất cao, nhẹ nhàng)

## 🧪 **CÁCH TEST:**

### **Bước 1: Khởi động hệ thống**
```bash
./start-system.sh
```

### **Bước 2: Mở Client App**
```bash
open client-app/index.html
```

### **Bước 3: Test Toast System**

#### **3.1 Test Manual Toasts:**
1. **Click "Test Toasts" button** (màu xanh dương)
2. **Kết quả**: 4 toast notifications xuất hiện lần lượt:
   - ✅ Success toast (xanh)
   - ⚠️ Warning toast (vàng)  
   - ❌ Error toast (đỏ)
   - ℹ️ Info toast (xanh dương)

#### **3.2 Test Real-time Policy Events:**
1. **Connect agent**: agent1 / Agent Smith
2. **Test 3 policy events:**
   - **Create Policy**: Click "Create Policy" → 🎉 Success toast
   - **Terminate Policy**: Click "Terminate Policy" → ⚠️ Warning toast
   - **Activate Product**: Click "Activate Product" → ℹ️ Info toast

#### **3.3 Test Chat Notifications:**
1. **Mở 2 browser tabs**
2. **Connect 2 agents khác nhau**
3. **Test notifications:**
   - **Group message**: Gửi message → 💬 Info toast
   - **Private message**: Gửi private → 🔒 Info toast + 📤 Success toast
   - **Agent join/leave**: Connect/disconnect → 👋 Info/Warning toast

### **Bước 4: Test Notification Management**

#### **4.1 Clear Functions:**
- **"Clear Notifications"**: Xóa notification area + reset counter
- **"Clear Toasts"**: Xóa tất cả toast popups

#### **4.2 Notification Counter:**
- **Counter Display**: "Notifications: X" tăng theo real-time
- **Badge Popup**: Badge hiển thị "X new notifications"
- **Auto-hide**: Badge tự động biến mất sau 3 giây

## 🔍 **EXPECTED RESULTS:**

### ✅ **Toast Notifications:**
```
┌─────────────────────────────┐
│ ✅ Policy Created!      ✕  │
│ Policy POL-123 created      │
│ ▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░  │
└─────────────────────────────┘
```

### ✅ **Visual Effects:**
- Toast slide in từ phải → trái
- Progress bar chạy từ 100% → 0%
- Badge bounce animation
- Smooth transitions

### ✅ **Sound Alerts:**
- Mỗi toast có âm thanh riêng
- Âm thanh ngắn (0.3 giây)
- Tần số khác nhau theo loại

### ✅ **Real-time Updates:**
- Notifications xuất hiện ngay khi event xảy ra
- Counter tự động tăng
- Badge popup hiển thị

## 🎮 **DEMO SCRIPT:**

```bash
# 1. Start system
./start-system.sh

# 2. Open client app
open client-app/index.html

# 3. Test sequence:
# - Click "Test Toasts" → See 4 different toasts
# - Connect agent → See connection toast
# - Click "Create Policy" → See policy created toast
# - Click "Terminate Policy" → See termination toast
# - Click "Activate Product" → See activation toast
# - Open 2nd tab, connect 2nd agent → See agent joined toast
# - Send messages → See message toasts
# - Click "Clear Toasts" → All toasts disappear
```

## 🔧 **Troubleshooting:**

### **Toasts không hiển thị:**
- Check browser console (F12)
- Ensure JavaScript không có lỗi
- Kiểm tra CSS styles load đúng

### **Sound không hoạt động:**
- Browser có thể block autoplay audio
- Click vào page trước để enable audio context
- Check browser audio settings

### **Animations không smooth:**
- Ensure CSS animations được support
- Check browser performance
- Reduce animation duration nếu cần

## 📊 **TECHNICAL DETAILS:**

### **Toast Structure:**
```html
<div class="toast success">
  <div class="toast-header">
    <span class="toast-title">✅ Success</span>
    <button class="toast-close">×</button>
  </div>
  <div class="toast-body">Message content</div>
  <div class="toast-progress"></div>
</div>
```

### **CSS Animations:**
- `slideIn`: translateX(100%) → translateX(0)
- `slideOut`: translateX(0) → translateX(100%)
- `progress`: width(100%) → width(0%)
- `bounce`: translateY(0) → translateY(-10px) → translateY(0)

### **Sound Generation:**
```javascript
// Web Audio API
const audioContext = new AudioContext();
const oscillator = audioContext.createOscillator();
oscillator.frequency.setValueAtTime(800, audioContext.currentTime);
oscillator.type = 'sine';
```

## 🎯 **FEATURES IMPLEMENTED:**

### ✅ **Core Features:**
- [x] Toast popup notifications
- [x] 4 notification types with colors
- [x] Auto-dismiss with countdown
- [x] Manual close button
- [x] Slide animations
- [x] Sound alerts
- [x] Notification counter
- [x] Badge popup
- [x] Clear functions

### ✅ **Integration:**
- [x] SignalR real-time events
- [x] Policy event notifications
- [x] Chat message notifications
- [x] Agent status notifications
- [x] API event triggers

---

## 🎉 **REAL-TIME NOTIFICATIONS SYSTEM HOÀN THÀNH!**

**Hệ thống thông báo real-time với UI/UX chuyên nghiệp đã sẵn sàng sử dụng!**