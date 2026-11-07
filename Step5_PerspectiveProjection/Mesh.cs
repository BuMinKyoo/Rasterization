using System;
using System.Collections.Generic;
using System.Numerics;

class Mesh
{
    public List<Vector3> Vertices = new List<Vector3>();
    public List<int> Indices = new List<int>();
    public List<Vector3> Colors = new List<Vector3>();
    public List<Vector2> TextureCoords = new List<Vector2>();

    public Vector3[] VertexBuffer = Array.Empty<Vector3>();
    public int[] IndexBuffer = Array.Empty<int>();
    public Vector3[] ColorBuffer = Array.Empty<Vector3>();
    public Vector2[] UvBuffer = Array.Empty<Vector2>();

    public Vector3 Scale = Vector3.One;
    public Vector3 Translation = Vector3.Zero;
    public float RotationX = 0.0f;

    public void InitSquare(float width, float height, Vector3 color)
    {
        Vector3 scale = new Vector3(width * 0.5f, height * 0.5f, 1.0f);
        Vertices.Add(new Vector3(-1, -1, 0) * scale);
        Vertices.Add(new Vector3(-1, 1, 0) * scale);
        Vertices.Add(new Vector3(1, 1, 0) * scale);
        Vertices.Add(new Vector3(1, -1, 0) * scale);

        for (int i = 0; i < Vertices.Count; i++)
            Colors.Add(color);

        Indices.AddRange(new int[] { 0, 1, 2, 0, 2, 3 });
        TextureCoords.AddRange(new Vector2[]
        {
            new Vector2(0, 1), new Vector2(0, 0),
            new Vector2(1, 0), new Vector2(1, 1)
        });
    }

    public void CopyToBuffer()
    {
        VertexBuffer = Vertices.ToArray();
        IndexBuffer = Indices.ToArray();
        ColorBuffer = Colors.ToArray();
        UvBuffer = TextureCoords.ToArray();

        Vertices.Clear();
        Indices.Clear();
        Colors.Clear();
        TextureCoords.Clear();
    }
}
