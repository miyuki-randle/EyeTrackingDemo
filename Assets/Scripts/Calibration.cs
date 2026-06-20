using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class Calibration : MonoBehaviour
{
    public static Calibration Instance { get; private set; }

    public EyeTracking eyeTracker;
    public GameObject calibrationDot;

    public float calibrationDuration = 2f;

    Vector2 sum;
    int samples;

    public bool isCalibrating = true;

    void Start()
    {
        if (Instance == null)
            Instance = this;
        StartCoroutine(Calibrate());
    }

    IEnumerator Calibrate()
    {
        calibrationDot.SetActive(true);
        sum = Vector2.zero;
        samples = 0;

        float timer = 0f;

        while (timer < calibrationDuration)
        {
            sum += eyeTracker.gaze;
            samples++;

            timer += Time.deltaTime;
            yield return null;
        }

        Vector2 offset = sum / samples;
        eyeTracker.SetCalibrationOffset(offset);

        calibrationDot.SetActive(false);
        Debug.Log("Calibration complete: " + offset);
        isCalibrating = false;
    }

    public void Recalibrate()
    {
        isCalibrating = true;
        StartCoroutine(Calibrate());
    }
}
