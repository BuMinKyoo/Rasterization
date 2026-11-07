using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

class Rasterization
{
    public readonly int Width;
    public readonly int Height;
    public Mesh[] Meshes;

    public Vector3[] VertexBuffer = Array.Empty<Vector3>();
    public int[] IndexBuffer = Array.Empty<int>();
    public Vector3[] ColorBuffer = Array.Empty<Vector3>();
    public Vector2[] UvBuffer = Array.Empty<Vector2>();
    public float[] DepthBuffer;

    public bool CullBackface = true;
    public bool UsePerspectiveProjection = false;
    public bool UsePerspectiveCorrectInterpolation = false;
    public float DistEyeToScreen = 1.0f;

    public Rasterization(int width, int height)
    {
        Width = width;
        Height = height;

        Mesh square1 = new Mesh();
        square1.InitSquare(1.0f, 1.0f, Vector3.One);
        square1.RotationX = -3.141592f * 30 / 180;
        square1.Scale = Vector3.One;
        square1.Translation = new Vector3(-0.6f, 0.0f, 0.1f);
        square1.CopyToBuffer();

        Mesh square2 = new Mesh();
        square2.InitSquare(1.0f, 1.0f, Vector3.One);
        square2.RotationX = -3.141592f * 30 / 180;
        square2.Scale = Vector3.One;
        square2.Translation = new Vector3(0.6f, 0.0f, 1.0f);
        square2.CopyToBuffer();

        Meshes = new Mesh[] { square1, square2 };
        DepthBuffer = new float[width * height];
    }

    public Vector3 RotateAboutX(Vector3 v, float theta)
    {
        return new Vector3(v.X,
            v.Y * MathF.Cos(theta) - v.Z * MathF.Sin(theta),
            v.Y * MathF.Sin(theta) + v.Z * MathF.Cos(theta));
    }

    public Vector3 MyVertexShader(Vector3 position, Mesh mesh)
    {
        return RotateAboutX(position * mesh.Scale, mesh.RotationX) + mesh.Translation;
    }

    // GLM의 mod와 같은 계산입니다.
    static float Mod(float value, float divisor)
    {
        return value - divisor * MathF.Floor(value / divisor);
    }

    public Vector3 MyPixelShader(Vector3 color, Vector2 uv)
    {
        const float size = 5.0f;
        float cellX = Mod(MathF.Floor(uv.X * size), 2.0f);
        float cellY = Mod(MathF.Floor(uv.Y * size), 2.0f);
        if (Mod(cellX + cellY, 2.0f) != 0.0f)
            return color;

        return new Vector3(1, 0, 0) * uv.Y
             + new Vector3(0, 0, 1) * (1.0f - uv.Y);
    }

    // 월드 좌표를 정투영 또는 원근 투영으로 화면에 옮깁니다.
    public Vector2 ProjectWorldToRaster(Vector3 point)
    {
        int width = Width;
        int height = Height;
        Vector2 pointProj = new Vector2(point.X, point.Y);
        if (UsePerspectiveProjection)
        {
            float scale = DistEyeToScreen / (DistEyeToScreen + point.Z);
            pointProj *= scale;
        }
        // 1. 화면 가로세로 비율을 반영해 NDC 좌표를 구합니다.
        float aspect = (float)width / height;
        float ndcX = pointProj.X / aspect;
        float ndcY = pointProj.Y;

        // 2. NDC의 가로와 세로 범위 2를 픽셀 개수에 대응시킵니다.
        float xScale = 2.0f / width;
        float yScale = 2.0f / height;

        // 3. X의 시작을 0으로 옮기고, 위가 0이 되도록 Y를 뒤집습니다.
        float shiftedX = ndcX + 1.0f;
        float flippedY = 1.0f - ndcY;
        float rasterX = shiftedX / xScale;
        float rasterY = flippedY / yScale;

        // 4. 픽셀 중심을 정수 좌표 (x, y)로 취급합니다.
        rasterX -= 0.5f;
        rasterY -= 0.5f;

        return new Vector2(rasterX, rasterY);
    }

    // 두 벡터의 외적에서 Z 성분만 계산합니다.
    public float EdgeFunction(Vector2 v0, Vector2 v1, Vector2 point)
    {
        Vector2 a = v1 - v0;
        Vector2 b = point - v0;
        return a.X * b.Y - a.Y * b.X;
    }

