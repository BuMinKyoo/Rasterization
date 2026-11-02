using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

struct MyVertex
{
    public Vector3 Position;
    public Vector3 Color;
}

struct MyTriangle
{
    public MyVertex V0, V1, V2;
}

class Rasterization
{
    public MyTriangle Triangle = new MyTriangle
    {
        V0 = new MyVertex
        {
            Position = new Vector3(0, 0.5f, 1),
            Color = new Vector3(1, 0, 0)
        },
        V1 = new MyVertex
        {
            Position = new Vector3(1, -0.5f, 1),
            Color = new Vector3(0, 1, 0)
        },
        V2 = new MyVertex
        {
            Position = new Vector3(-1, -0.5f, 1),
            Color = new Vector3(0, 0, 1)
        }
    };

    // 정투영으로 월드 좌표를 화면의 픽셀 좌표로 옮깁니다.
    public Vector2 ProjectWorldToRaster(Vector3 point, int width, int height)
    {
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

    public Bitmap Render(int width, int height)
    {
        int[] pixels = new int[width * height];
        Array.Fill(pixels, Color.Black.ToArgb());

        // 1. 세 꼭짓점을 월드 좌표에서 화면 좌표로 변환합니다.
        Vector2 v0 = ProjectWorldToRaster(Triangle.V0.Position, width, height);
        Vector2 v1 = ProjectWorldToRaster(Triangle.V1.Position, width, height);
        Vector2 v2 = ProjectWorldToRaster(Triangle.V2.Position, width, height);

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
                float alpha1 = EdgeFunction(point, v2, v0);
                float alpha2 = EdgeFunction(v0, v1, point);

                // 현재 꼭짓점 순서에서 세 값이 모두 0 이상이면 내부입니다.
                if (alpha0 >= 0 && alpha1 >= 0 && alpha2 >= 0)
                {
                    float area = alpha0 + alpha1 + alpha2;
                    if (area == 0.0f)
                        continue;

                    // 4. 작은 삼각형의 넓이 비율로 세 정점의 색을 섞습니다.
                    float w0 = alpha0 / area;
                    float w1 = alpha1 / area;
                    float w2 = alpha2 / area;

                    Vector3 color = w0 * Triangle.V0.Color
                                  + w1 * Triangle.V1.Color
                                  + w2 * Triangle.V2.Color;

                    int r = (int)(Math.Clamp(color.X, 0, 1) * 255);
                    int g = (int)(Math.Clamp(color.Y, 0, 1) * 255);
                    int b = (int)(Math.Clamp(color.Z, 0, 1) * 255);
                    pixels[x + width * y] = (255 << 24) | (r << 16) | (g << 8) | b;
                }
            }
        }

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
