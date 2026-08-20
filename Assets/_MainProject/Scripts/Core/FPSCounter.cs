using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private bool isUseFrr = true;
    [SerializeField] private bool isShowFPS;
    [SerializeField] private bool isRequestDontSleep = true;
    [SerializeField] private int frameRateTarget = 60;
    private float _deltaTime;

    private void Start()
    {
        if (isUseFrr) Application.targetFrameRate = frameRateTarget;

        if (isRequestDontSleep) Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void Update()
    {
        if (isShowFPS) _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        if (!isShowFPS) return;
        int w = Screen.width, h = Screen.height;

        var style = new GUIStyle();

        var height = h * 2 / 100;
        var rect = new Rect(0, 0, w, height);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 3 / 100;
        style.normal.textColor = new Color(0f, 1f, 0f, 1.0f);
        var msec = _deltaTime * 1000.0f;
        var fps = 1.0f / _deltaTime;
        var text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

        GUI.Label(rect, text, style);
    }
}