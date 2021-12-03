# Queue-it KnownUser SDK for ASP.NET Core
Before getting started please read the [documentation](https://github.com/queueit/Documentation/tree/main/serverside-connectors) to get acquainted with server-side connectors.

Connector supports ASP.NET Core 2.0+.

You can find the latest released version [here](https://github.com/queueit/KnownUser.V3.ASPNETCORE/releases/latest) or download latest version [![NuGet](http://img.shields.io/nuget/v/QueueIT.KnownUser.V3.AspNetCore.svg)](https://www.nuget.org/packages/QueueIT.KnownUser.V3.AspNetCore/)

## Implementation
The KnownUser validation must be done on *all requests except requests for static and cached pages, resources like images, css files and ...*. 
So, if you add the KnownUser validation logic to a central place like in Startup.cs, then be sure that the Triggers only fire on page requests (including ajax requests) and not on e.g. image.

This example is using the *[IntegrationConfigProvider](https://github.com/queueit/KnownUser.V3.ASPNETCORE/blob/master/Documentation/IntegrationConfigProvider.cs)* to download the queue configuration. The provider is an example of how the download and caching of the configuration can be done. This is just an example, but if you make your own downloader, please cache the result for 5 - 10 minutes to limit number of download requests. **You should NEVER download the configuration as part of the request handling**.

The following method is all that is needed to validate that a user has been through the queue:

*Startup.cs*
```CSharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.Use(async(context, next)=> {
        QueueIT.KnownUser.V3.AspNetCore.SDKInitializer.SetHttpContext(context);
        if (KnownUserValidator.DoValidation(context))
        {
            await next.Invoke();
        }
    });
    app.UseMvc();
}
```

*KnownUserValidator.cs*
```CSharp
public class KnownUserValidator
{
    public static bool DoValidation(HttpContext context)
    {
        try
        {
            var customerId = "Your Queue-it customer ID";
            var secretKey = "Your 72 char secrete key as specified in Go Queue-it self-service platform";
            var apiKey = "Your api-key as specified in Go Queue-it self-service platform";

            var requestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
            var queueitToken = context.Request.Query[KnownUser.QueueITTokenKey];
            var pureUrl = Regex.Replace(requestUrl, @"([\?&])(" + KnownUser.QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);

            var integrationConfig = IntegrationConfigProvider.GetCachedIntegrationConfig(customerId, apiKey); // download and cache using polling

            //Verify if the user has been through the queue
            var validationResult = KnownUser.ValidateRequestByIntegrationConfig(pureUrl, queueitToken, integrationConfig, customerId, secretKey);

            if (validationResult.DoRedirect)
            {
                context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate, max-age=0");               
                context.Response.Headers.Add("Pragma", "no-cache");
                context.Response.Headers.Add("Expires", "Fri, 01 Jan 1990 00:00:00 GMT");

                if (validationResult.IsAjaxResult)
                {
                    context.Response.Headers.Add(validationResult.AjaxQueueRedirectHeaderKey, validationResult.AjaxRedirectUrl);
                    context.Response.Headers.Add("Access-Control-Expose-Headers", validationResult.AjaxQueueRedirectHeaderKey);
                    return false;
                }

                //Send the user to the queue - either becuase hash was missing or becuase is was invalid
                context.Response.Redirect(validationResult.RedirectUrl);
                return false;
            }
            else
            {
                //Request can continue - we remove queueittoken form querystring parameter to avoid sharing of user specific token
                //if there was a match 
                if (requestUrl.Contains(KnownUser.QueueITTokenKey) && validationResult.ActionType == "Queue")
                {
                    context.Response.Redirect(pureUrl);
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            return true;
            // There was an error validating the request
            // Use your own logging framework to log the error
            // This was a configuration error, so we let the user continue
        }
    }
}
```

## Implementation using inline queue configuration
Specify the configuration in code without using the Trigger/Action paradigm. In this case it is important *only to queue-up page requests* and not requests for resources. 

The following is an example of how to specify the configuration in code:
 
```CSharp
public class KnownUserValidator
{
    public static bool DoValidation(HttpContext context)
    {
        try
        {
            var customerId = "Your Queue-it customer ID";
            var secretKey = "Your 72 char secrete key as specified in Go Queue-it self-service platform";

            var requestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
            var queueitToken = context.Request.Query[KnownUser.QueueITTokenKey];
            var pureUrl = Regex.Replace(requestUrl, @"([\?&])(" + KnownUser.QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);

            var eventConfig = new QueueEventConfig()
            {
                EventId = "event1", // ID of the queue to use
                //CookieDomain = ".mydomain.com", // Optional - Domain name where the Queue-it session cookie should be saved.
                QueueDomain = "queue.mydomain.com", // Domain name of the queue.
                CookieValidityMinute = 15, // Validity of the Queue-it session cookie should be positive number.
                ExtendCookieValidity = true, // Should the Queue-it session cookie validity time be extended each time the validation runs?
                //Culture = "en-US", // Optional - Culture of the queue layout in the format specified here: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx. If unspecified then settings from Event will be used. 
                //LayoutName = "MyCustomLayoutName" // Optional - Name of the queue layout. If unspecified then settings from Event will be used.
            };

            //Verify if the user has been through the queue
            var validationResult = KnownUser.ResolveQueueRequestByLocalConfig(pureUrl, queueitToken, eventConfig, customerId, secretKey);

            if (validationResult.DoRedirect)
            {
                context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate, max-age=0");
                context.Response.Headers.Add("Pragma", "no-cache");
                context.Response.Headers.Add("Expires", "Fri, 01 Jan 1990 00:00:00 GMT");
                if (validationResult.IsAjaxResult)
                {
                    context.Response.Headers.Add(validationResult.AjaxQueueRedirectHeaderKey, validationResult.AjaxRedirectUrl);
                    context.Response.Headers.Add("Access-Control-Expose-Headers", validationResult.AjaxQueueRedirectHeaderKey);
                    return false;
                }

                //Send the user to the queue - either becuase hash was missing or becuase is was invalid
                context.Response.Redirect(validationResult.RedirectUrl);
                return false;
            }
            else
            {
                //Request can continue - we remove queueittoken form querystring parameter to avoid sharing of user specific token
                //if there was a match 
                if (requestUrl.Contains(KnownUser.QueueITTokenKey) && validationResult.ActionType == "Queue")
                {
                    context.Response.Redirect(pureUrl);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            return true;
            // There was an error validating the request
            // Use your own logging framework to log the error
            // This was a configuration error, so we let the user continue
        }
    }
}
```

## Helper functions
The [QueueITHelpers.cs](https://github.com/queueit/KnownUser.V3.ASPNETCORE/blob/master/Documentation/QueueITHelpers.cs) file includes some helper functions 
to make the reading of the `queueittoken` easier.

## Advanced Features
### Request body trigger

The connector supports triggering on request body content. An example could be a POST call with specific item ID where you want end-users to queue up for.
For this to work, you will need to contact Queue-it support or enable request body triggers in your integration settings in your GO Queue-it platform account.
Once enabled you will need to update your integration so request body is available for the connector.  
You need to create a custom HttpRequest similar to this one:

```CSharp
public class CustomHttpRequest : QueueIT.KnownUserV3.SDK.HttpRequest
{
    public string RequestBody { get; set; }

    public override string GetRequestBodyAsString()
    {
        return RequestBody ?? base.GetRequestBodyAsString();
    }
}
```

Then, on each request, before calling the `DoValidation()` method, you should initialize the SDK with your custom HttpRequest implementation:

```CSharp
var customRequest = new CustomHttpRequest(context)
{
    RequestBody = await GetRequestBody(context)
};
SDKInitializer.SetHttpRequest(customRequest);
```

The `GetRequestBody()` function could be implemented like below. Make sure to set the `maxBytesToRead` to something appropriate for your needs.

```CSharp
/*
 * Example of how the request body can be read and rewinded
 */
private async Task<string> GetRequestBody(HttpContext context)
{
    // Limit the number of bytes needed to read, from the body, to avoid reading large requests
    var maxBytesToRead = 1024 * 50;

    // Ensure that the body can be read multiple times. Threshold is the size of the memory buffer.
    // Body data which exceeds the threshold will be written to disk
    context.Request.EnableBuffering(bufferThreshold: maxBytesToRead);

    string body;

    // Leave the body open so the next middleware can read it.
    using (var reader = new StreamReader(
        context.Request.Body,
        encoding: Encoding.UTF8,
        detectEncodingFromByteOrderMarks: false,
        leaveOpen: true))
    {
        var buffer = new char[maxBytesToRead];
        var totalBytesRead = await reader.ReadBlockAsync(buffer, 0, maxBytesToRead);

        body = new string(buffer, 0, totalBytesRead);

        // Reset the request body stream position so the next middleware can read it
        context.Request.Body.Position = 0;
    }

    return body;
}
```

### Ignore specific HTTP verbs
You can ignore specific HTTP methods, by checking the request method, before calling `DoValidation()`. If you are using CORS, it might be best to ignore `OPTIONS` requests, since they are used by CORS to retrieve your server's configuration.
You can ignore `OPTIONS` requests using the following method:

```CSharp
private static bool IsIgnored(HttpContext context)
{
    return context.Request.Method.Equals("options", StringComparison.OrdinalIgnoreCase);
}
```

### Complete middleware example with advanced features

Below is a complete example how to use the two advanced features above, as a middleware.

*Startup.cs*
```CSharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.Use(async (context, next) =>
    {
        QueueIT.KnownUser.V3.AspNetCore.SDKInitializer.SetHttpContext(context);

        var customRequest = new CustomHttpRequest(context)
        {
            RequestBody = await GetRequestBody(context)
        };
        QueueIT.KnownUser.V3.AspNetCore.SDKInitializer.SetHttpRequest(customRequest);

        if (IsIgnored(context))
        {
            await next.Invoke();
            return;
        }

        if (KnownUserValidator.DoValidation(context))
        {
            await next.Invoke();
        }
    });

    app.UseMvc();
}
```