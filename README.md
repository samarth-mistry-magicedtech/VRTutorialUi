# MagicedTech VR Tutorial UI (VRTutorialUI)

Reusable **VR tutorial UI** package for Unity, built for:

- URP
- OpenXR
- XR Interaction Toolkit
- TextMeshPro

It provides:

- JSON-driven tutorial slides (header, body, footer, buttons)
- Per-slide `canContinue` gating (block Next until an activity is complete)
- Retry that reloads the scene
- Exit hook via UnityEvent
- Narration support (slide change event)
- Helper scripts for gating and narration

---

## 1. Installation (via Git URL + version tags)

You do **not** need to clone the repo manually. Install it directly via Unity Package Manager.

### 1.1 Git repository URL (latest `main`)

If you want to always track the latest `main` branch:

```text
https://github.com/samarth-mistry-magicedtech/VRTutorialUi.git
```

### 1.2 Using a specific tagged version (recommended)

After you create Git tags (for example `v1.0.0`), you can install a specific version by appending the tag after a `#` in the URL:

```text
https://github.com/samarth-mistry-magicedtech/VRTutorialUi.git#v1.0.0
```

Replace `v1.0.0` with the release tag you want to use.

### Steps

1. Open Unity.
2. Go to:
   - `Window → Package Manager`.
3. In the Package Manager window:
   - Click the **+** button (top-left).
   - Choose **Add package from git URL…**.
4. Paste one of the URLs above, for example:

   ```text
   https://github.com/samarth-mistry-magicedtech/VRTutorialUi.git#v1.0.0
   ```

5. Click **Add**.
6. Unity will download and import the package.

You will now see it under **Packages** in the Project window (based on the `name` and `displayName` in `package.json`).

---

## 2. Package Structure

Inside the package:

```text
VRTutorialUI/
  package.json
  Runtime/
    Scripts/
      TutorialUiController.cs
      TutorialNarrationController.cs
      TutorialCanContinueSetter.cs
    Prefabs/
      VRTutorialUI.prefab          
    Resources/
      ExampleTutorialSlides.json
    Scenes/
      VRTutorialUI_Demo.unity
  Editor/
```

You are free to customize the prefab and scenes in your own project; the scripts and JSON format are designed to be reusable.

---

## 3. Slides JSON Format

Default example file:

- `Runtime/Resources/ExampleTutorialSlides.json`

Schema:

```json
{
  "slides": [
    {
      "id": "intro",
      "header": "Welcome to the MagicedTech VR experience.",
      "body": "In this short tutorial we will walk you through the basic controls, how to interact with objects around you, and what the UI panels mean. Take a moment to look around and get comfortable. When you are ready, press Next to continue.",
      "footer": "Look at the panel and press Next to continue.",
      "buttons": [
        { "label": "Start Tutorial", "action": "next" }
      ],
      "canContinue": true
    },
    {
      "id": "controls",
      "header": "Controls",
      "body": "Use your right-hand controller to point at buttons and UI elements.\n\nSqueeze the trigger to select, and use the thumbstick to turn or move if your project enables locomotion. When you see an object highlighted, it means it can be interacted with. Follow the on-screen hints to perform the required action.",
      "footer": "Try pointing at this panel with your controller, then press Next.",
      "buttons": [
        { "label": "Next Step", "action": "next" }
      ],
      "canContinue": false
    },
    {
      "id": "finished",
      "header": "Tutorial Complete",
      "body": "You have reached the end of this tutorial.\n\nYou can go through the instructions again if you want to refresh the controls, or exit and continue to the main experience. Your progress so far will not be lost.",
      "footer": "Choose Retry to replay the tutorial, or Exit to continue.",
      "buttons": [
        { "label": "Retry Tutorial", "action": "retry" },
        { "label": "Exit Tutorial", "action": "exit" }
      ],
      "canContinue": true
    }
  ]
}
```

Notes:

- `id` is used by narration mapping and any project-specific logic.
- `buttons[0].label` is used as the Next button text.
- `canContinue`:
  - `true`  → Next is enabled when you enter the slide.
  - `false` → Next is disabled until you unlock it via code.

Create your own JSON assets with the same schema and assign them to the controller.

---

## 4. Core Components

### 4.1 TutorialUiController

**Namespace:** `MagicedTech.TutorialUI`

Attach to your world-space VR tutorial UI root (e.g. on the `VRTutorialUI` prefab).

**Required inspector fields:**

- **UI Text References**
  - `Header Text` → TextMeshProUGUI for slide header.
  - `Body Text`   → TextMeshProUGUI for body text.
  - `Footer Text` → TextMeshProUGUI for footer/hints.

- **Button Roots (enable/disable)**
  - `Next Button Root`  → GameObject for the Next button.
  - `Retry Button Root` → GameObject for the Retry button.
  - `Exit Button Root`  → GameObject for the Exit button.

- **Button Label Texts**
  - `Next Button Label Text` → TextMeshProUGUI used to show the Next button label.

