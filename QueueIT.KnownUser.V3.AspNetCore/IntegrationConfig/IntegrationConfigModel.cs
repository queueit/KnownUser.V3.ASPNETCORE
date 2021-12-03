using System.Collections.Generic;

namespace QueueIT.KnownUser.V3.AspNetCore.IntegrationConfig
{
    public class IntegrationConfigModel
    {
        public string Name { get; set; }
        public string EventId { get; set; }
        public string CookieDomain { get; set; }
        public bool? IsCookieHttpOnly { get; set; }
        public bool? IsCookieSecure { get; set; }
        public string LayoutName { get; set; }
        public string Culture { get; set; }
        public bool? ExtendCookieValidity { get; set; }
        public int? CookieValidityMinute { get; set; }
        public string QueueDomain { get; set; }
        public string RedirectLogic { get; set; }
        public string ForcedTargetUrl { get; set; }
        public string ActionType { get; set; }
        public IEnumerable<TriggerModel> Triggers { get; set; }
    }

    public class CustomerIntegration
    {
        public CustomerIntegration()
        {
            this.Integrations = new List<IntegrationConfigModel>();
            this.Version = -1;
        }
        //sorted list of integrations
        public IEnumerable<IntegrationConfigModel> Integrations { get; set; }
        public int Version { get; set; }
    }

    public class TriggerPart
    {
        public string ValidatorType { get; set; }
        public string Operator { get; set; }
        public string ValueToCompare { get; set; }
        public string[] ValuesToCompare { get; set; }
        public bool IsNegative { get; set; }
        public bool IsIgnoreCase { get; set; }
        //UrlValidator
        public string UrlPart { get; set; }
        //CookieValidator
        public string CookieName { get; set; }
        //HttpHeaderValidator
        public string HttpHeaderName { get; set; }
    }

    public class TriggerModel
    {
        public TriggerModel()
        {
            this.TriggerParts = new List<TriggerPart>();
        }
        public IEnumerable<TriggerPart> TriggerParts { get; set; }
        public string LogicalOperator { get; set; }
    }

    internal static class ValidatorType
    {
        public const string UrlValidator = "UrlValidator";
        public const string CookieValidator = "CookieValidator";
        public const string UserAgentValidator = "UserAgentValidator";
        public const string HttpHeaderValidator = "HttpHeaderValidator";
        public const string RequestBodyValidator = "RequestBodyValidator";
    }

    internal static class UrlPartType
    {
        public const string HostName = "HostName";
        public const string PagePath = "PagePath";
        public const string PageUrl = "PageUrl";
    }

    internal static class ComparisonOperatorType
    {
        public const string EqualS = "Equals";
        public const string Contains = "Contains";
        public const string EqualsAny = "EqualsAny";
        public const string ContainsAny = "ContainsAny";
    }

    internal static class LogicalOperatorType
    {
        public const string Or = "Or";
        public const string And = "And";
    }

    internal static class ActionType
    {
        public const string IgnoreAction = "Ignore";
        public const string CancelAction = "Cancel";
        public const string QueueAction = "Queue";
    }
}
