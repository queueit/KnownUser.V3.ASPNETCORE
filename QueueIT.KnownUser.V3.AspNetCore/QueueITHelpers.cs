using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;

namespace QueueIT.KnownUser.V3.AspNetCore
{
    internal class QueueParameterHelper
    {
        public const string TimeStampKey = "ts";
        public const string ExtendableCookieKey = "ce";
        public const string CookieValidityMinutesKey = "cv";
        public const string HashKey = "h";
        public const string EventIdKey = "e";
        public const string QueueIdKey = "q";
        public const string RedirectTypeKey = "rt";
        public const char KeyValueSeparatorChar = '_';
        public const char KeyValueSeparatorGroupChar = '~';

        public static QueueUrlParams ExtractQueueParams(string queueitToken)
        {
            if (string.IsNullOrEmpty(queueitToken))
                return null;

            QueueUrlParams result = new QueueUrlParams()
            {
                QueueITToken = queueitToken
            };
            var paramList = result.QueueITToken.Split(KeyValueSeparatorGroupChar);
            foreach (var paramKeyValue in paramList)
            {
                var keyValueArr = paramKeyValue.Split(KeyValueSeparatorChar);
                if (keyValueArr.Length != 2)
                    continue;

                switch (keyValueArr[0])
                {
                    case TimeStampKey:
                        result.TimeStamp = DateTimeHelper.GetDateTimeFromUnixTimeStamp(keyValueArr[1]);
                        break;
                    case CookieValidityMinutesKey:
                        {
                            int cookieValidity = 0;
                            if (int.TryParse(keyValueArr[1], out cookieValidity))
                            {
                                result.CookieValidityMinutes = cookieValidity;
                            }
                            else
                            {
                                result.CookieValidityMinutes = null;
                            }
                            break;
                        }
                    case HashKey:
                        result.HashCode = keyValueArr[1];
                        break;
                    case EventIdKey:
                        result.EventId = keyValueArr[1];
                        break;
                    case ExtendableCookieKey:
                        {
                            bool extendCookie;
                            if (!bool.TryParse(keyValueArr[1], out extendCookie))
                                extendCookie = false;
                            result.ExtendableCookie = extendCookie;
                            break;
                        }

                    case QueueIdKey:
                        result.QueueId = keyValueArr[1];
                        break;
                    case RedirectTypeKey:
                        result.RedirectType = keyValueArr[1];
                        break;
                }
            }
            result.QueueITTokenWithoutHash =
               result.QueueITToken.Replace($"{KeyValueSeparatorGroupChar}{HashKey}{KeyValueSeparatorChar}{result.HashCode}", "");
            return result;
        }
    }

    internal class ConnectorDiagnostics
    {
        internal bool IsEnabled { get; private set; }
        internal bool HasError { get; private set; }
        internal RequestValidationResult ValidationResult { get; private set; }

        private void SetStateWithTokenError(string customerId, string errorCode)
        {
            HasError = true;
            ValidationResult = new RequestValidationResult(
                "ConnectorDiagnosticsRedirect",
                redirectUrl: string.Format("https://{0}.api2.queue-it.net/{0}/diagnostics/connector/error/?code={1}", customerId, errorCode)
            );
        }

        private void SetStateWithSetupError()
        {
            HasError = true;
            ValidationResult = new RequestValidationResult(
                "ConnectorDiagnosticsRedirect",
                redirectUrl: "https://api2.queue-it.net/diagnostics/connector/error/?code=setup"
            );
        }

        internal static ConnectorDiagnostics Verify(string customerId, string secretKey, string queueitToken)
        {
            var diagnostics = new ConnectorDiagnostics();

            var qParams = QueueParameterHelper.ExtractQueueParams(queueitToken);

            if (qParams == null)
                return diagnostics;

            if (qParams.RedirectType == null)
                return diagnostics;

            if (!string.Equals(qParams.RedirectType, "debug", StringComparison.OrdinalIgnoreCase))
                return diagnostics;

            if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(secretKey))
            {
                diagnostics.SetStateWithSetupError();
                return diagnostics;
            }

            if (HashHelper.GenerateSHA256Hash(secretKey, qParams.QueueITTokenWithoutHash) != qParams.HashCode)
            {
                diagnostics.SetStateWithTokenError(customerId, "hash");
                return diagnostics;
            }

            if (qParams.TimeStamp < DateTime.UtcNow)
            {
                diagnostics.SetStateWithTokenError(customerId, "timestamp");
                return diagnostics;
            }

            diagnostics.IsEnabled = true;

            return diagnostics;
        }
    }

    internal static class DateTimeHelper
    {
        public static DateTime GetDateTimeFromUnixTimeStamp(string timeStampString)
        {
            long timestampSeconds;
            if (!long.TryParse(timeStampString, out timestampSeconds))
                timestampSeconds = 0;
            DateTime date1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return date1970.AddSeconds(timestampSeconds);
        }
        public static long GetUnixTimeStampFromDate(DateTime time)
        {
            return (long)(time.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }

    internal static class HashHelper
    {
        public static string GenerateSHA256Hash(string secretKey, string stringToHash)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] data = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }

    internal class QueueUrlParams
    {
        public DateTime TimeStamp { get; set; } = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public string EventId { get; set; }
        public string HashCode { get; set; }
        public bool ExtendableCookie { get; set; }
        public int? CookieValidityMinutes { get; set; }
        public string QueueITToken { get; set; }
        public string QueueITTokenWithoutHash { get; set; }
        public string QueueId { get; set; }
        public string RedirectType { get; set; }
    }

    internal class CookieHelper
    {
        public static NameValueCollection ToNameValueCollectionFromValue(string cookieValue)
        {
            try
            {
                NameValueCollection result = new NameValueCollection();
                var items = cookieValue.Split('&');
                foreach (var item in items)
                {
                    var keyValue = item.Split('=');
                    result.Add(keyValue[0], keyValue[1]);
                }
                return result;
            }
            catch
            {
                return new NameValueCollection();
            }
        }

        public static string ToValueFromNameValueCollection(NameValueCollection cookieValues)
        {
            List<string> values = new List<string>();

            foreach (string key in cookieValues)
                values.Add($"{key}={cookieValues[key]}");

            var result = string.Join("&", values);
            return result;
        }
    }
}
