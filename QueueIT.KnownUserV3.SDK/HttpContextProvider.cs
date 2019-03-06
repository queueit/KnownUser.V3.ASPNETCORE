using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("QueueIT.KnownUserV3.SDK.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace QueueIT.KnownUserV3.SDK
{
    public class SDKInitializer
    {
        public static void SetHttpContext(HttpContext context)
        {
            HttpContextProvider.SetHttpContext(context);
        }
    }

    class HttpContextProvider : IHttpContextProvider
    {
        IHttpRequest _httpRequest;
        public IHttpRequest HttpRequest
        {
            get
            {
                if (_httpRequest == null)
                {
                    throw new Exception("Call HttpContextProvider.SetHttpContext to config SDK");
                }
                return _httpRequest;
            }
        }
        IHttpResponse _httpResponse;
        public IHttpResponse HttpResponse
        {
            get
            {
                if (_httpResponse == null)
                {
                    throw new Exception("Call HttpContextProvider.SetHttpContext to config SDK");
                }
                return _httpResponse;
            }
        }
        public static IHttpContextProvider Instance
        {
            get;
            private set;
        } = new HttpContextProvider();

        public static void SetHttpContext(HttpContext context)
        {
            (Instance as HttpContextProvider)._httpRequest = new HttpRequest(context);
            (Instance as HttpContextProvider)._httpResponse = new HttpResponse(context);
        }
    }

    class HttpRequest : IHttpRequest
    {
        HttpContext _context;

        public HttpRequest(HttpContext context)
        {
            this._context = context;
            Headers = new NameValueCollection();
            foreach (var name in _context.Request.Headers.Keys)
            {
                Headers.Add(name, _context.Request.Headers[name]);
            }
            Url = new Uri($"{_context.Request.Scheme}://{_context.Request.Host}{_context.Request.Path}{_context.Request.QueryString}");
        }

        public string UserAgent => _context.Request.Headers["User-Agent"].ToString();

        public NameValueCollection Headers { get; private set; }

        public Uri Url { get; private set; }

        public string UserHostAddress => _context.Connection.RemoteIpAddress.ToString();

        public string GetCookieValue(string cookieKey)
        {
            return _context.Request.Cookies[cookieKey];
        }
    }

    class HttpResponse : IHttpResponse
    {
        HttpContext _context;
        public HttpResponse(HttpContext context)
        {
            this._context = context;
        }

        public void SetCookie(string cookieName, string cookieValue, string domain, DateTime expiration)
        {
            var cookieOptions = new CookieOptions() { Expires = expiration, HttpOnly = false, Secure = false };

            if (!string.IsNullOrEmpty(domain))
            {
                cookieOptions.Domain = domain;
            }
            _context.Response.Cookies.Append(cookieName, cookieValue, cookieOptions);
        }
    }
}
