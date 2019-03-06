using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Xunit;

namespace QueueIT.KnownUserV3.SDK.Tests.IntegrationConfig
{
    public class ComparisonOperatorHelperTest
    {
        [Fact]
        public void Evaluate_Equals()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, false, false, "test1", "test1", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, false, false, "test1", "Test1", null));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, false, true, "test1", "Test1", null));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, true, false, "test1", "Test1", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, true, false, "test1", "test1", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, true, true, "test1", "Test1", null));
        }

        [Fact]
        public void Evaluate_Contains()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, false, false, "test_test1_test", "test1", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, false, false, "test_test1_test", "Test1", null));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, false, true, "test_test1_test", "Test1", null));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, true, false, "test_test1_test", "Test1", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, true, true, "test_test1", "Test1", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, true, false, "test_test1", "test1", null));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, false, false, "test_dsdsdsdtest1", "*", null));
        }

        [Fact]
        public void Evaluate_StartsWith()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.StartsWith, false, false, "test1_test1_test", "test1", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.StartsWith, false, false, "test1_test1_test", "Test1", null));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.StartsWith, false, true, "test1_test1_test", "Test1", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.StartsWith, true, true, "test1_test1_test", "Test1", null));
        }

        [Fact]
        public void Evaluate_EndsWith()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EndsWith, false, false, "test1_test1_testshop", "shop", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EndsWith, false, false, "test1_test1_testshop2", "shop", null));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EndsWith, false, true, "test1_test1_testshop", "Shop", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EndsWith, true, true, "test1_test1_testshop", "Shop", null));
        }

        [Fact]
        public void Evaluate_MatchesWith()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.MatchesWith, false, false, "test1_test1_testshop", ".*shop.*", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.MatchesWith, false, false, "test1_test1_testshop2", ".*Shop.*", null));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.MatchesWith, false, true, "test1_test1_testshop", ".*Shop.*", null));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.MatchesWith, true, true, "test1_test1_testshop", ".*Shop.*", null));
        }

        [Fact]
        public void Evaluate_EqualsAny()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualsAny, false, false, "test1", null, new string[] { "test1" }));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualsAny, false, false, "test1", null, new string[] { "Test1" }));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualsAny, false, true, "test1", null, new string[] { "Test1" }));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualsAny, true, false, "test1", null, new string[] { "Test1" }));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualsAny, true, false, "test1", null, new string[] { "test1" }));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualsAny, true, true, "test1", null, new string[] { "Test1" }));
        }

        [Fact]
        public void Evaluate_ContainsAny()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.ContainsAny, false, false, "test_test1_test", null, new string[] { "test1" }));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.ContainsAny, false, false, "test_test1_test", null, new string[] { "Test1" }));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.ContainsAny, false, true, "test_test1_test", null, new string[] { "Test1" }));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.ContainsAny, true, false, "test_test1_test", null, new string[] { "Test1" }));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.ContainsAny, true, true, "test_test1", null, new string[] { "Test1" }));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.ContainsAny, true, false, "test_test1", null, new string[] { "test1" }));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.ContainsAny, false, false, "test_dsdsdsdtest1", null, new string[] { "*" }));
        }
    }

    public class CookieValidatorHelperTest
    {
        [Fact]
        public void Evaluate_Test()
        {
            var triggerPart = new TriggerPart()
            {
                CookieName = "c1",
                Operator = ComparisonOperatorType.Contains,
                ValueToCompare = "1"
            };
            var request = new KnownUserTest.MockHttpRequest();
            Assert.False(CookieValidatorHelper.Evaluate(triggerPart, request));

            request.CookiesValue.Add("c5", "5");
            request.CookiesValue.Add("c1", "1");
            request.CookiesValue.Add("c2", "test");
            Assert.True(CookieValidatorHelper.Evaluate(triggerPart, request));

            triggerPart.ValueToCompare = "5";
            Assert.False(CookieValidatorHelper.Evaluate(triggerPart, request));


            triggerPart.ValueToCompare = "Test";
            triggerPart.IsIgnoreCase = true;
            triggerPart.CookieName = "c2";
            Assert.True(CookieValidatorHelper.Evaluate(triggerPart, request));

            triggerPart.ValueToCompare = "Test";
            triggerPart.IsIgnoreCase = true;
            triggerPart.IsNegative = true;
            triggerPart.CookieName = "c2";
            Assert.False(CookieValidatorHelper.Evaluate(triggerPart, request));
        }
    }

    public class UrlValidatorHelperTest
    {
        [Fact]
        public void Evaluate_Test()
        {
            var triggerPart = new TriggerPart()
            {
                UrlPart = UrlPartType.PageUrl,
                Operator = ComparisonOperatorType.Contains,
                ValueToCompare = "http://test.tesdomain.com:8080/test?q=1"
            };
            Assert.False(UrlValidatorHelper.Evaluate(triggerPart, "http://test.tesdomain.com:8080/test?q=2"));

            triggerPart.ValueToCompare = "/Test/t1";
            triggerPart.UrlPart = UrlPartType.PagePath;
            triggerPart.Operator = ComparisonOperatorType.EqualS;
            triggerPart.IsIgnoreCase = true;
            Assert.True(UrlValidatorHelper.Evaluate(triggerPart, "http://test.tesdomain.com:8080/test/t1?q=2&y02"));


            triggerPart.UrlPart = UrlPartType.HostName;
            triggerPart.ValueToCompare = "test.tesdomain.com";
            triggerPart.Operator = ComparisonOperatorType.Contains;
            Assert.True(UrlValidatorHelper.Evaluate(triggerPart, "http://m.test.tesdomain.com:8080/test?q=2"));


            triggerPart.UrlPart = UrlPartType.HostName;
            triggerPart.ValueToCompare = "test.tesdomain.com";
            triggerPart.IsNegative = true;
            triggerPart.Operator = ComparisonOperatorType.Contains;
            Assert.False(UrlValidatorHelper.Evaluate(triggerPart, "http://m.test.tesdomain.com:8080/test?q=2"));

        }
    }

    public class UserAgentValidatorHelperTest
    {
        [Fact]
        public void Evaluate_Test()
        {
            var triggerPart = new TriggerPart()
            {
                Operator = ComparisonOperatorType.Contains,
                ValueToCompare = "googlebot"
            };
            Assert.False(UserAgentValidatorHelper.Evaluate(triggerPart, "Googlebot sample useraagent"));

            triggerPart.ValueToCompare = "googlebot";
            triggerPart.Operator = ComparisonOperatorType.EqualS;
            triggerPart.IsIgnoreCase = true;
            triggerPart.IsNegative = true;
            Assert.True(UserAgentValidatorHelper.Evaluate(triggerPart, "oglebot sample useraagent"));

            triggerPart.ValueToCompare = "googlebot";
            triggerPart.Operator = ComparisonOperatorType.Contains;
            triggerPart.IsIgnoreCase = false;
            triggerPart.IsNegative = true;
            Assert.False(UserAgentValidatorHelper.Evaluate(triggerPart, "googlebot"));

            triggerPart.ValueToCompare = "googlebot";
            triggerPart.IsIgnoreCase = true;
            triggerPart.IsNegative = false;
            triggerPart.Operator = ComparisonOperatorType.Contains;
            Assert.True(UserAgentValidatorHelper.Evaluate(triggerPart, "Googlebot"));

            triggerPart.ValueToCompare = null;
            triggerPart.ValuesToCompare = new string[] { "googlebot" };
            triggerPart.IsIgnoreCase = true;
            triggerPart.IsNegative = false;
            triggerPart.Operator = ComparisonOperatorType.ContainsAny;
            Assert.True(UserAgentValidatorHelper.Evaluate(triggerPart, "Googlebot"));

            triggerPart.ValuesToCompare = new string[] { "googlebot" };
            triggerPart.IsIgnoreCase = true;
            triggerPart.IsNegative = true;
            triggerPart.Operator = ComparisonOperatorType.EqualsAny;
            Assert.True(UserAgentValidatorHelper.Evaluate(triggerPart, "oglebot sample useraagent"));
        }
    }

    public class HttpHeaderValidatorTest
    {
        [Fact]
        public void Evaluate_Test()
        {
            var triggerPart = new TriggerPart()
            {
                HttpHeaderName = "c1",
                Operator = ComparisonOperatorType.Contains,
                ValueToCompare = "1"
            };
            var httpHeaders = new NameValueCollection() { };
            Assert.False(HttpHeaderValidatorHelper.Evaluate(triggerPart, httpHeaders));

            httpHeaders.Add("c5", "5");
            httpHeaders.Add("c1", "1");
            httpHeaders.Add("c2", "test");
            Assert.True(HttpHeaderValidatorHelper.Evaluate(triggerPart, httpHeaders));

            triggerPart.ValueToCompare = "5";
            Assert.False(HttpHeaderValidatorHelper.Evaluate(triggerPart, httpHeaders));

            triggerPart.ValueToCompare = "Test";
            triggerPart.IsIgnoreCase = true;
            triggerPart.HttpHeaderName = "c2";
            Assert.True(HttpHeaderValidatorHelper.Evaluate(triggerPart, httpHeaders));

            triggerPart.ValueToCompare = "Test";
            triggerPart.IsIgnoreCase = true;
            triggerPart.IsNegative = true;
            triggerPart.HttpHeaderName = "c2";
            Assert.False(HttpHeaderValidatorHelper.Evaluate(triggerPart, httpHeaders));
        }
    }

    public class IntegrationEvaluatorTest
    {
        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_And_NotMatched()
        {
            var testObject = new IntegrationEvaluator();

            var customerIntegration = new CustomerIntegration()
            {
                Integrations = new List<IntegrationConfigModel> {
                     new IntegrationConfigModel()
                     {

                         Triggers = new List<TriggerModel>() {
                                                new TriggerModel() {
                                                    LogicalOperator = LogicalOperatorType.Or,
                                                    TriggerParts = new List<TriggerPart>() {
                                                            new TriggerPart() {
                                                                CookieName ="c1",
                                                                Operator = ComparisonOperatorType.EqualS,
                                                                ValueToCompare ="value1",
                                                                ValidatorType= ValidatorType.CookieValidator
                                                            },
                                                            new TriggerPart() {

                                                                ValidatorType= ValidatorType.UserAgentValidator,
                                                                ValueToCompare= "test",
                                                                Operator= ComparisonOperatorType.Contains
                                                                }
                                                        }
                                                    }
                    }
                }
              }
            };

            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");



            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri,
                new KnownUserTest.MockHttpRequest()) == null);
        }

        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_And_Matched()
        {
            var testObject = new IntegrationEvaluator();

            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                            new TriggerModel() {
                                                                LogicalOperator = LogicalOperatorType.And,
                                                                TriggerParts = new List<TriggerPart>() {
                                                                    new TriggerPart() {
                                                                        CookieName ="c1",
                                                                        Operator = ComparisonOperatorType.EqualS,
                                                                        IsIgnoreCase= true,
                                                                        ValueToCompare ="value1",
                                                                        ValidatorType= ValidatorType.CookieValidator
                                                                    },
                                                                    new TriggerPart() {
                                                                        UrlPart = UrlPartType.PageUrl,
                                                                        ValidatorType= ValidatorType.UrlValidator,
                                                                        ValueToCompare= "test",
                                                                        Operator= ComparisonOperatorType.Contains
                                                                        }

                                                                }
                                                    }
                                              }
                                            }

            }
            };


            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");


            var httpRequestMock = new KnownUserTest.MockHttpRequest() { CookiesValue = new NameValueCollection() { { "c1", "Value1" } } };
            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, httpRequestMock).Name == "integration1");
        }

        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_And_NotMatched_UserAgent()
        {
            var testObject = new IntegrationEvaluator();

            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                            new TriggerModel() {
                                                                LogicalOperator = LogicalOperatorType.And,
                                                                TriggerParts = new List<TriggerPart>() {
                                                                    new TriggerPart() {
                                                                        CookieName ="c1",
                                                                        Operator = ComparisonOperatorType.EqualS,
                                                                        IsIgnoreCase= true,
                                                                        ValueToCompare ="value1",
                                                                        ValidatorType= ValidatorType.CookieValidator
                                                                    },
                                                                    new TriggerPart() {
                                                                        UrlPart = UrlPartType.PageUrl,
                                                                        ValidatorType= ValidatorType.UrlValidator,
                                                                        ValueToCompare= "test",
                                                                        Operator= ComparisonOperatorType.Contains
                                                                        },
                                                                   new TriggerPart() {
                                                                        ValidatorType= ValidatorType.UserAgentValidator,
                                                                        ValueToCompare= "Googlebot",
                                                                        Operator= ComparisonOperatorType.Contains,
                                                                        IsIgnoreCase= true,
                                                                        IsNegative= true
                                                                        }

                                                                }
                                                    }
                                              }
                                            }

            }
            };


            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");

            var httpRequestMock = new KnownUserTest.MockHttpRequest()
            {
                CookiesValue = new NameValueCollection() { { "c1", "Value1" } },
                UserAgent = "bot.html google.com googlebot test"
            };
            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, httpRequestMock) == null);
        }

        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_And_NotMatched_HttpHeader()
        {
            var testObject = new IntegrationEvaluator();

            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                            new TriggerModel() {
                                                                LogicalOperator = LogicalOperatorType.And,
                                                                TriggerParts = new List<TriggerPart>() {
                                                                    new TriggerPart() {
                                                                        CookieName ="c1",
                                                                        Operator = ComparisonOperatorType.EqualS,
                                                                        IsIgnoreCase= true,
                                                                        ValueToCompare ="value1",
                                                                        ValidatorType= ValidatorType.CookieValidator
                                                                    },
                                                                    new TriggerPart() {
                                                                        UrlPart = UrlPartType.PageUrl,
                                                                        ValidatorType= ValidatorType.UrlValidator,
                                                                        ValueToCompare= "test",
                                                                        Operator= ComparisonOperatorType.Contains
                                                                        },
                                                                   new TriggerPart() {
                                                                       HttpHeaderName = "Akamai-bot",
                                                                       ValidatorType = ValidatorType.HttpHeaderValidator,
                                                                        ValueToCompare= "bot",
                                                                        Operator= ComparisonOperatorType.Contains,
                                                                        IsIgnoreCase= true,
                                                                        IsNegative= true
                                                                        }

                                                                }
                                                    }
                                              }
                                            }

            }
            };


            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");


            var httpRequestMock = new KnownUserTest.MockHttpRequest()
            {
                CookiesValue = new NameValueCollection() { { "c1", "Value1" } },
                Headers = new NameValueCollection() { { "Akamai-bot", "bot" } }
            };
            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, httpRequestMock) == null);
        }

        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_Or_NotMatched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                                 new TriggerModel() {
                                                                    LogicalOperator = LogicalOperatorType.Or,
                                                                    TriggerParts = new List<TriggerPart>() {
                                                                        new TriggerPart() {
                                                                            CookieName ="c1",
                                                                            Operator = ComparisonOperatorType.EqualS,
                                                                            ValueToCompare ="value1",
                                                                            ValidatorType= ValidatorType.CookieValidator
                                                                        },
                                                                        new TriggerPart() {
                                                                            UrlPart = UrlPartType.PageUrl,
                                                                            ValidatorType= ValidatorType.UrlValidator,
                                                                             IsIgnoreCase= true,
                                                                            IsNegative= true,
                                                                            ValueToCompare= "tesT",
                                                                            Operator= ComparisonOperatorType.Contains
                                                                            }

                                                                    }
                                                                }
                                                    }
                                              }
                                            }

            };

            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");


            var httpRequestMock = new KnownUserTest.MockHttpRequest()
            {
                CookiesValue = new NameValueCollection() { { "c2", "value1" } }
            };
            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, httpRequestMock) == null);
        }

        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_Or_Matched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                           new TriggerModel() {
                                                                LogicalOperator = LogicalOperatorType.Or,
                                                                TriggerParts = new List<TriggerPart>() {
                                                                    new TriggerPart() {
                                                                        CookieName ="c1",
                                                                        Operator = ComparisonOperatorType.EqualS,
                                                                        ValueToCompare ="value1",
                                                                        ValidatorType= ValidatorType.CookieValidator
                                                                    },
                                                                    new TriggerPart() {
                                                                        UrlPart = UrlPartType.PageUrl,
                                                                        ValidatorType= ValidatorType.UrlValidator,
                                                                        ValueToCompare= "tesT",
                                                                        Operator= ComparisonOperatorType.Contains
                                                                        }

                                                                }
                                                        }
                                                    }
                                              }
                                            }

            };


            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");
            var httpRequestMock = new KnownUserTest.MockHttpRequest()
            {
                CookiesValue = new NameValueCollection() { { "c1", "value1" } }
            };
            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, httpRequestMock).Name == "integration1");
        }

        [Fact]
        public void GetMatchedIntegrationConfig_TwoTriggers_Matched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                        LogicalOperator = LogicalOperatorType.And,
                                                        TriggerParts = new List<TriggerPart>() {
                                                            new TriggerPart() {
                                                                CookieName ="c1",
                                                                Operator = ComparisonOperatorType.EqualS,
                                                                ValueToCompare ="value1",
                                                                ValidatorType= ValidatorType.CookieValidator
                                                            }


                                                        }
                                                    },
                                                        new TriggerModel()
                                                        {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>()
                                                            {
                                                              new TriggerPart() {
                                                                    UrlPart = UrlPartType.PageUrl,
                                                                    ValidatorType= ValidatorType.UrlValidator,
                                                                    ValueToCompare= "*",
                                                                    Operator= ComparisonOperatorType.Contains
                                                                    }
                                                            }
                                                         }
                                                  }
                                              }
                                     }

            };


            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");

            var httpRequestMock = new KnownUserTest.MockHttpRequest();
            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, httpRequestMock).Name == "integration1", string.Empty);
        }

        [Fact]
        public void GetMatchedIntegrationConfig_TwoTriggers_NotMatched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>() {
                                                                new TriggerPart() {
                                                                    CookieName ="c1",
                                                                    Operator = ComparisonOperatorType.EqualS,
                                                                    ValueToCompare ="value1",
                                                                    ValidatorType= ValidatorType.CookieValidator
                                                                }


                                                            }
                                                        },
                                                        new TriggerModel()
                                                        {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>()
                                                            {
                                                                 new TriggerPart() {
                                                                    UrlPart = UrlPartType.PageUrl,
                                                                    ValidatorType= ValidatorType.UrlValidator,
                                                                    ValueToCompare= "tesT",
                                                                    Operator= ComparisonOperatorType.Contains
                                                                    }
                                                            }
                                                        }
                                                  }
                                              }
                                     }

            };

            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");

            var httpRequestMock = new KnownUserTest.MockHttpRequest();
            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, httpRequestMock) == null);
        }

        [Fact]
        public void GetMatchedIntegrationConfig_ThreeIntegrationsInOrder_SecondMatched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration0",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>() {
                                                                new TriggerPart() {
                                                                    CookieName ="c1",
                                                                    Operator = ComparisonOperatorType.EqualS,
                                                                    ValueToCompare ="value1",
                                                                    ValidatorType= ValidatorType.CookieValidator
                                                                }


                                                            }
                                                        }
                                                  }
                                              },
                                               new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>() {
                                                                new TriggerPart() {
                                                                    CookieName ="c1",
                                                                    Operator = ComparisonOperatorType.EqualS,
                                                                    ValueToCompare ="Value1",
                                                                    ValidatorType= ValidatorType.CookieValidator
                                                                }


                                                            }
                                                        }
                                                  }
                                              },
                                              new IntegrationConfigModel()
                                             {
                                                 Name= "integration2",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>() {
                                                                new TriggerPart() {
                                                                    UrlPart= UrlPartType.PageUrl,
                                                                    Operator = ComparisonOperatorType.Contains,

                                                                    ValueToCompare ="test",
                                                                    ValidatorType= ValidatorType.UrlValidator
                                                                }


                                                            }
                                                        }
                                                  }
                                              }
                                     }

            };

            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");

            var httpRequestMock = new KnownUserTest.MockHttpRequest()
            {
                CookiesValue = new NameValueCollection() { { "c1", "Value1" } }
            };
            Assert.False(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, httpRequestMock).Name == "integration2");
        }
    }
}