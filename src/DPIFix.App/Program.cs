
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ImageMagick;

namespace DPIFix.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly NumericUpDown dpiInput;
        private readonly Label statusLabel;
        private string? sourcePath;

        public MainForm()
        {
            Text = "AI Export Fixer Pro";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(720, 390);
            MinimumSize = new Size(650, 350);
            Font = new Font("Segoe UI", 10);

            dpiInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 2400,
                Value = 300,
                Width = 120
            };

            statusLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(650, 0),
                Text = "ابتدا یک تصویر انتخاب کنید."
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 2,
                RowCount = 5
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 190));

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 50));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 50));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 60));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            var heading = new Label
            {
                Text = "AI Export Fixer Pro",
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold)
            };

            layout.Controls.Add(heading, 0, 0);
            layout.SetColumnSpan(heading, 2);

            var openButton = new Button
            {
                Text = "انتخاب تصویر",
                AutoSize = true,
                Padding = new Padding(8)
            };

            openButton.Click += (_, _) => SelectImage();
            layout.Controls.Add(openButton, 0, 1);

            var dpiLabel = new Label
            {
                Text = "DPI خروجی:",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            layout.Controls.Add(dpiLabel, 0, 2);
            layout.Controls.Add(dpiInput, 1, 2);

            var saveButton = new Button
            {
                Text = "ذخیره و بررسی DPI",
                AutoSize = true,
                Padding = new Padding(8)
            };

            saveButton.Click += (_, _) => SaveImage();
            layout.Controls.Add(saveButton, 0, 3);

            statusLabel.Padding = new Padding(0, 10, 0, 0);
            layout.Controls.Add(statusLabel, 0, 4);
            layout.SetColumnSpan(statusLabel, 2);

            Controls.Add(layout);
        }

        private void SelectImage()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "انتخاب تصویر",
                Filter =
                    "Image files|*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.bmp;*.webp"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                using var image = new MagickImage(dialog.FileName);

                sourcePath = dialog.FileName;

                statusLabel.Text =
                    $"فایل: {Path.GetFileName(sourcePath)}\n" +
                    $"ابعاد پیکسلی: {image.Width} × {image.Height}\n" +
                    $"DPI خوانده‌شده: " +
                    $"{image.Density.X:0.##} × " +
                    $"{image.Density.Y:0.##}\n" +
                    "فایل اصلی بدون تغییر باقی می‌ماند.";
            }
            catch (Exception ex)
            {
                sourcePath = null;

                MessageBox.Show(
                    this,
                    ex.Message,
                    "خطا در خواندن تصویر",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SaveImage()
        {
            if (string.IsNullOrWhiteSpace(sourcePath) ||
                !File.Exists(sourcePath))
            {
                MessageBox.Show(
                    this,
                    "ابتدا یک تصویر انتخاب کنید.",
                    "توجه",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "ذخیره تصویر اصلاح‌شده",
                FileName =
                    Path.GetFileNameWithoutExtension(sourcePath) +
                    $"_{dpiInput.Value}dpi.jpg",
                Filter =
                    "JPEG image|*.jpg|" +
                    "PNG image|*.png|" +
                    "TIFF image|*.tiff",
                DefaultExt = "jpg",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                double targetDpi = (double)dpiInput.Value;
                string extension =
                    Path.GetExtension(dialog.FileName).ToLowerInvariant();

                MagickFormat outputFormat = extension switch
                {
                    ".jpg" or ".jpeg" => MagickFormat.Jpeg,
                    ".png" => MagickFormat.Png,
                    ".tif" or ".tiff" => MagickFormat.Tiff,
                    _ => throw new InvalidOperationException(
                        "فرمت خروجی پشتیبانی نمی‌شود.")
                };

                ulong originalWidth;
                ulong originalHeight;

                using (var image = new MagickImage(sourcePath))
                {
                    originalWidth = image.Width;
                    originalHeight = image.Height;

                    // فقط فرادادهٔ چگالی تغییر می‌کند.
                    // هیچ Resize یا تغییر ابعاد پیکسلی انجام نمی‌شود.
                    image.Density = new Density(
                        targetDpi,
                        targetDpi,
                        DensityUnit.PixelsPerInch);

                    image.Format = outputFormat;
                    image.Write(dialog.FileName);
                }

                // فایل ذخیره‌شده را دوباره از دیسک می‌خوانیم.
                using var check = new MagickImage(dialog.FileName);

                bool dimensionsMatch =
                    check.Width == originalWidth &&
                    check.Height == originalHeight;

                bool dpiMatches =
                    Math.Abs(check.Density.X - targetDpi) < 0.5 &&
                    Math.Abs(check.Density.Y - targetDpi) < 0.5;

                double printWidthCm =
                    check.Width / targetDpi * 2.54;

                double printHeightCm =
                    check.Height / targetDpi * 2.54;

                statusLabel.Text =
                    $"فایل خروجی: {Path.GetFileName(dialog.FileName)}\n" +
                    $"ابعاد پیکسلی: {check.Width} × {check.Height}\n" +
                    $"DPI خوانده‌شده: " +
                    $"{check.Density.X:0.##} × " +
                    $"{check.Density.Y:0.##}\n" +
                    $"ابعاد پیکسلی حفظ شده: " +
                    $"{(dimensionsMatch ? "بله" : "خیر")}\n" +
                    $"DPI تأیید شده: {(dpiMatches ? "بله" : "خیر")}\n" +
                    $"اندازه چاپ محاسبه‌شده: " +
                    $"{printWidthCm:0.##} × " +
                    $"{printHeightCm:0.##} سانتی‌متر";

                if (dimensionsMatch && dpiMatches)
                {
                    MessageBox.Show(
                        this,
                        "فایل ذخیره شد و DPI آن پس از ذخیره دوباره بررسی شد.\n\n" +
                        "اکنون فایل خروجی را در سایت مقصد آزمایش کنید.",
                        "بررسی موفق",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        this,
                        "بررسی خروجی موفق نبود.\n" +
                        "مقادیر گزارش‌شده را بررسی کنید و این فایل را " +
                        "تأییدشده تلقی نکنید.",
                        "نیاز به بررسی",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "خطا در ذخیره یا بررسی تصویر:\n\n" + ex.Message,
                    "خطا",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
