using System;
using System.Web;
using System.Collections.Generic;
using QueueIT.KnownUserV3.SDK.IntegrationConfig;

namespace QueueIT.KnownUserV3.SDK
{
    internal interface IUserInQueueService
    {
        RequestValidationResult ValidateQueueRequest(
            string targetUrl,
            string queueitToken,
            QueueEventConfig config,
            string customerId,
            string secretKey);

        RequestValidationResult ValidateCancelRequest(
            string targetUrl,
            CancelEventConfig config,
            string customerId,
            string secretKey);

        RequestValidationResult GetIgnoreResult();

        void ExtendQueueCookie(
            string eventId,
            int cookieValidityMinutes,
            string cookieDomain,
            string secretKey);
    }

    internal class UserInQueueService : IUserInQueueService
    {
        internal const string SDK_VERSION = "3.5.1";
        private readonly IUserInQueueStateRepository _userInQueueStateRepository;

        public UserInQueueService(IUserInQueueStateRepository queueStateRepository)
        {
            this._userInQueueStateRepository = queueStateRepository;
        }

        public RequestValidationResult ValidateQueueRequest(
            string targetUrl,
            string queueitToken,
            QueueEventConfig config,
            string customerId,
            string secretKey)
        {
            var state = _userInQueueStateRepository.GetState(config.EventId, config.CookieValidityMinute, secretKey);
            if (state.IsValid)
            {
                if (state.IsStateExtendable && config.ExtendCookieValidity)
                {
                    this._userInQueueStateRepository.Store(config.EventId,
                        state.QueueId,
                        null,
                        config.CookieDomain,
                        state.RedirectType,
                        secretKey);
                }
                return new RequestValidationResult(ActionType.QueueAction)
                {
                    EventId = config.EventId,
                    QueueId = state.QueueId,
                    RedirectType = state.RedirectType
                };
            }

            QueueUrlParams queueParmas = QueueParameterHelper.ExtractQueueParams(queueitToken);

            if (queueParmas != null)
            {
                return GetQueueITTokenValidationResult(targetUrl, config.EventId, config, queueParmas, customerId, secretKey);
            }
            else
            {
                return GetInQueueRedirectResult(targetUrl, config, customerId);
            }
        }

        private RequestValidationResult GetQueueITTokenValidationResult(
            string targetUrl,
            string eventId,
            QueueEventConfig config,
            QueueUrlParams queueParams,
            string customerId,
            string secretKey)
        {
            string calculatedHash = HashHelper.GenerateSHA256Hash(secretKey, queueParams.QueueITTokenWithoutHash);
            if (calculatedHash != queueParams.HashCode)
                return GetVaidationErrorResult(customerId, targetUrl, config, queueParams, "hash");

            if (queueParams.EventId != eventId)
                return GetVaidationErrorResult(customerId, targetUrl, config, queueParams, "eventid");

            if (queueParams.TimeStamp < DateTime.UtcNow)
                return GetVaidationErrorResult(customerId, targetUrl, config, queueParams, "timestamp");

            this._userInQueueStateRepository.Store(
                config.EventId,
                queueParams.QueueId,
                queueParams.CookieValidityMinutes,
                config.CookieDomain,
                queueParams.RedirectType,
                secretKey);

            return new RequestValidationResult(ActionType.QueueAction)
            {
                EventId = config.EventId,
                QueueId = queueParams.QueueId,
                RedirectType = queueParams.RedirectType
            };
        }

        private RequestValidationResult GetVaidationErrorResult(
            string customerId,
             string targetUrl,
             QueueEventConfig config,
             QueueUrlParams qParams,
             string errorCode)
        {
            var query = GetQueryString(customerId, config.EventId, config.Version, config.Culture, config.LayoutName) +
                $"&queueittoken={qParams.QueueITToken}" +
                $"&ts={DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow)}" +
                (!string.IsNullOrEmpty(targetUrl) ? $"&t={HttpUtility.UrlEncode(targetUrl)}" : "");

            var domainAlias = config.QueueDomain;
            if (!domainAlias.EndsWith("/"))
                domainAlias = domainAlias + "/";

            var redirectUrl = $"https://{domainAlias}error/{errorCode}/?{query}";

            return new RequestValidationResult(ActionType.QueueAction)
            {
                RedirectUrl = redirectUrl,
                EventId = config.EventId
            };
        }

        private RequestValidationResult GetInQueueRedirectResult(
            string targetUrl,
            QueueEventConfig config,
            string customerId)
        {
            var redirectUrl = "https://" + config.QueueDomain + "?" +
                GetQueryString(customerId, config.EventId, config.Version, config.Culture, config.LayoutName) +
                    (!string.IsNullOrEmpty(targetUrl) ? $"&t={HttpUtility.UrlEncode(targetUrl)}" : "");

            return new RequestValidationResult(ActionType.QueueAction)
            {
                RedirectUrl = redirectUrl,
                EventId = config.EventId
            };
        }

        private string GetQueryString(
            string customerId,
            string eventId,
            int configVersion,
            string culture = null,
            string layoutName = null)
        {
            List<string> queryStringList = new List<string>();
            queryStringList.Add($"c={HttpUtility.UrlEncode(customerId)}");
            queryStringList.Add($"e={HttpUtility.UrlEncode(eventId)}");
            queryStringList.Add($"ver=v3-aspnetcore-{SDK_VERSION}");
            queryStringList.Add($"cver={configVersion.ToString()}");

            if (!string.IsNullOrEmpty(culture))
                queryStringList.Add(string.Concat("cid=", HttpUtility.UrlEncode(culture)));

            if (!string.IsNullOrEmpty(layoutName))
                queryStringList.Add(string.Concat("l=", HttpUtility.UrlEncode(layoutName)));

            return string.Join("&", queryStringList);
        }

        public void ExtendQueueCookie(
            string eventId,
            int cookieValidityMinutes,
            string cookieDomain,
            string secretKey)
        {
            this._userInQueueStateRepository.ReissueQueueCookie(eventId, cookieValidityMinutes, cookieDomain, secretKey);
        }

        public RequestValidationResult ValidateCancelRequest(
            string targetUrl,
            CancelEventConfig config,
            string customerId,
            string secretKey)
        {
            //we do not care how long cookie is valid while canceling cookie
            var state = _userInQueueStateRepository.GetState(config.EventId, -1, secretKey, false);

            if (state.IsValid)
            {
                this._userInQueueStateRepository.CancelQueueCookie(config.EventId, config.CookieDomain);

                var query = GetQueryString(customerId, config.EventId, config.Version) +
                         (!string.IsNullOrEmpty(targetUrl) ? $"&r={HttpUtility.UrlEncode(targetUrl)}" : "");

                var domainAlias = config.QueueDomain;
                if (!domainAlias.EndsWith("/"))
                    domainAlias = domainAlias + "/";

                var redirectUrl = "https://" + domainAlias + "cancel/" + customerId + "/" + config.EventId + "/?" + query;

                return new RequestValidationResult(ActionType.CancelAction)
                {
                    RedirectUrl = redirectUrl,
                    EventId = config.EventId,
                    QueueId = state.QueueId,
                    RedirectType = state.RedirectType
                };
            }
            else
            {
                return new RequestValidationResult(ActionType.CancelAction)
                {
                    RedirectUrl = null,
                    EventId = config.EventId,
                    QueueId = null,
                    RedirectType = null
                };
            }
        }

        public RequestValidationResult GetIgnoreResult()
        {
            return new RequestValidationResult(ActionType.IgnoreAction);
        }
    }
}