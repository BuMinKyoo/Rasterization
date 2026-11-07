using System.Drawing;
using System.Windows.Forms;

class DepthWindow : Form
{
    readonly Rasterization rasterization = new Rasterization(640, 480);
    readonly PictureBox canvas = new PictureBox();

    public DepthWindow()
    {
        Text = "래스터화 - 깊이 버퍼";
        ClientSize = new Size(640, 480);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        canvas.Dock = DockStyle.Fill;
        canvas.BackColor = Color.Black;
        canvas.SizeMode = PictureBoxSizeMode.Normal;
        Controls.Add(canvas);
        canvas.Image = rasterization.RenderImage();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            canvas.Image?.Dispose();
        base.Dispose(disposing);
    }
}
