using QueueIT.KnownUser.V3.AspNetCore.IntegrationConfig;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Timers;

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
        private static string _apiKey;

        public static CustomerIntegration GetCachedIntegrationConfig(string customerId, string apiKey)
        {
            if (!_isInitialized)
            {
                _customerId = customerId;
                _apiKey = apiKey;

                lock (_lockObject)
                {
                    if (!_isInitialized)
                    {
                        RefreshCache(init: true);
                        _timer = new Timer
                        {
                            Interval = _RefreshIntervalS * 1000,
                            AutoReset = false
                        };
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
            RefreshCache(init: false);
            _timer.Start();
        }

        private static void RefreshCache(bool init)
        {
            int tryCount = 0;
            var configUrl = $"https://{_customerId}.queue-it.net/status/integrationconfig/secure/{_customerId}";

            while (tryCount < 5)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(configUrl);
                    request.Headers.Add("api-key", _apiKey);
                    request.Timeout = _downloadTimeoutMS;

                    using HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception(
                            $"It was not sucessful retriving config file status code {response.StatusCode} from {configUrl}");
                    }

                    using StreamReader reader = new StreamReader(response.GetResponseStream());
                    var deserialized = JsonSerializer.Deserialize<CustomerIntegration>(reader.ReadToEnd());

                    _cachedIntegrationConfig = deserialized ?? throw new Exception("CustomerIntegration is null");

                    return;
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