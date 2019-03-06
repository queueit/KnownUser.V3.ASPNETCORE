using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Timers;
using System.Web.Script.Serialization;

namespace QueueIT.KnownUserV3.SDK.IntegrationConfigLoader
{
    /// <summary>
    /// This is an example showing how the Integration Configuration can be downloaded from Go Queue-it. 
    /// It is also deserialized to a CustomerIntegration object and cached in memory for 5 minutes.
    /// The retry logic will try 5 times with 5 seconds interval and with a 4 second timeout.
    /// </summary>
    public static class IntegrationConfigProvider
    {

        private const int _downloadTimeoutMS = 4000;
        internal static int _RefreshIntervalS = 5 * 60;
        internal static double _RetryExceptionSleepS = 5;
        private static Timer _timer;
        private static readonly object _lockObject = new object();
        static CustomerIntegration _cachedIntegrationConfig;
        private static bool _isInitialized = false;
        private static string _customerId;
        public static CustomerIntegration GetCachedIntegrationConfig(string customerId)
        {
            if(!_isInitialized)
            {
                _customerId = customerId;
                lock (_lockObject)
                {
                    if (!_isInitialized)
                    {
                        RefreshCache(init:true);
                        _timer = new Timer();
                        _timer.Interval = _RefreshIntervalS * 1000;
                        _timer.AutoReset = false;
                        _timer.Elapsed += TimerElapsed;
                        _timer.Start();
                        _isInitialized = true;
                    }
                }
            }
            return _cachedIntegrationConfig;
        }

   
        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            RefreshCache(init:false);
            _timer.Start();
        }

        private static void RefreshCache(bool init)
        {
            int tryCount = 0;
            while (tryCount < 5)
            {
                var timeBaseQueryString = (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds.ToString();
                var configUrl = string.Format("https://{0}.queue-it.net/status/integrationconfig/{0}?qr={1}", _customerId, timeBaseQueryString);
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(configUrl);
                    request.Timeout = _downloadTimeoutMS;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                            throw new Exception($"It was not sucessful retriving config file status code {response.StatusCode} from {configUrl}");
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            JavaScriptSerializer deserializer = new JavaScriptSerializer();
                            var deserialized = deserializer.Deserialize<CustomerIntegration>(reader.ReadToEnd());
                            if (deserialized == null)
                                throw new Exception("CustomerIntegration is null");
                             _cachedIntegrationConfig = deserialized;

                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ++tryCount;
                    if (tryCount >= 5)
                    {
                        //Use your favorit logging framework to log the exceptoin
                        break;
                    }
                    if (!init)
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(_RetryExceptionSleepS));
                    else
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(0.200 * tryCount));
                }
            }
        }
    }
}
