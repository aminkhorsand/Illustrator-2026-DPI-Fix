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
        MaximumSize = new Size(650, 0),
        Text = "ابتدا یک تصویر انتخاب کنید."
    };

    private string? sourcePath;

    public MainForm()
    {
        Text = "AI Export Fixer Pro";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(760, 390);
        MinimumSize = new Size(650, 330);
        Font = new Font("Segoe UI", 10);

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

        layout.Controls.Add(
            new Label
            {
                Text = "DPI خروجی:",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 2);

        layout.Controls.Add(dpiInput, 1, 2);

        var saveButton = new Button
        {
            Text = "ذخیره و بررسی DPI",
            AutoSize = true,
            Padding = new Padding(
