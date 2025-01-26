using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WallPaint : MonoBehaviour
{
    public GameObject point; //Внешняя точка

    //public Material material; // Материал для отображения текстуры
    public float circleRadius = 0.1f; // Радиус кружка вокруг стартовой точки
    public float minDistance = 0.1f; //Минимально допустимое растояние между точками фигуры (плавность фигуры)
    public float lineWidth = 0.1f;
    public bool magnetic = true;

    private MeshFilter meshFilter; // MeshFilter прямоугольного меша
    private MeshRenderer meshRenderer; // MeshRenderer прямоугольного меша

    private Camera mainCamera; // Камера для управления

    private List<Vector3> selectedVertices = new List<Vector3>();
    private List<Vector3> selectedNormals = new List<Vector3>();
    private List<Vector3> externalVert = new List<Vector3>();
    private List<Vector2> externalUV = new List<Vector2>();
    private Mesh mesh;
    private Vector2[] originalUVs;
    private bool isSelecting = false;

    private LineRenderer lineRenderer;
    private Vector3 startPoint;
    private Vector3 normalBase;
    private List<Vector2> selectedPoints = new List<Vector2>();
    private int numMesh = 0;
    private ExtractExternalVert externalEdges;
    private List<ExtractExternalVert.Edge> edges;
    private List<GameObject> selectedObjects = new List<GameObject>();

    private GameObject wall = null;
    private GameObject startPointGO = null;
    private List<GameObject> walls = new List<GameObject>();
    private ChangeTexture changeTexture;
    private SelectTexture selectTexture;

    void Start()
    {
        changeTexture = GetComponent<ChangeTexture>();
        selectTexture = GetComponent<SelectTexture>();
        mainCamera = Camera.main;

        // Инициализируем LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Простой материал
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
    }

    void StartDrawing()
    {
        wall = changeTexture.ChangeTextureObject;
        meshFilter = wall.GetComponent<MeshFilter>();
        meshRenderer = wall.GetComponent<MeshRenderer>();
        mesh = meshFilter.mesh;
        originalUVs = mesh.uv;

        externalEdges = new ExtractExternalVert(wall);
        externalVert = externalEdges.GetVertices();
        externalUV = externalEdges.GetUvs();
        edges = externalEdges.GetEdges();
        
        //пока выделяем обьект кружками
        //удаляем старые
        foreach (var go in selectedObjects)
        {
            Destroy(go);
        }

        selectedObjects.Clear();
        foreach (var vert in externalVert)
        {
            var go = Instantiate(point, vert, Quaternion.identity, transform);
            selectedObjects.Add(go);
        }
    }

    void Update()
    {
        //Отслеживаем изменения в Changtexture
        if (wall != changeTexture.ChangeTextureObject)
        {
            StartDrawing();
            ResetDraw();
        }

        if (Input.GetMouseButtonDown(0)) // Начало выделения
        {
            isSelecting = true;
            numMesh++;
            ResetDraw();
            AddVertex();
        }


        if (Input.GetMouseButton(0) && isSelecting)
        {
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                AddVertex(true);
                Debug.Log("Shift Pressed UP !!!");
            }
            else AddVertex();
        }

        if (Input.GetMouseButtonUp(0)) // Завершение выделения
        {
            isSelecting = false;
            ResetDraw();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Destroy(walls[walls.Count - 1]);
            walls.RemoveAt(walls.Count - 1);
        }

        // Обновляем LineRenderer для отрисовки контура
        UpdateLineRenderer();
    }

    void ResetDraw()
    {
        selectedVertices.Clear();
        selectedNormals.Clear();
        selectedPoints.Clear();
        lineRenderer.positionCount = 0;
        if (startPointGO != null) Destroy(startPointGO);
    }

    private void AddVertex(bool shiftUP = false)
    {
        //невыбран объект для рисования
        if (!wall) return;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == wall)
            {
                if (selectedVertices.Count > 0)
                {
                    if (Vector3.Distance(startPoint, hit.point) < minDistance)
                    {
                        if (selectedVertices.Count > 2)
                        {
                            isSelecting = false;
                            CreateMeshFromSelection();
                            ResetDraw();
                            return;
                        }
                    }
                }

                // Преобразуем мировые координаты в UV
                Vector2 uv = hit.textureCoord;
                Vector2 uv2 = Vector2.zero;

                var vertex = GetVertex(hit.point, hit.normal, out uv2);
                if (uv2 != Vector2.zero) uv = uv2;

                if ((!selectedVertices.Contains(vertex)) || (shiftUP))
                {
                    if (vertex == Vector3.zero) return;
                    if ((Input.GetKey(KeyCode.LeftShift)) && (selectedVertices.Count > 1))
                    {
                        selectedVertices[selectedVertices.Count - 1] = vertex;
                        selectedPoints[selectedPoints.Count - 1] = uv;
                        Debug.Log("Shift Pressed !!!");
                    }
                    else
                    {
                        selectedVertices.Add(vertex);
                        selectedPoints.Add(uv);
                        if (selectedVertices.Count == 1)
                        {
                            //normalBase = hit.normal;
                            startPointGO = Instantiate(point, selectedVertices[0], Quaternion.identity, transform);
                            startPointGO.transform.localScale = Vector3.one*circleRadius;
                            normalBase = GetNormal(hit);
                            startPoint = vertex;
                        }
                    }
                }
            }
        }
    }

    //Магнит к вертексам и ребрам коллайдера
    Vector3 GetVertex(Vector3 point, Vector3 normal, out Vector2 uv)
    {
        float dist = minDistance;
        uv = Vector2.zero;

        if (!magnetic) return point;

        if (selectedVertices.Count > 0)
            //    if ((selectedVertices.Count > 0) && (dist == minDistance))
        {
            if (Vector3.Distance(point, selectedVertices[selectedVertices.Count - 1]) < minDistance)
                return Vector3.zero;
        }

        var res = point;
        //Проверяем на близость к внешним вертексам
        for (int i = 0; i < externalVert.Count; i++)
        {
            if (Vector3.Distance(point, externalVert[i]) < dist)
            {
                res = externalVert[i];
                dist = Vector3.Distance(point, externalVert[i]);
                uv = externalUV[i];
                //uv = GetUV(externalVert[i], normal);
            }
        }

        //вертексы не найдены идем по ребрам
        if (dist == minDistance)
            //if (dist == 1000f)
        {
            // Близко к внешнему ребру    
            foreach (var edge in edges)
            {
                //Ближайшая точка на ребре
                var p = WallPaintFunc.FindEdgePoint(point, edge.vertex1pos, edge.vertex2pos);
                if (Vector3.Distance(p, point) < dist)
                {
                    res = p;
                    dist = Vector3.Distance(p, point);
                    uv = GetUV(p, normal);
                }
            }
        }

        return res;
    }

    Vector2 GetUV(Vector3 point, Vector3 normal)
    {
        //RaycastHit hit;
        float rayLength = 0.1f;
        Ray ray = new Ray(point + normal * rayLength * 0.5f, -normal);
        if (Physics.Raycast(ray, out RaycastHit hit, rayLength))
        {
            if (hit.collider.gameObject == wall)
            {
                Debug.Log("UVhit ok !");
                return hit.textureCoord;
            }
            else Debug.Log("UVhit false !");
        }

        return Vector2.zero;
    }

    Vector3 GetNormal(RaycastHit hitInfo)
    {
        Vector3 normal = hitInfo.normal;
        // Проверяем, есть ли MeshCollider
        MeshCollider meshCollider = hitInfo.collider as MeshCollider;

        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            // Получаем индекс треугольника
            int triangleIndex = hitInfo.triangleIndex;

            // Получаем меш
            Mesh mesh = meshCollider.sharedMesh;

            // Получаем вершины треугольника
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            Vector3 vertexA = meshCollider.transform.TransformPoint(vertices[triangles[triangleIndex * 3]]);
            Vector3 vertexB = meshCollider.transform.TransformPoint(vertices[triangles[triangleIndex * 3 + 1]]);
            Vector3 vertexC = meshCollider.transform.TransformPoint(vertices[triangles[triangleIndex * 3 + 2]]);

            // Вычисляем нормаль треугольника
            normal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;

            Debug.Log($"Нормаль треугольника: {normal}");
        }

        return normal;
    }

    private void UpdateLineRenderer()
    {
        if (!isSelecting) return;
        if (selectedVertices.Count > 1)
        {
            //lineRenderer.positionCount = selectedVertices.Count + 1; // Замыкаем линию
            lineRenderer.positionCount = selectedVertices.Count;
            for (int i = 0; i < selectedVertices.Count; i++)
            {
                lineRenderer.SetPosition(i, selectedVertices[i] + normalBase * 0.01f);
            }

            //lineRenderer.SetPosition(selectedVertices.Count, selectedVertices[0]); // Замыкаем линию
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (isSelecting && selectedVertices.Count > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(startPoint, circleRadius); // Отображаем круг вокруг стартовой точки
        }

        foreach (var edge in edges)
        {
            Gizmos.DrawLine(edge.vertex1pos, edge.vertex2pos);
        }
    }

    private void CreateMeshFromSelection()
    {
        if (selectedPoints.Count < 3) return;

        // Создаем новый объект для выделенной области
        GameObject newWall = new GameObject("NewWall"+numMesh);
        newWall.transform.SetParent(transform);
        walls.Add(newWall);
        MeshRenderer newRenderer = newWall.AddComponent<MeshRenderer>();
        MeshFilter newFilter = newWall.AddComponent<MeshFilter>();

        // Перемещаем объект к исходному мешу
        //newObject.transform.position = meshRenderer.transform.position;
        //newObject.transform.rotation = meshRenderer.transform.rotation;
        newWall.transform.localScale = meshRenderer.transform.localScale;

        // Создаем новый меш
        Mesh newMesh = new Mesh();

        // Преобразуем UV в мировые координаты
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < selectedPoints.Count; i++)
        {
            Vector2 uv = selectedPoints[i];
            Vector3 worldPoint = selectedVertices[i] + normalBase * 0.001f * numMesh;

            vertices.Add(worldPoint);
            uvs.Add(uv);

            //впуклость не работает
            //замена на статик WallPaintFunc  
            /*
            if (i >= 2)
            {
                // Проверяем порядок точек (по часовой или против часовой стрелке)
                if (!IsClockwise(uvs[0], uvs[i - 1], uvs[i]))
                {
                    triangles.Add(0);
                    triangles.Add(i);
                    triangles.Add(i - 1);
                }
                else
                {
                    triangles.Add(0);
                    triangles.Add(i - 1);
                    triangles.Add(i);
                }
            }
        */
        }

        newMesh.vertices = vertices.ToArray();
        var triangles2 = WallPaintFunc.Contour(vertices,normalBase);
        if (triangles2 == null)
        {
            walls.Remove(newWall);
            Destroy(newWall); 
            return;
        } 
        newMesh.triangles = triangles2.ToArray();
        newMesh.uv = uvs.ToArray();
        newMesh.RecalculateNormals();

        newFilter.mesh = newMesh;

        // Копируем материал исходного меша
        Material baseMaterial = meshRenderer.material;
        Material newMaterial = new Material(baseMaterial);

        // Заменяем текстуру, если указана
        if (selectTexture.currentTexture != null)
        {
            newMaterial.mainTexture = selectTexture.currentTexture;
        }
        else
        {
            Debug.LogWarning("Текстура для нового материала не установлена.");
        }

        // Применяем новый материал
        newRenderer.material = newMaterial;
    }
}