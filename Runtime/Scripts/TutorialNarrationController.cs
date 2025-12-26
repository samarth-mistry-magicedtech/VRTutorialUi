using UnityEngine;
using MagicedTech.TutorialUI;

namespace MagicedTech.TutorialUI
{
    /// <summary>
    /// Simple narration controller that plays an AudioClip for each tutorial slide.
    /// VR usage: attach alongside TutorialUiController and assign an AudioSource and
    /// per-slide audio clips via the inspector.
    /// </summary>
    public class TutorialNarrationController : MonoBehaviour
    {
        [System.Serializable]
        private class SlideNarration
        {
            public string slideId;
            public AudioClip clip;
        }

        [Header("References")]
        [SerializeField]
        private TutorialUiController tutorialUI;

        [SerializeField]
        private AudioSource audioSource;

        [Header("Slide Narrations")]
        [SerializeField]
        private SlideNarration[] slideNarrations;

        private void OnEnable()
        {
            if (tutorialUI != null)
            {
                tutorialUI.OnSlideChanged += HandleSlideChanged;
            }
        }

        private void OnDisable()
        {
            if (tutorialUI != null)
            {
                tutorialUI.OnSlideChanged -= HandleSlideChanged;
            }
        }

        private void HandleSlideChanged(TutorialUiController.SlideChangedData data)
        {
            if (audioSource == null || slideNarrations == null)
            {
                return;
            }

            for (int i = 0; i < slideNarrations.Length; i++)
            {
                SlideNarration entry = slideNarrations[i];
                if (entry == null || string.IsNullOrEmpty(entry.slideId) || entry.clip == null)
                {
                    continue;
                }

                if (entry.slideId == data.Id)
                {
                    audioSource.Stop();
                    audioSource.clip = entry.clip;
                    audioSource.Play();
                    break;
                }
            }
        }
    }
}
