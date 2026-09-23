# Codex Pet

## Ứng dụng Windows tự chạy khi đăng nhập

Bản Windows nằm trong [`CodexPet.Windows`](CodexPet.Windows/). Ứng dụng chạy nền ở khay hệ thống và hiện pet nhắc vận động sau mỗi 25 phút. Pet tự di chuyển, kéo thả được, nhấn 3 lần để tắt lời nhắc, hoặc tự ẩn sau 2 phút. Sau đó chu kỳ 25 phút bắt đầu lại.

Để biên dịch, cài vào `%LOCALAPPDATA%\CodexPet` và chạy thử popup ngay:

```powershell
& .\CodexPet.Windows\Install.ps1
```

Ứng dụng tự đăng ký trong `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, nên nó chạy khi bạn **đăng nhập Windows**. Một Windows Service thuần không thể hiện popup trực tiếp trên desktop vì chạy ở phiên hệ thống tách biệt. Không cần quyền quản trị hay cài .NET SDK; script dùng trình biên dịch .NET Framework có sẵn trên máy này.

Nhấp phải biểu tượng ở khay hệ thống để chọn **Nhắc ngay**, bật/tắt **Tự chạy khi đăng nhập**, hoặc **Thoát**. Trước khi cài lại bản mới, thoát ứng dụng từ khay hệ thống. Mã pet được vẽ dạng pixel ngay trong ứng dụng; không cần file ảnh đi kèm.

## Widget cho web

Widget nhắc đứng dậy vận động mỗi 25 phút. Pet xuất hiện tối đa 2 phút, đi ngang cuối màn hình, có thể kéo thả và nhấn 3 lần để tắt lời nhắc. Sau khi tắt, bộ đếm 25 phút bắt đầu lại.

## Chạy thử

Mở `index.html` bằng trình duyệt và bấm **Xem pet ngay**. Không cần cài package hay chạy build.

## Nhúng vào website

Sao chép `codex-pet.js` vào thư mục public của website, rồi thêm trước `</body>`:

```html
<script src="/codex-pet.js" defer></script>
```

Widget tự gắn vào trang, không cần thêm phần tử HTML. Nếu website là ứng dụng nhiều trang, đặt script trong layout chung. Widget chỉ hiển thị khi tab website đang mở và không thể nổi ngoài cửa sổ trình duyệt.

### Cấu hình

```html
<script src="/codex-pet.js" defer
        data-interval-seconds="1500"
        data-duration-seconds="120"
        data-message="Đứng dậy tập thể dục thôi!"></script>
```

Để xem thử ngay khi tải trang, thêm `data-start-immediately`. Các số giây phải lớn hơn 0; giá trị không hợp lệ sẽ dùng mặc định. Có thể gọi `document.querySelector('codex-pet').showNow()` từ mã của website.

Nếu muốn tự tạo widget, dùng `<script src="/codex-pet.js" data-manual defer></script>` và thêm `<codex-pet></codex-pet>` ở vị trí bất kỳ. Các thuộc tính của phần tử tương ứng là `interval-seconds`, `duration-seconds`, `message` và `start-immediately`.

## Asset

Tài liệu [bàn giao Windows](CODEX_PET_WINDOWS_HANDOFF.md) nhắc tới `codex-spritesheet.webp`, nhưng file đó không có trong kho này. Widget web hiện dùng SVG tự vẽ, không tải asset từ dịch vụ ngoài.
