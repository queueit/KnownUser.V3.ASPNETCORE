using NSubstitute;
using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Xunit;

namespace QueueIT.KnownUserV3.SDK.Tests
{
    public class UserInQueueServiceTest
    {
        #region ExtendableCookie Cookie

        [Fact]
        public void ValidateRequest_ValidState_ExtendableCookie_NoCookieExtensionFromConfig_DoNotRedirectDoNotStoreCookieWithExtension()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();
            string queueId = "queueId";
            var config = new QueueEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false
            };

            cookieProviderMock.GetState("", 0, "")
                .ReturnsForAnyArgs(new StateInfo(true, queueId, null, "idle"));


            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateQueueRequest("url", "token", config, "testCustomer", "key");
            Assert.True(!result.DoRedirect);
            Assert.True(result.QueueId == queueId);
            Assert.True(result.RedirectType == "idle");

            cookieProviderMock.DidNotReceiveWithAnyArgs().Store("", queueId, null, "", "", "");
            Assert.True(config.EventId == result.EventId);

        }

        [Fact]
        public void ValidateRequest_ValidState_ExtendableCookie_CookieExtensionFromConfig_DoNotRedirectDoStoreCookieWithExtension()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();
            string queueId = "queueId";

            var config = new QueueEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testdomain",
                CookieValidityMinute = 20,
                ExtendCookieValidity = true,
                CookieDomain = ".testdomain.com"
            };

            cookieProviderMock.GetState("", 20, "").ReturnsForAnyArgs(new StateInfo(true, queueId, null, "disabled"));

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateQueueRequest("url", "token", config, "testCustomer", "key");
            Assert.True(!result.DoRedirect);
            Assert.True(result.QueueId == queueId);
            Assert.True(result.RedirectType == "disabled");


            cookieProviderMock.Received().Store(
                                "e1",
                               queueId,
                                null,
                                config.CookieDomain,
                                "disabled",
                                "key");
            cookieProviderMock.Received().GetState("e1", 20, "key");
            Assert.True(config.EventId == result.EventId);
        }

        #endregion

        [Fact]
        public void ValidateRequest_ValidState_NoExtendableCookie_DoNotRedirectDoNotStoreCookieWithExtension()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();
            string queueId = "queueId";

            var config = new QueueEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = true
            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            cookieProviderMock.GetState("", 10, "")
              .ReturnsForAnyArgs(new StateInfo(true, queueId, 3, "idle"));

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateQueueRequest("url", "token", config, "testCustomer", customerKey);
            Assert.True(result.QueueId == queueId);
            Assert.True(result.RedirectType == "idle");
            Assert.True(!result.DoRedirect);
            cookieProviderMock.DidNotReceiveWithAnyArgs().Store(null, null, 0, null, "", null);
            Assert.True(config.EventId == result.EventId);
            cookieProviderMock.Received().GetState("e1", 10, customerKey);
        }

        [Fact]
        public void ValidateRequest_NoCookie_TampredToken_RedirectToErrorPageWithHashError_DoNotStoreCookie()
        {
            Exception expectedException = new Exception();
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();

            var config = new QueueEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Version = 100
            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var queueId = "iopdb821-a825-49da-acd0-5d376f2068db";

            cookieProviderMock.GetState("", 10, "").ReturnsForAnyArgs(new StateInfo(false, "", null, ""));
            string hash = "";

            var queueitToken = QueueITTokenGenerator.GenerateToken(
                                  DateTime.UtcNow.AddHours(1),
                                  "e1",
                                  queueId,
                                  false,
                                  20,
                                  customerKey,
                                  out hash,
                                  "idle");

            queueitToken = queueitToken.Replace("False", "True");
            var targetUrl = "http://test.test.com?b=h";
            var knownUserVersion = UserInQueueService.SDK_VERSION;
            var expectedErrorUrl = $"https://testDomain.com/error/hash/?c=testCustomer&e=e1" +
                $"&ver=v3-aspnetcore-{knownUserVersion}"
                + $"&cver=100"
                + $"&queueittoken={queueitToken}"
                + $"&t={HttpUtility.UrlEncode(targetUrl)}";


            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);
            var result = testObject.ValidateQueueRequest(targetUrl, queueitToken, config, "testCustomer", customerKey);
            Assert.True(result.DoRedirect);

            var regex = new Regex("&ts=[^&]*");
            var match = regex.Match(result.RedirectUrl);
            var serverTimestamp = DateTimeHelper.GetDateTimeFromUnixTimeStamp(match.Value.Replace("&ts=", "").Replace("&", ""));
            Assert.True(DateTime.UtcNow.Subtract(serverTimestamp) < TimeSpan.FromSeconds(10));
            var redirectUrl = regex.Replace(result.RedirectUrl, "");
            Assert.True(redirectUrl.ToUpper() == expectedErrorUrl.ToUpper());
            Assert.True(config.EventId == result.EventId);
            cookieProviderMock.DidNotReceiveWithAnyArgs().Store("", "", null, "", "", "");
        }

        [Fact]
        public void ValidateRequest_NoCookie_ExpiredTimeStampInToken_RedirectToErrorPageWithTimeStampError_DoNotStoreCookie()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();

            var config = new QueueEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Version = 100
            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var queueId = "iopdb821-a825-49da-acd0-5d376f2068db";

            cookieProviderMock.GetState("", 10, "").ReturnsForAnyArgs(new StateInfo(false, "", null, ""));
            string hash = null;
            var queueitToken = QueueITTokenGenerator.GenerateToken(
                                    DateTime.UtcNow.AddHours(-1),
                                    "e1",
                                    queueId,
                                    true,
                                    20,
                                    customerKey,
                                    out hash,
                                    "queue"
                            );
            var targetUrl = "http://test.test.com?b=h";
            var knownUserVersion = UserInQueueService.SDK_VERSION;
            var expectedErrorUrl = $"https://testDomain.com/error/timestamp/?c=testCustomer&e=e1" +
                $"&ver=v3-aspnetcore-{knownUserVersion}"
                + $"&cver=100"
                + $"&queueittoken={queueitToken}"
                + $"&t={HttpUtility.UrlEncode(targetUrl)}";

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateQueueRequest(targetUrl, queueitToken, config, "testCustomer", customerKey);
            Assert.True(result.DoRedirect);
            var regex = new Regex("&ts=[^&]*");
            var match = regex.Match(result.RedirectUrl);
            var serverTimestamp = DateTimeHelper.GetDateTimeFromUnixTimeStamp(match.Value.Replace("&ts=", "").Replace("&", ""));
            Assert.True(DateTime.UtcNow.Subtract(serverTimestamp) < TimeSpan.FromSeconds(10));
            var redirectUrl = regex.Replace(result.RedirectUrl, "");
            Assert.True(redirectUrl.ToUpper() == expectedErrorUrl.ToUpper());
            Assert.True(config.EventId == result.EventId);
            cookieProviderMock.DidNotReceiveWithAnyArgs().Store("", "", null, "", null, "");
        }

        [Fact]
        public void ValidateRequest_NoCookie_EventIdMismatch_RedirectToErrorPageWithEventIdMissMatchError_DoNotStoreCookie()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();
            var config = new QueueEventConfig()
            {
                EventId = "e2",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Version = 10

            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var queueId = "iopdb821-a825-49da-acd0-5d376f2068db";
            cookieProviderMock.GetState("", 10, "").ReturnsForAnyArgs(new StateInfo(false, "", null, ""));
            string hash = "";

            var queueitToken = QueueITTokenGenerator.GenerateToken(
                                  DateTime.UtcNow.AddHours(1),
                                  "e1",
                                  queueId,
                                  true,
                                  null,
                                  customerKey,
                                  out hash, "queue"
                          );

            var targetUrl = "http://test.test.com?b=h";
            var knownUserVersion = UserInQueueService.SDK_VERSION;
            var expectedErrorUrl = $"https://testDomain.com/error/eventid/?c=testCustomer&e=e2" +
                $"&ver=v3-aspnetcore-{knownUserVersion}" + "&cver=10"
                + $"&queueittoken={queueitToken}"
                + $"&t={HttpUtility.UrlEncode(targetUrl)}";

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateQueueRequest(targetUrl, queueitToken, config, "testCustomer", customerKey);
            Assert.True(result.DoRedirect);
            var regex = new Regex("&ts=[^&]*");
            var match = regex.Match(result.RedirectUrl);
            var serverTimestamp = DateTimeHelper.GetDateTimeFromUnixTimeStamp(match.Value.Replace("&ts=", "").Replace("&", ""));
            Assert.True(DateTime.UtcNow.Subtract(serverTimestamp) < TimeSpan.FromSeconds(10));

            var redirectUrl = regex.Replace(result.RedirectUrl, "");
            Assert.True(redirectUrl.ToUpper() == expectedErrorUrl.ToUpper());
            Assert.True(config.EventId == result.EventId);
            cookieProviderMock.DidNotReceiveWithAnyArgs().Store("", "", null, "", null, "");

        }

        [Fact]
        public void ValidateRequest_NoCookie_ValidToken_ExtendableCookie_DoNotRedirect_StoreExtendableCookie()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();

            var config = new QueueEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false
            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "iopdb821-a825-49da-acd0-5d376f2068db";
            cookieProviderMock.GetState("", 10, "").ReturnsForAnyArgs(new StateInfo(false, "", null, ""));
            string hash = "";

            var queueitToken = QueueITTokenGenerator.GenerateToken(
                                  DateTime.UtcNow.AddHours(1),
                                  "e1",
                                  queueId,
                                  true,
                                  null,
                                  customerKey,
                                  out hash,
                                  "queue");

            var targetUrl = "http://test.test.com?b=h";
            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateQueueRequest(targetUrl, queueitToken, config, "testCustomer", customerKey);
            Assert.True(!result.DoRedirect);

            cookieProviderMock.Received().Store(
                                    "e1",
                                     queueId,
                                     null,
                                    config.CookieDomain,
                                     "queue",
                                     customerKey);
            Assert.True(result.QueueId == queueId);
            Assert.True(result.RedirectType == "queue");
            Assert.True(config.EventId == result.EventId);
        }

        [Fact]
        public void ValidateRequest_NoCookie_ValidToken_CookieValidityMinuteFromToken_DoNotRedirect_StoreNonExtendableCookie()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();

            var config = new QueueEventConfig()
            {
                EventId = "eventid",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = true
            };
            var customerKey = "secretekeyofuser";
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";

            cookieProviderMock.GetState("", 10, "").ReturnsForAnyArgs(new StateInfo(false, "", null, ""));

            var queueitToken = "e_eventid~q_f8757c2d-34c2-4639-bef2-1736cdd30bbb~ri_34678c2d-34c2-4639-bef2-1736cdd30bbb~ts_1797033600~ce_False~cv_3~rt_DirectLink~h_5ee2babc3ac9fae9d80d5e64675710c371876386e77209f771007dc3e093e326";

            var targetUrl = "http://test.test.com?b=h";

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateQueueRequest(targetUrl, queueitToken, config, "testCustomer", customerKey);
            Assert.True(!result.DoRedirect);
            Assert.True(result.QueueId == queueId);
            Assert.True(result.RedirectType == "DirectLink");
            Assert.True(config.EventId == result.EventId);



            cookieProviderMock.Received().Store(
                                     "eventid",
                                     queueId,
                                    3,
                                     config.CookieDomain,
                                     "DirectLink",
                                     customerKey);

        }

        [Fact]
        public void ValidateRequest_NoCookie_WithoutToken_RedirectToQueue()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();

            var config = new QueueEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Culture = null,
                LayoutName = "testlayout",
                Version = 10

            };

            cookieProviderMock.GetState("", 0, "").ReturnsForAnyArgs(new StateInfo(false, "", null, ""));

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);
            var targetUrl = "http://test.test.com?b=h";
            var knownUserVersion = UserInQueueService.SDK_VERSION;

            var expectedUrl = $"https://testDomain.com?c=testCustomer&e=e1" +
             $"&ver=v3-aspnetcore-{knownUserVersion}" +
             $"&cver=10" +
             $"&l={config.LayoutName}" +
             $"&t={HttpUtility.UrlEncode(targetUrl)}";
            var result = testObject.ValidateQueueRequest(targetUrl, "", config, "testCustomer", "key");

            Assert.True(result.DoRedirect);
            Assert.True(result.RedirectUrl.ToUpper() == expectedUrl.ToUpper());
            cookieProviderMock.DidNotReceiveWithAnyArgs().Store(null, null, null, null, null, null);
            Assert.True(config.EventId == result.EventId);
        }

        [Fact]
        public void ValidateRequest_NoCookie_WithoutToken_RedirectToQueue_NotargetUrl()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();

            var config = new QueueEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Culture = null,
                LayoutName = "testlayout",
                Version = 10

            };

            cookieProviderMock.GetState("", 0, "").ReturnsForAnyArgs(new StateInfo(false, "", null, null));

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var knownUserVersion = UserInQueueService.SDK_VERSION;

            var expectedUrl = $"https://testDomain.com?c=testCustomer&e=e1" +
             $"&ver=v3-aspnetcore-{knownUserVersion}" +
             $"&cver=10" +
             $"&l={config.LayoutName}";
            var result = testObject.ValidateQueueRequest(null, "", config, "testCustomer", "key");

            Assert.True(result.DoRedirect);
            Assert.True(result.RedirectUrl.ToUpper() == expectedUrl.ToUpper());
            cookieProviderMock.DidNotReceiveWithAnyArgs().Store(null, null, null, null, null, null);
            Assert.True(config.EventId == result.EventId);
        }

        [Fact]
        public void ValidateRequest_NoCookie_InValidToken()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();

            var config = new QueueEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Culture = null,
                LayoutName = "testlayout",
                Version = 10

            };
            cookieProviderMock.GetState("", 0, "").ReturnsForAnyArgs(new StateInfo(false, null, null, null));

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);
            var targetUrl = "http://test.test.com?b=h";
            var knownUserVersion = UserInQueueService.SDK_VERSION;

            var expectedUrl = $"https://testDomain.com?c=testCustomer&e=e1" +
                $"&ver=v3-aspnetcore-{knownUserVersion}" +
                $"&cver=10" +
                $"&l={config.LayoutName}" +
                $"&t={HttpUtility.UrlEncode(targetUrl)}";

            var result = testObject.ValidateQueueRequest(targetUrl, "ts_sasa~cv_adsasa~ce_falwwwse~q_944c1f44-60dd-4e37-aabc-f3e4bb1c8895", config, "testCustomer", "key");

            Assert.True(result.DoRedirect);
            Assert.StartsWith($"https://testDomain.com/error/hash/?c=testCustomer&e=e1&ver=v3-aspnetcore-{knownUserVersion}&cver=10&l=testlayout&queueittoken=ts_sasa~cv_adsasa~ce_falwwwse~q_944c1f44-60dd-4e37-aabc-f3e4bb1c8895&", result.RedirectUrl);
            cookieProviderMock.DidNotReceiveWithAnyArgs().Store(null, null, null, null, null, null);
            Assert.True(config.EventId == result.EventId);

        }

        [Fact]
        public void ValidateCancelRequest()
        {
            var cookieProviderMock = Substitute.For<IUserInQueueStateRepository>();
            string queueId = "queueId";
            var config = new CancelEventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieDomain = "testdomain",
                Version = 10
            };
            var knownUserVersion = UserInQueueService.SDK_VERSION;
            var expectedUrl = $"https://testDomain.com/cancel/testCustomer/e1/?c=testCustomer&e=e1" +
                            $"&ver=v3-aspnetcore-{knownUserVersion}"
                            + $"&cver=10&r=" + "url";
            cookieProviderMock.GetState("", 0, "", false)
                  .ReturnsForAnyArgs(new StateInfo(true, queueId, 3, "idle"));

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateCancelRequest("url", config, "testCustomer", "key");
            cookieProviderMock.Received().GetState("e1", -1, "key", false);
            cookieProviderMock.Received().CancelQueueCookie("e1", "testdomain");
            Assert.True(result.DoRedirect);
            Assert.True(result.QueueId == queueId);
            Assert.True(result.RedirectUrl.ToLower() == expectedUrl.ToLower());
            Assert.True(config.EventId == result.EventId);
        }

        [Fact]
        public void GetIgnoreRequest()
        {
            UserInQueueService testObject = new UserInQueueService(null);
            var result = testObject.GetIgnoreResult();
            Assert.True(result.ActionType == ActionType.IgnoreAction);
            Assert.False(result.DoRedirect);
            Assert.Null(result.EventId);
            Assert.Null(result.QueueId);
            Assert.Null(result.RedirectUrl);
        }
    }

    public class QueueITTokenGenerator
    {
        public static string GenerateToken(
            DateTime timeStamp,
            string eventId,
            string queueId,
            bool extendableCookie,
            int? cookieValidityMinutes,
            string secretKey,
            out string hash,
            string redirectType)
        {
            var paramList = new List<string>();
            paramList.Add(QueueParameterHelper.TimeStampKey + QueueParameterHelper.KeyValueSeparatorChar + GetUnixTimestamp(timeStamp));
            if (cookieValidityMinutes != null)
                paramList.Add(QueueParameterHelper.CookieValidityMinutesKey + QueueParameterHelper.KeyValueSeparatorChar + cookieValidityMinutes);
            paramList.Add(QueueParameterHelper.EventIdKey + QueueParameterHelper.KeyValueSeparatorChar + eventId);
            paramList.Add(QueueParameterHelper.ExtendableCookieKey + QueueParameterHelper.KeyValueSeparatorChar + extendableCookie);
            paramList.Add(QueueParameterHelper.QueueIdKey + QueueParameterHelper.KeyValueSeparatorChar + queueId);
            if (redirectType != null)
                paramList.Add(QueueParameterHelper.RedirectTypeKey + QueueParameterHelper.KeyValueSeparatorChar + redirectType);
            var tokenWithoutHash = string.Join(QueueParameterHelper.KeyValueSeparatorGroupChar.ToString(), paramList);
            hash = GetSHA256Hash(tokenWithoutHash, secretKey);

            return tokenWithoutHash + QueueParameterHelper.KeyValueSeparatorGroupChar.ToString() + QueueParameterHelper.HashKey + QueueParameterHelper.KeyValueSeparatorChar + hash;
        }

        private static string GetUnixTimestamp(DateTime dateTime)
        {
            return ((Int32)(dateTime.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalSeconds).ToString();
        }

        public static string GetSHA256Hash(string stringToHash, string key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] data = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }
    }
}
