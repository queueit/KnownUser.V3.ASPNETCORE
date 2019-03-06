using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace QueueIT.KnownUserV3.SDK.Sample
{
    public class QueueParameterHelper
    {
        public const string QueueITTokenKey = "queueittoken";
        public const string TimeStampKey = "ts";
        public const string ExtendableCookieKey = "ce";
        public const string CookieValidityMinuteKey = "cv";
        public const string HashKey = "h";
        public const string EventIdKey = "e";
        public const string QueueIdKey = "q";
        public const string RedirectTypeKey = "rt";
        public const char KeyValueSeparatorChar = '_';
        public const char KeyValueSeparatorGroupChar = '~';
        public static QueueUrlParams ExtractQueueParams(string queueitToken)
        {
            try
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

                    switch (keyValueArr[0])
                    {
                        case TimeStampKey:
                            result.TimeStamp = DateTimeHelper.GetUnixTimeStampAsDate(keyValueArr[1]);
                            break;
                        case CookieValidityMinuteKey:
                            {
                                int cookieValidity = 0;
                                if (int.TryParse(keyValueArr[1], out cookieValidity))
                                {
                                    result.CookieValidityMinute = cookieValidity;
                                }
                                else
                                {
                                    result.CookieValidityMinute = null;
                                }
                                break;
                            }

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
                        case HashKey:
                            result.HashCode = keyValueArr[1];
                            break;
                        case RedirectTypeKey:
                            result.RedirectType = keyValueArr[1];
                            break;
                        case QueueIdKey:
                            result.QueueId = keyValueArr[1];
                            break;
                    }
                }

                result.QueueITTokenWithoutHash =
                    result.QueueITToken.Replace($"{KeyValueSeparatorGroupChar}{HashKey}{KeyValueSeparatorChar}{result.HashCode}", "");
                return result;
            }
            catch
            {
                return null;
            }
        }

        public static string GetPureUrlWithoutQueueITToken(string url)
        {
            return Regex.Replace(url, @"([\?&])(" + QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);
        }


    }
    public static class DateTimeHelper
    {
        public static DateTime GetUnixTimeStampAsDate(string timeStampString)
        {
            long timestampSeconds = long.Parse(timeStampString);
            DateTime date1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return date1970.AddSeconds(timestampSeconds);
        }
        public static long GetUnixTimeStampFromDate(DateTime time)
        {
            return (long)(time.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }


    public static class HashHelper
    {
        public static string GenerateSHA256Hash(string secretKey, string stringToHash)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                return HttpUtility.UrlEncode(HttpUtility.UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToHash))));
            }
        }
    }


    public class QueueUrlParams
    {
        public DateTime TimeStamp { get; set; }
        public string EventId { get; set; }
        public string HashCode { get; set; }
        public bool ExtendableCookie { get; set; }
        public int? CookieValidityMinute { get; set; }
        public string QueueITToken { get; set; }
        public string QueueITTokenWithoutHash { get; set; }
        public string RedirectType { get; set; }
        public string QueueId { get; set; }
    }
}
