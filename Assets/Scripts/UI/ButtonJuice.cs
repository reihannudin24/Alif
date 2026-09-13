using UnityEngine;
using UnityEngine.EventSystems;
using LitMotion;
using LitMotion.Extensions;

namespace Alif.UI
{
    /// <summary>
    /// Micro-animation juice for UI buttons using LitMotion.
    /// Provides punchy scale animations on hover, selection, and click.
    /// </summary>
    [DisallowMultipleComponent]
    public class ButtonJuice : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private float _pressScale = 0.95f;
        [SerializeField] private float _duration = 0.12f;

        private Vector3 _originalScale = Vector3.one;
        private MotionHandle _motionHandle;
        private bool _isPointerOver = false;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            transform.localScale = _originalScale;
        }

        private void OnDisable()
        {
            if (_motionHandle.IsActive()) _motionHandle.Cancel();
            transform.localScale = _originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;
            AnimateScale(_originalScale * _hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;
            AnimateScale(_originalScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateScale(_originalScale * _pressScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateScale(_isPointerOver ? _originalScale * _hoverScale : _originalScale);
        }

        public void OnSelect(BaseEventData eventData)
        {
            AnimateScale(_originalScale * _hoverScale);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!_isPointerOver)
            {
                AnimateScale(_originalScale);
            }
        }

        private void AnimateScale(Vector3 targetScale)
        {
            if (_motionHandle.IsActive())
            {
                _motionHandle.Cancel();
            }

            _motionHandle = LMotion.Create(transform.localScale, targetScale, _duration)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(transform);
        }
    }
}
