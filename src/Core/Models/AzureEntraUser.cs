using System;
using System.Collections.Generic;

namespace Core.Models
{
    /// <summary>
    /// Domain model representing an Azure Active Directory user.
    /// 
    /// This model is independent of Microsoft.Graph.Models.User, allowing:
    /// - Mapping from Graph SDK models without leaking SDK types
    /// - Validation and transformation at domain level
    /// - Support for multiple sources (Graph, custom API, etc.)
    /// </summary>
    public class AzureEntraUser
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Mail { get; set; }
        public string UserPrincipalName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string OfficeLocation { get; set; }
        public string MobilePhone { get; set; }
        public string PreferredLanguage { get; set; }
        public bool AccountEnabled { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public List<string> BusinessPhones { get; set; } = new();
    }
}