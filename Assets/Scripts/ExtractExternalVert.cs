using System.Collections.Generic;
using UnityEngine;

public class ExtractExternalVert
{
    GameObject wall;
    private List<Edge> externalEdges;
    private List<Vector3> vertices;
    private List<Vector2> uvs;
    
    public ExtractExternalVert(GameObject wall)
    {
        externalEdges = new List<Edge>();
        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        this.wall = wall;
        MeshCollider meshCollider = wall.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            Debug.LogError("MeshCollider не найден на объекте.");
            return;
        }

        Mesh mesh = meshCollider.sharedMesh;
        if (mesh == null)
        {
            Debug.LogError("Mesh в MeshCollider отсутствует.");
            return;
        }

        GetExternalEdges(mesh);

        // Количество найденных внешних рёбер
        Debug.Log($"Найдено {externalEdges.Count} внешних рёбер.");
        foreach (var edge in externalEdges)
        {
            Debug.Log($"Ребро: {edge.vertex1} -> {edge.vertex2}");
        }
    }

    public List<Vector3> GetVertices()
    {

        List<Vector3> worldVertices = new List<Vector3>();
        foreach (var vertex in vertices)
        {
            
            worldVertices.Add(wall.transform.TransformPoint(vertex));
        }
        return worldVertices;
    }

    public List<Edge> GetEdges()
    {
        return externalEdges;
    }
    public List<Vector2> GetUvs()
    {
        return uvs;
    }
    
    // Метод для получения внешних рёбер
    void GetExternalEdges(Mesh mesh)
    {
        Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();

        // Получаем вершины и треугольники
        Vector3[] vert = mesh.vertices;
        Vector2[] uv = mesh.uv;
        int[] triangles = mesh.triangles;

        // Проходим по всем треугольникам
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            //Определяем мировые координаты
            var vw1 = wall.transform.TransformPoint(vert[v1]);
            var vw2 = wall.transform.TransformPoint(vert[v2]);
            var vw3 = wall.transform.TransformPoint(vert[v3]);
            
            // Добавляем рёбра треугольника
            AddEdge(edgeCount, new Edge(v1,vw1, v2,vw2));
            AddEdge(edgeCount, new Edge(v2,vw2, v3,vw3));
            AddEdge(edgeCount, new Edge(v3,vw3, v1,vw1));
        }

        // Отбираем только те рёбра, которые встречаются один раз
        //List<Edge> externalEdges = new List<Edge>();
        foreach (var edge in edgeCount)
        {
            if (edge.Value == 1) // Ребро принадлежит только одной грани
            {
                externalEdges.Add(edge.Key);
                //Добавляем вертексы
                if (!vertices.Contains(vert[edge.Key.vertex1]));
                {
                    vertices.Add(vert[edge.Key.vertex1]);
                    uvs.Add(uv[edge.Key.vertex1]);
                }
                if (!vertices.Contains(vert[edge.Key.vertex2]));
                {
                    vertices.Add(vert[edge.Key.vertex2]);
                    uvs.Add(uv[edge.Key.vertex2]);
                }
            }
        }
    }

    // Метод для добавления рёбер в словарь с подсчётом
    void AddEdge(Dictionary<Edge, int> edgeCount, Edge edge)
    {
        if (edgeCount.ContainsKey(edge))
        {
            edgeCount[edge]++;
        }
        else
        {
            edgeCount[edge] = 1;
        }
    }
    // Структура для представления ребра
    public struct Edge
    {
        public int vertex1;
        public Vector3  vertex1pos;
        public int vertex2;
        public Vector3  vertex2pos;

        public Edge(int v1,Vector3 pos1, int v2,Vector3 pos2)
        {
            vertex1pos = Vector3.zero;
            vertex2pos = Vector3.zero;
            // Упорядочиваем вершины, чтобы (A, B) и (B, A) считались одним ребром
            if (v1 < v2)
            {
                vertex1 = v1;
                vertex1pos = pos1;
                vertex2 = v2;
                vertex2pos = pos2;
            }
            else
            {
                vertex1 = v2;
                vertex1pos = pos2;
                vertex2 = v1;
                vertex2pos = pos1;
            }
        }

        // Переопределяем методы для корректного использования в словарях
        public override bool Equals(object obj)
        {
            if (!(obj is Edge)) return false;
            Edge other = (Edge)obj;
            return vertex1 == other.vertex1 && vertex2 == other.vertex2;
        }

        public override int GetHashCode()
        {
            return vertex1.GetHashCode() ^ vertex2.GetHashCode();
        }
        
        
    }
}
