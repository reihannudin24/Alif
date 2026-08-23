using System.Collections;
using TMPro;
using UnityEngine;
using Alif.Dialogue;
using Alif.Systems;

namespace Alif.UI
{
    /// <summary>
    /// Memberi feedback singkat setiap pilihan menggeser neraca 100%.
    /// Komponen ditempel di Canvas (bukan root panel) agar tetap mendengarkan event ketika
    /// panel feedback sedang disembunyikan.
    /// </summary>
    public class ScoreBalanceFeedbackUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private float _visibleSeconds = 3.2f;

        private Coroutine _hideRoutine;
        private bool _subscribed;

        private void OnEnable()
        {
            Subscribe();
            SetVisible(false);
        }

        private void Update()
        {
            // DialogueManager dan Canvas bisa dibuat dengan urutan Awake berbeda, jadi retry
            // ringan ini memastikan subscription tetap terjadi pada startup scene.
            if (!_subscribed)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || DialogueManager.Instance == null)
            {
                return;
            }

            DialogueManager.Instance.OnChoiceSelected += HandleChoiceSelected;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || DialogueManager.Instance == null)
            {
                _subscribed = false;
                return;
            }

            DialogueManager.Instance.OnChoiceSelected -= HandleChoiceSelected;
            _subscribed = false;
        }

        private void HandleChoiceSelected(DialogueChoice choice)
        {
            if (choice == null || ScoreSystem.Instance == null ||
                (Mathf.Approximately(choice.FinancialLogicChange, 0f) &&
                 Mathf.Approximately(choice.ShariaComplianceChange, 0f) &&
                 Mathf.Approximately(choice.BalanceCorrection, 0f)))
            {
                return;
            }

            float financial = ScoreSystem.Instance.FinancialLogicPercent01 * 100f;
            float sharia = ScoreSystem.Instance.ShariaCompliancePercent01 * 100f;
            string explanation = string.IsNullOrWhiteSpace(choice.OutcomeText)
                ? "Pilihanmu menggeser neraca keputusan."
                : choice.OutcomeText;

            if (_messageText != null)
            {
                _messageText.text = $"{explanation}\n<color=#78B7F0>Finansial {financial:0}%</color>  |  <color=#E1B448>Syariah {sharia:0}%</color>\n<color=#FFFFFFB8>Target sehat: 50% / 50%</color>";
            }

            SetVisible(true);
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(_visibleSeconds);
            SetVisible(false);
            _hideRoutine = null;
        }

        private void SetVisible(bool visible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(visible);
            }
        }
    }
}
