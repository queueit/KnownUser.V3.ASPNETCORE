using System;
using Xunit;

namespace QueueIT.KnownUser.V3.AspNetCore.Tests
{
    public class UrlParameterProviderTest
    {
        [Fact]
        public void TryExtractQueueParams_Test()
        {
            var queueitToken = "ts_1480593661~cv_10~ce_false~q_944c1f44-60dd-4e37-aabc-f3e4bb1c8895~c_customerid~e_eventid~rt_disabled~h_218b734e-d5be-4b60-ad66-9b1b326266e2";

            var queueParameter = QueueParameterHelper.ExtractQueueParams(queueitToken);
            Assert.True(queueParameter.TimeStamp == new DateTime(2016, 12, 1, 12, 1, 1, DateTimeKind.Utc));
            Assert.True(queueParameter.EventId == "eventid");
            Assert.True(queueParameter.CookieValidityMinutes == 10);
            Assert.True(queueParameter.ExtendableCookie == false);
            Assert.True(queueParameter.HashCode == "218b734e-d5be-4b60-ad66-9b1b326266e2");
            Assert.True(queueParameter.QueueITToken == queueitToken);
            Assert.True(queueParameter.QueueITTokenWithoutHash == "ts_1480593661~cv_10~ce_false~q_944c1f44-60dd-4e37-aabc-f3e4bb1c8895~c_customerid~e_eventid~rt_disabled");
        }

        [Fact]
        public void TryExtractQueueParams_NotValidFormat_Test()
        {
            var queueitToken = "ts_sasa~cv_adsasa~ce_falwwwse~q_944c1f44-60dd-4e37-aabc-f3e4bb1c8895";

            var queueParameter = QueueParameterHelper.ExtractQueueParams(queueitToken);
            Assert.True(queueParameter.TimeStamp == new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            Assert.True(queueParameter.EventId == null);
            Assert.True(queueParameter.CookieValidityMinutes == null);
            Assert.True(queueParameter.ExtendableCookie == false);
            Assert.True(queueParameter.HashCode == null);
            Assert.True(queueParameter.QueueITToken == queueitToken);
        }
    }
}
