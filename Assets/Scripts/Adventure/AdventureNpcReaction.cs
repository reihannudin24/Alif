using System.Collections;
using Alif.Campaign;
using UnityEngine;

namespace Alif.Adventure
{
    public sealed class AdventureNpcReaction : MonoBehaviour
    {
        public SpriteRenderer Body;
        public Sprite ReactionPose;
        public Sprite EmoteSprite;
        public float Duration = 1.2f;

        Animator _animator;
        Sprite _idleSprite;
        SpriteRenderer _emote;
        Coroutine _reaction, _idle;

        void Awake()
        {
            if (!Body) Body = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            if (Body) _idleSprite = Body.sprite;
        }

        void OnEnable()
        {
            if (!CampaignUI.ReducedMotion) _idle = StartCoroutine(LookAround());
        }

        void OnDisable()
        {
            if (_reaction != null) StopCoroutine(_reaction);
            if (_idle != null) StopCoroutine(_idle);
            Restore();
        }

        public void React()
        {
            if (Body && !_idleSprite) _idleSprite = Body.sprite;
            if (_reaction != null) StopCoroutine(_reaction);
            _reaction = StartCoroutine(ReactRoutine());
        }

        IEnumerator ReactRoutine()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (Body && player) Body.flipX = player.transform.position.x < transform.position.x;
            if (_animator && ReactionPose) _animator.enabled = false;
            if (Body && ReactionPose) Body.sprite = ReactionPose;
            if (EmoteSprite)
            {
                if (!_emote)
                {
                    var child = new GameObject("Sprite emote"); child.transform.SetParent(transform, false); child.transform.localPosition = new Vector3(0, 1.2f);
                    _emote = child.AddComponent<SpriteRenderer>(); _emote.sortingOrder = 110;
                }
                _emote.sprite = EmoteSprite; _emote.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(Duration);
            Restore(); _reaction = null;
        }

        IEnumerator LookAround()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(7f, 12f));
                if (_reaction == null && Body) Body.flipX = !Body.flipX;
                yield return new WaitForSeconds(Random.Range(.6f, 1.1f));
                if (_reaction == null && Body) Body.flipX = !Body.flipX;
            }
        }

        void Restore()
        {
            if (_emote) _emote.gameObject.SetActive(false);
            if (Body && _idleSprite) Body.sprite = _idleSprite;
            if (_animator) _animator.enabled = true;
        }
    }
}
