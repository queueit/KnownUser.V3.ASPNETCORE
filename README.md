>You can find the latest released version [here](https://github.com/queueit/KnownUser.V3.ASPNETCORE/releases/latest).
>Download latest version from here: TBA

# Queue-it KnownUser SDK for ASP.NET Core
The Queue-it Security Framework is used to ensure that end users cannot bypass the queue by adding a server-side integration to your server. It supports ASP.NET Core 2.0.2.
## Introduction
When a user is redirected back from the queue to your website, the queue engine can attach a query string parameter (`queueittoken`) containing some information about the user. 
The most important fields of the `queueittoken` are:

 - q - the users unique queue identifier
 - ts - a timestamp of how long this redirect is valid
 - h - a hash of the token


The high level logic is as follows:

![The KnownUser validation flow](https://github.com/queueit/KnownUser.V3.ASPNETCORE/blob/master/Documentation/KnownUserFlow.png)

 1. User requests a page on your server
 2. The validation method sees that the has no Queue-it session cookie and no `queueittoken` and sends him to the correct queue based on the configuration
 3. User waits in the queue
 4. User is redirected back to your website, now with a `queueittoken`
 5. The validation method validates the `queueittoken` and creates a Queue-it session cookie
 6. The user browses to a new page and the Queue-it session cookie will let him go there without queuing again

## How to validate a user
To validate that the current user is allowed to enter your website (has been through the queue) these steps are needed:

 1. Providing the queue configuration to the KnownUser validation
 2. Validate the `queueittoken` and store a session cookie


### 1. Providing the queue configuration
The recommended way is to use the Go Queue-it self-service portal to setup the configuration. 
The configuration specifies a set of Triggers and Actions. A Trigger is an expression matching one, more or all URLs on your website. 
When a user enter your website and the URL matches a Trigger-expression the corresponding Action will be triggered. 
The Action specifies which queue the users should be sent to. 
In this way you can specify which queue(s) should protect which page(s) on the fly without changing the server-side integration.

### 2. Validate the `queueittoken` and store a session cookie
To validate that the user has been through the queue, use the `KnownUser.ValidateRequestByIntegrationConfig()` method. 
This call will validate the timestamp and hash and if valid create a "QueueITAccepted-SDFrts345E-V3_[EventId]" cookie with a TTL as specified in the configuration.
If the timestamp or hash is invalid, the user is send back to the queue.


## Implementation
The KnownUser validation must be done on *all requests except requests for static resources like images, css files and ...*. 
So, if you add the KnownUser validation logic to a central place like in Startup.cs, then be sure that the Triggers only fire on page requests (including ajax requests) and not on e.g. image.

The following method is all that is needed to validate that a user has been through the queue:
```
TBA
```

## Alternative Implementation

### Queue configuration

If your application server (maybe due to security reasons) is not allowed to do external GET requests, then you have three options:

1. Manually download the configuration file from Queue-it Go self-service portal, save it on your application server and load it from local disk
2. Use an internal gateway server to download the configuration file and save to application server
3. Specify the configuration in code without using the Trigger/Action paradigm. In this case it is important *only to queue-up page requests* and not requests for resources or AJAX calls. 

The following is an example of how to specify the configuration in code:
 
```
TBA
```
### Protecting ajax calls on static pages
If you have some static html pages (might be behind cache servers) and you have some ajax calls from those pages needed to be protected by KnownUser library you need to follow these steps:
1) You are using v.3.5.1 (or later) of the KnownUser library.
2) Make sure KnownUser code will not run on static pages (by ignoring those URLs in your integration configuration).
3) Add below JavaScript tags to static pages:
```
<script type="text/javascript" src="//static.queue-it.net/script/queueclient.min.js"></script>
<script
  data-queueit-intercept-domain="{YOUR_CURRENT_DOMAIN}"
  data-queueit-intercept="true"
  data-queueit-c="{YOUR_CUSTOMER_ID}"
  type="text/javascript"
  src="//static.queue-it.net/script/queueconfigloader.min.js">
</script>
```
4) Use the following method to protect all dynamic calls (including dynamic pages and ajax calls).

```
TBA
```
