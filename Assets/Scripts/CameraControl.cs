using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public EyeTracking eyeTracker;

    [Header("Look Settings")]
    public float maxYaw = 35f;
    public float maxPitch = 25f;
    public float smoothing = 6f;

    Quaternion neutralRotation;

    void Start()
    {
        neutralRotation = transform.localRotation;
    }

    void Update()
    {
        if (Calibration.Instance.isCalibrating)
        {
            transform.localRotation = neutralRotation;
            return;
        }

        Vector2 gaze = eyeTracker.calibratedGaze;
        gaze = Vector2.ClampMagnitude(gaze, 1f);

        // --- Soft saturation ---
        float yaw = Mathf.Tan(gaze.x) * maxYaw;
        float pitch = Mathf.Tan(gaze.y) * maxPitch;

        Quaternion target = Quaternion.Euler(-pitch, yaw, 0);
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            neutralRotation * target,
            smoothing * Time.deltaTime
        );
    }
}