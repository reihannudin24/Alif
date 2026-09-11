using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alif.UI
{
    /// <summary>
    /// Menampilkan tujuan cerita aktif sebagai checklist ringkas. Logic cerita tetap berada
    /// di controller chapter; komponen ini hanya menyimpan dan menggambar state tampilan.
    /// </summary>
    public class ObjectiveUI : MonoBehaviour
    {
        public static ObjectiveUI Instance { get; private set; }

        [Header("References")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _stepsText;
        [SerializeField] private Image _accentImage;

        [Header("Colors")]
        [SerializeField] private Color _activeColor = new Color(0.95f, 0.69f, 0.2f, 1f);
        [SerializeField] private Color _completeColor = new Color(0.48f, 0.78f, 0.39f, 1f);
        [SerializeField] private Color _pendingColor = new Color(1f, 1f, 1f, 0.55f);

        private string _title = "Tujuan saat ini";
        private string[] _steps = Array.Empty<string>();
        private int _currentStep;
        private bool _isComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Render();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <param name="currentStep">Index langkah aktif. Semua index sebelumnya dianggap selesai.</param>
        public void SetObjective(string title, string[] steps, int currentStep = 0)
        {
            _title = string.IsNullOrWhiteSpace(title) ? "Tujuan saat ini" : title;
            _steps = steps ?? Array.Empty<string>();
            _currentStep = Mathf.Clamp(currentStep, 0, _steps.Length);
            _isComplete = _steps.Length > 0 && _currentStep >= _steps.Length;
            Render();
        }

        public void SetCurrentStep(int currentStep)
        {
            _currentStep = Mathf.Clamp(currentStep, 0, _steps.Length);
            _isComplete = _steps.Length > 0 && _currentStep >= _steps.Length;
            Render();
        }

        public void CompleteObjective()
        {
            _currentStep = _steps.Length;
            _isComplete = _steps.Length > 0;
            Render();
        }

        private void Render()
        {
            if (_titleText != null)
            {
                _titleText.text = _title;
            }

            if (_progressText != null)
            {
                int completedSteps = _isComplete ? _steps.Length : Mathf.Min(_currentStep, _steps.Length);
                _progressText.text = _steps.Length == 0
                    ? string.Empty
                    : _isComplete ? "SELESAI" : $"{completedSteps}/{_steps.Length}";
                _progressText.color = _isComplete ? _completeColor : _activeColor;
            }

            if (_accentImage != null)
            {
                _accentImage.color = _isComplete ? _completeColor : _activeColor;
            }

            if (_stepsText == null)
            {
                return;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < _steps.Length; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                string color = ColorUtility.ToHtmlStringRGBA(
                    i < _currentStep || _isComplete
                        ? _completeColor
                        : i == _currentStep ? _activeColor : _pendingColor);

                if (i < _currentStep || _isComplete)
                {
                    builder.Append($"<color=#{color}>[OK] {_steps[i]}</color>");
                }
                else if (i == _currentStep)
                {
                    builder.Append($"<color=#{color}><b>> {_steps[i]}</b></color>");
                }
                else
                {
                    builder.Append($"<color=#{color}>[ ] {_steps[i]}</color>");
                }
            }

            _stepsText.text = builder.ToString();
        }
    }
}
