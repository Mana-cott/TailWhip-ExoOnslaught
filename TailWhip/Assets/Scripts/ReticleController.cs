using UnityEngine;

public class ReticleController : MonoBehaviour
{
    [Header("Reticle Settings")]
    [SerializeField] private float maxHorizontalOffset = 60f;
    [SerializeField] private float verticalOffset = 0f;
    [SerializeField] private float smoothTime = 0.12f;
    private RectTransform rectTransform;
     private float currX;
     private float velocityY;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void UpdateReticle(float horizontalInput)
    {
        float targetX = Mathf.Clamp(horizontalInput, -1f, 1f) * maxHorizontalOffset;
        currX = Mathf.SmoothDamp(currX, targetX, ref velocityY, smoothTime);

        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        rectTransform.position = center + new Vector2(currX, verticalOffset);
    }

    public Vector2 GetReticlePosition()
    {
        return rectTransform.position;
    }

    public float GetNormalizedOffset()
    {
        return maxHorizontalOffset > 0f ? currX / maxHorizontalOffset : 0f;
    }
}
