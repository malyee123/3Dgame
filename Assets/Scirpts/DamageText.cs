using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float fadeDuration = 0.8f;

    private TextMeshPro text;
    private float timer = 0f;
    private Color originalColor;
    private bool initialized = false;
    private Transform target;
    private Vector3 offset;

    public void Init(float damage, Transform followTarget)
    {
        text = GetComponent<TextMeshPro>();
        if (text == null) { Destroy(gameObject); return; }
        text.text = ((int)damage).ToString();
        originalColor = text.color;
        target = followTarget;
        offset = transform.position - followTarget.position;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
        timer += Time.deltaTime;

        if (target != null)
            transform.position = target.position + offset + Vector3.up * moveSpeed * timer;
        else
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        if (timer >= fadeDuration) Destroy(gameObject);
    }
}