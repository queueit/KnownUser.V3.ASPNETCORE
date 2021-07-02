# Queue-it KnownUser SDK for ASP.NET Core
Before getting started please read the [documentation](https://github.com/queueit/Documentation/tree/main/serverside-connectors) to get acquainted with server-side connectors.

Connector supports ASP.NET Core 2.0+.

You can find the latest released version [here](https://github.com/queueit/KnownUser.V3.ASPNETCORE/releases/latest) or download latest version [![NuGet](http://img.shields.io/nuget/v/QueueIT.KnownUser.V3.AspNetCore.svg)](https://www.nuget.org/packages/QueueIT.KnownUser.V3.AspNetCore/)

## Implementation
The KnownUser validation must be done on *all requests except requests for static and cached pages, resources like images, css files and ...*. 
So, if you add the KnownUser validation logic to a central place like in Startup.cs, then be sure that the Triggers only fire on page requests (including ajax requests) and not on e.g. image.

The following method is all that is needed to validate that a user has been through the queue:

*Startup.cs*
```
...
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    ...

    app.Use(async(context, next)=> {
        QueueIT.KnownUser.V3.AspNetCore.SDKInitializer.SetHttpContext(context);
        if (KnownUserValidator.DoValidation(context))
        {
            await next.Invoke();
        }
    });
    app.UseMvc();
}
...
```

*KnownUserValidator.cs*
```
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
 
```
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
              EventId = "event1", //ID of the queue to use
              //CookieDomain = ".mydomain.com", //Optional - Domain name where the Queue-it session cookie should be saved.
              QueueDomain = "queue.mydomain.com", //Domain name of the queue.
              CookieValidityMinute = 15, //Validity of the Queue-it session cookie should be positive number.
              ExtendCookieValidity = true, //Should the Queue-it session cookie validity time be extended each time the validation runs?
              //Culture = "en-US", //Optional - Culture of the queue layout in the format specified here: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx. If unspecified then settings from Event will be used. 
              //LayoutName = "MyCustomLayoutName" //Optional - Name of the queue layout. If unspecified then settings from Event will be used.
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
