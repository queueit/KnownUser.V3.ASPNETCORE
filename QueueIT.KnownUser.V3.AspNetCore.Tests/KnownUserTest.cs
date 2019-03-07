using QueueIT.KnownUser.V3.AspNetCore.IntegrationConfig;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Xunit;

namespace QueueIT.KnownUser.V3.AspNetCore.Tests
{
    public class KnownUserTest
    {
        public class MockHttpRequest : IHttpRequest
        {
            public MockHttpRequest()
            {
                Headers = new NameValueCollection();
            }

            public NameValueCollection CookiesValue { get; set; } = new NameValueCollection();


            public string UserHostAddress
            {
                get;
                set;
            }
            public NameValueCollection Headers
            {
                get;
                set;
            }

            public string UserAgent
            {
                get;
                set;
            }


            public Uri Url
            {
                get;
                set;
            }


            public string GetCookieValue(string cookieKey)
            {
                return this.CookiesValue[cookieKey];
            }
        }

        public class MockHttpResponse : IHttpResponse
        {
            public Dictionary<string, Dictionary<string, object>> CookiesValue { get; set; } =
                new Dictionary<string, Dictionary<string, object>>();

            public void SetCookie(string cookieName, string cookieValue, string domain, DateTime expiration)
            {
                CookiesValue.Add(cookieName,
                    new Dictionary<string, object>() {
                                        { nameof(cookieName), cookieName},
                                        { nameof(cookieValue), cookieValue},
                                        { nameof(domain), domain},
                                        { nameof(expiration), expiration}}
                    );
            }
        }

        internal class HttpContextMock : IHttpContextProvider
        {
            public IHttpRequest HttpRequest { get; set; } = new MockHttpRequest();
            public IHttpResponse HttpResponse { get; set; } = new MockHttpResponse();
        }

        class UserInQueueServiceMock : IUserInQueueService
        {
            public List<List<string>> validateQueueRequestCalls = new List<List<string>>();
            public List<List<string>> extendQueueCookieCalls = new List<List<string>>();
            public List<List<string>> cancelRequestCalls = new List<List<string>>();
            public List<List<string>> ignoreRequestCalls = new List<List<string>>();

            public RequestValidationResult ValidateQueueRequest(string targetUrl, string queueitToken, QueueEventConfig config, string customerId, string secretKey)
            {
                List<string> args = new List<string>();
                args.Add(targetUrl);
                args.Add(queueitToken);
                args.Add(config.CookieDomain + ":"
                        + config.LayoutName + ":"
                        + config.Culture + ":"
                        + config.EventId + ":"
                        + config.QueueDomain + ":"
                        + config.ExtendCookieValidity.ToString().ToLower() + ":"
                        + config.CookieValidityMinute + ":"
                        + config.Version);
                args.Add(customerId);
                args.Add(secretKey);
                validateQueueRequestCalls.Add(args);

                return new RequestValidationResult("Queue");
            }

            public void ExtendQueueCookie(string eventId, int cookieValidityMinute, string cookieDomain, string secretKey)
            {
                List<string> args = new List<string>();
                args.Add(eventId);
                args.Add(cookieValidityMinute.ToString());
                args.Add(cookieDomain);
                args.Add(secretKey);
                extendQueueCookieCalls.Add(args);
            }

            public RequestValidationResult ValidateCancelRequest(string targetUrl, CancelEventConfig config, string customerId, string secretKey)
            {
                List<string> args = new List<string>();
                args.Add(targetUrl);
                args.Add(config.CookieDomain + ":"
                        + config.EventId + ":"
                        + config.QueueDomain + ":"
                        + config.Version);
                args.Add(customerId);
                args.Add(secretKey);
                cancelRequestCalls.Add(args);

                return new RequestValidationResult("Cancel");
            }

            public RequestValidationResult GetIgnoreResult()
            {
                ignoreRequestCalls.Add(new List<string>());
                return new RequestValidationResult("Ignore");
            }
        }