    public void DrawIndexedTriangle(int startIndex, int[] pixels)
    {
        int width = Width;
        int height = Height;
        int i0 = IndexBuffer[startIndex];
        int i1 = IndexBuffer[startIndex + 1];
        int i2 = IndexBuffer[startIndex + 2];

        Vector2 v0 = ProjectWorldToRaster(VertexBuffer[i0]);
        Vector2 v1 = ProjectWorldToRaster(VertexBuffer[i1]);
        Vector2 v2 = ProjectWorldToRaster(VertexBuffer[i2]);

        float area = EdgeFunction(v0, v1, v2);
        if (CullBackface && area < 0.0f)
            return;
        if (area == 0.0f)
            return;

        Vector3 c0 = ColorBuffer[i0];
        Vector3 c1 = ColorBuffer[i1];
        Vector3 c2 = ColorBuffer[i2];
        Vector2 uv0 = UvBuffer[i0];
        Vector2 uv1 = UvBuffer[i1];
        Vector2 uv2 = UvBuffer[i2];

        int xMin = (int)MathF.Floor(MathF.Min(v0.X, MathF.Min(v1.X, v2.X)));
        int yMin = (int)MathF.Floor(MathF.Min(v0.Y, MathF.Min(v1.Y, v2.Y)));
        int xMax = (int)MathF.Ceiling(MathF.Max(v0.X, MathF.Max(v1.X, v2.X)));
        int yMax = (int)MathF.Ceiling(MathF.Max(v0.Y, MathF.Max(v1.Y, v2.Y)));
        xMin = Math.Clamp(xMin, 0, width - 1);
        yMin = Math.Clamp(yMin, 0, height - 1);
        xMax = Math.Clamp(xMax, 0, width - 1);
        yMax = Math.Clamp(yMax, 0, height - 1);

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                Vector2 point = new Vector2(x, y);
                float w0 = EdgeFunction(v1, v2, point) / area;
                float w1 = EdgeFunction(v2, v0, point) / area;
                float w2 = EdgeFunction(v0, v1, point) / area;

                if (w0 >= 0.0f && w1 >= 0.0f && w2 >= 0.0f)
                {
                    float z0 = VertexBuffer[i0].Z + DistEyeToScreen;
                    float z1 = VertexBuffer[i1].Z + DistEyeToScreen;
                    float z2 = VertexBuffer[i2].Z + DistEyeToScreen;

                    if (UsePerspectiveProjection && UsePerspectiveCorrectInterpolation)
                    {
                        w0 /= z0;
                        w1 /= z1;
                        w2 /= z2;

                        float wSum = w0 + w1 + w2;
                        w0 /= wSum;
                        w1 /= wSum;
                        w2 /= wSum;
                    }

                    float depth = w0 * z0 + w1 * z1 + w2 * z2;
                    Vector3 color = w0 * c0 + w1 * c1 + w2 * c2;
                    Vector2 uv = w0 * uv0 + w1 * uv1 + w2 * uv2;

                    if (depth < DepthBuffer[x + width * y])
                    {
                        DepthBuffer[x + width * y] = depth;
                        Vector3 pixelColor = MyPixelShader(color, uv);
                        int r = (int)(Math.Clamp(pixelColor.X, 0, 1) * 255);
                        int g = (int)(Math.Clamp(pixelColor.Y, 0, 1) * 255);
                        int b = (int)(Math.Clamp(pixelColor.Z, 0, 1) * 255);
                        pixels[x + width * y] = (255 << 24) | (r << 16) | (g << 8) | b;
                    }
                }
            }
        }
    }

    public void Render(int[] pixels)
    {
        Array.Resize(ref DepthBuffer, pixels.Length);
        Array.Fill(DepthBuffer, 10.0f);

        foreach (Mesh mesh in Meshes)
        {
            Array.Resize(ref VertexBuffer, mesh.VertexBuffer.Length);
            Array.Resize(ref ColorBuffer, mesh.ColorBuffer.Length);
            Array.Resize(ref UvBuffer, mesh.UvBuffer.Length);

            for (int i = 0; i < mesh.VertexBuffer.Length; i++)
            {
                VertexBuffer[i] = MyVertexShader(mesh.VertexBuffer[i], mesh);
                ColorBuffer[i] = mesh.ColorBuffer[i];
                UvBuffer[i] = mesh.UvBuffer[i];
            }

            IndexBuffer = (int[])mesh.IndexBuffer.Clone();
            for (int i = 0; i < IndexBuffer.Length; i += 3)
                DrawIndexedTriangle(i, pixels);
        }
    }

    public Bitmap RenderImage()
    {
        int width = Width;
        int height = Height;
        int[] pixels = new int[width * height];
        Array.Fill(pixels, Color.Black.ToArgb());
        Render(pixels);

        // 계산한 픽셀 배열을 Bitmap으로 옮겨 창에 표시합니다.
        Bitmap image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        BitmapData data = image.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (int y = 0; y < height; y++)
                Marshal.Copy(pixels, y * width,
                    IntPtr.Add(data.Scan0, y * data.Stride), width);
        }
        finally
        {
            image.UnlockBits(data);
        }
        return image;
    }
}
