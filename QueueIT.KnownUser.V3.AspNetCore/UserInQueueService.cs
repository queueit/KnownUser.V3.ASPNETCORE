using QueueIT.KnownUser.V3.AspNetCore.IntegrationConfig;
using System;
using System.Collections.Generic;

namespace QueueIT.KnownUser.V3.AspNetCore
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

        RequestValidationResult GetIgnoreResult(string actionName);

        void ExtendQueueCookie(
            string eventId,
            int cookieValidityMinutes,
            string cookieDomain,
            bool isCookieHttpOnly,
            bool isCookieSecure,
            string secretKey);
    }

    internal class UserInQueueService : IUserInQueueService
    {
        internal const string SDK_VERSION = "v3-aspnetcore-" + "3.7.0";

        private readonly IUserInQueueStateRepository _userInQueueStateRepository;

        public UserInQueueService(IUserInQueueStateRepository queueStateRepository)
        {
            _userInQueueStateRepository = queueStateRepository;
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
                    _userInQueueStateRepository.Store(config.EventId,
                        state.QueueId,
                        null,
                        config.CookieDomain,
                        config.IsCookieHttpOnly,
                        config.IsCookieSecure,
                        state.RedirectType,
                        secretKey);
                }
                return new RequestValidationResult(ActionType.QueueAction,
                    eventId: config.EventId,
                    queueId: state.QueueId,
                    redirectType: state.RedirectType,
                    actionName: config.ActionName);
            }
            QueueUrlParams queueParams = QueueParameterHelper.ExtractQueueParams(queueitToken);

            RequestValidationResult requestValidationResult;
            bool isTokenValid = false;
            
            if (queueParams != null)
            {
                var tokenValidationResult = ValidateToken(config, queueParams, secretKey);
                isTokenValid = tokenValidationResult.IsValid;

                if (isTokenValid)
                {
                    requestValidationResult = GetValidTokenResult(config, queueParams, secretKey);
                }
                else
                {
                    requestValidationResult = GetErrorResult(customerId, targetUrl, config, queueParams, tokenValidationResult.ErrorCode);
                }
            }
            else
            {
                requestValidationResult = GetQueueResult(targetUrl, config, customerId);
            }
            
            if (state.IsFound && !isTokenValid)
            {
                _userInQueueStateRepository.CancelQueueCookie(config.EventId, config.CookieDomain, config.IsCookieHttpOnly, config.IsCookieSecure);
            }
            
            return requestValidationResult;
        }

        private RequestValidationResult GetValidTokenResult(
            QueueEventConfig config,
            QueueUrlParams queueParams,
            string secretKey)
        {
            _userInQueueStateRepository.Store(
                config.EventId,
                queueParams.QueueId,
                queueParams.CookieValidityMinutes,
                config.CookieDomain,
                config.IsCookieHttpOnly,
                config.IsCookieSecure,
                queueParams.RedirectType,
                secretKey);

            return new RequestValidationResult(
                ActionType.QueueAction,
                eventId: config.EventId,
                queueId: queueParams.QueueId,
                redirectType: queueParams.RedirectType,
                actionName: config.ActionName);
        }

        private RequestValidationResult GetErrorResult(
            string customerId,
            string targetUrl,
            QueueEventConfig config,
            QueueUrlParams qParams,
            string errorCode)
        {
            var query = GetQueryString(customerId, config.EventId, config.Version, config.ActionName, config.Culture, config.LayoutName) +
                $"&queueittoken={qParams.QueueITToken}" +
                $"&ts={DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow)}" +
                (!string.IsNullOrEmpty(targetUrl) ? $"&t={Uri.EscapeDataString(targetUrl)}" : "");

            var redirectUrl = GenerateRedirectUrl(config.QueueDomain, $"error/{errorCode}/", query);

            return new RequestValidationResult(
                ActionType.QueueAction,
                redirectUrl: redirectUrl,
                eventId: config.EventId,
                actionName: config.ActionName);
        }

        private RequestValidationResult GetQueueResult(
            string targetUrl,
            QueueEventConfig config,
            string customerId)
        {
            var query = GetQueryString(customerId, config.EventId, config.Version, config.ActionName, config.Culture, config.LayoutName) +
                            (!string.IsNullOrEmpty(targetUrl) ? $"&t={Uri.EscapeDataString(targetUrl)}" : "");

            var redirectUrl = GenerateRedirectUrl(config.QueueDomain, "", query);

            return new RequestValidationResult(
                ActionType.QueueAction,
                redirectUrl: redirectUrl,
                eventId: config.EventId,
                actionName: config.ActionName);
        }

        private string GetQueryString(
            string customerId,
            string eventId,
            int configVersion,
            string actionName,
            string culture = null,
            string layoutName = null)
        {
            List<string> queryStringList = new List<string>();
            queryStringList.Add($"c={Uri.EscapeDataString(customerId)}");
            queryStringList.Add($"e={Uri.EscapeDataString(eventId)}");
            queryStringList.Add($"ver={SDK_VERSION}");
            queryStringList.Add($"cver={configVersion.ToString()}");
            queryStringList.Add($"man={Uri.EscapeDataString(actionName)}");

            if (!string.IsNullOrEmpty(culture))
                queryStringList.Add(string.Concat("cid=", Uri.EscapeDataString(culture)));

            if (!string.IsNullOrEmpty(layoutName))
                queryStringList.Add(string.Concat("l=", Uri.EscapeDataString(layoutName)));

            return string.Join("&", queryStringList);
        }

        private string GenerateRedirectUrl(string queueDomain, string uriPath, string query)
        {
            if (!queueDomain.EndsWith("/"))
                queueDomain += "/";

            return $"https://{queueDomain}{uriPath}?{query}";
        }

        public void ExtendQueueCookie(
            string eventId,
            int cookieValidityMinutes,
            string cookieDomain,
            bool isCookieHttpOnly,
            bool isCookieSecure,
            string secretKey)
        {
            _userInQueueStateRepository.ReissueQueueCookie(eventId, cookieValidityMinutes, cookieDomain, isCookieHttpOnly, isCookieSecure, secretKey);
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
                _userInQueueStateRepository.CancelQueueCookie(config.EventId, config.CookieDomain, config.IsCookieHttpOnly, config.IsCookieSecure);

                var query = GetQueryString(customerId, config.EventId, config.Version, config.ActionName) +
                                (!string.IsNullOrEmpty(targetUrl) ? $"&r={Uri.EscapeDataString(targetUrl)}" : "");

                var uriPath = $"cancel/{customerId}/{config.EventId}";
                if (!string.IsNullOrEmpty(state.QueueId))
                {
                    uriPath += $"/{state.QueueId}";
                }

                var redirectUrl = GenerateRedirectUrl(config.QueueDomain, uriPath, query);

                return new RequestValidationResult(ActionType.CancelAction,
                    redirectUrl: redirectUrl,
                    eventId: config.EventId,
                    queueId: state.QueueId,
                    redirectType: state.RedirectType,
                    actionName: config.ActionName);
            }
            else
            {
                return new RequestValidationResult(ActionType.CancelAction,
                    eventId: config.EventId,
                    actionName: config.ActionName);
            }
        }

        public RequestValidationResult GetIgnoreResult(string actionName)
        {
            return new RequestValidationResult(ActionType.IgnoreAction, actionName: actionName);
        }

        private TokenValidationResult ValidateToken(
            QueueEventConfig config,
            QueueUrlParams queueParams,
            string secretKey)
        {
            string calculatedHash = HashHelper.GenerateSHA256Hash(secretKey, queueParams.QueueITTokenWithoutHash);

            if (calculatedHash != queueParams.HashCode)
                return new TokenValidationResult(false, "hash");

            if (queueParams.EventId != config.EventId)
                return new TokenValidationResult(false, "eventid");

            if (queueParams.TimeStamp < DateTime.UtcNow)
                return new TokenValidationResult(false, "timestamp");

            return new TokenValidationResult(true, null);
        }

        private class TokenValidationResult
        {
            public TokenValidationResult(
                bool isValid,
                string errorCode)
            {
                IsValid = isValid;
                ErrorCode = errorCode;
            }

            public bool IsValid { get; }
            public string ErrorCode { get; }
        }
    }
}