using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodexPet.Windows
{
    internal sealed class SettingsForm : Form
    {
        private readonly NumericUpDown interval = Number(1, 180);
        private readonly NumericUpDown duration = Number(10, 600);
        private readonly NumericUpDown speed = Number(0, 20);
        private readonly TextBox message = new TextBox();
        private readonly CheckBox startup = new CheckBox();

        public PetSettings ResultSettings { get; private set; }

        public SettingsForm(PetSettings current)
        {
            Text = "Cài đặt Codex Pet";
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(520, 520);
            MinimumSize = MaximumSize = Size;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 248, 244);
            AutoScaleMode = AutoScaleMode.Dpi;

            Panel header = new Panel();
            header.Bounds = new Rectangle(0, 0, 520, 86);
            header.BackColor = Color.FromArgb(21, 47, 59);
            Controls.Add(header);
            header.Controls.Add(Label("Cài đặt Codex Pet", 24, 15, 470, 32, 22, FontStyle.Bold, Color.White));
            header.Controls.Add(Label("Điều chỉnh lời nhắc cho giờ làm việc của bạn", 25, 52, 470, 22, 10, FontStyle.Regular, Color.FromArgb(207, 238, 229)));

            Panel card = new Panel();
            card.Bounds = new Rectangle(24, 106, 472, 322);
            card.BackColor = Color.White;
            card.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(card);

            AddRow(card, 18, "Nhắc mỗi", "Thời gian giữa hai lần nhắc", interval, "phút");
            AddRow(card, 78, "Hiển thị trong", "Pet tự ẩn khi hết thời gian", duration, "giây");
            AddRow(card, 138, "Tốc độ di chuyển", "0 = đứng yên", speed, "px / 50 ms");

            card.Controls.Add(Label("Lời nhắc", 18, 202, 185, 24, 11, FontStyle.Bold, Color.FromArgb(21, 47, 59)));
            message.Bounds = new Rectangle(18, 230, 430, 29);
            message.MaxLength = 80;
            card.Controls.Add(message);

            startup.Bounds = new Rectangle(18, 274, 420, 30);
            startup.Text = "Tự chạy khi đăng nhập Windows";
            startup.ForeColor = Color.FromArgb(21, 47, 59);
            card.Controls.Add(startup);

            Button reset = Button("Mặc định", 24, 449, 105, false);
            reset.Click += delegate { LoadValues(PetSettings.Defaults()); };
            Controls.Add(reset);

            Button cancel = Button("Hủy", 276, 449, 96, false);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);

            Button save = Button("Lưu cài đặt", 382, 449, 114, true);
            save.Click += delegate { SaveValues(); };
            Controls.Add(save);
            AcceptButton = save;
            CancelButton = cancel;

            Controls.Add(Label("Sau khi lưu, chu kỳ chờ sẽ tính lại từ đầu.", 24, 489, 472, 20, 9, FontStyle.Regular, Color.FromArgb(87, 102, 106)));
            LoadValues(current);
        }

        private static NumericUpDown Number(int min, int max)
        {
            NumericUpDown control = new NumericUpDown();
            control.Minimum = min;
            control.Maximum = max;
            control.TextAlign = HorizontalAlignment.Right;
            return control;
        }

        private static Label Label(string text, int x, int y, int width, int height, float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.Bounds = new Rectangle(x, y, width, height);
            label.Font = new Font("Segoe UI", size, style);
            label.ForeColor = color;
            return label;
        }

        private static Button Button(string text, int x, int y, int width, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.Bounds = new Rectangle(x, y, width, 38);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = primary ? Color.FromArgb(40, 184, 169) : Color.White;
            button.ForeColor = Color.FromArgb(21, 47, 59);
            button.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            return button;
        }

        private static void AddRow(Panel parent, int y, string title, string help, NumericUpDown control, string unit)
        {
            parent.Controls.Add(Label(title, 18, y, 220, 23, 11, FontStyle.Bold, Color.FromArgb(21, 47, 59)));
            parent.Controls.Add(Label(help, 18, y + 25, 220, 21, 9, FontStyle.Regular, Color.FromArgb(87, 102, 106)));
            control.Bounds = new Rectangle(280, y + 4, 90, 28);
            parent.Controls.Add(control);
            parent.Controls.Add(Label(unit, 379, y + 7, 72, 22, 9, FontStyle.Regular, Color.FromArgb(87, 102, 106)));
        }

        private void LoadValues(PetSettings values)
        {
            interval.Value = values.IntervalMinutes;
            duration.Value = values.DurationSeconds;
            speed.Value = values.SpeedPixels;
            message.Text = values.Message;
            startup.Checked = values.StartupEnabled;
        }

        private void SaveValues()
        {
            string text = message.Text.Trim();
            if (text.Length == 0)
            {
                MessageBox.Show(this, "Nhập nội dung lời nhắc hoặc chọn Mặc định.", "Thiếu lời nhắc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                message.Focus();
                return;
            }
            ResultSettings = new PetSettings
            {
                IntervalMinutes = (int)interval.Value,
                DurationSeconds = (int)duration.Value,
                SpeedPixels = (int)speed.Value,
                Message = text,
                StartupEnabled = startup.Checked
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
