// Sound hooks; runs silent until clips are assigned.

using UnityEngine;

namespace Polarity.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameAudio : MonoBehaviour
    {
        [Header("Clips (optional — the game runs silent without them)")]
        [SerializeField] private AudioClip slide;
        [SerializeField] private AudioClip annihilate;
        [SerializeField] private AudioClip neutronBreak;
        [SerializeField] private AudioClip blocked;
        [SerializeField] private AudioClip undo;
        [SerializeField] private AudioClip win;
        [SerializeField] private AudioClip lose;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        public void PlaySlide() => Play(slide, 1f);
        public void PlayBlocked() => Play(blocked, 0.9f);
        public void PlayUndo() => Play(undo, 1f);
        public void PlayNeutronBreak() => Play(neutronBreak, 1f);
        public void PlayWin() => Play(win, 1f);
        public void PlayLose() => Play(lose, 1f);

        public void PlayAnnihilation(int pairCount) =>
            Play(annihilate, 1f + Mathf.Clamp(pairCount - 1, 0, 6) * 0.06f);

        private void Play(AudioClip clip, float pitch)
        {
            if (clip == null || _source == null) return;

            _source.pitch = pitch;
            _source.PlayOneShot(clip, volume);
        }
    }
}
