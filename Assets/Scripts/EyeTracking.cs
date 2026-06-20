using OpenCvSharp;
using OpenCvSharp.Demo;
using UnityEngine;
using UnityEngine.UI;

public class EyeTracking : WebCamera
{
    public RawImage cameraView;

    WebCamTexture webcam;
    Texture2D output;

    CascadeClassifier faceCascade;

    Mat frameMat;
    Mat grayMat;

    public Vector2 gaze;            // raw (-1 to 1)
    public Vector2 calibratedGaze;  // offset-corrected

    Vector2 calibrationOffset;
    bool calibrated;

    Vector2 filteredGaze;

    //float blinkValue;
    //float blinkFiltered;
    //public bool isBlinking;
    //float blinkTimer;
    //const float BLINK_OPEN_THRESHOLD = 22f;
    //const float BLINK_TIME = 0.12f;

    void Start()
    {
        webcam = new WebCamTexture();
        webcam.Play();

        output = new Texture2D(webcam.width, webcam.height, TextureFormat.RGBA32, false);
        cameraView.texture = output;

        frameMat = new Mat(webcam.height, webcam.width, MatType.CV_8UC4);
        grayMat = new Mat(webcam.height, webcam.width, MatType.CV_8UC1);

        faceCascade = new CascadeClassifier();
        faceCascade.Load("Assets/Resources/haarcascade_frontalface_default.xml");

        this.forceFrontalCamera = true;
    }

    protected override bool ProcessTexture(WebCamTexture input, ref Texture2D output)
    {
        if (!input.didUpdateThisFrame) return false;

        frameMat = OpenCvSharp.Unity.TextureToMat(input, TextureParameters);
        Cv2.CvtColor(frameMat, grayMat, ColorConversionCodes.RGBA2GRAY);
        Cv2.EqualizeHist(grayMat, grayMat);

        var faces = faceCascade.DetectMultiScale(
            grayMat, 1.1, 2, 0,
            new Size(150, 150)
        );

        if (faces.Length == 0)
        {
            output = OpenCvSharp.Unity.MatToTexture(frameMat, output);
            return true;
        }

        OpenCvSharp.Rect face = faces[0];
        Cv2.Rectangle(frameMat, face, Scalar.Blue, 2);

        // === FIXED EYE ROIs (FACE RELATIVE) ===
        OpenCvSharp.Rect leftEyeROI = new OpenCvSharp.Rect(
            face.X + (int)(face.Width * 0.12f),
            face.Y + (int)(face.Height * 0.28f),
            (int)(face.Width * 0.32f),
            (int)(face.Height * 0.22f)
        );

        OpenCvSharp.Rect rightEyeROI = new OpenCvSharp.Rect(
            face.X + (int)(face.Width * 0.56f),
            face.Y + (int)(face.Height * 0.28f),
            (int)(face.Width * 0.32f),
            (int)(face.Height * 0.22f)
        );

        Vector2 gazeSum = Vector2.zero;
        int validEyes = 0;

        ProcessEye(leftEyeROI, ref gazeSum, ref validEyes);
        ProcessEye(rightEyeROI, ref gazeSum, ref validEyes);

        //blinkValue /= 2f; // average both eyes
        //blinkFiltered = Mathf.Lerp(blinkFiltered, blinkValue, 0.3f);

        //// Blink detection
        //if (blinkFiltered < BLINK_OPEN_THRESHOLD)
        //{
        //    blinkTimer += Time.deltaTime;
        //    if (blinkTimer > BLINK_TIME)
        //        isBlinking = true;
        //}
        //else
        //{
        //    blinkTimer = 0f;
        //    isBlinking = false;
        //}

        //blinkValue = 0f;

        if (validEyes > 0)
        {
            gaze = gazeSum / validEyes;

            // Clamp before filtering
            gaze = Vector2.ClampMagnitude(gaze, 0.7f);

            // Temporal smoothing
            filteredGaze = Vector2.Lerp(filteredGaze, gaze, 0.15f);

            calibratedGaze = calibrated
                ? filteredGaze - calibrationOffset
                : filteredGaze;
        }

        output = OpenCvSharp.Unity.MatToTexture(frameMat, output);
        return true;
    }

    void ProcessEye(OpenCvSharp.Rect roi, ref Vector2 gazeSum, ref int validEyes)
    {
        if (roi.X < 0 || roi.Y < 0 ||
            roi.Right >= grayMat.Width ||
            roi.Bottom >= grayMat.Height)
            return;

        Mat eyeMat = grayMat.SubMat(roi);
        Cv2.GaussianBlur(eyeMat, eyeMat, new Size(7, 7), 0);

        Cv2.AdaptiveThreshold(
            eyeMat,
            eyeMat,
            255,
            AdaptiveThresholdTypes.GaussianC,
            ThresholdTypes.BinaryInv,
            11,
            2
        );

        // Measure intensity variance (open eye = high variance)
        Cv2.MeanStdDev(eyeMat, out _, out Scalar stddev);
        //blinkValue += (float)stddev.Val0;

        // === PUPIL DETECTION ===
        Cv2.FindContours(
            eyeMat,
            out Point[][] contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple
        );

        double maxArea = 0;
        Point pupil = new Point(-1, -1);

        foreach (var c in contours)
        {
            double area = Cv2.ContourArea(c);
            if (area > maxArea)
            {
                Moments m = Cv2.Moments(c);
                if (m.M00 > 0)
                {
                    pupil = new Point(
                        (int)(m.M10 / m.M00),
                        (int)(m.M01 / m.M00)
                    );
                    maxArea = area;
                }
            }
        }

        if (pupil.X < 0) return;

        Point eyeCenter = new Point(roi.Width / 2, roi.Height / 2);

        float gazeX = (float)(pupil.X - eyeCenter.X) / eyeCenter.X;
        float gazeY = (float)(pupil.Y - eyeCenter.Y) / eyeCenter.Y;

        gazeSum += new Vector2(gazeX, -gazeY);
        validEyes++;

        // Debug visuals
        Cv2.Rectangle(frameMat, roi, Scalar.Green, 1);
        Cv2.Circle(
            frameMat,
            new Point(roi.X + pupil.X, roi.Y + pupil.Y),
            4,
            Scalar.Red,
            -1
        );
    }

    public void SetCalibrationOffset(Vector2 offset)
    {
        calibrationOffset = offset;
        calibrated = true;
    }
}