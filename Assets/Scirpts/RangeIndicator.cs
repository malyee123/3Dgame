using System;
using UnityEngine;

public class RangeIndicator : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private const int segments = 64;

    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 0f, 0f, 0.8f);
        lineRenderer.endColor = new Color(1f, 0f, 0f, 0.8f);
        lineRenderer.sortingLayerName = "Default";
        lineRenderer.sortingOrder = 5;
    }

    public void SetRange(float radius)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lineRenderer.SetPosition(i, transform.position + new Vector3(x, y, 0f));
        }
    }

    internal void SetRange(object appliedRange)
    {
        throw new NotImplementedException();
    }
}