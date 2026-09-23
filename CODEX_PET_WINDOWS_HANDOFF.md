# Bàn giao: Codex Exercise Pet cho Windows

> Mục tiêu: đồng nghiệp build một desktop pet giống bản Linux hiện tại: pet sprite trong suốt, luôn nổi trên desktop, nhắc đứng dậy tập thể dục mỗi 25 phút, chạy ở đáy màn hình, kéo-thả được và click 3 lần để tắt reminder hiện tại.

## 1. Bản gốc hiện tại

| Hạng mục | Giá trị |
| --- | --- |
| Source Linux | `iva-api-server/codex_pet_widget.py` |
| Asset gốc | `iva-api-server/codex-spritesheet.webp` |
| Stack | Python 3 + GTK 3 (PyGObject) |
| Chu kỳ reminder | `1500` giây = 25 phút |
| Thời gian hiển thị | `120` giây = 2 phút |
| Sprite cell | `192 × 208 px` |
| Animation đang dùng | hàng `4`, 5 frame đầu |
| Scale hiện tại | `1.5x` = `288 × 312 px` |

### Hành vi cần giữ

1. App khởi động thì chờ 25 phút.
2. Khi đến giờ: hiện bubble “Đứng dậy tập thể dục thôi!”, pet tại đáy màn hình và luôn nổi trên các cửa sổ khác.
3. Pet đổi frame khoảng mỗi 140 ms.
4. Pet đi từ trái sang phải ở đáy màn hình, `3 px / 50 ms`; đến mép phải thì quay lại trái.
5. Kéo pet để đặt vị trí. Sau khi kéo, tự di chuyển dừng cho lần reminder đó.
6. Click thường vào pet 3 lần để tắt reminder. Bubble báo số click còn lại.
7. Sau 2 phút, hoặc khi dismiss, window ẩn và timer 25 phút bắt đầu lại.

## 2. Quyết định kỹ thuật cho Windows

### Dùng WPF, .NET 8 (khuyến nghị)

- `WPF` có sẵn window trong suốt (`AllowsTransparency`), window borderless, timer UI và kéo thả.
- Không cần Electron, Python runtime, GTK hay dependency UI bên thứ ba.
- Build được bằng Visual Studio 2022 Community hoặc `.NET SDK 8` trên Windows 10/11.
- Dùng `DispatcherTimer` thay vì `System.Timers.Timer`, vì mọi cập nhật UI chạy đúng UI thread.

Không dùng `GTK/GDK` từ bản Linux: API di chuyển window và transparency phụ thuộc X11/Wayland, không phải hướng triển khai ổn định cho Windows.

## 3. Asset pipeline

WPF mặc định không đọc WebP ổn định nếu không thêm package. Để project Windows không có dependency, export 5 frame PNG trước.

### Input

```text
codex-spritesheet.webp
```

### Output cần có

```text
CodexPet.Windows/
  Assets/
    frame-0.png
    frame-1.png
    frame-2.png
    frame-3.png
    frame-4.png
```

Mỗi frame là crop ở `x = frameIndex × 192`, `y = 4 × 208`, kích thước `192 × 208`.

### Script export frame (chạy một lần trên máy có Python)

```python
from pathlib import Path
from PIL import Image

CELL_WIDTH = 192
CELL_HEIGHT = 208
ROW = 4
FRAME_COUNT = 5

source = Image.open("codex-spritesheet.webp").convert("RGBA")
output = Path("Assets")
output.mkdir(exist_ok=True)

for frame_index in range(FRAME_COUNT):
    left = frame_index * CELL_WIDTH
    top = ROW * CELL_HEIGHT
    frame = source.crop((left, top, left + CELL_WIDTH, top + CELL_HEIGHT))
    frame.save(output / f"frame-{frame_index}.png")
```

Lệnh cài Pillow nếu chưa có:

```powershell
py -m pip install Pillow
py export_frames.py
```

`ponytail:` bản đầu chỉ export 5 frame của row 4. Thêm idle/walk/exercise state machine khi đã xác định ý nghĩa từng row của spritesheet.

## 4. Khởi tạo project Windows

```powershell
dotnet new wpf -n CodexPet.Windows -f net8.0
cd CodexPet.Windows
mkdir Assets
```

Copy 5 file PNG vào `Assets/`, rồi thay các file dưới đây. Không cần cài package NuGet.

## 5. Code mẫu WPF hoàn chỉnh

### `CodexPet.Windows.csproj`

Đảm bảo asset được copy cạnh executable khi chạy/publish:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="Assets\*.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

### `App.xaml`

```xml
<Application x:Class="CodexPet.Windows.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
  <Application.Resources />
</Application>
```

### `MainWindow.xaml`

