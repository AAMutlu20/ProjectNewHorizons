namespace Abilities
{
    /// <summary>
    /// Closes CooldownAbilityChoiceEntry&lt;TStats&gt; for Shockwave. Unity's
    /// Add Component menu cannot show or attach an open generic type directly
    /// -- it needs a concrete, closed, named class to serialize. This is
    /// that class: it adds nothing of its own, it just exists so
    /// "ShockwaveChoiceEntry" shows up in Add Component and behaves exactly
    /// like CooldownAbilityChoiceEntry&lt;ShockwaveStats&gt;.
    ///
    /// Attach to: the Entry_Shockwave GameObject in [Systems].
    /// </summary>
    public class ShockwaveChoiceEntry : CooldownAbilityChoiceEntry<ShockwaveStats> { }
}
