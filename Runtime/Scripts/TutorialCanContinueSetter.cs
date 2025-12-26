using UnityEngine;

namespace MagicedTech.TutorialUI
{
    /// <summary>
    /// Helper component that sets canContinue = true for the current slide
    /// on an associated TutorialUiController.
    /// VR usage: attach this to any GameObject and call AllowContinue()
    /// from your interaction logic or UnityEvents when the slide's task
    /// has been completed.
    /// </summary>
    public class TutorialCanContinueSetter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private TutorialUiController tutorialUI;

        /// <summary>
        /// Marks the current slide as allowed to continue and updates the
        /// Next button's interactable state.
        /// Call this from your gameplay logic (e.g., when the player
        /// completes the required action for this slide).
        /// </summary>
        public void AllowContinue()
        {
            if (tutorialUI == null)
            {
                Debug.LogWarning("[TutorialCanContinueSetter] TutorialUiController reference is not assigned.");
                return;
            }

            Debug.Log("[TutorialCanContinueSetter] AllowContinue called. Enabling canContinue for current slide.");
            tutorialUI.SetCanContinueForCurrentSlide(true);
        }
    }
}