```xml
<Window x:Class="CodexPet.Windows.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Width="430"
        Height="540"
        WindowStyle="None"
        ResizeMode="NoResize"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="True">
  <Border Background="Transparent"
          MouseLeftButtonDown="Pet_MouseLeftButtonDown"
          MouseMove="Pet_MouseMove"
          MouseLeftButtonUp="Pet_MouseLeftButtonUp">
    <StackPanel Margin="14">
      <TextBlock x:Name="MessageText"
                 Text="Đứng dậy tập thể dục thôi!"
                 FontSize="34"
                 FontWeight="Black"
                 Foreground="White"
                 Background="#172033"
                 Padding="22,14"
                 TextAlignment="Center"
                 TextWrapping="Wrap"
                 HorizontalAlignment="Center" />
      <Image x:Name="PetImage"
             Width="288"
             Height="312"
             Stretch="Fill"
             HorizontalAlignment="Center"
             SnapsToDevicePixels="True"
             RenderOptions.BitmapScalingMode="HighQuality" />
    </StackPanel>
  </Border>
</Window>
```

### `MainWindow.xaml.cs`

```csharp
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CodexPet.Windows;

public partial class MainWindow : Window
{
    private const int IntervalSeconds = 1_500;
    private const int DurationSeconds = 120;
    private const double SpeedPixels = 3;
    private const int ClicksToDismiss = 3;

    private readonly DispatcherTimer reminderTimer = new() { Interval = TimeSpan.FromSeconds(IntervalSeconds) };
    private readonly DispatcherTimer dismissTimer = new() { Interval = TimeSpan.FromSeconds(DurationSeconds) };
    private readonly DispatcherTimer animationTimer = new() { Interval = TimeSpan.FromMilliseconds(140) };
    private readonly DispatcherTimer movementTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly List<BitmapImage> frames;

    private bool isExercising;
    private bool autoMove;
    private bool isDragging;
    private Point dragStart;
    private Point windowStart;
    private int frameIndex;
    private int clickCount;

    public MainWindow()
    {
        InitializeComponent();
        frames = LoadFrames();
        PetImage.Source = frames[0];

        reminderTimer.Tick += (_, _) => StartExercise();
        dismissTimer.Tick += (_, _) => Dismiss();
        animationTimer.Tick += (_, _) => RenderNextFrame();
        movementTimer.Tick += (_, _) => MovePet();

        Loaded += (_, _) =>
        {
            Hide();
            reminderTimer.Start();
        };
    }

    private static List<BitmapImage> LoadFrames()
    {
        var assetDirectory = Path.Combine(AppContext.BaseDirectory, "Assets");
        var loadedFrames = new List<BitmapImage>();

        for (var index = 0; index < 5; index++)
        {
            var framePath = Path.Combine(assetDirectory, $"frame-{index}.png");
            if (!File.Exists(framePath))
            {
                throw new FileNotFoundException("Missing pet animation frame.", framePath);
            }

            loadedFrames.Add(new BitmapImage(new Uri(framePath)));
        }

        return loadedFrames;
    }

    private void StartExercise()
    {
        reminderTimer.Stop();
        dismissTimer.Stop();
        isExercising = true;
        autoMove = true;
        clickCount = 0;
        frameIndex = 0;
        MessageText.Text = "Đứng dậy tập thể dục thôi!";

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left;
        Top = workArea.Bottom - Height;
        Show();
        Activate();
        animationTimer.Start();
        movementTimer.Start();
        dismissTimer.Start();
    }

    private void RenderNextFrame()
    {
        if (!isExercising)
        {
            return;
        }

        PetImage.Source = frames[frameIndex++ % frames.Count];
    }

    private void MovePet()
    {
        if (!isExercising || !autoMove)
        {
            return;
        }

        var workArea = SystemParameters.WorkArea;
        var nextLeft = Left + SpeedPixels;
        Left = nextLeft + Width > workArea.Right ? workArea.Left : nextLeft;
    }

    private void Pet_MouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
    {
        dragStart = eventArgs.GetPosition(null);
        windowStart = new Point(Left, Top);
        isDragging = false;
        Mouse.Capture((IInputElement)sender);
    }

    private void Pet_MouseMove(object sender, MouseEventArgs eventArgs)
    {
        if (eventArgs.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var position = eventArgs.GetPosition(null);
        var delta = position - dragStart;
        if (!isDragging && Math.Abs(delta.X) + Math.Abs(delta.Y) <= 4)
        {
            return;
        }

        isDragging = true;
        autoMove = false;
        Left = windowStart.X + delta.X;
        Top = windowStart.Y + delta.Y;
    }

    private void Pet_MouseLeftButtonUp(object sender, MouseButtonEventArgs eventArgs)
    {
        Mouse.Capture(null);
        if (!isDragging)
        {
            clickCount++;
            var remaining = ClicksToDismiss - clickCount;
            if (remaining <= 0)
            {
                Dismiss();
            }
            else
            {
                MessageText.Text = $"Nhấn thêm {remaining} lần để tắt";
            }
        }
    }

    private void Dismiss()
    {
        if (!isExercising)
        {
            return;
        }

        isExercising = false;
        animationTimer.Stop();
        movementTimer.Stop();
        dismissTimer.Stop();
        Hide();
        reminderTimer.Start();
    }
}
```

