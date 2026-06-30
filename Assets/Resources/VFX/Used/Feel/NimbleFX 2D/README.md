# NimbleFX 2D (UI + Sprite)

A lightweight, code-driven animation toolkit for **Unity** that lets you animate **UI (UGUI)** elements and **2D sprites** without Animator, animation clips, or external tween libraries. Ships with **7 effects**, clean inspectors, loop/delay controls, and optional callbacks.

---

## ✨ What’s Inside

**Two drop-in components:**

- `UIAnimations` — for **UGUI** (Buttons, Images, any `RectTransform` with `Image`).
- `SpriteAnimations` — for **SpriteRenderer** objects in world space.

**7 effects (both scripts):**
1. **SpriteSheetEffect** — flipbook playback (`Sprite[]` + frame rate + loop).
2. **BounceEffect** — squash/stretch style bounce using a sine curve.
3. **GlowEffect** — alpha pulsing (Image/SpriteRenderer color.a).
4. **ScaleEffect** — scale between current and target (ping-pong / loop with delay).
5. **ShakeEffect** — position shake with time-based damping.
6. **RotateEffect** — simple rotation by degrees/second (optional loop/delay).
7. **ElasticResizing** — springy overshoot and settle effect.

No Jobs/Burst. Pure `Update`-driven with early-outs for minimal CPU when idle.

---


## 🚀 Quick Start

### UI (UGUI)
1. Add **UIAnimations** to any GameObject with a **RectTransform**.
2. Assign **Targetted Sprite** to an object with an **Image** component.
3. Pick an **Animation Type** and tweak settings.
4. Either enable **Auto Start** or call `StartSelectedEffect()`.

> To play on a Button click: link the Button’s **OnClick** to `UIAnimations.ExcuteAfterClick()` or `StartSelectedEffect()`.

### Sprite (SpriteRenderer)
1. Add **SpriteAnimations** to a GameObject with **SpriteRenderer**.
2. Assign **Targetted Sprite** to that GameObject.
3. Configure the effect and press Play, or call `StartSelectedEffect()`.

---

## 🧪 Usage Examples

### Start/Stop the selected effect (UI)
```csharp
public class PlayUIFx : MonoBehaviour
{
    public UIAnimations uiFx;
    public void Play()  => uiFx.StartSelectedEffect();
    public void Stop()  => uiFx.StopSelectedEffect();
}
```

### Hook a completion callback (UI via UnityEvent)
- In the Inspector, wire the `onComplete` event to any method you like.
- Or invoke through code and pass the UnityEvent receiver beforehand, then call:
```csharp
uiFx.StartSelectedEffect(); // onComplete will be raised when the effect ends
```

### Programmatic setup (Sprite)
```csharp
public class SetupSpriteFx : MonoBehaviour
{
    public SpriteAnimations spriteFx;
    void Start()
    {
        spriteFx.animationType = AnimationType.BounceEffect;
        spriteFx.loop = true;
        spriteFx.loopDelay = 0.15f;
        spriteFx.autoStart = true; // or call StartSelectedEffect()
    }
}
```

### Sprite Sheet playback
- Assign `spriteFrames[]` and set `frameRate`.
- Choose **SpriteSheetEffect** and enable **Loop** if needed.

---

## ⚙️ Key Inspector Options

- **Auto Start** – Starts the selected effect on `Start()`.
- **Loop / Loop Delay** – Repeat with an optional delay between cycles.
- **Per-effect tunables** – Duration, intensity, target scale, rotation speed, etc.
- **UI-only**: `onComplete` (**UnityEvent**) for designer-friendly callbacks.
- **Sprite-only**: `onComplete` via `System.Action` pattern.

> Performance: Each effect’s code early-outs when inactive. Idle components have minimal cost. Avoid extremely high frame rates in SpriteSheetEffect and huge sprite arrays.

---

## 🧩 API Reference (high level)

Common helpers:
```csharp
StartSelectedEffect();   // Starts the chosen AnimationType
StopSelectedEffect();    // Stops the current effect
```

Per-effect (UI variant shows UnityEvent overloads):
```csharp
// UIAnimations
StartBounce(UnityEngine.Events.UnityEvent onFinish = null);
StartGlow(UnityEngine.Events.UnityEvent onFinish = null);
StartScale(UnityEngine.Events.UnityEvent onFinish = null);
StartShake(UnityEngine.Events.UnityEvent onFinish = null);
StartRotation(UnityEngine.Events.UnityEvent onFinish = null);
StartSpriteSheet(UnityEngine.Events.UnityEvent onFinish = null);
StartElasticResize(UnityEngine.Events.UnityEvent onFinish = null);
ExcuteAfterClick(); // convenience to StartSelectedEffect(onComplete)

// SpriteAnimations
StartBounce(System.Action onFinish = null);
StartGlow(System.Action onFinish = null);
StartScale(System.Action onFinish = null);
StartShake(System.Action onFinish = null);
StartRotation(System.Action onFinish = null);
StartSpriteSheet(); // set frames + frameRate
StartElasticResize(System.Action onFinish = null);
```

Stop methods exist for each effect (`StopBounce`, `StopGlow`, etc.) and reset to the original transform/color where appropriate.

---

## ✅ Requirements & Compatibility

- Unity **2021.3 LTS+** recommended (works with UGUI and 2D).
- Render Pipeline agnostic (URP/HDRP/Built-in).  
- No external dependencies.

> If you need 2019/2020 support, the code is simple enough and should work; confirm in a fresh project.

---

## 🧠 Tips & Gotchas

- **UI Glow** changes `Image.color.a`. If your Image uses a custom material/shader that ignores alpha, glow will appear inactive.
- **Layout Groups**: Scaling UI inside a `LayoutGroup` may be damped by layout recalculations. Consider `LayoutElement.ignoreLayout = true` during effects.
- **Shake** uses **localPosition**. For world-space canvases/sprites, ensure parent transforms aren’t also animated unexpectedly.
- **SpriteSheetEffect**: Keep `spriteFrames` arrays modest; very large arrays cost memory and inspector time.

---

## 📄 License

MIT License — free to use in commercial and non‑commercial projects. Redistribution of the package **as-is** is not allowed. (Adjust this to your preference.)

---

## 🙋 Support

Have questions or feature requests?  
Add an issue or reach out: **Bharatmehra232@gmail.com**

---

## 🗺️ Roadmap (nice-to-have)

- Curves per effect (ease in/out selection).
- TimeScale-independent option.
- Editor preview buttons & demo scene.
- Preset library for common UI micro-interactions.
