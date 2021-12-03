using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("QueueIT.KnownUser.V3.AspNetCore.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace QueueIT.KnownUser.V3.AspNetCore
{
    public static class SDKInitializer
    {
        public static void SetHttpContext(HttpContext context)
        {
            HttpContextProvider.SetHttpContext(context);
        }

        public static void SetHttpRequest(IHttpRequest httpRequest)
        {
            HttpContextProvider.SetHttpRequest(httpRequest);
        }
    }

    internal class HttpContextProvider : IHttpContextProvider
    {
        private IHttpRequest _httpRequest;
        public IHttpRequest HttpRequest
        {
            get
            {
                if (_httpRequest == null)
                {
                    throw new Exception("Call SDKInitializer.SetHttpContext to configure SDK");
                }

                return _httpRequest;
            }
        }

        private IHttpResponse _httpResponse;
        public IHttpResponse HttpResponse
        {
            get
            {
                if (_httpResponse == null)
                {
                    throw new Exception("Call SDKInitializer.SetHttpContext to configure SDK");
                }

                return _httpResponse;
            }
        }

        public static IHttpContextProvider Instance { get; } = new HttpContextProvider();

        public static void SetHttpContext(HttpContext context)
        {
            ((HttpContextProvider)Instance)._httpRequest = new HttpRequest(context);
            ((HttpContextProvider)Instance)._httpResponse = new HttpResponse(context);
        }

        public static void SetHttpRequest(IHttpRequest httpRequest)
        {
            ((HttpContextProvider)Instance)._httpRequest = httpRequest;
        }
    }

    public class HttpRequest : IHttpRequest
    {
        private HttpContext _context;

        public HttpRequest(HttpContext context)
        {
            _context = context;
            Headers = new NameValueCollection();
            foreach (var name in _context.Request.Headers.Keys)
            {
                Headers.Add(name, _context.Request.Headers[name]);
            }
            Url = new Uri($"{_context.Request.Scheme}://{_context.Request.Host}{_context.Request.Path}{_context.Request.QueryString}");
        }

        public string UserAgent => _context.Request.Headers["User-Agent"].ToString();

        public NameValueCollection Headers { get; }

        public Uri Url { get; }

        public string UserHostAddress => _context.Connection.RemoteIpAddress.ToString();

        public string GetCookieValue(string cookieKey)
        {
            return _context.Request.Cookies[cookieKey];
        }

        public virtual string GetRequestBodyAsString()
        {
            return string.Empty;
        }
    }

    internal class HttpResponse : IHttpResponse
    {
        private HttpContext _context;

        public HttpResponse(HttpContext context)
        {
            _context = context;
        }

        public void SetCookie(string cookieName, string cookieValue, string domain, DateTime expiration, bool isHttpOnly, bool isSecure)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = expiration,
                HttpOnly = isHttpOnly,
                Secure = isSecure,
            };

            if (!string.IsNullOrEmpty(domain))
            {
                cookieOptions.Domain = domain;
            }
            _context.Response.Cookies.Append(cookieName, cookieValue, cookieOptions);
        }
    }
}