## 6. Chạy, kiểm tra và build

Chạy trong lúc phát triển:

```powershell
dotnet run
```

Để test nhanh, tạm đổi trong `MainWindow.xaml.cs`:

```csharp
private const int IntervalSeconds = 10;
private const int DurationSeconds = 8;
```

Checklist thủ công:

1. Sau 10 giây, pet và bubble hiện sát đáy work area.
2. Pet đổi frame liên tục và đi ngang.
3. Pet đến mép phải thì quay lại trái.
4. Kéo pet; pet nằm đúng vị trí mới và không tự đi tiếp trong reminder này.
5. Click 1–2 lần; bubble báo số lần còn lại.
6. Click lần 3; pet ẩn.
7. Hết `DurationSeconds`; pet tự ẩn và hẹn reminder mới.

Build binary self-contained x64:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output nằm tại:

```text
bin\Release\net8.0-windows\win-x64\publish\
```

## 7. Tự khởi động khi đăng nhập

Với bản MVP, không cần Windows Service. Tạo shortcut của executable trong Startup folder:

```powershell
$exe = "C:\duong-dan\CodexPet.Windows.exe"
$startup = [Environment]::GetFolderPath('Startup')
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path $startup 'Codex Pet.lnk'))
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = Split-Path $exe
$shortcut.Save()
```

Sau đó đăng xuất/đăng nhập lại để kiểm tra. App chạy theo user session, nên UI transparent/topmost hoạt động đúng.

## 8. Lưu ý Windows cần xử lý sau MVP

- **Multi-monitor:** `SystemParameters.WorkArea` chỉ lấy primary monitor. Nếu cần, dùng `Screen.FromHandle(new WindowInteropHelper(this).Handle).WorkingArea` từ `System.Windows.Forms` để đi theo màn hình hiện tại.
- **DPI scaling:** test ở 100%, 125% và 150%. Nếu sprite bị mờ, đổi `RenderOptions.BitmapScalingMode` sang `NearestNeighbor` để có pixel-art sắc hơn.
- **Taskbar:** `SystemParameters.WorkArea` đã tránh taskbar ở màn hình chính.
- **Click-through:** hiện tại pet nhận chuột để drag/click. Không bật click-through trước khi có shortcut/hotkey để mở lại pet.
- **Sleep/hibernate:** MVP tính interval theo timer sống. Nếu cần nhắc theo thời gian thực sau khi máy wake, lưu `DateTimeOffset nextReminderAt` và đối chiếu khi app được kích hoạt.
- **Accessibility:** bổ sung nút close/exit trong tray icon trước khi phát hành nội bộ. MVP chưa có tray icon để giữ code tối thiểu.

## 9. Reference code của bản Linux hiện tại

File gốc: `iva-api-server/codex_pet_widget.py`.

