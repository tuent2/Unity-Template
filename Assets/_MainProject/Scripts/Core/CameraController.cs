using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Rooms
{
    public class CameraController : MonoBehaviour
    {
        private const float Width = 1080f;
        private const float Height = 2160f;

        public static CameraController Instance { get; private set; }
        public static Camera MainCamera => Instance != null ? Instance.GetMainCamera() : Camera.main;
        public static Camera UICamera => Instance != null ? Instance.GetUICamera() : Camera.main;

        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera mainCameraUI;
        [SerializeField] private float uiCanvasPlaneDistance = 100f;
        private Tween shakeCameraTween;

        public float ValueSafeArea { get; private set; } = 1f;
        public Vector2 SafeAreaWorldInsets { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CalculateSafeArea();
        }

        public void SetCamera(Camera cameraRef, Camera uiCameraRef = null)
        {
            if (cameraRef != null)
                mainCamera = cameraRef;

            if (uiCameraRef != null)
                mainCameraUI = uiCameraRef;

            CalculateSafeArea();
        }

        public void FocusOnPosition(
            Vector3 targetPosition,
            float targetZoom,
            float moveDuration,
            float zoomDuration,
            float offsetY,
            bool instant = false,
            Action onCompleted = null)
        {
            Camera cameraRef = GetMainCamera();
            if (cameraRef == null)
            {
                onCompleted?.Invoke();
                return;
            }

            cameraRef.DOKill(true);
            cameraRef.transform.DOKill(true);

            if (instant)
            {
                cameraRef.transform.position = targetPosition;
                ApplyZoom(cameraRef, targetZoom);
                onCompleted?.Invoke();
                return;
            }

            Tween moveTween = GameViewMoveToPos(targetPosition, offsetY, true, moveDuration);
            Tween zoomTween = CreateZoomTween(cameraRef, targetZoom, zoomDuration);
            if (zoomTween != null)
                zoomTween.SetEase(Ease.Linear);

            if (onCompleted == null)
                return;

            if (moveTween == null && zoomTween == null)
            {
                onCompleted.Invoke();
                return;
            }

            Sequence focusSequence = DOTween.Sequence();
            if (moveTween != null)
                focusSequence.Join(moveTween);
            if (zoomTween != null)
                focusSequence.Join(zoomTween);
            focusSequence.OnComplete(() => onCompleted.Invoke());
        }

        private Tween GameViewMoveToPos(Vector3 worldPosition, float offsetY, bool isIgnoreOffset = false, float duration = 0.5f)
        {
            Camera cameraRef = GetMainCamera();
            if (cameraRef == null)
                return null;

            Vector3 targetPos = worldPosition;
            if (!isIgnoreOffset)
                targetPos.y -= offsetY;

            cameraRef.transform.DOKill(true);
            return cameraRef.transform.DOMove(targetPos, duration).SetEase(Ease.InQuad);
        }

        private void ForceGameViewPos(Vector3 worldPosition, float zoomDuration, float offsetY, bool isIgnoreOffset = false)
        {
            Camera cameraRef = GetMainCamera();
            if (cameraRef == null)
                return;

            Vector3 targetPos = worldPosition;
            if (!isIgnoreOffset)
                targetPos.y -= offsetY;

            cameraRef.transform.DOKill(true);
            cameraRef.transform.DOMove(targetPos, zoomDuration).SetEase(Ease.Linear);
        }

        public static Vector3 WorldToUILocalPosition(Vector3 worldPositionInMainCamera)
        {
            if (Instance == null)
                return worldPositionInMainCamera;

            Camera worldCamera = Instance.GetMainCamera();
            Camera uiCamera = Instance.GetUICamera();
            if (worldCamera == null)
                return worldPositionInMainCamera;

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPositionInMainCamera);
            if (uiCamera == null || uiCamera == worldCamera)
            {
                screenPoint.z = 0f;
                return screenPoint;
            }

            screenPoint.z = Instance.GetUICanvasPlaneDistance();
            Vector3 uiWorldPosition = uiCamera.ScreenToWorldPoint(screenPoint);
            uiWorldPosition.z = 0f;
            return uiWorldPosition;
        }

        public void ShakeCamera(float duration = 0.3f, float strength = 0.2f, int vibrato = 30, float randomness = 90f)
        {
            Transform cameraTransform = GetCameraTransform();
            if (cameraTransform == null)
                return;

            shakeCameraTween?.Kill();
            shakeCameraTween = cameraTransform.DOShakePosition(duration, strength, vibrato, randomness, false, true);
        }

        public void ShakeCameraHorizontal(float duration = 0.3f, float strength = 0.32f, int vibrato = 20, bool fadeOut = true)
        {
            Transform cameraTransform = GetCameraTransform();
            if (cameraTransform == null)
                return;

            shakeCameraTween?.Kill();
            shakeCameraTween = cameraTransform.DOShakePosition(
                0.2f,
                new Vector3(strength, 0f, 0f),
                vibrato,
                0f,
                false,
                fadeOut);
        }

        public void StopShakeCamera()
        {
            shakeCameraTween?.Kill();
            shakeCameraTween = null;
        }

        public void CalculateSafeArea()
        {
            ValueSafeArea = (Screen.height / (float)Screen.width) / (Height / Width);
            if (ValueSafeArea < 1f)
                ValueSafeArea = 1f;

            SafeAreaWorldInsets = GetSafeAreaWorldInsets();
        }

        public Vector2 GetSafeAreaWorldInsets()
        {
            Camera cameraRef = GetMainCamera();
            if (cameraRef == null)
                return Vector2.zero;

            float screenHeight = Screen.height;
            Rect safe = Screen.safeArea;

            float bottomInsetPx = safe.yMin;
            float topInsetPx = screenHeight - safe.yMax;

            Vector3 worldBottomInset = cameraRef.ScreenToWorldPoint(new Vector3(0f, bottomInsetPx, cameraRef.nearClipPlane));
            Vector3 worldBottomScreen = cameraRef.ScreenToWorldPoint(new Vector3(0f, 0f, cameraRef.nearClipPlane));
            float bottomWorldDistance = Mathf.Abs(worldBottomInset.y - worldBottomScreen.y);

            Vector3 worldTopInset = cameraRef.ScreenToWorldPoint(new Vector3(0f, screenHeight, cameraRef.nearClipPlane));
            Vector3 worldTopSafe = cameraRef.ScreenToWorldPoint(new Vector3(0f, safe.yMax, cameraRef.nearClipPlane));
            float topWorldDistance = Mathf.Abs(worldTopInset.y - worldTopSafe.y);

            return new Vector2(bottomWorldDistance, topWorldDistance);
        }

        private Camera GetMainCamera()
        {
            if (mainCamera != null)
                return mainCamera;

            mainCamera = Camera.main;
            return mainCamera;
        }

        private Camera GetUICamera()
        {
            if (mainCameraUI != null)
                return mainCameraUI;

            return mainCamera != null ? mainCamera : Camera.main;
        }

        private float GetUICanvasPlaneDistance()
        {
            return uiCanvasPlaneDistance;
        }

        private Transform GetCameraTransform()
        {
            Camera cameraRef = GetMainCamera();
            return cameraRef != null ? cameraRef.transform : null;
        }

        private Tween CreateZoomTween(Camera cameraRef, float targetZoom, float zoomDuration)
        {
            if (cameraRef.orthographic)
                return cameraRef.DOOrthoSize(targetZoom, zoomDuration);

            return cameraRef.DOFieldOfView(targetZoom, zoomDuration);
        }

        private void ApplyZoom(Camera cameraRef, float targetZoom)
        {
            if (cameraRef.orthographic)
            {
                cameraRef.orthographicSize = targetZoom;
                return;
            }

            cameraRef.fieldOfView = targetZoom;
        }
    }
}
