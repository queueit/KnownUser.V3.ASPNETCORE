namespace QueueIT.KnownUser.V3.AspNetCore
{
    public class RequestValidationResult
    {
        public RequestValidationResult(
            string actionType,
            string eventId = null,
            string queueId = null,
            string redirectUrl = null,
            string redirectType = null,
            string actionName = null,
            bool isAjaxResult = false)
        {
            ActionType = actionType;
            EventId = eventId;
            QueueId = queueId;
            RedirectUrl = redirectUrl;
            RedirectType = redirectType;
            ActionName = actionName;
            IsAjaxResult = isAjaxResult;
        }

        public string RedirectUrl { get; }
        public string QueueId { get; }
        public bool DoRedirect
        {
            get
            {
                return !string.IsNullOrEmpty(RedirectUrl);
            }
        }
        public string EventId { get; }
        public string ActionType { get; }
        public string ActionName { get; }
        public string RedirectType { get; }
        public bool IsAjaxResult { get; internal set; }
        public string AjaxQueueRedirectHeaderKey
        {
            get
            {
                return "x-queueit-redirect";
            }
        }
        public string AjaxRedirectUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(RedirectUrl))
                {
                    return System.Uri.EscapeDataString(RedirectUrl);
                }
                return string.Empty;
            }
        }
    }

    public class QueueEventConfig
    {
        public QueueEventConfig()
        {
            Version = -1;
            ActionName = "unspecified";
        }

        public string EventId { get; set; }
        public string LayoutName { get; set; }
        public string Culture { get; set; }
        public string QueueDomain { get; set; }
        public bool ExtendCookieValidity { get; set; }
        public int CookieValidityMinute { get; set; }
        public string CookieDomain { get; set; }
        public bool IsCookieHttpOnly { get; set; }
        public bool IsCookieSecure { get; set; }
        public int Version { get; set; }
        public string ActionName { get; set; }

        public override string ToString()
        {
            return $"EventId:{EventId}" +
                   $"&Version:{Version}" +
                   $"&QueueDomain:{QueueDomain}" +
                   $"&CookieDomain:{CookieDomain}" +
                   $"&IsCookieHttpOnly:{IsCookieHttpOnly}" +
                   $"&IsCookieSecure:{IsCookieSecure}" +
                   $"&ExtendCookieValidity:{ExtendCookieValidity}" +
                   $"&CookieValidityMinute:{CookieValidityMinute}" +
                   $"&LayoutName:{LayoutName}" +
                   $"&Culture:{Culture}" +
                   $"&ActionName:{ActionName}";
        }
    }

    public class CancelEventConfig
    {
        public CancelEventConfig()
        {
            Version = -1;
            ActionName = "unspecified";
        }

        public string EventId { get; set; }
        public string QueueDomain { get; set; }
        public int Version { get; set; }
        public string CookieDomain { get; set; }
        public bool IsCookieHttpOnly { get; set; }
        public bool IsCookieSecure { get; set; }
        public string ActionName { get; set; }

        public override string ToString()
        {
            return $"EventId:{EventId}" +
                   $"&Version:{Version}" +
                   $"&QueueDomain:{QueueDomain}" +
                   $"&CookieDomain:{CookieDomain}" +
                   $"&IsCookieHttpOnly:{IsCookieHttpOnly}" +
                   $"&IsCookieSecure:{IsCookieSecure}" +
                   $"&ActionName:{ActionName}";
        }
    }
}
