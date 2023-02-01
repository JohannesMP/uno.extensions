---
uid: Overview.Authentication
---
# Authentication

Uno.Extensions.Authentication is designed to make it simple to add authentication to an application. Authenticating a user may be used to restrict access to specific parts of the application, or in order to supply an access token when calling a back-end service. 

There are two aspects to the Authentication extensions:
- Authentication - the the process of authenticating the user and acquiring tokens
- Authorization - tokens (acquired via authentication) can be queried to control access to parts of the application or supplied to service call so the user can be authorized to access the back-end service

## IAuthenticationService

The `IAuthenticationService` interface defines the methods that an application can call to authenticate the user.  

````csharp
public interface IAuthenticationService
{
	string[] Providers { get; }
	ValueTask<bool> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials = default, string? provider = null, CancellationToken? cancellationToken = default);
	ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = default);
	ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken? cancellationToken = default);
	ValueTask<bool> IsAuthenticated(CancellationToken? cancellationToken = default);
	event EventHandler LoggedOut;
}
````
There are any number of different application workflows that require authentication but they typically boil down to using one or more of the `IAuthenticationService` methods. For example

**Login on launch**  
In this scenario the user is required to be authenticated in order to access the application. This is a workflow that redirects the user to a login prompt if they aren't authenticated when the application is launched.
-   Launch app
-   App invokes `IAuthenticationService.RefreshAsync` to refresh any existing tokens (eg retrieve a new Access Token by supplying a Refresh Token to an authentication endpoint). 
-   If `RefreshAsync` returns true, the user is logged in, so navigate to the home page of the application
-   If `RefreshAsync` returns false, navigate to the login page of the application
-   On the login page, user enter credentials and clicks Login, the app invokes `IAuthenticationService.LoginAsync` and supplies credentials)
-   If `LoginAsync` returns true, app navigates to home page
-   The user might decide to logout of the application, which invokes the `IAuthenticationService.LogoutAsync` method, the application then navigates back to the login page.

**User login requested**  
In this scenario the application doesn't require the user to be authenticated unless they want to access certain parts of the application (or there is additional/different information that's available to the user if they've logged in)
-   Launch app
-   App invokes `IAuthenticationService.RefreshAsync` to refresh any existing tokens and to determine if the user is authenticated. The user is directed to the home page of the application, either as an unauthenticated or authenticated user (depending on the app, this may show different data).
-   User attempts to navigate to a part of the application that needs them to be authenticated, or just clicks a sign-in button so they can access the current page as an authenticated user.
-   App navigates to the login page where the user can enter their credentials. The app then invokes `IAuthenticationService.LoginAsync` to authenticate the user.
-   If `LoginAsync` returns true, the user is then either navigated to the part of the application they were attempting to access, or back to the view they were on.
-   The user can logout of the application, which again invokes the `IAuthenticationService.LogoutAsync` method

## Authentication Providers (IAuthenticationProvider)

The process of authentication with a given authority is implemented by an authentication provider (i.e. implements IAuthenticationProvider). Multiple providers, and in fact, multiple instances of any provider, can be registered during application startup. For example, an application may want to provide support for authenticating using Facebook, Twitter and Apple - each of these has a different backend service that you need to connect with. In the application the user selects which registered provider to use by supplying the `provider` argument when invoking the `IAuthenticationService.LoginAsync` method. This argument is optional and is not required if only a single provider has been registered.

Note: The `IAuthenticationProvider` implementations are all marked as internal as they should be configured via the extensions methods on the `IHostBuilder` and the builder interface for the corresponding implementation. 

### Custom
The `CustomAuthenticationProvider` provides a basic implementation of the `IAuthenticationProvider` that requires callback methods to be defined for performing login, refresh and logout actions. Learn [Custom authentication](xref:Learn.Tutorials.Authentication.HowToAuthentication)

### MSAL
The `MsalAuthenticationProvider` (in the Uno.Extensions.Authentication.Msal.UI or Uno.Extensions.Authentication.Msal.WinUI packages) wraps the [MSAL library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) from Microsoft into an implementation of `IAuthenticationProvider`. This implementation ignores any credentials passed into the `LoginAsync` method, instead invoking the web based authentication process required to authentication with Microsoft. Learn [Msal authentication](xref:Learn.Tutorials.Authentication.HowToMsalAuthentication)

### Oidc
The `OidcAuthenticationProvider` (in the Uno.Extensions.Authentication.Oidc.UI or Uno.Extensions.Authentication.Oidc.WinUI packages) wraps support for any [OpenID Connect](https://openid.net/connect/) backend, including [IdentityServer](https://duendesoftware.com/products/identityserver). Learn [Oidc authentication](xref:Learn.Tutorials.Authentication.HowToOidcAuthentication)

### Web
The `WebAuthenticationProvider` provides an implementation that displays a web view in order for the user to login. After login, the web view redirects back to the application, along with any tokens. Learn [Web authentication](xref:Learn.Tutorials.Authentication.HowToWebAuthentication)

## Http Handlers
Once a user has been authenticated, the tokens are cached and are available for use when invoking service calls. Rather than developers having to access the tokens and manually appending the tokens to the http request, the Authentication extensions includes http handlers which will be inserted into the request pipeline in order to apply the tokens as required.

### Authorization Header
The `HeaderHandler` is used to apply the access token to the http request using the `Authorization` header. The default scheme is `Bearer` but this can be override to use a different scheme, such as basic.

### Cookies
The `CookieHandler` is used to apply the access token, and/or refresh token, to the http request as cookies. This requires the cookie name for access tokena and request token to be specified as part of configuring the application. Learn how to use [Cookies](xref:Learn.Tutorials.Authentication.HowToCookieAuthorization) to authorize



