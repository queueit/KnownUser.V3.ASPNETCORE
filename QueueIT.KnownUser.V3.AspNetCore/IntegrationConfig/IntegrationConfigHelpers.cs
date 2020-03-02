using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace QueueIT.KnownUser.V3.AspNetCore.IntegrationConfig
{
    internal class IntegrationEvaluator : IIntegrationEvaluator
    {
        public IntegrationConfigModel GetMatchedIntegrationConfig(CustomerIntegration customerIntegration, string currentPageUrl, IHttpRequest request)
        {
            if (request == null)
                throw new ArgumentException("request is null");

            foreach (var integration in customerIntegration.Integrations)
            {
                foreach (var trigger in integration.Triggers)
                {
                    if (EvaluateTrigger(trigger, currentPageUrl, request))
                    {
                        return integration;
                    }
                }
            }
            return null;
        }

        private bool EvaluateTrigger(TriggerModel trigger, string currentPageUrl, IHttpRequest request)
        {
            if (trigger.LogicalOperator == LogicalOperatorType.Or)
            {
                foreach (var part in trigger.TriggerParts)
                {
                    if (EvaluateTriggerPart(part, currentPageUrl, request))
                        return true;
                }
                return false;
            }
            else
            {
                foreach (var part in trigger.TriggerParts)
                {
                    if (!EvaluateTriggerPart(part, currentPageUrl, request))
                        return false;
                }
                return true;
            }
        }

        private bool EvaluateTriggerPart(TriggerPart triggerPart, string currentPageUrl, IHttpRequest request)
        {
            switch (triggerPart.ValidatorType)
            {
                case ValidatorType.UrlValidator:
                    return UrlValidatorHelper.Evaluate(triggerPart, currentPageUrl);
                case ValidatorType.CookieValidator:
                    return CookieValidatorHelper.Evaluate(triggerPart, request);
                case ValidatorType.UserAgentValidator:
                    return UserAgentValidatorHelper.Evaluate(triggerPart, request.UserAgent);
                case ValidatorType.HttpHeaderValidator:
                    return HttpHeaderValidatorHelper.Evaluate(triggerPart, request.Headers);
                default:
                    return false;
            }
        }
    }

    internal interface IIntegrationEvaluator
    {
        IntegrationConfigModel GetMatchedIntegrationConfig(
            CustomerIntegration customerIntegration, string currentPageUrl, IHttpRequest request);
    }

    internal class UrlValidatorHelper
    {
        public static bool Evaluate(TriggerPart triggerPart, string url)
        {
            return ComparisonOperatorHelper.Evaluate(
                triggerPart.Operator,
                triggerPart.IsNegative,
                triggerPart.IsIgnoreCase,
                GetUrlPart(triggerPart, url),
                triggerPart.ValueToCompare,
                triggerPart.ValuesToCompare);
        }

        private static string GetUrlPart(TriggerPart triggerPart, string url)
        {
            switch (triggerPart.UrlPart)
            {
                case UrlPartType.PagePath:
                    return GetPathFromUrl(url);
                case UrlPartType.PageUrl:
                    return url;
                case UrlPartType.HostName:
                    return GetHostNameFromUrl(url);
                default:
                    return string.Empty;
            }
        }

        public static string GetHostNameFromUrl(string url)
        {
            string urlMatcher = @"^(([^:/\?#]+):)?("
                + @"//(?<hostname>[^/\?#]*))?([^\?#]*)"
                + @"(\?([^#]*))?"
                + @"(#(.*))?";

            Regex re = new Regex(urlMatcher, RegexOptions.ExplicitCapture);
            Match m = re.Match(url);

            if (!m.Success)
                return string.Empty;

            return m.Groups["hostname"].Value;
        }

        public static string GetPathFromUrl(string url)
        {
            string urlMatcher = @"^(([^:/\?#]+):)?("
                + @"//([^/\?#]*))?(?<path>[^\?#]*)"
                + @"(\?([^#]*))?"
                + @"(#(.*))?";

            Regex re = new Regex(urlMatcher, RegexOptions.ExplicitCapture);
            Match m = re.Match(url);

            if (!m.Success)
                return string.Empty;

            return m.Groups["path"].Value;
        }
    }

    internal static class CookieValidatorHelper
    {
        public static bool Evaluate(TriggerPart triggerPart, IHttpRequest request)
        {
            return ComparisonOperatorHelper.Evaluate(triggerPart.Operator,
                triggerPart.IsNegative,
                triggerPart.IsIgnoreCase,
                GetCookie(triggerPart.CookieName, request),
                triggerPart.ValueToCompare,
                triggerPart.ValuesToCompare);
        }

        private static string GetCookie(string cookieName, IHttpRequest request)
        {
            var cookie = request.GetCookieValue(cookieName);

            if (cookie == null)
                return string.Empty;

            return cookie;
        }
    }

    internal static class UserAgentValidatorHelper
    {
        public static bool Evaluate(TriggerPart triggerPart, string userAgent)
        {
            return ComparisonOperatorHelper.Evaluate(triggerPart.Operator,
                triggerPart.IsNegative,
                triggerPart.IsIgnoreCase,
                userAgent ?? string.Empty,
                triggerPart.ValueToCompare,
                triggerPart.ValuesToCompare);
        }
    }

    internal static class HttpHeaderValidatorHelper
    {
        public static bool Evaluate(TriggerPart triggerPart, NameValueCollection httpHeaders)
        {
            return ComparisonOperatorHelper.Evaluate(triggerPart.Operator,
                triggerPart.IsNegative,
                triggerPart.IsIgnoreCase,
                httpHeaders?.Get(triggerPart.HttpHeaderName) ?? string.Empty,
                triggerPart.ValueToCompare,
                triggerPart.ValuesToCompare);
        }
    }

    internal static class ComparisonOperatorHelper
    {
        public static bool Evaluate(string opt, bool isNegative, bool isIgnoreCase, string value, string valueToCompare, string[] valuesToCompare)
        {
            value = value ?? string.Empty;
            valueToCompare = valueToCompare ?? string.Empty;
            valuesToCompare = valuesToCompare ?? new string[0];

            switch (opt)
            {
                case ComparisonOperatorType.EqualS:
                    return EqualS(value, valueToCompare, isNegative, isIgnoreCase);
                case ComparisonOperatorType.Contains:
                    return Contains(value, valueToCompare, isNegative, isIgnoreCase);             
                case ComparisonOperatorType.EqualsAny:
                    return EqualsAny(value, valuesToCompare, isNegative, isIgnoreCase);
                case ComparisonOperatorType.ContainsAny:
                    return ContainsAny(value, valuesToCompare, isNegative, isIgnoreCase);
                default:
                    return false;
            }
        }

        private static bool Contains(string value, string valueToCompare, bool isNegative, bool ignoreCase)
        {
            if (valueToCompare == "*" && !string.IsNullOrEmpty(value))
                return true;

            var evaluation = false;

            if (ignoreCase)
                evaluation = value.IndexOf(valueToCompare, StringComparison.OrdinalIgnoreCase) >= 0;
            else
                evaluation = value.Contains(valueToCompare);

            if (isNegative)
                return !evaluation;
            else
                return evaluation;
        }

        private static bool EqualS(string value, string valueToCompare, bool isNegative, bool ignoreCase)
        {
            var evaluation = false;

            if (ignoreCase)
                evaluation = string.Equals(value, valueToCompare, StringComparison.OrdinalIgnoreCase);
            else
                evaluation = value == valueToCompare;

            if (isNegative)
                return !evaluation;
            else
                return evaluation;
        }

        private static bool EqualsAny(string value, string[] valuesToCompare, bool isNegative, bool isIgnoreCase)
        {
            foreach (var valueToCompare in valuesToCompare)
            {
                if (EqualS(value, valueToCompare, false, isIgnoreCase))
                    return !isNegative;
            }
            
            return isNegative;
        }

        private static bool ContainsAny(string value, string[] valuesToCompare, bool isNegative, bool isIgnoreCase)
        {
            foreach (var valueToCompare in valuesToCompare)
            {
                if (Contains(value, valueToCompare, false, isIgnoreCase))
                    return !isNegative;
            }
            
            return isNegative;
        }
    }
}
