using UnityEngine;
using UnityEngine.UI;

public class CursorControl : MonoBehaviour
{
    public EyeTracking eyeTracker;

    [Header("Movement")]
    public float screenMargin = 0.45f;
    public float smoothing = 12f;

    [Header("Blink")]
    float lastBlinkTime;
    public float blinkCooldown = 0.5f;

    [Header("Dwell")]
    public float dwellTime = 2.5f;
    public float dwellRadius = 12f;

    RectTransform rect;
    Vector2 smoothedPos;
    Vector2 lastPos;
    float dwellTimer;

    Image image;
    public Sprite handOpen;
    public Sprite handClosed;
    bool clicking;

    bool hasObject;
    HoldableObject objInHand;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        smoothedPos = rect.anchoredPosition;
        clicking = false;
        hasObject = false;
        objInHand = null;
    }

    void Update()
    {
        if (Calibration.Instance.isCalibrating) return;

        MoveCursor();

        //// --- Blick detection ---
        //if (eyeTracker.isBlinking && Time.time - lastBlinkTime > blinkCooldown)
        //{
        //    TriggerClick();
        //    lastBlinkTime = Time.time;
        //}

        // --- Dwell detection ---
        if (Vector2.Distance(smoothedPos, lastPos) < dwellRadius)
            dwellTimer += Time.deltaTime;
        else
            dwellTimer = 0f;

        lastPos = smoothedPos;

        if (dwellTimer >= dwellTime)
        {
            TriggerClick();
            dwellTimer = 0f;
        }
    }

    void MoveCursor()
    {
        Vector2 gaze = eyeTracker.calibratedGaze;

        // --- Clamp gaze ---
        gaze = Vector2.ClampMagnitude(gaze, 1f);

        // --- Absolute screen mapping ---
        Vector2 target = new Vector2(
            gaze.x * Screen.width * screenMargin,
            gaze.y * Screen.height * screenMargin
        );

        // --- Smooth cursor ---
        smoothedPos = Vector2.Lerp(
            smoothedPos,
            target,
            smoothing * Time.deltaTime
        );

        rect.anchoredPosition = smoothedPos;
    }

    void TriggerClick()
    {
        Debug.Log("Gaze click");
        if (clicking)
        {
            Release();
        }
        else
        {
            Grab();
        }
    }

    void Grab()
    {
        clicking = true;
        image.sprite = handClosed;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, rect.position);
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 999f))
        {
            HoldableObject obj = hit.collider.gameObject.GetComponent<HoldableObject>();
            if (obj)
            {
                obj.PickUp(gameObject);
                objInHand = obj;
                hasObject = true;
            }
            else 
                Release();
        }
    }

    void Release()
    {
        clicking = false;
        image.sprite = handOpen;
        if (hasObject)
        {
            objInHand.Drop();
            objInHand = null;
            hasObject = false;
        }
    }
}