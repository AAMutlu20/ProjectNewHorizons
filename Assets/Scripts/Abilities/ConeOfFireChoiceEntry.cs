namespace Abilities
{
    /// <summary>
    /// Closes CooldownAbilityChoiceEntry&lt;TStats&gt; for Cone of Fire.
    /// See ShockwaveChoiceEntry for why this exists.
    ///
    /// Attach to: the Entry_ConeOfFire GameObject in [Systems].
    /// </summary>
    public class ConeOfFireChoiceEntry : CooldownAbilityChoiceEntry<ConeOfFireStats> { }
}
