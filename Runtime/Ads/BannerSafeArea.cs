using UnityEngine;

namespace AdsIAPKit
{
    /// <summary>
    /// Keeps a UI panel clear of the banner ad. Put it on the RectTransform that holds a scene's
    /// content (stretched to fill its parent, not the background): it insets the bottom edge
    /// (or the top edge for a top banner) by the banner's height while a banner is on screen,
    /// and restores the original offsets when the banner hides or ads are removed.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("AdsIAPKit/Banner Safe Area")]
    public class BannerSafeArea : MonoBehaviour
    {
        [Tooltip("Reserve the space as soon as a banner is requested, before it loads, so the layout does not jump when it appears.")]
        public bool reserveBeforeLoad = true;

        [Tooltip("Height to reserve before the banner loads, in dp. Phone banners are 50-60 dp, tablet banners 90 dp.")]
        public float reservedHeightDp = 50f;

        [Tooltip("Extra gap between the banner and the content, in canvas units.")]
        public float padding = 0f;

        private RectTransform _rect;
        private Canvas        _canvas;
        private Vector2       _baseOffsetMin;
        private Vector2       _baseOffsetMax;
        private bool          _captured;
        private float         _lastScale = -1f;
        private float         _lastScreenHeight = -1f;

        /// <summary>The inset currently applied, in canvas units.</summary>
        public float CurrentInset { get; private set; }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            CaptureBaseOffsets();
        }

        private void OnEnable()
        {
            AdsManager.OnBannerLayoutChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            AdsManager.OnBannerLayoutChanged -= Apply;
            Restore();
        }

        private void LateUpdate()
        {
            // Re-apply when the canvas scale or screen changes (rotation, resolution, Device Simulator).
            var scale = _canvas != null ? _canvas.rootCanvas.scaleFactor : 1f;
            if (!Mathf.Approximately(scale, _lastScale) || !Mathf.Approximately(Screen.height, _lastScreenHeight))
                Apply();
        }

        /// <summary>
        /// Call after changing the RectTransform's offsets from code so they become the new baseline.
        /// </summary>
        public void CaptureBaseOffsets()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            _baseOffsetMin = _rect.offsetMin;
            _baseOffsetMax = _rect.offsetMax;
            _captured = true;
        }

        /// <summary>Recomputes the inset from the current banner state.</summary>
        public void Apply()
        {
            if (!_captured) CaptureBaseOffsets();
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

            _lastScale = _canvas != null ? _canvas.rootCanvas.scaleFactor : 1f;
            _lastScreenHeight = Screen.height;

            var heightPixels = AdsManager.IsBannerLoaded
                ? AdsManager.BannerHeightPixels
                : reserveBeforeLoad && AdsManager.IsBannerRequested ? AdsManager.DpToPixels(reservedHeightDp) : 0f;

            var inset = heightPixels > 0f ? heightPixels / Mathf.Max(0.0001f, _lastScale) + padding : 0f;
            CurrentInset = inset;

            var offsetMin = _baseOffsetMin;
            var offsetMax = _baseOffsetMax;
            switch (AdsManager.BannerEdge)
            {
                case BannerEdge.Bottom: offsetMin.y += inset; break;
                case BannerEdge.Top:    offsetMax.y -= inset; break;
            }

            if (_rect.offsetMin != offsetMin) _rect.offsetMin = offsetMin;
            if (_rect.offsetMax != offsetMax) _rect.offsetMax = offsetMax;
        }

        private void Restore()
        {
            if (!_captured || _rect == null) return;
            _rect.offsetMin = _baseOffsetMin;
            _rect.offsetMax = _baseOffsetMax;
            CurrentInset = 0f;
        }
    }
}
