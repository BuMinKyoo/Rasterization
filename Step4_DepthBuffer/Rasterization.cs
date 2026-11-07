using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

class Rasterization
{
    public readonly int Width;
    public readonly int Height;

    public Mesh Circle1 = new Mesh();
    public Mesh Circle2 = new Mesh();
    public Mesh Circle3 = new Mesh();

    public Vector3 Center1 = new Vector3(0.0f, 0.0f, 0.1f);
    public Vector3 Center2 = new Vector3(-0.3f, 0.0f, 0.3f);
    public Vector3 Center3 = new Vector3(0.3f, 0.0f, 0.7f);

    public Vector3[] VertexBuffer = Array.Empty<Vector3>();
    public int[] IndexBuffer = Array.Empty<int>();
    public Vector3[] ColorBuffer = Array.Empty<Vector3>();
    public float[] DepthBuffer = Array.Empty<float>();

    public Rasterization(int width, int height)
    {
        Width = width;
        Height = height;

        Circle1.InitCircle(0.3f, 30, new Vector3(1.0f, 0.0f, 0.0f));
        Circle2.InitCircle(0.5f, 30, new Vector3(0.0f, 0.0f, 1.0f));
        Circle3.InitCircle(0.5f, 30, new Vector3(1.0f, 1.0f, 0.0f));

        Circle1.VertexBuffer = Circle1.Vertices.ToArray();
        Circle1.IndexBuffer = Circle1.Indices.ToArray();
        Circle1.ColorBuffer = Circle1.Colors.ToArray();

        Circle2.VertexBuffer = Circle2.Vertices.ToArray();
        Circle2.IndexBuffer = Circle2.Indices.ToArray();
        Circle2.ColorBuffer = Circle2.Colors.ToArray();

        Circle3.VertexBuffer = Circle3.Vertices.ToArray();
        Circle3.IndexBuffer = Circle3.Indices.ToArray();
        Circle3.ColorBuffer = Circle3.Colors.ToArray();

        Circle1.Vertices.Clear();
        Circle1.Indices.Clear();
        Circle1.Colors.Clear();
        Circle2.Vertices.Clear();
        Circle2.Indices.Clear();
        Circle2.Colors.Clear();
        Circle3.Vertices.Clear();
        Circle3.Indices.Clear();
        Circle3.Colors.Clear();
    }

    // 정투영으로 월드 좌표를 화면의 픽셀 좌표로 옮깁니다.
    public Vector2 ProjectWorldToRaster(Vector3 point)
    {
        int width = Width;
        int height = Height;
        // 1. 화면 가로세로 비율을 반영해 NDC 좌표를 구합니다.
        float aspect = (float)width / height;
        float ndcX = point.X / aspect;
        float ndcY = point.Y;

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

    // 인덱스 세 개가 가리키는 삼각형 하나를 그립니다.
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

        Vector3 c0 = ColorBuffer[i0];
        Vector3 c1 = ColorBuffer[i1];
        Vector3 c2 = ColorBuffer[i2];

        // 2. 삼각형을 감싸는 사각형(Bounding box)을 구합니다.
        float minX = MathF.Min(v0.X, MathF.Min(v1.X, v2.X));
        float minY = MathF.Min(v0.Y, MathF.Min(v1.Y, v2.Y));
        float maxX = MathF.Max(v0.X, MathF.Max(v1.X, v2.X));
        float maxY = MathF.Max(v0.Y, MathF.Max(v1.Y, v2.Y));

        int xMin = (int)MathF.Floor(minX);
        int yMin = (int)MathF.Floor(minY);
        int xMax = (int)MathF.Ceiling(maxX);
        int yMax = (int)MathF.Ceiling(maxY);

        xMin = Math.Clamp(xMin, 0, width - 1);
        yMin = Math.Clamp(yMin, 0, height - 1);
        xMax = Math.Clamp(xMax, 0, width - 1);
        yMax = Math.Clamp(yMax, 0, height - 1);

        // 3. Bounding box 안의 픽셀만 검사합니다.
        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                Vector2 point = new Vector2(x, y);

                float alpha0 = EdgeFunction(v1, v2, point);
                float alpha1 = EdgeFunction(v2, v0, point);
                float alpha2 = EdgeFunction(v0, v1, point);

                // 현재 꼭짓점 순서에서 세 값이 모두 0 이상이면 내부입니다.
                if (alpha0 >= 0 && alpha1 >= 0 && alpha2 >= 0)
                {
                    float area = alpha0 + alpha1 + alpha2;
                    if (area == 0.0f)
                        continue;

                    // 인덱스로 가져온 세 정점의 색을 넓이 비율로 섞습니다.
                    Vector3 color = (alpha0 * c0 + alpha1 * c1 + alpha2 * c2) / area;

                    // 정투영에서만 정확한 Z 좌표 보간입니다.
                    float depth = (alpha0 * VertexBuffer[i0].Z +
                                   alpha1 * VertexBuffer[i1].Z +
                                   alpha2 * VertexBuffer[i2].Z) / area;

                    if (depth < DepthBuffer[x + width * y])
                    {
                        DepthBuffer[x + width * y] = depth;
                        int r = (int)(Math.Clamp(color.X, 0, 1) * 255);
                        int g = (int)(Math.Clamp(color.Y, 0, 1) * 255);
                        int b = (int)(Math.Clamp(color.Z, 0, 1) * 255);
                        pixels[x + width * y] = (255 << 24) | (r << 16) | (g << 8) | b;
                    }
                }
            }
        }

    }

    public void Render(int[] pixels)
    {
        // 매번 깊이 버퍼를 1.0f로 초기화합니다.
        Array.Resize(ref DepthBuffer, pixels.Length);
        Array.Fill(DepthBuffer, 1.0f);

        // 가장 앞의 빨간 원 그리기
        Array.Resize(ref VertexBuffer, Circle1.VertexBuffer.Length);
        for (int i = 0; i < Circle1.VertexBuffer.Length; i++)
        {
            VertexBuffer[i] = Circle1.VertexBuffer[i] + Center1;
        }

        IndexBuffer = (int[])Circle1.IndexBuffer.Clone();
        ColorBuffer = (Vector3[])Circle1.ColorBuffer.Clone();

        for (int i = 0; i < IndexBuffer.Length; i += 3)
        {
            DrawIndexedTriangle(i, pixels);
        }

        // 파란 원 그리기
        Array.Resize(ref VertexBuffer, Circle2.VertexBuffer.Length);
        for (int i = 0; i < Circle2.VertexBuffer.Length; i++)
        {
            VertexBuffer[i] = Circle2.VertexBuffer[i] + Center2;
        }

        IndexBuffer = (int[])Circle2.IndexBuffer.Clone();
        ColorBuffer = (Vector3[])Circle2.ColorBuffer.Clone();

        for (int i = 0; i < IndexBuffer.Length; i += 3)
        {
            DrawIndexedTriangle(i, pixels);
        }

        // 가장 뒤의 노란 원 그리기
        Array.Resize(ref VertexBuffer, Circle3.VertexBuffer.Length);
        for (int i = 0; i < Circle3.VertexBuffer.Length; i++)
        {
            VertexBuffer[i] = Circle3.VertexBuffer[i] + Center3;
        }

        IndexBuffer = (int[])Circle3.IndexBuffer.Clone();
        ColorBuffer = (Vector3[])Circle3.ColorBuffer.Clone();

        for (int i = 0; i < IndexBuffer.Length; i += 3)
        {
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
