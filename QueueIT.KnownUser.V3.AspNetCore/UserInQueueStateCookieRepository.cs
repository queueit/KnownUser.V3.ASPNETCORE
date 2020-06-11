using System;
using System.Collections.Specialized;

namespace QueueIT.KnownUser.V3.AspNetCore
{
    internal interface IUserInQueueStateRepository
    {
        void Store(
            string eventId,
            string queueId,
            int? fixedCookieValidityMinutes,
            string cookieDomain,
            string redirectType,
            string secretKey);

        StateInfo GetState(
            string eventId,
            int cookieValidityMinutes,
            string secretKey,
            bool validateTime = true);

        void CancelQueueCookie(
            string eventId,
            string cookieDomain);

        void ReissueQueueCookie(
            string eventId,
            int cookieValidityMinutes,
            string cookieDomain,
            string secretKey);
    }

    internal class UserInQueueStateCookieRepository : IUserInQueueStateRepository
    {
        private const string _QueueITDataKey = "QueueITAccepted-SDFrts345E-V3";
        private const string _HashKey = "Hash";
        private const string _IssueTimeKey = "IssueTime";
        private const string _QueueIdKey = "QueueId";
        private const string _EventIdKey = "EventId";
        private const string _RedirectTypeKey = "RedirectType";
        private const string _FixedCookieValidityMinutesKey = "FixedValidityMins";

        private IHttpContextProvider _httpContextProvider;


        internal static string GetCookieKey(string eventId)
        {
            return $"{_QueueITDataKey}_{eventId}";
        }

        public UserInQueueStateCookieRepository(IHttpContextProvider httpContextProvider)
        {
            this._httpContextProvider = httpContextProvider;
        }

        public void Store(
            string eventId,
            string queueId,
            int? fixedCookieValidityMinutes,
            string cookieDomain,
            string redirectType,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);

            CreateCookie(
                eventId, queueId,
                Convert.ToString(fixedCookieValidityMinutes),
                redirectType, cookieDomain, secretKey);
        }

        public StateInfo GetState(string eventId,
            int cookieValidityMinutes,
            string secretKey,
            bool validateTime = true)
        {
            try
            {
                var cookieKey = GetCookieKey(eventId);
                var cookie = _httpContextProvider.HttpRequest.GetCookieValue(cookieKey);
                if (cookie == null)
                    return new StateInfo(false, false, string.Empty, null, string.Empty);

                var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookie);
                if (!IsCookieValid(secretKey, cookieValues, eventId, cookieValidityMinutes, validateTime))
                    return new StateInfo(true, false, string.Empty, null, string.Empty);

                return new StateInfo(
                    true,
                    true, cookieValues[_QueueIdKey],
                    !string.IsNullOrEmpty(cookieValues[_FixedCookieValidityMinutesKey])
                        ? int.Parse(cookieValues[_FixedCookieValidityMinutesKey]) : (int?)null,
                    cookieValues[_RedirectTypeKey]);
            }
            catch (Exception)
            {
                return new StateInfo(true, false, string.Empty, null, string.Empty);
            }
        }

        private string GenerateHash(
            string eventId,
            string queueId,
            string fixedCookieValidityMinutes,
            string redirectType,
            string issueTime,
            string secretKey)
        {
            string valueToHash = string.Concat(eventId, queueId, fixedCookieValidityMinutes, redirectType, issueTime);
            return HashHelper.GenerateSHA256Hash(secretKey, valueToHash);
        }

        public void CancelQueueCookie(string eventId, string cookieDomain)
        {
            var cookieKey = GetCookieKey(eventId);
            _httpContextProvider.HttpResponse.SetCookie(cookieKey, string.Empty, cookieDomain, DateTime.UtcNow.AddDays(-1d));
        }

        public void ReissueQueueCookie(
            string eventId,
            int cookieValidityMinutes,
            string cookieDomain,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);
            var cookie = _httpContextProvider.HttpRequest.GetCookieValue(cookieKey);

            if (cookie == null)
                return;

            var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookie);

            if (!IsCookieValid(secretKey, cookieValues, eventId, cookieValidityMinutes, true))
                return;

            CreateCookie(
                           eventId, cookieValues[_QueueIdKey],
                           cookieValues[_FixedCookieValidityMinutesKey],
                           cookieValues[_RedirectTypeKey],
                           cookieDomain, secretKey);
        }

        private void CreateCookie(
            string eventId,
            string queueId,
            string fixedCookieValidityMinutes,
            string redirectType,
            string cookieDomain,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow).ToString();

            NameValueCollection cookieValues = new NameValueCollection();
            cookieValues.Add(_EventIdKey, eventId);
            cookieValues.Add(_QueueIdKey, queueId);
            if (!string.IsNullOrEmpty(fixedCookieValidityMinutes))
            {
                cookieValues.Add(_FixedCookieValidityMinutesKey, fixedCookieValidityMinutes);
            }
            cookieValues.Add(_RedirectTypeKey, redirectType.ToLower());
            cookieValues.Add(_IssueTimeKey, issueTime);
            cookieValues.Add(_HashKey, GenerateHash(eventId.ToLower(), queueId, fixedCookieValidityMinutes, redirectType.ToLower(), issueTime, secretKey));

            _httpContextProvider.HttpResponse.SetCookie(cookieKey, CookieHelper.ToValueFromNameValueCollection(cookieValues),
                cookieDomain, DateTime.UtcNow.AddDays(1));
        }

        private bool IsCookieValid(
            string secretKey,
            NameValueCollection cookieValues,
            string eventId,
            int cookieValidityMinutes,
            bool validateTime)
        {
            var storedHash = cookieValues[_HashKey];
            var issueTimeString = cookieValues[_IssueTimeKey];
            var queueId = cookieValues[_QueueIdKey];
            var eventIdFromCookie = cookieValues[_EventIdKey];
            var redirectType = cookieValues[_RedirectTypeKey];
            var fixedCookieValidityMinutes = cookieValues[_FixedCookieValidityMinutesKey];

            var expectedHash = GenerateHash(
                eventIdFromCookie,
                queueId,
                fixedCookieValidityMinutes,
                redirectType,
                issueTimeString,
                secretKey);

            if (!expectedHash.Equals(storedHash))
                return false;

            if (eventId.ToLower() != eventIdFromCookie.ToLower())
                return false;

            if (validateTime)
            {
                var validity = !string.IsNullOrEmpty(fixedCookieValidityMinutes) ? int.Parse(fixedCookieValidityMinutes) : cookieValidityMinutes;
                var expirationTime = DateTimeHelper.GetDateTimeFromUnixTimeStamp(issueTimeString).AddMinutes(validity);
                if (expirationTime < DateTime.UtcNow)
                    return false;
            }
            return true;
        }
    }

    internal class StateInfo
    {
        public bool IsFound { get; }
        public bool IsValid { get; }
        public string QueueId { get; }
        public bool IsStateExtendable
        {
            get
            {
                return IsValid && !FixedCookieValidityMinutes.HasValue;
            }
        }
        public int? FixedCookieValidityMinutes { get; }
        public string RedirectType { get; }

        public StateInfo(bool isFound, bool isValid, string queueId, int? fixedCookieValidityMinutes, string redirectType)
        {
            IsFound = isFound;
            IsValid = isValid;
            QueueId = queueId;
            FixedCookieValidityMinutes = fixedCookieValidityMinutes;
            RedirectType = redirectType;
        }
    }
}
