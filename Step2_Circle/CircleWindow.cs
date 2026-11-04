using System.Drawing;
using System.Windows.Forms;

// 계산한 픽셀을 보여주는 작은 창입니다.
class CircleWindow : Form
{
    readonly Rasterization rasterization = new Rasterization();
    readonly PictureBox canvas = new PictureBox();

    public CircleWindow()
    {
        Text = "래스터화 - 삼각형으로 원 만들기";
        ClientSize = new Size(640, 480);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        canvas.Dock = DockStyle.Fill;
        canvas.BackColor = Color.Black;
        canvas.SizeMode = PictureBoxSizeMode.Normal;
        Controls.Add(canvas);

        canvas.Image = rasterization.Render(640, 480);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            canvas.Image?.Dispose();
        base.Dispose(disposing);
    }
}
