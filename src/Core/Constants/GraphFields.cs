namespace Core.Constants
{
    /// <summary>
    /// Common Graph API field selections.
    /// 
    /// WHY: Avoids scattering magic strings throughout the codebase.
    /// Enables reuse and consistency across multiple features.
    /// </summary>
    public static class GraphFields
    {
        /// <summary>
        /// All commonly-used user properties from Graph API.
        /// </summary>
        public static readonly string[] All =
        [
            "id",
            "displayName",
            "mail",
            "userPrincipalName",
            "givenName",
            "surname",
            "jobTitle",
            "department",
            "officeLocation",
            "mobilePhone",
            "preferredLanguage",
            "accountEnabled",
            "createdDateTime",
            "businessPhones"
        ];

        /// <summary>
        /// Essential user properties (minimal set).
        /// Useful for faster queries when full data not needed.
        /// </summary>
        public static readonly string[] Essential =
        [
            "id",
            "displayName",
            "mail",
            "userPrincipalName"
        ];
    }
}