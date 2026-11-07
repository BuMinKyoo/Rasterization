using System;
using System.Drawing;
using System.Windows.Forms;

class AnimationWindow : Form
{
    readonly Rasterization rasterization = new Rasterization(640, 480);
    readonly PictureBox canvas = new PictureBox();
    readonly FlowLayoutPanel panel = new FlowLayoutPanel();
    readonly System.Windows.Forms.Timer animationTimer =
        new System.Windows.Forms.Timer { Interval = 33 };

    public AnimationWindow()
    {
        Text = "래스터화 - 태양, 지구, 달";
        ClientSize = new Size(860, 480);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        canvas.Dock = DockStyle.Fill;
        canvas.BackColor = Color.Black;
        canvas.SizeMode = PictureBoxSizeMode.Zoom;
        panel.Dock = DockStyle.Right;
        panel.Width = 210;
        panel.Padding = new Padding(12);
        panel.FlowDirection = FlowDirection.TopDown;
        panel.WrapContents = false;
        Controls.Add(canvas);
        Controls.Add(panel);

        AddSlider("지구 각속도", rasterization.EarthAngularVelocity, 0, 2,
            value => rasterization.EarthAngularVelocity = value);
        AddSlider("달 각속도", rasterization.MoonAngularVelocity, 0, 5,
            value => rasterization.MoonAngularVelocity = value);

        animationTimer.Tick += (_, _) =>
        {
            rasterization.Update();
            UpdateImage();
        };
        UpdateImage();
        animationTimer.Start();
    }

    void AddSlider(string title, float value, float min, float max, Action<float> changed)
    {
        Label label = new Label { Text = $"{title}: {value:F2}", AutoSize = true };
        panel.Controls.Add(label);

        TrackBar slider = new TrackBar
        {
            Width = 170,
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
        {
            animationTimer.Stop();
            animationTimer.Dispose();
            canvas.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