        private void AssertRequestCookieContent(string[] cookieValues, params string[] expectedValues)
        {
            Assert.True(cookieValues.Count(v => v.StartsWith("ServerUtcTime=")) == 1);
            Assert.True(cookieValues.Count(v => v.StartsWith("RequestIP=")) == 1);
            Assert.True(cookieValues.Count(v => v.StartsWith("RequestHttpHeader_Via=")) == 1);
            Assert.True(cookieValues.Count(v => v.StartsWith("RequestHttpHeader_Forwarded=")) == 1);
            Assert.True(cookieValues.Count(v => v.StartsWith("RequestHttpHeader_XForwardedFor=")) == 1);
            Assert.True(cookieValues.Count(v => v.StartsWith("RequestHttpHeader_XForwardedHost=")) == 1);
            Assert.True(cookieValues.Count(v => v.StartsWith("RequestHttpHeader_XForwardedProto=")) == 1);

            var utcTimeInCookie = cookieValues.FirstOrDefault(v => v.StartsWith("ServerUtcTime")).Split('=')[1];
            Assert.True(string.Compare(expectedValues[0], utcTimeInCookie) <= 0);
            Assert.True(string.Compare(DateTime.UtcNow.ToString("o"), utcTimeInCookie) >= 0);

            Assert.True(cookieValues.Any(v => v == $"RequestIP={expectedValues[1]}"));
            Assert.True(cookieValues.Any(v => v == $"RequestHttpHeader_Via={expectedValues[2]}"));
            Assert.True(cookieValues.Any(v => v == $"RequestHttpHeader_Forwarded={expectedValues[3]}"));
            Assert.True(cookieValues.Any(v => v == $"RequestHttpHeader_XForwardedFor={expectedValues[4]}"));
            Assert.True(cookieValues.Any(v => v == $"RequestHttpHeader_XForwardedHost={expectedValues[5]}"));
            Assert.True(cookieValues.Any(v => v == $"RequestHttpHeader_XForwardedProto={expectedValues[6]}"));
        }

        [Fact]
        public void CancelRequestByLocalConfig_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            var cancelEventConfig = new CancelEventConfig() { CookieDomain = "cookiedomain", EventId = "eventid", QueueDomain = "queuedomain", Version = 1 };
            // Act
            var result = KnownUser.CancelRequestByLocalConfig("url", "queueitToken", cancelEventConfig, "customerid", "secretekey");

            // Assert
            Assert.Equal("url", mock.cancelRequestCalls[0][0]);
            Assert.Equal("cookiedomain:eventid:queuedomain:1", mock.cancelRequestCalls[0][1]);
            Assert.Equal("customerid", mock.cancelRequestCalls[0][2]);
            Assert.Equal("secretekey", mock.cancelRequestCalls[0][3]);
            Assert.False(result.IsAjaxResult);
            KnownUser._HttpContextProvider = null;
        }

        [Fact]
        public void CancelRequestByLocalConfig_AjaxCall_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock()
            {
                HttpRequest = new MockHttpRequest()
                { Headers = new NameValueCollection() { { "x-queueit-ajaxpageurl", "http%3A%2F%2Furl" } } }
            };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            var cancelEventConfig = new CancelEventConfig() { CookieDomain = "cookiedomain", EventId = "eventid", QueueDomain = "queuedomain", Version = 1 };
            // Act
            var result = KnownUser.CancelRequestByLocalConfig("url", "queueitToken", cancelEventConfig, "customerid", "secretekey");

            // Assert
            Assert.Equal("http://url", mock.cancelRequestCalls[0][0]);
            Assert.Equal("cookiedomain:eventid:queuedomain:1", mock.cancelRequestCalls[0][1]);
            Assert.Equal("customerid", mock.cancelRequestCalls[0][2]);
            Assert.Equal("secretekey", mock.cancelRequestCalls[0][3]);
            Assert.True(result.IsAjaxResult);

