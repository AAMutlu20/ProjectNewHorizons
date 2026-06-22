namespace Abilities
{
    /// <summary>
    /// Closes CooldownAbilityChoiceEntry&lt;TStats&gt; for Laser Beam.
    /// See ShockwaveChoiceEntry for why this exists.
    ///
    /// Attach to: the Entry_LaserBeam GameObject in [Systems].
    /// </summary>
    public class LaserBeamChoiceEntry : CooldownAbilityChoiceEntry<LaserBeamStats> { }
}
