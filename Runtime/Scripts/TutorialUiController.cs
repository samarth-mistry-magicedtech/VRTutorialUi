using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace MagicedTech.TutorialUI
{
    /// <summary>
    /// Controls a VR tutorial UI driven by JSON slide data.
    /// VR usage: attach to a world-space UI root, assign TextMeshPro fields and button roots,
    /// then bind XR button interactions to the public button handler methods.
    /// </summary>
    public class TutorialUiController : MonoBehaviour
    {
        /// <summary>
        /// Data describing the currently active slide. Used by external systems such as narration.
        /// </summary>
        public struct SlideChangedData
        {
            public string Id;
            public string Header;
            public string Body;
            public string Footer;
        }

        [Serializable]
        private class SlideButtonDefinition
        {
            public string label;
            public string action; // "next", "retry", "exit" or custom
        }

        [Serializable]
        private class SlideDefinition
        {
            public string id;
            public string header;
            public string body;
            public string footer;
            public List<SlideButtonDefinition> buttons;
            public bool canContinue = true;
        }

        [Serializable]
        private class SlideCollection
        {
            public List<SlideDefinition> slides;
        }

        [Header("UI Text References")]
        [SerializeField]
        private TextMeshProUGUI headerText;

        [SerializeField]
        private TextMeshProUGUI bodyText;

        [SerializeField]
        private TextMeshProUGUI footerText;

        [Header("Button Roots (enable/disable)")]
        [SerializeField]
        private GameObject nextButtonRoot;

        [SerializeField]
        private GameObject retryButtonRoot;

        [SerializeField]
        private GameObject exitButtonRoot;

        [Header("Button Label Texts")]
        [SerializeField]
        private TextMeshProUGUI nextButtonLabelText;

        [Header("Button Interactability")]
        [Tooltip("Optional component used to toggle the interactable state of the Next button.")]
        [SerializeField]
        private Behaviour nextButtonInteractableComponent;

        [Header("Slide Configuration JSON")]
        [Tooltip("JSON TextAsset containing slide definitions. See ExampleTutorialSlides.json for schema.")]
        [SerializeField]
        private TextAsset slidesJson;

        [Header("Events")]
        [Tooltip("Invoked when the user chooses to exit on the last slide.")]
        public UnityEvent OnExitRequested;

        /// <summary>
        /// Invoked whenever the current slide changes.
        /// External systems (e.g., narration) can subscribe to this.
        /// </summary>
        public event System.Action<SlideChangedData> OnSlideChanged;

        private SlideCollection slideCollection;
        private int currentSlideIndex;

        /// <summary>
        /// Starts the tutorial from the first slide.
        /// </summary>
        public void StartTutorial()
        {
            EnsureSlidesLoaded();

            if (slideCollection == null || slideCollection.slides == null || slideCollection.slides.Count == 0)
            {
                Debug.LogWarning("[TutorialUiController] No slides available to start tutorial.");
                return;
            }

            currentSlideIndex = 0;
            ApplyCurrentSlide();
        }

        /// <summary>
        /// Handler for the Next button. Bind this from your XR button.
        /// </summary>
        public void OnNextButtonPressed()
        {
            EnsureSlidesLoaded();

            if (!HasSlides())
            {
                return;
            }

            // Respect canContinue flag per slide.
            SlideDefinition current = slideCollection.slides[currentSlideIndex];
            if (!current.canContinue)
            {
                Debug.Log("[TutorialUiController] Next pressed but canContinue is false for this slide.");
                return;
            }

            if (IsLastSlide())
            {
                Debug.Log("[TutorialUiController] Next pressed on last slide. Ignoring.");
                return;
            }

            currentSlideIndex++;
            ApplyCurrentSlide();
        }

        /// <summary>
        /// Handler for the Retry button on the last slide.
        /// </summary>
        public void OnRetryButtonPressed()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        /// <summary>
        /// Handler for the Exit button on the last slide.
        /// </summary>
        public void OnExitButtonPressed()
        {
            if (!IsLastSlide())
            {
                Debug.Log("[TutorialUiController] Exit pressed but current slide is not the last one.");
            }

            OnExitRequested?.Invoke();
        }

        private void Awake()
        {
            if (slidesJson != null)
            {
                LoadSlidesFromJson(slidesJson.text);
            }
            else
            {
                Debug.LogWarning("[TutorialUiController] Slides JSON TextAsset is not assigned.");
            }
        }

        private void Start()
        {
            if (HasSlides())
            {
                currentSlideIndex = Mathf.Clamp(currentSlideIndex, 0, slideCollection.slides.Count - 1);
                ApplyCurrentSlide();
            }
        }

        private void EnsureSlidesLoaded()
        {
            if (slideCollection != null)
            {
                return;
            }

            if (slidesJson != null)
            {
                LoadSlidesFromJson(slidesJson.text);
            }
        }

        private void LoadSlidesFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[TutorialUiController] Provided JSON string is null or empty.");
                slideCollection = null;
                return;
            }

            try
            {
                // JsonUtility requires a wrapper type; SlideCollection provides that.
                slideCollection = JsonUtility.FromJson<SlideCollection>(json);

                if (slideCollection == null || slideCollection.slides == null || slideCollection.slides.Count == 0)
                {
                    Debug.LogWarning("[TutorialUiController] Parsed slide collection is empty.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TutorialUiController] Failed to parse slides JSON: {ex.Message}");
                slideCollection = null;
            }
        }

        private bool HasSlides()
        {
            return slideCollection != null && slideCollection.slides != null && slideCollection.slides.Count > 0;
        }

        private bool IsLastSlide()
        {
            return HasSlides() && currentSlideIndex >= slideCollection.slides.Count - 1;
        }

        private void ApplyCurrentSlide()
        {
            if (!HasSlides())
            {
                ClearText();
                SetButtonVisibility(false, false, false);
                UpdateNextButtonInteractable(false);
                return;
            }

            currentSlideIndex = Mathf.Clamp(currentSlideIndex, 0, slideCollection.slides.Count - 1);
            SlideDefinition slide = slideCollection.slides[currentSlideIndex];

            if (headerText != null)
            {
                headerText.text = slide.header ?? string.Empty;
            }

            if (bodyText != null)
            {
                bodyText.text = slide.body ?? string.Empty;
            }

            if (footerText != null)
            {
                footerText.text = slide.footer ?? string.Empty;
            }

            // Notify listeners that the slide has changed.
            SlideChangedData changedData = new SlideChangedData
            {
                Id = slide.id,
                Header = slide.header,
                Body = slide.body,
                Footer = slide.footer
            };

            OnSlideChanged?.Invoke(changedData);

            // Update button label for Next based on slide button definitions when available.
            if (nextButtonLabelText != null)
            {
                string label = "Next";

                if (slide.buttons != null && slide.buttons.Count > 0 && !string.IsNullOrWhiteSpace(slide.buttons[0].label))
                {
                    label = slide.buttons[0].label;
                }

                nextButtonLabelText.text = label;
            }

            // Apply canContinue to Next button interactability for this slide.
            UpdateNextButtonInteractable(slide.canContinue);

            if (IsLastSlide())
            {
                // Last slide: enforce Retry / Exit behavior regardless of JSON button configuration.
                SetButtonVisibility(false, true, true);
            }
            else
            {
                // Non-last slides: typically Next only.
                SetButtonVisibility(true, false, false);
            }
        }

        /// <summary>
        /// Allows external systems to enable or disable continuation for the current slide.
        /// For example, a project-specific script can call this when certain conditions are met.
        /// </summary>
        /// <param name="canContinue">If true, Next becomes interactable; otherwise it is disabled.</param>
        public void SetCanContinueForCurrentSlide(bool canContinue)
        {
            if (!HasSlides())
            {
                return;
            }

            SlideDefinition slide = slideCollection.slides[currentSlideIndex];
            slide.canContinue = canContinue;
            UpdateNextButtonInteractable(canContinue);
        }

        private void ClearText()
        {
            if (headerText != null)
            {
                headerText.text = string.Empty;
            }

            if (bodyText != null)
            {
                bodyText.text = string.Empty;
            }

            if (footerText != null)
            {
                footerText.text = string.Empty;
            }
        }

        private void SetButtonVisibility(bool showNext, bool showRetry, bool showExit)
        {
            if (nextButtonRoot != null)
            {
                nextButtonRoot.SetActive(showNext);
            }

            if (retryButtonRoot != null)
            {
                retryButtonRoot.SetActive(showRetry);
            }

            if (exitButtonRoot != null)
            {
                exitButtonRoot.SetActive(showExit);
            }
        }

        private void UpdateNextButtonInteractable(bool canContinue)
        {
            if (nextButtonInteractableComponent == null)
            {
                return;
            }

            // If the assigned component is a standard UI Button, use its interactable
            // property so that the button's color states (normal/disabled) update correctly.
            if (nextButtonInteractableComponent is UnityEngine.UI.Button uiButton)
            {
                uiButton.interactable = canContinue;
            }
            else
            {
                // Fallback for non-Button behaviours: toggle enabled state.
                nextButtonInteractableComponent.enabled = canContinue;
            }
        }
    }
}