            KnownUser._HttpContextProvider = null;
        }

        [Fact]
        public void CancelRequestByLocalConfig_NullQueueDomain_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            CancelEventConfig eventConfig = new CancelEventConfig();
            eventConfig.EventId = "eventid";
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "QueueDomain from cancelEventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_EventIdNull_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            CancelEventConfig eventConfig = new CancelEventConfig();
            eventConfig.CookieDomain = "domain";
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "EventId from cancelEventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_CancelEventConfigNull_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", null, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "cancelEventConfig can not be null.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_CustomerIdNull_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", new CancelEventConfig(), null, "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "customerId can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_SeceretKeyNull_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", new CancelEventConfig(), "customerid", null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "secretKey can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_TargetUrl_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig(null, "queueitToken", new CancelEventConfig(), "customerid", "secretkey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "targeturl can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ExtendQueueCookie_NullEventId_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ExtendQueueCookie(null, 0, null, null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "eventId can not be null or empty.";
            }

            // Assert
            Assert.True(mock.extendQueueCookieCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ExtendQueueCookie_InvalidCookieValidityMinutes_Test()
        {
            // Arrange        
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ExtendQueueCookie("eventId", 0, "cookiedomain", null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "cookieValidityMinute should be greater than 0.";
            }

            // Assert
            Assert.True(mock.extendQueueCookieCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ExtendQueueCookie_NullSecretKey_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ExtendQueueCookie("eventId", 20, "cookiedomain", null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "secretKey can not be null or empty.";
            }

            // Assert
            Assert.True(mock.extendQueueCookieCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ExtendQueueCookie_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            // Act
            KnownUser.ExtendQueueCookie("eventId", 20, "cookiedomain", "secretKey");

            // Assert
            Assert.Equal("eventId", mock.extendQueueCookieCalls[0][0]);
            Assert.Equal("20", mock.extendQueueCookieCalls[0][1]);
            Assert.Equal("cookiedomain", mock.extendQueueCookieCalls[0][2]);
            Assert.Equal("secretKey", mock.extendQueueCookieCalls[0][3]);
        }

        [Fact]
        public void ResolveQueueRequestByLocalConfig_NullCustomerId_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", null, null, "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "customerId can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveQueueRequestByLocalConfig_NullSecretKey_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", null, "customerId", null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "secretKey can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveQueueRequestByLocalConfig_NullEventConfig_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", null, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "eventConfig can not be null.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveRequestByLocalEventConfigNullEventIdTest()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            //eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "EventId from eventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveRequestByLocalEventConfig_NullQueueDomain_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            //eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "QueueDomain from eventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveQueueRequestByLocalConfig_InvalidCookieValidityMinute_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            //eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "CookieValidityMinute from eventConfig should be greater than 0.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveRequestByLocalEventConfig_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            var result = KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");

            // Assert
            Assert.Equal("targetUrl", mock.validateQueueRequestCalls[0][0]);
            Assert.Equal("queueitToken", mock.validateQueueRequestCalls[0][1]);
            Assert.Equal("cookieDomain:layoutName:culture:eventId:queueDomain:true:10:12", mock.validateQueueRequestCalls[0][2]);
            Assert.Equal("customerId", mock.validateQueueRequestCalls[0][3]);
            Assert.Equal("secretKey", mock.validateQueueRequestCalls[0][4]);
            Assert.False(result.IsAjaxResult);
        }
        [Fact]
        public void ResolveRequestByLocalEventConfig_AjaxCall_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock()
            {
                HttpRequest = new MockHttpRequest()
                { Headers = new NameValueCollection() { { "x-queueit-ajaxpageurl", "http%3A%2F%2Furl" } } }
            };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            var result = KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");

            // Assert
            Assert.Equal("http://url", mock.validateQueueRequestCalls[0][0]);
            Assert.Equal("queueitToken", mock.validateQueueRequestCalls[0][1]);
            Assert.Equal("cookieDomain:layoutName:culture:eventId:queueDomain:true:10:12", mock.validateQueueRequestCalls[0][2]);
            Assert.Equal("customerId", mock.validateQueueRequestCalls[0][3]);
            Assert.Equal("secretKey", mock.validateQueueRequestCalls[0][4]);
            Assert.True(result.IsAjaxResult);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_EmptyCurrentUrl_Test()
        {
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            // Arrange        
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ValidateRequestByIntegrationConfig("", null, null, null, null);
            }
            catch (Exception ex)
            {
                exceptionWasThrown = ex.Message == "currentUrlWithoutQueueITToken can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_EmptyIntegrationsConfig_Test()
        {
            // Arrange 
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ValidateRequestByIntegrationConfig("currentUrl", "queueitToken", null, null, null);
            }
            catch (Exception ex)
            {
                exceptionWasThrown = ex.Message == "customerIntegrationInfo can not be null.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_QueueAction()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            TriggerPart triggerPart1 = new TriggerPart();
            triggerPart1.Operator = "Contains";
            triggerPart1.ValueToCompare = "event1";
            triggerPart1.UrlPart = "PageUrl";
            triggerPart1.ValidatorType = "UrlValidator";
            triggerPart1.IsNegative = false;
            triggerPart1.IsIgnoreCase = true;

            TriggerPart triggerPart2 = new TriggerPart();
            triggerPart2.Operator = "Contains";
            triggerPart2.ValueToCompare = "googlebot";
            triggerPart2.ValidatorType = "UserAgentValidator";
            triggerPart2.IsNegative = false;
            triggerPart2.IsIgnoreCase = false;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart1, triggerPart2 };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            //config.ActionType = "Queue";
            config.EventId = "event1";
            config.CookieDomain = ".test.com";
            config.LayoutName = "Christmas Layout by Queue-it";
            config.Culture = "da-DK";
            config.ExtendCookieValidity = true;
            config.CookieValidityMinute = 20;
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "knownusertest.queue-it.net";
            config.RedirectLogic = "AllowTParameter";
            config.ForcedTargetUrl = "";
            config.ActionType = ActionType.QueueAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;
            var httpContextMock = new HttpContextMock()
            {
                HttpRequest =
                new MockHttpRequest()
                {
                    UserAgent = "googlebot",
                    Headers = new NameValueCollection()
                }
            };
            KnownUser._HttpContextProvider = httpContextMock;
            // Act
            var result = KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 1);
            Assert.Equal("http://test.com?event1=true", mock.validateQueueRequestCalls[0][0]);
            Assert.Equal("queueitToken", mock.validateQueueRequestCalls[0][1]);
            Assert.Equal(".test.com:Christmas Layout by Queue-it:da-DK:event1:knownusertest.queue-it.net:true:20:3", mock.validateQueueRequestCalls[0][2]);
            Assert.Equal("customerId", mock.validateQueueRequestCalls[0][3]);
            Assert.Equal("secretKey", mock.validateQueueRequestCalls[0][4]);
            Assert.False(result.IsAjaxResult);
            KnownUser._HttpContextProvider = null;
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_AjaxCall_QueueAction()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            TriggerPart triggerPart1 = new TriggerPart();
            triggerPart1.Operator = "Contains";
            triggerPart1.ValueToCompare = "event1";
            triggerPart1.UrlPart = "PageUrl";
            triggerPart1.ValidatorType = "UrlValidator";
            triggerPart1.IsNegative = false;
            triggerPart1.IsIgnoreCase = true;

            TriggerPart triggerPart2 = new TriggerPart();
            triggerPart2.Operator = "Contains";
            triggerPart2.ValueToCompare = "googlebot";
            triggerPart2.ValidatorType = "UserAgentValidator";
            triggerPart2.IsNegative = false;
            triggerPart2.IsIgnoreCase = false;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart1, triggerPart2 };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            //config.ActionType = "Queue";
            config.EventId = "event1";
            config.CookieDomain = ".test.com";
            config.LayoutName = "Christmas Layout by Queue-it";
            config.Culture = "da-DK";
            config.ExtendCookieValidity = true;
            config.CookieValidityMinute = 20;
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "knownusertest.queue-it.net";
            config.RedirectLogic = "AllowTParameter";
            config.ForcedTargetUrl = "";
            config.ActionType = ActionType.QueueAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;

            var httpContextMock = new HttpContextMock()
            {
                HttpRequest =
                new MockHttpRequest()
                {
                    UserAgent = "googlebot",

                    Headers = new NameValueCollection() { { "x-queueit-ajaxpageurl", "http%3A%2F%2Furl" } }
                }
            };
            KnownUser._HttpContextProvider = httpContextMock;
            // Act
            var result = KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 1);
            Assert.Equal("http://url", mock.validateQueueRequestCalls[0][0]);
            Assert.Equal("queueitToken", mock.validateQueueRequestCalls[0][1]);
            Assert.Equal(".test.com:Christmas Layout by Queue-it:da-DK:event1:knownusertest.queue-it.net:true:20:3", mock.validateQueueRequestCalls[0][2]);
            Assert.Equal("customerId", mock.validateQueueRequestCalls[0][3]);
            Assert.Equal("secretKey", mock.validateQueueRequestCalls[0][4]);
            Assert.True(result.IsAjaxResult);

            KnownUser._HttpContextProvider = null;
        }



        [Fact]
        public void ValidateRequestByIntegrationConfig_NotMatch_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[0];
            customerIntegration.Version = 3;

            // Act
            RequestValidationResult result = KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.False(result.DoRedirect);
        }

        [Theory]
        [InlineData("ForcedTargetUrl", "http://forcedtargeturl.com")]
        [InlineData("ForecedTargetUrl", "http://forcedtargeturl.com")]
        [InlineData("EventTargetUrl", "")]
        public void ValidateRequestByIntegrationConfig_RedirectLogic_Test(string redirectLogic, string forcedTargetUrl)
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            TriggerPart triggerPart = new TriggerPart();
            triggerPart.Operator = "Contains";
            triggerPart.ValueToCompare = "event1";
            triggerPart.UrlPart = "PageUrl";
            triggerPart.ValidatorType = "UrlValidator";
            triggerPart.IsNegative = false;
            triggerPart.IsIgnoreCase = true;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            //config.ActionType = "Queue";
            config.EventId = "event1";
            config.CookieDomain = ".test.com";
            config.LayoutName = "Christmas Layout by Queue-it";
            config.Culture = "da-DK";
            config.ExtendCookieValidity = true;
            config.CookieValidityMinute = 20;
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "knownusertest.queue-it.net";
            config.RedirectLogic = redirectLogic;
            config.ForcedTargetUrl = forcedTargetUrl;
            config.ActionType = ActionType.QueueAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;

            // Act
            KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 1);
            Assert.Equal(forcedTargetUrl, mock.validateQueueRequestCalls[0][0]);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_IgnoreAction()
        {
            // Arrange
            TriggerPart triggerPart = new TriggerPart();
            triggerPart.Operator = "Contains";
            triggerPart.ValueToCompare = "event1";
            triggerPart.UrlPart = "PageUrl";
            triggerPart.ValidatorType = "UrlValidator";
            triggerPart.IsNegative = false;
            triggerPart.IsIgnoreCase = true;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            config.EventId = "eventid";
            config.CookieDomain = "cookiedomain";
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "queuedomain";
            config.ActionType = ActionType.IgnoreAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;

            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            // Act
            var result = KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerid", "secretkey");

            // Assert
            Assert.True(mock.ignoreRequestCalls.Count() == 1);
            Assert.False(result.IsAjaxResult);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_AjaxCall_IgnoreAction()
        {
            // Arrange
            TriggerPart triggerPart = new TriggerPart();
            triggerPart.Operator = "Contains";
            triggerPart.ValueToCompare = "event1";
            triggerPart.UrlPart = "PageUrl";
            triggerPart.ValidatorType = "UrlValidator";
            triggerPart.IsNegative = false;
            triggerPart.IsIgnoreCase = true;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            config.EventId = "eventid";
            config.CookieDomain = "cookiedomain";
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "queuedomain";
            config.ActionType = ActionType.IgnoreAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;

            var httpContextMock = new HttpContextMock()
            {
                HttpRequest = new MockHttpRequest()
                {
                    Headers = new NameValueCollection() { { "x-queueit-ajaxpageurl", "url" } }
                }
            };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            // Act
            var result = KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerid", "secretkey");

            // Assert
            Assert.True(mock.ignoreRequestCalls.Count() == 1);
            Assert.True(result.IsAjaxResult);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_CancelAction()
        {
            // Arrange

            TriggerPart triggerPart = new TriggerPart();
            triggerPart.Operator = "Contains";
            triggerPart.ValueToCompare = "event1";
            triggerPart.UrlPart = "PageUrl";
            triggerPart.ValidatorType = "UrlValidator";
            triggerPart.IsNegative = false;
            triggerPart.IsIgnoreCase = true;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            config.EventId = "eventid";
            config.CookieDomain = "cookiedomain";
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "queuedomain";
            config.ActionType = ActionType.CancelAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;

            var httpContextMock = new HttpContextMock() { };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            // Act
            var result = KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerid", "secretkey");

            // Assert
            Assert.Equal("http://test.com?event1=true", mock.cancelRequestCalls[0][0]);
            Assert.Equal("cookiedomain:eventid:queuedomain:3", mock.cancelRequestCalls[0][1]);
            Assert.Equal("customerid", mock.cancelRequestCalls[0][2]);
            Assert.Equal("secretkey", mock.cancelRequestCalls[0][3]);
            Assert.False(result.IsAjaxResult);
        }
        [Fact]
        public void ValidateRequestByIntegrationConfig_AjaxCall_CancelAction()
        {
            // Arrange

            TriggerPart triggerPart = new TriggerPart();
            triggerPart.Operator = "Contains";
            triggerPart.ValueToCompare = "event1";
            triggerPart.UrlPart = "PageUrl";
            triggerPart.ValidatorType = "UrlValidator";
            triggerPart.IsNegative = false;
            triggerPart.IsIgnoreCase = true;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            config.EventId = "eventid";
            config.CookieDomain = "cookiedomain";
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "queuedomain";
            config.ActionType = ActionType.CancelAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;

            var httpContextMock = new HttpContextMock()
            {
                HttpRequest = new MockHttpRequest()
                {
                    Headers = new NameValueCollection() { { "x-queueit-ajaxpageurl", "http%3A%2F%2Furl" } }
                }
            };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            // Act
            var result = KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerid", "secretkey");

            // Assert
            Assert.Equal(mock.cancelRequestCalls[0][0], "http://url");
            Assert.Equal("cookiedomain:eventid:queuedomain:3", mock.cancelRequestCalls[0][1]);
            Assert.Equal("customerid", mock.cancelRequestCalls[0][2]);
            Assert.Equal("secretkey", mock.cancelRequestCalls[0][3]);
            Assert.True(result.IsAjaxResult);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_Debug()
        {
            // Arrange 
            string requestIP = "80.35.35.34";
            string viaHeader = "1.1 example.com";
            string forwardedHeader = "for=192.0.2.60;proto=http;by=203.0.113.43";
            string xForwardedForHeader = "129.78.138.66, 129.78.64.103";
            string xForwardedHostHeader = "en.wikipedia.org:8080";
            string xForwardedProtoHeader = "https";
            var mockResponse = new MockHttpResponse()
            {
            };
            var httpContextMock = new HttpContextMock()
            {
                HttpRequest = new MockHttpRequest()
                {
                    Headers = new NameValueCollection() {
                        { "Via", viaHeader },
                        { "Forwarded", forwardedHeader },
                        { "X-Forwarded-For", xForwardedForHeader },
                        { "X-Forwarded-Host", xForwardedHostHeader },
                        { "X-Forwarded-Proto", xForwardedProtoHeader }
                    },
                    UserHostAddress = requestIP
                    //QueryStringValue = new NameValueCollection()
                    //{
                    //    { "queueittoken", "queueittoken_value" },
                    //    { "queueitdebug", "queueitdebug_value" }
                    //}
                    ,
                    Url = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue"),

                },
                HttpResponse = mockResponse
            };

            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            TriggerPart triggerPart1 = new TriggerPart();
            triggerPart1.Operator = "Contains";
            triggerPart1.ValueToCompare = "event1";
            triggerPart1.UrlPart = "PageUrl";
            triggerPart1.ValidatorType = "UrlValidator";
            triggerPart1.IsNegative = false;
            triggerPart1.IsIgnoreCase = true;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart1 };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            //config.ActionType = "Queue";
            config.EventId = "event1";
            config.CookieDomain = ".test.com";
            config.LayoutName = "Christmas Layout by Queue-it";
            config.Culture = "da-DK";
            config.ExtendCookieValidity = true;
            config.CookieValidityMinute = 20;
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "knownusertest.queue-it.net";
            config.RedirectLogic = "AllowTParameter";
            config.ForcedTargetUrl = "";


            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;
            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
                Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");

            var utcTimeBeforeActionWasPerformed = DateTime.UtcNow.ToString("o");

            // Act
            RequestValidationResult result = KnownUser.ValidateRequestByIntegrationConfig($"http://test.com?event1=true", queueitToken, customerIntegration, "customerId", "secretKey");

            // Assert
            var cookieValues = HttpUtility.UrlDecode(mockResponse.CookiesValue["queueitdebug"]["cookieValue"].ToString()).Split('|');
            Assert.True(cookieValues.Any(v => v == $"PureUrl=http://test.com?event1=true"));
            Assert.True(cookieValues.Any(v => v == $"ConfigVersion=3"));
            Assert.True(cookieValues.Any(v => v == $"MatchedConfig=event1action"));
            Assert.True(cookieValues.Any(v => v == $"QueueitToken={queueitToken}"));
            Assert.True(cookieValues.Any(v => v == $"OriginalUrl=http://test.com/?event1=true&queueittoken=queueittokenvalue"));
            Assert.True(cookieValues.Any(v => v == $"TargetUrl=http://test.com?event1=true"));
            Assert.True(cookieValues.Any(v => v == $"QueueConfig=EventId:event1&Version:3&QueueDomain:knownusertest.queue-it.net&CookieDomain:.test.com&ExtendCookieValidity:True&CookieValidityMinute:20&LayoutName:Christmas Layout by Queue-it&Culture:da-DK"));

            AssertRequestCookieContent(cookieValues,
                utcTimeBeforeActionWasPerformed, requestIP, viaHeader, forwardedHeader, xForwardedForHeader, xForwardedHostHeader, xForwardedProtoHeader);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_Debug_WithoutMatch()
        {
            // Arrange 
            string requestIP = "80.35.35.34";
            string viaHeader = "1.1 example.com";
            string forwardedHeader = "for=192.0.2.60;proto=http;by=203.0.113.43";
            string xForwardedForHeader = "129.78.138.66, 129.78.64.103";
            string xForwardedHostHeader = "en.wikipedia.org:8080";
            string xForwardedProtoHeader = "https";
            var fakeHttpResponse = new MockHttpResponse();
            var httpContextMock = new HttpContextMock()
            {
                HttpRequest = new MockHttpRequest()
                {
                    Headers = new NameValueCollection() {
                        { "Via", viaHeader },
                        { "Forwarded", forwardedHeader },
                        { "X-Forwarded-For", xForwardedForHeader },
                        { "X-Forwarded-Host", xForwardedHostHeader },
                        { "X-Forwarded-Proto", xForwardedProtoHeader }
                    },
                    UserHostAddress = requestIP,

                    Url = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue")
                }
                ,
                HttpResponse = fakeHttpResponse
            };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { };
            customerIntegration.Version = 10;

            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
                        Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");

            var utcTimeBeforeActionWasPerformed = DateTime.UtcNow.ToString("o");

            // Act
            RequestValidationResult result = KnownUser
                .ValidateRequestByIntegrationConfig("http://test.com?event1=true",
                queueitToken, customerIntegration, "customerId", "secretKey");

            // Assert
            var cookieValues = HttpUtility.UrlDecode(fakeHttpResponse.CookiesValue["queueitdebug"]["cookieValue"].ToString()).Split('|');
            Assert.True(cookieValues.Any(v => v == $"PureUrl=http://test.com?event1=true"));
            Assert.True(cookieValues.Any(v => v == $"QueueitToken={queueitToken}"));
            Assert.True(cookieValues.Any(v => v == $"ConfigVersion=10"));
            Assert.True(cookieValues.Any(v => v == $"OriginalUrl=http://test.com/?event1=true&queueittoken=queueittokenvalue"));
            Assert.True(cookieValues.Any(v => v == $"MatchedConfig=NULL"));

            AssertRequestCookieContent(cookieValues,
                utcTimeBeforeActionWasPerformed, requestIP, viaHeader, forwardedHeader, xForwardedForHeader, xForwardedHostHeader, xForwardedProtoHeader);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_Debug_WithoutMatch_NotValidHash()
        {
            // Arrange 
            var fakeHttpResponse = new MockHttpResponse();
            var httpContextMock = new HttpContextMock()
            {
                HttpRequest = new MockHttpRequest()
                {

                    Url = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue")
                }
                ,
                HttpResponse = fakeHttpResponse
            };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);


            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { };
            customerIntegration.Version = 10;

            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
                        Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");
            // Act
            RequestValidationResult result = KnownUser
                .ValidateRequestByIntegrationConfig("http://test.com?event1=true",
                queueitToken + "test", customerIntegration, "customerId", "secretKey");

            Assert.True(fakeHttpResponse.CookiesValue.Count == 0);
        }

        [Fact]
        public void ResolveQueueRequestByLocalConfig_Debug()
        {
            // Arrange 
            var fakeHttpResponse = new MockHttpResponse();
            string requestIP = "80.35.35.34";
            string viaHeader = "1.1 example.com";
            string forwardedHeader = "for=192.0.2.60;proto=http;by=203.0.113.43";
            string xForwardedForHeader = "129.78.138.66, 129.78.64.103";
            string xForwardedHostHeader = "en.wikipedia.org:8080";
            string xForwardedProtoHeader = "https";

            var httpContextMock = new HttpContextMock()
            {
                HttpRequest = new MockHttpRequest()
                {
                    Headers = new NameValueCollection() {
                        { "Via", viaHeader },
                        { "Forwarded", forwardedHeader },
                        { "X-Forwarded-For", xForwardedForHeader },
                        { "X-Forwarded-Host", xForwardedHostHeader },
                        { "X-Forwarded-Proto", xForwardedProtoHeader }
                    },
                    UserHostAddress = requestIP,
                    Url = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue")
                }
                ,
                HttpResponse = fakeHttpResponse
            };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);


            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;
            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
            Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");

            var utcTimeBeforeActionWasPerformed = DateTime.UtcNow.ToString("o");

            // Act
            RequestValidationResult result = KnownUser.ResolveQueueRequestByLocalConfig("http://test.com?event1=true", queueitToken, eventConfig, "customerId", "secretKey");

            // Assert
            var cookieValues = HttpUtility.UrlDecode(fakeHttpResponse.CookiesValue["queueitdebug"]["cookieValue"].ToString()).Split('|');
            Assert.True(cookieValues.Any(v => v == $"QueueitToken={queueitToken}"));
            Assert.True(cookieValues.Any(v => v == $"OriginalUrl=http://test.com/?event1=true&queueittoken=queueittokenvalue"));
            Assert.True(cookieValues.Any(v => v == $"TargetUrl=http://test.com?event1=true"));
            Assert.True(cookieValues.Any(v => v == $"QueueConfig=EventId:eventId&Version:12&QueueDomain:queueDomain&CookieDomain:cookieDomain&ExtendCookieValidity:True&CookieValidityMinute:10&LayoutName:layoutName&Culture:culture"));

            AssertRequestCookieContent(cookieValues,
                utcTimeBeforeActionWasPerformed, requestIP, viaHeader, forwardedHeader, xForwardedForHeader, xForwardedHostHeader, xForwardedProtoHeader);
        }

        [Fact]
        public void CancelRequestByLocalConfig_Debug()
        {
            // Arrange 
            var fakeHttpResponse = new MockHttpResponse();
            string requestIP = "80.35.35.34";
            string viaHeader = "1.1 example.com";
            string forwardedHeader = "for=192.0.2.60;proto=http;by=203.0.113.43";
            string xForwardedForHeader = "129.78.138.66, 129.78.64.103";
            string xForwardedHostHeader = "en.wikipedia.org:8080";
            string xForwardedProtoHeader = "https";

            var httpContextMock = new HttpContextMock()
            {
                HttpRequest = new MockHttpRequest()
                {
                    Headers = new NameValueCollection() {
                        { "Via", viaHeader },
                        { "Forwarded", forwardedHeader },
                        { "X-Forwarded-For", xForwardedForHeader },
                        { "X-Forwarded-Host", xForwardedHostHeader },
                        { "X-Forwarded-Proto", xForwardedProtoHeader }
                    },
                    UserHostAddress = requestIP,
                    Url = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue")
                }
                ,
                HttpResponse = fakeHttpResponse
            };
            KnownUser._HttpContextProvider = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);


            CancelEventConfig eventConfig = new CancelEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.Version = 12;

            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
            Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");

            var utcTimeBeforeActionWasPerformed = DateTime.UtcNow.ToString("o");

            // Act
            RequestValidationResult result = KnownUser.CancelRequestByLocalConfig("http://test.com?event1=true", queueitToken, eventConfig, "customerId", "secretKey");

            // Assert
            var cookieValues = HttpUtility.UrlDecode(fakeHttpResponse.CookiesValue["queueitdebug"]["cookieValue"].ToString()).Split('|');
            Assert.True(cookieValues.Any(v => v == $"QueueitToken={queueitToken}"));
            Assert.True(cookieValues.Any(v => v == $"OriginalUrl=http://test.com/?event1=true&queueittoken=queueittokenvalue"));
            Assert.True(cookieValues.Any(v => v == $"TargetUrl=http://test.com?event1=true"));
            Assert.True(cookieValues.Any(v => v == $"CancelConfig=EventId:eventId&Version:12&QueueDomain:queueDomain&CookieDomain:cookieDomain"));

            AssertRequestCookieContent(cookieValues,
                utcTimeBeforeActionWasPerformed, requestIP, viaHeader, forwardedHeader, xForwardedForHeader, xForwardedHostHeader, xForwardedProtoHeader);
        }
    }
    public class RequestValidationResultTest
    {
        [Fact]
        public void AjaxRedirectUrl_Test()
        {
            var testObject = new RequestValidationResult("Queue") { IsAjaxResult = true, RedirectUrl = "http://url" };

            Assert.True(testObject.AjaxRedirectUrl.ToLower() == "http%3A%2F%2Furl".ToLower());

        }
    }
}