```python
#!/usr/bin/env python3
"""Standalone exercise reminder rendered from Codex's original pet spritesheet."""

import argparse
from pathlib import Path

import gi

gi.require_version("Gtk", "3.0")
gi.require_version("GdkPixbuf", "2.0")
gi.require_version("Gdk", "3.0")
from gi.repository import Gdk, GLib, GdkPixbuf, Gtk

CELL_WIDTH = 192
CELL_HEIGHT = 208
SPRITESHEET = Path(__file__).with_name("codex-spritesheet.webp")


class CodexPetWidget:
    def __init__(self, interval_seconds: int, duration_seconds: int):
        if not SPRITESHEET.exists():
            raise SystemExit(f"Missing spritesheet: {SPRITESHEET}")

        self.interval_ms = interval_seconds * 1000
        self.duration_ms = duration_seconds * 1000
        self.frame_index = 0
        self.is_exercising = False
        self.click_count = 0
        self.velocity_x = 3
        self.auto_move = True
        self.drag_origin = None
        self.dragged = False
        self.source = GdkPixbuf.Pixbuf.new_from_file(str(SPRITESHEET))

        self.window = Gtk.Window(type=Gtk.WindowType.POPUP)
        rgba_visual = self.window.get_screen().get_rgba_visual()
        if rgba_visual is not None:
            self.window.set_visual(rgba_visual)
        self.window.set_keep_above(True)
        self.window.set_decorated(False)
        self.window.set_resizable(False)
        self.window.set_app_paintable(True)
        self.window.set_default_size(430, 540)

        self.box = Gtk.Box(orientation=Gtk.Orientation.VERTICAL, spacing=4)
        self.message = Gtk.Label()
        self.message.set_use_markup(True)
        self.message.set_markup("<span weight='heavy' size='xx-large'>Đứng dậy tập thể dục thôi!</span>")
        self.box.pack_start(self.message, False, False, 0)

        self.pet_image = Gtk.Image()
        self.box.pack_start(self.pet_image, True, True, 0)
        self.events = Gtk.EventBox()
        self.events.set_visible_window(False)
        self.events.add_events(
            Gdk.EventMask.BUTTON_PRESS_MASK
            | Gdk.EventMask.BUTTON_RELEASE_MASK
            | Gdk.EventMask.POINTER_MOTION_MASK
        )
        self.events.connect("button-press-event", self.on_press)
        self.events.connect("button-release-event", self.on_release)
        self.events.connect("motion-notify-event", self.on_motion)
        self.events.add(self.box)
        self.window.add(self.events)

        GLib.timeout_add(self.interval_ms, self.start_exercise)

    def get_frame(self, row: int, column: int, scale: float):
        frame = GdkPixbuf.Pixbuf.new(
            self.source.get_colorspace(), self.source.get_has_alpha(),
            self.source.get_bits_per_sample(), CELL_WIDTH, CELL_HEIGHT,
        )
        self.source.copy_area(column * CELL_WIDTH, row * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT, frame, 0, 0)
        return frame if scale == 1 else frame.scale_simple(
            round(CELL_WIDTH * scale), round(CELL_HEIGHT * scale), GdkPixbuf.InterpType.HYPER,
        )

    def render(self):
        if not self.is_exercising:
            return False
        self.pet_image.set_from_pixbuf(self.get_frame(4, self.frame_index % 5, 1.5))
        self.frame_index += 1
        return True

    def on_press(self, _, event):
        self.drag_origin = (*self.window.get_position(), event.x_root, event.y_root)
        self.dragged = False
        return True

    def on_motion(self, _, event):
        if self.drag_origin is None:
            return False
        start_x, start_y, pointer_x, pointer_y = self.drag_origin
        delta_x, delta_y = event.x_root - pointer_x, event.y_root - pointer_y
        if abs(delta_x) + abs(delta_y) > 4:
            self.dragged = True
            self.auto_move = False
            self.window.move(int(start_x + delta_x), int(start_y + delta_y))
        return True

    def on_release(self, _, __):
        if not self.dragged:
            self.click_count += 1
            remaining = 3 - self.click_count
            if remaining == 0:
                self.dismiss()
            else:
                self.message.set_markup(f"<span weight='heavy' size='xx-large'>Nhấn thêm {remaining} lần để tắt</span>")
        self.drag_origin = None
        return True

    def move(self):
        if not self.is_exercising or not self.auto_move:
            return False
        screen = self.window.get_screen()
        screen_width, _ = screen.get_width(), screen.get_height()
        window_width, _ = self.window.get_size()
        current_x, current_y = self.window.get_position()
        next_x = current_x + self.velocity_x
        self.window.move(0 if next_x + window_width > screen_width else next_x, current_y)
        return True

    def start_exercise(self):
        self.is_exercising = True
        self.frame_index = self.click_count = 0
        self.auto_move = True
        self.message.set_markup("<span weight='heavy' size='xx-large'>Đứng dậy tập thể dục thôi!</span>")
        screen = self.window.get_screen()
        self.window.move(0, screen.get_height() - 560)
        self.window.show_all()
        GLib.timeout_add(140, self.render)
        GLib.timeout_add(50, self.move)
        GLib.timeout_add(self.duration_ms, self.dismiss)
        return False

    def dismiss(self):
        if not self.is_exercising:
            return False
        self.is_exercising = False
        self.window.hide()
        GLib.timeout_add(self.interval_ms, self.start_exercise)
        return False

    def run(self):
        Gtk.main()


def main():
    parser = argparse.ArgumentParser(description="Codex pet exercise reminder")
    parser.add_argument("--interval", type=int, default=1500)
    parser.add_argument("--duration", type=int, default=120)
    args = parser.parse_args()
    if args.interval < 1 or args.duration < 1:
        raise SystemExit("--interval and --duration must be positive")
    CodexPetWidget(args.interval, args.duration).run()


if __name__ == "__main__":
    main()
```

## 10. Phạm vi bàn giao

Done cho MVP: transparent floating pet, timer, animation, movement, drag, 3-click dismiss, publish Windows và startup shortcut.

Skipped: tray menu, exit control, multi-monitor tracking đầy đủ, persist cấu hình, notification native và state machine nhiều animation. Thêm khi app được dùng hằng ngày hoặc phát hành cho nhiều người.
