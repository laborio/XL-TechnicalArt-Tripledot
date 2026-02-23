# XL Technical Art - Unity UI -

Thanks for taking an interest into my technical test. It did take me about 15 hours to complete.
Main difficulties for me:  the bottom bar behavour, blur effect, some responsive behavours (modular buttons, modular popup prefab architecture decisions), various rendering bugs, perf optimizations.

## Project Setup
- Unity Editor: `6000.2.9f1`
- Render pipeline: URP (configured in project settings/assets)
- Open the main scene: `Assets/Scenes/Scene_Home.unity`

## Key Paths
- UI scripts: `Assets/Scripts/UI`
- UI prefabs: `Assets/Prefabs/UI`
- Design system asset: `Assets/Scripts/UI/Editor/UITheme.asset`
- Design system source: `Assets/Scripts/UI/Theme/UIStyles.json`


## Additional Notes


## UI Construction

For the top bar currencies, I rebuilt some elements (like the round button next to coins) directly in Unity instead of imported premade sprites. This gives better control over layout, and avoid being too much asset dependent with the UI designers. Also as a former UI designer I don't mind editing assets directly for our TA needs if production constraints allow it. 


## Theme / Styling System

I implemented a lightweight token-based theme system using a UITheme ScriptableObject.

Colors and text styles are centralized and applied through small binder components (UIThemeImage, UIThemeText). A simple JSON importer simulates a potential design-tool export workflow (could create a figma plugin to export designers design system directly in json), while still allowing manual editing in Unity.

The system is intentionally minimal for the scope of the test but designed to scale.

I also added stricter TMP font/material syncing to prevent atlas mismatches during style refreshes which was a problem in task3 and build testing.


## Bottom Bar

The bottom bar separates logic and animation:

BottomBarView handles selection state and events.

Animation behavior is handled separately (DOTween-based). Most animations in the project are DOTween because it allows AI agents to write and design them directly which is super fast for implementation while animator base requires more manual setup (which I'm also comfortable with)

Selection highlight is a single moving element (pop on first select, slide on switch).


## Popup System

The popup is built as a reusable system:

BasePopupView handles open/close lifecycle.

PopupManager centralizes flow.

PopupBackdropView handles dimming, blur and click-to-close.

Animations are DOTween-based and inspector-driven.

Blur is handled through URP post-processing weight tweening and only enabled while active.

## Localization

UI text uses a base prefab pattern and optional localization key component to allow integration with any localization.


## Performance

Tested on real iOS and Android devices. iOS remained stable at 60 FPS. Android ran in a stable lower frame bucket consistent with device-level pacing.
Layout rebuilds and tween lifecycles were managed to avoid unnecessary churn.

## Improvements

I would spend more time on task 3 creative design but building a stable consistent project seemed to be a priority for this test as Tripledot is probably very production oriented, while still maintaining a reasonable production time.
I'm confident that after getting used to the production process and tools, I'll be able to spend more time on creation and demonstrate my creativity for visual design and effects. 
