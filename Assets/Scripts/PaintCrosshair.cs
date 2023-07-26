using UnityEngine;
using UnityEngine.UI;

public class PaintCrosshair : MonoBehaviour
{
    [SerializeField] private PaintDrawer paintDrawer;
    [SerializeField] private Texture canvasPaintTexture;
    [SerializeField] private Texture defaultTexture;
    [SerializeField] private RawImage crosshair;
    [SerializeField] private RectTransform crosshairRect;

    private void Update()
    {
        if (paintDrawer.withinRange && paintDrawer.hoveringOverCanvas)
        {
            crosshair.texture = canvasPaintTexture;
            crosshairRect.sizeDelta = Vector2.one * Mathf.Lerp(8, 80, paintDrawer.penSizeMultiplier) * Mathf.Lerp(3, 1, paintDrawer.canvasHitDistance);
            return;
        }

        crosshair.texture = defaultTexture;
        crosshairRect.sizeDelta = Vector2.one * 32;
    }
}