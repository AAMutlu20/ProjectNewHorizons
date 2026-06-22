namespace Abilities
{
    /// <summary>
    /// Closes CooldownAbilityChoiceEntry&lt;TStats&gt; for Meteor Slam.
    /// See ShockwaveChoiceEntry for why this exists.
    ///
    /// Attach to: the Entry_MeteorSlam GameObject in [Systems].
    /// </summary>
    public class MeteorSlamChoiceEntry : CooldownAbilityChoiceEntry<MeteorSlamStats> { }
}
