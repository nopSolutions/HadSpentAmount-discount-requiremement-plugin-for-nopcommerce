namespace Nop.Plugin.DiscountRules.HadSpentAmount
{
    /// <summary>
    /// Represents constants for the discount requirement rule
    /// </summary>
    public static class DiscountRequirementDefaults
    {
        /// <summary>
        /// The system name of the discount requirement rule
        /// </summary>
        public const string SYSTEM_NAME = "DiscountRequirement.HadSpentAmount";

        /// <summary>
        /// The key of the settings to save restricted customer roles
        /// </summary>
        public const string SETTINGS_KEY = "DiscountRequirement.HadSpentAmount-{0}";

        /// <summary>
        /// The HTML field prefix for discount requirements
        /// </summary>
        public const string HTML_FIELD_PREFIX = "DiscountRulesHadSpentAmount{0}";
    }
}
