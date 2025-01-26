using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class WallPaintFunc
{
    private static List<int> triangles = new List<int>(); // Индексы треугольников
    private static Vector3 direction = Vector3.forward; 
    public static List<int> Contour(List<Vector3> points, Vector3 dir)
    {
        direction = dir;
        triangles.Clear();
        // 1. Проверка: должно быть минимум 3 точки
        if (points.Count < 3) return null;

        // 2. Проекция в 2D
        List<Vector2> projectedPoints = ProjectTo2D(points);

        // 3. Инициализация
        List<int> indices = new List<int>();
        List<int> remaining = new List<int>();
        for (int i = 0; i < projectedPoints.Count; i++) remaining.Add(i);

        // 4. Алгоритм Ear Clipping
        while (remaining.Count > 3)
        {
            bool earFound = false;

            for (int i = 0; i < remaining.Count; i++)
            {
                int prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                int curr = remaining[i];
                int next = remaining[(i + 1) % remaining.Count];

                if (IsEar(prev, curr, next, projectedPoints, remaining))
                {
                    // Проверка выпуклости треугольника
                    if (!IsClockwise(points[prev], points[curr], points[next]))
                    {
                        // Добавляем треугольник
                        indices.Add(prev);
                        indices.Add(curr);
                        indices.Add(next);
                    }
                    else
                    {
                        indices.Add(prev);
                        indices.Add(next);
                        indices.Add(curr);
                    }

                    // Удаляем текущее ухо
                    remaining.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            // Если ни одно ухо не найдено, контур некорректен
            if (!earFound)
            {
                Debug.LogError("Не удалось найти ухо. Контур, возможно, самопересекается.");
                return null;
            }
        }

        // Последний треугольник
        /*
        indices.Add(remaining[0]);
        indices.Add(remaining[1]);
        indices.Add(remaining[2]);
        */

        // Проверка выпуклости треугольника
        if (!IsClockwise(points[remaining[0]], points[remaining[1]], points[remaining[2]]))
        {
            // Добавляем треугольник
            indices.Add(remaining[0]);
            indices.Add(remaining[1]);
            indices.Add(remaining[2]);
        }
        else
        {
            indices.Add(remaining[0]);
            indices.Add(remaining[2]);
            indices.Add(remaining[1]);
        }
        
        return indices;
    }

    static List<Vector2> ProjectTo2D(List<Vector3> points)
    {
        // Проецируем на самую "расправленную" плоскость (например, XY)
        List<Vector2> projected = new List<Vector2>();
        foreach (Vector3 point in points)
        {
            projected.Add(new Vector2(point.x, point.z)); // Пример: проекция на XZ-плоскость
        }
        return projected;
    }

    static bool IsEar(int prev, int curr, int next, List<Vector2> points, List<int> remaining)
    {
        Vector2 a = points[prev];
        Vector2 b = points[curr];
        Vector2 c = points[next];

        // Проверка на отсутствие других точек внутри треугольника
        for (int i = 0; i < remaining.Count; i++)
        {
            int idx = remaining[i];
            if (idx == prev || idx == curr || idx == next) continue;

            //не работает надо починить
            if (PointInTriangle(points[idx], a, b, c)) return false;
        }

        return true;
    }
    
    //Проверяем нормаль треугольника
    static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
    {
        // Вычисляем направление: Z-компонента векторного произведения
        float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        return cross < 0;
    }
    
    static bool IsClockwise(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC)
    {
        // Вычисляем нормаль плоскости треугольника
        Vector3 normal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;

        // Определяем произвольное направление просмотра (например, от камеры или в мировом пространстве)
        Vector3 viewDirection = direction;

        // Если нормаль направлена противоположно viewDirection, инвертируем её
        if (Vector3.Dot(normal, viewDirection) < 0)
        {
            normal = -normal;
        }

        // Вычисляем знак площади проекции в плоскости треугольника
        float signedArea = Vector3.Dot(normal, Vector3.Cross(vertexB - vertexA, vertexC - vertexA));

        // Если площадь положительная — вершины против часовой стрелки, если отрицательная — по часовой
        return signedArea < 0;
    }

    static bool PointInTriangle(Vector3 point, Vector3 vertexA, Vector3 vertexB, Vector3 vertexC)
    {
        // Векторы треугольника
        Vector3 v0 = vertexC - vertexA;
        Vector3 v1 = vertexB - vertexA;
        Vector3 v2 = point - vertexA;

        // Вычисляем скалярные произведения
        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);

        // Вычисляем барицентрические координаты
        float denominator = dot00 * dot11 - dot01 * dot01;
        if (Mathf.Abs(denominator) < Mathf.Epsilon) return false; // Треугольник вырожденный

        float u = (dot11 * dot02 - dot01 * dot12) / denominator;
        float v = (dot00 * dot12 - dot01 * dot02) / denominator;

        // Проверяем, находится ли точка внутри треугольника
        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }
    
    static bool PointInTriangle2(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // Проверка, находится ли точка внутри треугольника (метод площадей)
        float areaOrig = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
        float area1 = Mathf.Abs((a.x - p.x) * (b.y - p.y) - (b.x - p.x) * (a.y - p.y));
        float area2 = Mathf.Abs((b.x - p.x) * (c.y - p.y) - (c.x - p.x) * (b.y - p.y));
        float area3 = Mathf.Abs((c.x - p.x) * (a.y - p.y) - (a.x - p.x) * (c.y - p.y));

        return Mathf.Approximately(areaOrig, area1 + area2 + area3);
    }
    
    // Метод для нахождения ближайшей точки на ребре к произвольной точке
    public static Vector3 FindEdgePoint(Vector3 point, Vector3 vertexA, Vector3 vertexB)
    {
        // Вектор ребра AB
        Vector3 AB = vertexB - vertexA;

        // Вектор от A к P
        Vector3 AP = point - vertexA;

        // Проекция AP на AB
        float t = Vector3.Dot(AP, AB) / Vector3.Dot(AB, AB);

        // Ограничиваем t в диапазоне [0, 1]
        t = Mathf.Clamp01(t);

        // Вычисляем ближайшую точку на ребре
        return vertexA + t * AB;
    }
}
