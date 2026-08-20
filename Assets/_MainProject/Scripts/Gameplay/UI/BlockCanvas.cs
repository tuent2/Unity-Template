using GameUp.Core;
using UnityEngine;

namespace Gameplay.UI
{
    /// <summary>
    /// Overlay canvas chặn input người chơi. Dùng ref-count qua <see cref="PushBlock"/> / <see cref="PopBlock"/>.
    /// Khi <see cref="Count"/> &gt; 0 thì chặn raycast; khi về 0 thì mở lại.
    /// Khi <see cref="Time.timeScale"/> = 0 thì không chặn để popup/UI pause vẫn nhận input.
    /// </summary>
    public sealed class BlockCanvas : MonoSingleton<BlockCanvas>
    {
        [SerializeField] private CanvasGroup canvasGroup;

        private int _count;
        private bool _wasTimeScalePaused;

        public int Count => _count;
        public bool IsBlocking => _count > 0;

        protected override void Awake()
        {
            base.Awake();
            ResolveCanvasGroup();
            _wasTimeScalePaused = IsTimeScalePaused;
            RefreshBlockingState();
        }

        private void Update()
        {
            bool isTimeScalePaused = IsTimeScalePaused;
            if (isTimeScalePaused == _wasTimeScalePaused)
                return;

            _wasTimeScalePaused = isTimeScalePaused;
            RefreshBlockingState();
        }

        public void PushBlock()
        {
            _count++;
            RefreshBlockingState();
        }

        public void PopBlock()
        {
            if (_count <= 0)
            {
                GULogger.Warning(nameof(BlockCanvas), "PopBlock called when Count is already 0.");
                return;
            }

            _count--;
            RefreshBlockingState();
        }

        public void ResetBlock()
        {
            _count = 0;
            RefreshBlockingState();
        }

        private void ResolveCanvasGroup()
        {
            if (canvasGroup != null)
                return;

            canvasGroup = GetComponent<CanvasGroup>();
        }

        private static bool IsTimeScalePaused => Time.timeScale <= 0f;

        private bool ShouldBlockRaycasts => IsBlocking && !IsTimeScalePaused;

        private void RefreshBlockingState()
        {
            if (canvasGroup == null)
                return;

            bool block = ShouldBlockRaycasts;
            canvasGroup.blocksRaycasts = block;
            canvasGroup.interactable = !block;
        }
    }
}
