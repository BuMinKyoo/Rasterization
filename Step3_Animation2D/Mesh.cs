using System;
using System.Collections.Generic;
using System.Numerics;

class Mesh
{
    public List<Vector3> Vertices = new List<Vector3>();
    public List<int> Indices = new List<int>();
    public List<Vector3> Colors = new List<Vector3>();

    // 강의에서 GPU 버퍼로 가정한 배열 사본입니다.
    public Vector3[] VertexBuffer = Array.Empty<Vector3>();
    public int[] IndexBuffer = Array.Empty<int>();
    public Vector3[] ColorBuffer = Array.Empty<Vector3>();

    public void InitCircle(float radius, int numTriangles, Vector3 color)
    {
        Vector3 center = Vector3.Zero;
        Vertices.EnsureCapacity(numTriangles + 1);
        Colors.EnsureCapacity(Vertices.Count);
        Indices.EnsureCapacity(numTriangles * 3);
        Vertices.Add(center);
        Colors.Add(color);

        const float kTwoPi = 2.0f * 3.141592f;
        float deltaTheta = kTwoPi / (float)numTriangles;

        for (float theta = 0.0f; theta < kTwoPi; theta += deltaTheta)
        {
            Vertices.Add(center +
                new Vector3(MathF.Cos(theta), MathF.Sin(theta), 0.0f) * radius);
            Colors.Add(color);
        }

        for (int i = 0; i < numTriangles; i++)
        {
            Indices.Add(0);
            if (i == numTriangles - 1)
                Indices.Add(1);
            else
                Indices.Add(i + 2);
            Indices.Add(i + 1);
        }
    }
}