- **Button Interactability**
  - `Next Button Interactable Component` →
    - For standard UI: the `Button` component of the Next button.
    - For custom XR buttons: any Behaviour whose `enabled` state represents interactability.

- **Slide Configuration JSON**
  - `Slides Json` → a `TextAsset` with the slides JSON (e.g. `ExampleTutorialSlides.json`).

**Events:**

- `On Exit Requested` (`UnityEvent`)
  - Fired when the Exit button is pressed on the last slide.
  - Use this to hide the tutorial or move to another scene.

- `OnSlideChanged` (`event Action<SlideChangedData>`) – code-level event
  - `SlideChangedData` exposes `Id`, `Header`, `Body`, `Footer`.
  - Used by `TutorialNarrationController` and any external listeners.

**Public methods (for buttons / XR events):**

- `StartTutorial()`
  - Starts or restarts from the first slide (optional; `Start()` already applies initial slide).

- `OnNextButtonPressed()`
  - Attempt to advance to the next slide.
  - Will *not* advance if:
    - Current slide is last slide, or
    - Current slide has `canContinue == false`.

- `OnRetryButtonPressed()`
  - Reloads the current scene using `SceneManager.LoadScene(scene.buildIndex)`.

- `OnExitButtonPressed()`
  - If on last slide, invokes `OnExitRequested`.

- `SetCanContinueForCurrentSlide(bool canContinue)`
  - For advanced users – manually set `canContinue` and update the Next button interactable state.

---

### 4.2 TutorialCanContinueSetter

Helper component to unlock progression for the current slide.

**Namespace:** `MagicedTech.TutorialUI`

Attach this to any GameObject where you handle slide-specific activities.

**Inspector:**

- `Tutorial UI` → your `TutorialUiController` instance.

**Public method:**

- `AllowContinue()`
  - Calls `tutorialUI.SetCanContinueForCurrentSlide(true)`.
  - Logs to the Console so you can see when it fires.

**Usage examples:**

- From code:

  ```csharp
  public class TreePlantingStep : MonoBehaviour
  {
      [SerializeField] private TutorialCanContinueSetter canContinueSetter;

      void OnTreePlanted()
      {
          canContinueSetter.AllowContinue();
      }
  }
  ```

- From UnityEvents / XR events:
  - On your XR interactable or trigger, add a listener calling `TutorialCanContinueSetter.AllowContinue()` when the player completes the action.

---

### 4.3 TutorialNarrationController

Provides basic slide-based narration using an `AudioSource`.

**Namespace:** `MagicedTech.TutorialUI`

Attach this alongside `TutorialUiController`.

**Inspector:**

- `Tutorial UI` → `TutorialUiController` reference.
- `Audio Source` → an `AudioSource` component.
- `Slide Narrations` → array of:
  - `slideId` → matches the `id` field from JSON.
  - `clip`   → `AudioClip` to play.

On each slide change, it listens to `OnSlideChanged` and plays the mapped clip.

You can replace this with your own implementation (e.g. TTS) by subscribing to `OnSlideChanged` directly.

---

## 5. Wiring Buttons

You can use standard UI Buttons with XR UI Input Module or your own XR interactions.

Typical setup:

- **Next button** → call `TutorialUiController.OnNextButtonPressed()`.
- **Retry button** → call `TutorialUiController.OnRetryButtonPressed()`.
- **Exit button** → call `TutorialUiController.OnExitButtonPressed()`.

In the Inspector:

1. On each Button or XR interactable event list (e.g. `On Click`, `Select Entered`):
   - Add the `TutorialUiController` GameObject.
   - Choose the appropriate public method.

2. For `Next Button Interactable Component`:
   - If it's a `Button`, drag the **Button component** so the Disabled color works.

---

## 6. Example Flow (with canContinue)

1. In your slides JSON, mark restricted slides with:

   ```json
   "canContinue": false
   ```

2. When the user reaches that slide:
   - Next button is visible but not interactable (disabled/gray).

3. When the user completes the required action (e.g., plants a tree):
   - Call `TutorialCanContinueSetter.AllowContinue()` (or `SetCanContinueForCurrentSlide(true)` from your own script).

4. Now the Next button becomes interactable and the user can proceed.

---

## 7. Quick Start Checklist

- Install the package via Git URL.
- Create or use a VR scene with XR rig + URP.
- Drop your `VRTutorialUI` prefab into the scene.
- Assign:
  - Header / Body / Footer TMP texts.
  - Next / Retry / Exit button roots.
  - Next button TMP label and Button component (for interactability).
  - Slides JSON `TextAsset`.
- Wire buttons to `OnNextButtonPressed`, `OnRetryButtonPressed`, `OnExitButtonPressed`.
- (Optional) Add `TutorialNarrationController` and set up slide audio.
- For slides requiring actions:
  - Set `"canContinue": false` in JSON.
  - Use `TutorialCanContinueSetter.AllowContinue()` when the action is complete.

You now have a reusable VR tutorial UI that you can drop into any URP + XR project and configure via JSON and simple hooks.
