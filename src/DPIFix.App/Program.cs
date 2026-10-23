using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ImageMagick;

namespace DPIFix.App;

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
    private readonly NumericUpDown dpiInput = new()
    {
        Minimum = 1,
        Maximum = 2400,
        Value = 300,
        Width = 100
    };

    private readonly Label statusLabel = new()
    {
        AutoSize = true,
        MaximumSize = new Size(620, 0),
        Text = "یک تصویر انتخاب کنید."
    };

    private string? sourcePath;

    public MainForm()
    {
        Text = "AI Export Fixer Pro";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(620, 300);
        Size = new Size(720, 360);
        Font = new Font("Segoe UI", 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = false
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

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
            Text = "ذخیره با DPI انتخابی",
            AutoSize = true,
            Padding = new Padding(8)
        };
        saveButton.Click += (_, _) => SaveImage();
        layout.Controls.Add(saveButton, 0, 3);

        statusLabel.Padding = new Padding(0, 12, 0, 0);
        layout.Controls.Add(statusLabel, 0, 4);
        layout.SetColumnSpan(statusLabel, 2);

        Controls.Add(layout);
    }

    private void SelectImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "انتخاب تصویر",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.bmp;*.webp|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var image = new MagickImage(dialog.FileName);
            sourcePath = dialog.FileName;

            statusLabel.Text =
                $"فایل: {Path.GetFileName(sourcePath)}\n" +
                $"ابعاد: {image.Width} × {image.Height} پیکسل\n" +
                $"DPI فعلی: {image.Density.X:0.##} × {image.Density.Y:0.##}\n" +
                "توجه: تغییر DPI به‌تنهایی تعداد پیکسل‌ها را افزایش نمی‌دهد.";
        }
        catch (Exception ex)
        {
            sourcePath = null;
            MessageBox.Show(this, ex.Message, "خطا در باز کردن تصویر",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveImage()
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            MessageBox.Show(this, "ابتدا یک تصویر انتخاب کنید.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "ذخیره تصویر اصلاح‌شده",
            FileName = Path.GetFileNameWithoutExtension(sourcePath) + "_300dpi.jpg",
            Filter = "JPEG image|*.jpg|PNG image|*.png|TIFF image|*.tiff|BMP image|*.bmp|WebP image|*.webp",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using (var image = new MagickImage(sourcePath))
            {
                double targetDpi = (double)dpiInput.Value;
                image.Density = new Density(
                    targetDpi, targetDpi, DensityUnit.PixelsPerInch);

                image.Write(dialog.FileName);
            }

            using var check = new MagickImage(dialog.FileName);

            statusLabel.Text =
                $"ذخیره شد: {Path.GetFileName(dialog.FileName)}\n" +
                $"ابعاد خروجی: {check.Width} × {check.Height} پیکسل\n" +
                $"DPI خوانده‌شده: {check.Density.X:0.##} × {check.Density.Y:0.##}\n" +
                "برای اطمینان از سازگاری با سایت مقصد، فایل خروجی را همان‌جا آزمایش کنید.";

            MessageBox.Show(this,
                "فایل خروجی ذخیره شد. مقدار DPI دوباره از فایل خوانده شد؛ " +
                "با این حال، پذیرش آن توسط همهٔ سایت‌ها تضمین‌شده نیست.",
                "پایان عملیات", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "خطا در ذخیره تصویر",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
