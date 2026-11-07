using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

class Rasterization
{
    public readonly int Width;
    public readonly int Height;

    public Mesh Sun = new Mesh();
    public Mesh Earth = new Mesh();
    public Mesh Moon = new Mesh();

    public float EarthAngle = 0.0f;
    public float EarthAngularVelocity = 0.3f;
    public float MoonAngle = 0.0f;
    public float MoonAngularVelocity = 1.0f;

    public float DistSunToEarth = 0.5f;
    public float DistEarthToMoon = 0.1f;

    public Vector3[] VertexBuffer = Array.Empty<Vector3>();
    public int[] IndexBuffer = Array.Empty<int>();
    public Vector3[] ColorBuffer = Array.Empty<Vector3>();

    public Rasterization(int width, int height)
    {
        Width = width;
        Height = height;

        Sun.InitCircle(0.1f, 10, new Vector3(1.0f, 1.0f, 1.0f));
        Earth.InitCircle(0.05f, 10, new Vector3(0.0f, 0.0f, 1.0f));
        Moon.InitCircle(0.02f, 10, new Vector3(1.0f, 1.0f, 0.0f));

        Sun.VertexBuffer = Sun.Vertices.ToArray();
        Sun.IndexBuffer = Sun.Indices.ToArray();
        Sun.ColorBuffer = Sun.Colors.ToArray();

        Earth.VertexBuffer = Earth.Vertices.ToArray();
        Earth.IndexBuffer = Earth.Indices.ToArray();
        Earth.ColorBuffer = Earth.Colors.ToArray();

        Moon.VertexBuffer = Moon.Vertices.ToArray();
        Moon.IndexBuffer = Moon.Indices.ToArray();
        Moon.ColorBuffer = Moon.Colors.ToArray();

        Sun.Vertices.Clear();
        Sun.Indices.Clear();
        Sun.Colors.Clear();
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

                    int r = (int)(Math.Clamp(color.X, 0, 1) * 255);
                    int g = (int)(Math.Clamp(color.Y, 0, 1) * 255);
                    int b = (int)(Math.Clamp(color.Z, 0, 1) * 255);
                    pixels[x + width * y] = (255 << 24) | (r << 16) | (g << 8) | b;
                }
            }
        }

    }

    public Vector3 RotateAboutZ(Vector3 v, float theta)
    {
        return new Vector3(
            v.X * MathF.Cos(theta) - v.Y * MathF.Sin(theta),
            v.X * MathF.Sin(theta) + v.Y * MathF.Cos(theta),
            v.Z);
    }

    public void Update()
    {
        const float dt = 1.0f / 30.0f;
        EarthAngle += EarthAngularVelocity * dt;
        MoonAngle += MoonAngularVelocity * dt;
    }

    public void Render(int[] pixels)
    {
        // 태양 그리기
        Array.Resize(ref VertexBuffer, Sun.VertexBuffer.Length);
        for (int i = 0; i < Sun.VertexBuffer.Length; i++)
        {
            VertexBuffer[i] = Sun.VertexBuffer[i];
        }

        // C++ vector 대입처럼 배열의 내용을 복사합니다.
        IndexBuffer = (int[])Sun.IndexBuffer.Clone();
        ColorBuffer = (Vector3[])Sun.ColorBuffer.Clone();

        for (int i = 0; i < IndexBuffer.Length; i += 3)
        {
            DrawIndexedTriangle(i, pixels);
        }

        // 지구 그리기
        Array.Resize(ref VertexBuffer, Earth.VertexBuffer.Length);
        for (int i = 0; i < Earth.VertexBuffer.Length; i++)
        {
            // GLM의 vec3 + float는 XYZ 모두에 같은 값을 더합니다.
            Vector3 temp = Earth.VertexBuffer[i] + new Vector3(DistSunToEarth);
            temp = RotateAboutZ(temp, EarthAngle);
            VertexBuffer[i] = temp;
        }

        IndexBuffer = (int[])Earth.IndexBuffer.Clone();
        ColorBuffer = (Vector3[])Earth.ColorBuffer.Clone();

        for (int i = 0; i < IndexBuffer.Length; i += 3)
        {
            DrawIndexedTriangle(i, pixels);
        }

        // 달 그리기: 원본처럼 CPU 정점 개수로 크기와 반복 범위를 정합니다.
        Array.Resize(ref VertexBuffer, Moon.Vertices.Count);
        for (int i = 0; i < Moon.Vertices.Count; i++)
        {
            Vector3 temp = Moon.VertexBuffer[i] + new Vector3(DistEarthToMoon);
            temp = RotateAboutZ(temp, MoonAngle);
            temp = temp + new Vector3(+DistSunToEarth);
            temp = RotateAboutZ(temp, EarthAngle);
            VertexBuffer[i] = temp;
        }

        IndexBuffer = (int[])Moon.IndexBuffer.Clone();
        ColorBuffer = (Vector3[])Moon.ColorBuffer.Clone();

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
