using System;
using System.Drawing;
using System.Windows.Forms;

class PerspectiveWindow : Form
{
    readonly Rasterization rasterization = new Rasterization(640, 480);
    readonly PictureBox canvas = new PictureBox();
    readonly FlowLayoutPanel panel = new FlowLayoutPanel();

    public PerspectiveWindow()
    {
        Text = "래스터화 - 원근 투영과 보간";
        ClientSize = new Size(880, 480);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        canvas.Dock = DockStyle.Fill;
        canvas.BackColor = Color.Black;
        canvas.SizeMode = PictureBoxSizeMode.Zoom;
        panel.Dock = DockStyle.Right;
        panel.Width = 230;
        panel.Padding = new Padding(12);
        panel.FlowDirection = FlowDirection.TopDown;
        panel.WrapContents = false;
        panel.AutoScroll = true;
        Controls.Add(canvas);
        Controls.Add(panel);

        AddCheckBox("뒷면 제외", rasterization.CullBackface,
            value => rasterization.CullBackface = value);
        AddCheckBox("원근 투영", rasterization.UsePerspectiveProjection,
            value => rasterization.UsePerspectiveProjection = value);
        AddCheckBox("원근 보정 보간", rasterization.UsePerspectiveCorrectInterpolation,
            value => rasterization.UsePerspectiveCorrectInterpolation = value);
        AddSlider("눈과 화면 거리", rasterization.DistEyeToScreen, 0.5f, 5,
            value => rasterization.DistEyeToScreen = value);
        AddSlider("왼쪽 사각형 Z", rasterization.Meshes[0].Translation.Z, 0.1f, 5,
            value => rasterization.Meshes[0].Translation.Z = value);
        AddSlider("오른쪽 사각형 Z", rasterization.Meshes[1].Translation.Z, 0.1f, 5,
            value => rasterization.Meshes[1].Translation.Z = value);

        UpdateImage();
    }

    void AddCheckBox(string title, bool value, Action<bool> changed)
    {
        CheckBox checkBox = new CheckBox
        {
            Text = title,
            Checked = value,
            AutoSize = true
        };
        checkBox.CheckedChanged += (_, _) =>
        {
            changed(checkBox.Checked);
            UpdateImage();
        };
        panel.Controls.Add(checkBox);
    }

    void AddSlider(string title, float value, float min, float max, Action<float> changed)
    {
        Label label = new Label { Text = $"{title}: {value:F2}", AutoSize = true };
        panel.Controls.Add(label);
        TrackBar slider = new TrackBar
        {
            Width = 180,
            Height = 32,
            AutoSize = false,
            Minimum = (int)MathF.Round(min * 100f),
            Maximum = (int)MathF.Round(max * 100f),
            Value = (int)MathF.Round(value * 100f),
            SmallChange = 1,
            LargeChange = 5,
            TickStyle = TickStyle.None,
            Margin = new Padding(3, 0, 3, 12)
        };
        slider.ValueChanged += (_, _) =>
        {
            float currentValue = slider.Value / 100f;
            label.Text = $"{title}: {currentValue:F2}";
            changed(currentValue);
            UpdateImage();
        };
        panel.Controls.Add(slider);
    }

    void UpdateImage()
    {
        Image? previous = canvas.Image;
        canvas.Image = rasterization.RenderImage();
        previous?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            canvas.Image?.Dispose();
        base.Dispose(disposing);
    }
}
