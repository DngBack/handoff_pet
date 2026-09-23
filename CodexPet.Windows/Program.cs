using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CodexPet.Windows
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            bool firstInstance;
            using (var mutex = new System.Threading.Mutex(true, "Local\\CodexPetExerciseReminder", out firstInstance))
            {
                if (!firstInstance) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PetContext(args));
            }
        }
    }

    internal sealed class PetContext : ApplicationContext
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "CodexPetReminder";
        private readonly Timer reminderTimer = new Timer();
        private readonly Timer dismissTimer = new Timer();
        private readonly Timer animationTimer = new Timer();
        private readonly Timer movementTimer = new Timer();
        private readonly Timer countdownTimer = new Timer();
        private readonly Timer startupTimer = new Timer();
        private readonly NotifyIcon tray;
        private readonly MenuItem startupItem;
        private readonly PetForm pet = new PetForm();
        private PetSettings settings;
        private DateTime hideAtUtc;
        private bool showing;
        private bool autoMove;
        private int clicks;
        private int frame;

        public PetContext(string[] args)
        {
            settings = PetSettings.Load();
            ApplyTimerSettings();
            animationTimer.Interval = 140;
            movementTimer.Interval = 50;
            countdownTimer.Interval = 1000;
            startupTimer.Interval = 100;
            reminderTimer.Tick += delegate { ShowReminder(); };
            dismissTimer.Tick += delegate { Dismiss(); };
            animationTimer.Tick += delegate { frame = (frame + 1) % 5; pet.AnimationFrame = frame; };
            movementTimer.Tick += delegate { MovePet(); };
            countdownTimer.Tick += delegate { UpdateCountdown(); };
            pet.PetClicked += delegate { HandleClick(); };
            pet.PetDragged += delegate { autoMove = false; };

            startupItem = new MenuItem("Tự chạy khi đăng nhập");
            startupItem.Checked = settings.StartupEnabled;
            startupItem.Click += delegate
            {
                startupItem.Checked = !startupItem.Checked;
                settings.StartupEnabled = startupItem.Checked;
                settings.Save();
                ApplyStartupPreference(startupItem.Checked);
            };
            var menu = new ContextMenu(new MenuItem[]
            {
                new MenuItem("Nhắc ngay", delegate { ShowReminder(); }),
                new MenuItem("Cài đặt...", delegate { OpenSettings(); }),
                startupItem,
                new MenuItem("Thoát", delegate { ExitThread(); })
            });
            tray = new NotifyIcon();
            tray.Icon = SystemIcons.Information;
            tray.Text = "Codex Pet - nhắc vận động";
            tray.ContextMenu = menu;
            tray.DoubleClick += delegate { OpenSettings(); };
            tray.Visible = true;

            ApplyStartupPreference(startupItem.Checked);
            bool showNow = false;
            bool openSettings = false;
            foreach (string arg in args)
            {
                if (string.Equals(arg, "--show-now", StringComparison.OrdinalIgnoreCase)) showNow = true;
                if (string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase)) openSettings = true;
            }
            startupTimer.Tick += delegate
            {
                startupTimer.Stop();
                if (showNow) ShowReminder();
                if (openSettings) OpenSettings();
            };
            if (showNow || openSettings) startupTimer.Start();
            if (!showNow) reminderTimer.Start();
        }

        private void ApplyTimerSettings()
        {
            reminderTimer.Interval = settings.IntervalMinutes * 60 * 1000;
            dismissTimer.Interval = settings.DurationSeconds * 1000;
        }

        private void OpenSettings()
        {
            using (SettingsForm dialog = new SettingsForm(settings))
            {
                dialog.TopMost = true;
                if (dialog.ShowDialog() != DialogResult.OK) return;
                settings = dialog.ResultSettings;
                settings.Save();
                startupItem.Checked = settings.StartupEnabled;
                ApplyStartupPreference(settings.StartupEnabled);
                ApplyTimerSettings();
                if (showing)
                {
                    pet.Message = settings.Message;
                    hideAtUtc = DateTime.UtcNow.AddSeconds(settings.DurationSeconds);
                    pet.RemainingSeconds = settings.DurationSeconds;
                    dismissTimer.Stop();
                    dismissTimer.Start();
                }
                else
                {
                    reminderTimer.Stop();
                    reminderTimer.Start();
                }
            }
        }

        private static void ApplyStartupPreference(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (enabled) key.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\"", RegistryValueKind.String);
                else key.DeleteValue(RunValueName, false);
            }
        }

        private void ShowReminder()
        {
            reminderTimer.Stop();
            dismissTimer.Stop();
            animationTimer.Stop();
            movementTimer.Stop();
            countdownTimer.Stop();
            startupTimer.Stop();
            showing = true;
            autoMove = settings.SpeedPixels > 0;
            clicks = 0;
            frame = 0;
            pet.AnimationFrame = 0;
            pet.Message = settings.Message;
            pet.RemainingClicks = 3;
            pet.RemainingSeconds = settings.DurationSeconds;
            hideAtUtc = DateTime.UtcNow.AddSeconds(settings.DurationSeconds);
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            pet.Location = new Point(workArea.Left + 8, workArea.Bottom - pet.Height);
            pet.Show();
            pet.BringToFront();
            animationTimer.Start();
            movementTimer.Start();
            countdownTimer.Start();
            dismissTimer.Start();
        }

        private void MovePet()
        {
            if (!showing || !autoMove) return;
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            int next = pet.Left + settings.SpeedPixels;
            pet.Left = next + pet.Width > workArea.Right ? workArea.Left + 8 : next;
        }

        private void HandleClick()
        {
            if (!showing) return;
            clicks++;
            if (clicks >= 3) Dismiss();
            else pet.RemainingClicks = 3 - clicks;
        }

        private void UpdateCountdown()
        {
            if (!showing) return;
            int remaining = (int)Math.Ceiling((hideAtUtc - DateTime.UtcNow).TotalSeconds);
            if (remaining <= 0) Dismiss();
            else pet.RemainingSeconds = remaining;
        }

        private void Dismiss()
        {
            if (!showing) return;
            showing = false;
            animationTimer.Stop();
            movementTimer.Stop();
            countdownTimer.Stop();
            dismissTimer.Stop();
            pet.Hide();
            reminderTimer.Start();
        }

        protected override void ExitThreadCore()
        {
            reminderTimer.Stop();
            dismissTimer.Stop();
            animationTimer.Stop();
            movementTimer.Stop();
            countdownTimer.Stop();
            startupTimer.Stop();
            tray.Visible = false;
            tray.Dispose();
            pet.Dispose();
            reminderTimer.Dispose();
            dismissTimer.Dispose();
            animationTimer.Dispose();
            movementTimer.Dispose();
            countdownTimer.Dispose();
            startupTimer.Dispose();
            base.ExitThreadCore();
        }
    }

    internal sealed class PetForm : Form
    {
        private readonly Bitmap[] sprites = new Bitmap[5];
        private readonly Rectangle spriteBounds = new Rectangle(26, 168, 288, 288);
        private bool pressed;
        private bool dragged;
        private Point pointerStart;
        private Point windowStart;
        private string message = "Đứng dậy tập thể dục thôi!";
        private int remainingClicks = 3;
        private int remainingSeconds = 120;
        private int animationFrame;

        public event EventHandler PetClicked;
        public event EventHandler PetDragged;

        public string Message
        {
            set { message = value; Invalidate(); }
        }

        public int AnimationFrame
        {
            set { animationFrame = value; Invalidate(new Rectangle(24, 160, 292, 300)); }
        }

        public int RemainingClicks
        {
            set { remainingClicks = value; Invalidate(new Rectangle(6, 115, 328, 40)); }
        }

        public int RemainingSeconds
        {
            set { remainingSeconds = value; Invalidate(new Rectangle(6, 115, 328, 40)); }
        }

        public PetForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            ClientSize = new Size(340, 476);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            for (int i = 0; i < sprites.Length; i++) sprites[i] = PixelPet.Create(i);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(Color.Magenta);
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            Rectangle bubble = new Rectangle(6, 6, 328, 146);
            using (Brush outline = new SolidBrush(Color.FromArgb(21, 47, 59)))
            using (Brush paper = new SolidBrush(Color.FromArgb(255, 251, 234)))
            using (Brush accent = new SolidBrush(Color.FromArgb(40, 184, 169)))
            using (Font titleFont = new Font("Segoe UI", 17, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font messageFont = new Font("Segoe UI", 21, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font helpFont = new Font("Segoe UI", 13, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                g.FillRectangle(outline, bubble);
                g.FillRectangle(paper, 10, 10, 320, 138);
                g.FillRectangle(accent, 10, 10, 320, 35);
                TextRenderer.DrawText(g, "CODEX PET  ·  NHẮC VẬN ĐỘNG", titleFont,
                    new Rectangle(20, 15, 300, 25), Color.FromArgb(21, 47, 59),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, message, messageFont, new Rectangle(20, 53, 300, 63),
                    Color.FromArgb(21, 47, 59), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                string footer = string.Format("{0}:{1:00} còn lại  ·  Nhấn {2} lần để ẩn",
                    remainingSeconds / 60, remainingSeconds % 60, remainingClicks);
                TextRenderer.DrawText(g, footer, helpFont, new Rectangle(20, 122, 300, 20),
                    Color.FromArgb(67, 88, 93), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                Point[] tail = new Point[] { new Point(154, 152), new Point(186, 152), new Point(170, 166) };
                g.FillPolygon(outline, tail);
            }

            int bob = animationFrame == 2 ? -6 : (animationFrame == 1 || animationFrame == 3 ? -3 : 0);
            Rectangle target = new Rectangle(spriteBounds.Left, spriteBounds.Top + bob, spriteBounds.Width, spriteBounds.Height);
            g.DrawImage(sprites[animationFrame], target, new Rectangle(0, 0, 48, 48), GraphicsUnit.Pixel);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int bob = animationFrame == 2 ? -6 : (animationFrame == 1 || animationFrame == 3 ? -3 : 0);
            Rectangle hitBounds = new Rectangle(spriteBounds.Left, spriteBounds.Top + bob, spriteBounds.Width, spriteBounds.Height);
            if (e.Button != MouseButtons.Left || !hitBounds.Contains(e.Location)) return;
            int px = (e.X - spriteBounds.Left) * 48 / spriteBounds.Width;
            int py = (e.Y - hitBounds.Top) * 48 / hitBounds.Height;
            if (px < 0 || px >= 48 || py < 0 || py >= 48 || sprites[animationFrame].GetPixel(px, py).A == 0) return;
            pressed = true;
            dragged = false;
            pointerStart = Cursor.Position;
            windowStart = Location;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!pressed || (Control.MouseButtons & MouseButtons.Left) == 0) return;
            Point current = Cursor.Position;
            int dx = current.X - pointerStart.X;
            int dy = current.Y - pointerStart.Y;
            if (!dragged && Math.Abs(dx) + Math.Abs(dy) <= 4) return;
            if (!dragged)
            {
                dragged = true;
                if (PetDragged != null) PetDragged(this, EventArgs.Empty);
            }
            Rectangle area = Screen.FromPoint(current).WorkingArea;
            int x = Math.Max(area.Left, Math.Min(area.Right - Width, windowStart.X + dx));
            int y = Math.Max(area.Top, Math.Min(area.Bottom - Height, windowStart.Y + dy));
            Location = new Point(x, y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left || !pressed) return;
            bool wasDragged = dragged;
            pressed = false;
            dragged = false;
            Capture = false;
            if (!wasDragged && PetClicked != null) PetClicked(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (Bitmap sprite in sprites) sprite.Dispose();
            base.Dispose(disposing);
        }
    }

    internal static class PixelPet
    {
        private static readonly Color Ink = Color.FromArgb(22, 47, 58);
        private static readonly Color Fur = Color.FromArgb(40, 184, 169);
        private static readonly Color Light = Color.FromArgb(255, 251, 234);
        private static readonly Color Cheek = Color.FromArgb(255, 151, 151);

        public static Bitmap Create(int phase)
        {
            Bitmap sprite = new Bitmap(48, 48, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(sprite))
            using (Brush ink = new SolidBrush(Ink))
            using (Brush fur = new SolidBrush(Fur))
            using (Brush light = new SolidBrush(Light))
            using (Brush cheek = new SolidBrush(Cheek))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.None;

                // Tail and ears sit behind the face.
                g.FillRectangle(ink, 34, 30, 7, 5);
                g.FillRectangle(ink, 39, 24, 5, 10);
                g.FillRectangle(ink, 43, 19, 4, 10);
                g.FillRectangle(fur, 36, 30, 5, 3);
                g.FillRectangle(fur, 41, 25, 2, 7);
                g.FillRectangle(fur, 44, 21, 2, 6);
                g.FillPolygon(ink, new Point[] { new Point(8, 18), new Point(7, 2), new Point(11, 1), new Point(19, 15) });
                g.FillPolygon(ink, new Point[] { new Point(29, 15), new Point(37, 1), new Point(41, 2), new Point(40, 18) });
                g.FillPolygon(fur, new Point[] { new Point(10, 16), new Point(9, 5), new Point(12, 4), new Point(17, 16) });
                g.FillPolygon(fur, new Point[] { new Point(31, 16), new Point(36, 4), new Point(39, 5), new Point(38, 16) });
                g.FillRectangle(cheek, 11, 9, 3, 5);
                g.FillRectangle(cheek, 34, 9, 3, 5);

                // Body, arms and feet are drawn on the same coarse pixel grid.
                g.FillRectangle(ink, 13, 29, 22, 14);
                g.FillRectangle(fur, 15, 30, 18, 11);
                g.FillRectangle(ink, 8, 32, 7, 7);
                g.FillRectangle(ink, 33, 32, 7, 7);
                g.FillRectangle(fur, 9, 33, 5, 5);
                g.FillRectangle(fur, 34, 33, 5, 5);
                int step = (phase == 1 || phase == 3) ? 2 : 0;
                g.FillRectangle(ink, 13 - step, 40, 10, 5);
                g.FillRectangle(ink, 26 + step, 40, 10, 5);
                g.FillRectangle(fur, 15 - step, 40, 7, 3);
                g.FillRectangle(fur, 27 + step, 40, 7, 3);

                // Head and face.
                g.FillRectangle(ink, 10, 13, 28, 24);
                g.FillRectangle(ink, 8, 17, 32, 16);
                g.FillRectangle(fur, 11, 15, 26, 20);
                g.FillRectangle(fur, 9, 19, 30, 12);
                g.FillRectangle(light, 13, 20, 22, 14);
                g.FillRectangle(light, 11, 23, 26, 8);
                g.FillRectangle(ink, 17, 23, 3, 5);
                g.FillRectangle(ink, 28, 23, 3, 5);
                g.FillRectangle(light, 18, 23, 1, 1);
                g.FillRectangle(light, 29, 23, 1, 1);
                g.FillRectangle(cheek, 11, 29, 6, 3);
                g.FillRectangle(cheek, 31, 29, 6, 3);
                g.FillRectangle(ink, 23, 29, 2, 2);
                g.FillRectangle(ink, 20, 32, 3, 1);
                g.FillRectangle(ink, 25, 32, 3, 1);
                g.FillRectangle(light, 21, 36, 6, 2);
            }
            return sprite;
        }
    }
}
