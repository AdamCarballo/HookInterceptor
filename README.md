Hook Interceptor
====
🎛 Unity + StreamDeck – Call the Unity Editor using deep links with custom payloads

Overview
----
Ever wanted to connect a [StreamDeck](https://www.elgato.com/en/gaming/stream-deck) or any other device to be used as a debug or editor keyboard? Maybe you wanted a way to call the Unity Editor from an external source and send some data at the same time?

This package will allow you to use [deep links](https://developer.android.com/training/app-links/deep-linking) to call the Unity Editor and invoke methods or change variables with parameters attached to the URL. This means that any device that can call URLs on your computer will be able to communicate with Unity by just "opening a URL".

Hook Interceptor will **literally** hijack the Asset Store (don't worry, only the window, and it will still work if the link is not a _hook_ link) and instead allow you to invoke other methods based on a defined data structure.

This API (for lack of a better name) is only one way, so Unity will be able to listen for hooks, but the application that sent the URL in the first link will have no way to know Unity received it. There are multiple ways to fix this issue, like creating a [socket](https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket?view=netframework-4.8) and sending data back, but none of that is tackled on this package.

The main purpose of this package was to enable a [StreamDeck](https://www.elgato.com/en/gaming/stream-deck) or a remote controller like [Touch Portal](https://www.touch-portal.com/) to interact with the Unity Editor, as a visual replacement of [Menu Items](https://docs.unity3d.com/ScriptReference/MenuItem.html) supported natively by Unity.

Install
----
The source code is available directly from the repo, or if you want you can download only the [Asset Package](https://github.com/AdamCarballo/HookInterceptor/releases) and import from there.

Tested and developed using **Unity 2019.1.2f1**. There is no reason it shouldn't work on older or newer versions, but if something goes wrong, please [let me know](https://github.com/AdamCarballo/HookInterceptor/issues).


#### Preferences:
After importing the package, go to Unity Preferences > Hook Interceptor. Here you can tweak how the interceptor will handle incoming URLs:
- **Allow Intercepting** - If disabled, URL hooks won't be intercepted using the default logic. Only manually intercepting and formatting will work.
- **Allow Formatting** - If disabled, URL hooks won't be formatted using the default logic and methods using attributes won't be called. Only manually formatting will work.
- **Use secure hooks** - If enabled only URL hooks with a [Secure Key](#secure-key) will be allowed and parsed.
- **Secure key** - Key that an allowed URL hook must contain. Any length and characters are allowed, minus / and \.
- **Parsing exceptions** - Each string listed here will not be parsed, which means Formatted() won't be called. You must parse the exceptions manually yourself. Separate each exception with a comma.
- **Logging level** - Logging level. Essential is recommended.

#### Secure Key:
A new secure key will be generated after importing the package for the first time, and `Use secure hooks` will be enabled by default. The main purpose of this is to avoid unwanted URLs from being parsed by Unity. Any app or website can trigger a deep link that the interceptor will try to parse, but with a secure key only URLs containing your exact key will be allowed.

If `Use secure hooks` is enabled, only URLs containing your secret key will parse. If this setting is disabled both URLs containing a key and URLs without it will parse.

**Note:** Nothing about this key is **secure**. From how it's created to the way it's stored and used. Think of it as a way to avoid a colleague from *unknowingly* triggering your method `DestroyEverythingWithoutAsking()` that for some reason you mapped to a hook.

Usage
----
Hook Interceptor is set up to be extremely easy to use, and at the same time allows complete customization for advanced users.<br>
There are two main sections to know about:
- URLs
- Parsing & Formatting

This section contains an overview of all the scripts, how to use attributes, how to create URLs and how to expand with custom parsing and formatting.

#### HookInterceptor.cs
Main class that intercepts, formats and manages URLs schemes and payloads.<br>
This script listens for URLs and contains all the callbacks related to hooks.
___

#### HookAttribute.cs
Attributes used by the default formatter.
___

#### HookAttributesParser.cs
Finds and stores all members using a HookAttribute derived attribute.<br>
This script handles scripts using attributes, as it must be called with an object reference `Add(object)` to add members using reflection.<br>
It has multiple checks to avoid being called (or hold any data) outside the Editor.
___

#### HookInterceptorPreferences.cs
Preferences holder and renderer for Unity's Preferences window.
___

### Using attributes:
This is the simplest way to use hooks. By adding an attribute on methods, fields or properties you can modify its values, or in case of methods, invoke them using a hook.<br>
The parser uses [reflection](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/reflection) to find references, and it will work with public, private, internal or static members.

Hook attributes require a string array to be defined. This will be the URL they will listen to be called.<br>
For example, `com.unity3d.kharma:hook/debug/settings` will call attributes with `new[] {"debug", "settings"}`.

```csharp
[HookField(new[] {"testing", "intField"})]
public int _intField;

[HookProperty(new[] {"testing", "floatProperty"})]
public float FloatProperty {get; set;}

[HookMethod(new[] {"testing", "method"})]
public void Method() {}

[HookMethod(new[] {"testing", "methodWithParam"})]
public void MethodWithParam(bool value) {}
```
**Note:** Only `object`, `string`, `bool`, `int` and `float` types are allowed as URL parameters. Methods are allowed up to one parameter.

### Creating URLs:
A normal URL will normally look something like this:
`com.unity3d.kharma:hook/key=BGE325GFE/example/child/param=42`

We'll go step by step and deconstruct this URL. The main thing to know about URLs is that each section requires to be escaped by a `/`. The final parameter is not required to be escaped, but nothing will happen if it is.

1. The way this tool works, it requires that all URLs start with `com.unity3d.kharma:` as this is the deep link Unity has configured for the Asset Store.

2. After that, URLs must also contain `hook`. This prevents any calls that want to reach the actual Asset Store to be ignored by the interceptor.

3. **Optional:** if [Use Secure Hooks](#secure-key) is enabled, URLs must contain `key=` followed by the key defined in preferences.

4. After all the configuration sections, you can add as many sections as you want. Of course, the more sections used the more organized your hooks will be, but also more complex will be to maintain. In this case: `example/child`

5. **Optional:** finally, URLs can contain data by sending it as the last section `param=` followed by the data to send.

### Custom parsing and formatting:
If using attributes is not the best idea for your project (shared code-base) or you want to do some custom parsing or formatting, there are some callbacks you can subscribe to manually parse and/or format URLs.

**Note:** Make sure to disable `Allow Formatting` or, for more extreme URLs `Allow Intercepting` on the Preferences window to avoid the interceptor from trying to make sense of a custom URLs.

All the following callbacks are inside `HookInterceptor`:

#### Intercepted()
Delegate for when a URL is intercepted with hook data.<br>
The URL still contains a key (if any) and hasn't been checked. You are strongly recommended to use `InterceptedSecurely()` instead.

This is the only callback that will work if both `Allow Formatting` and `Allow Intercepting` are disabled.<br>
Minimum required URL format: `com.unity3d.kharma:hook/`
___

#### InterceptedSecurely()
Delegate for when a URL is intercepted securely with hook data.<br>
The URL has been checked against a local key (if any).

This callback will only work if at least `Allow Intercepting` is enabled.<br>
Minimum required URL format: `com.unity3d.kharma:hook/key=XXX/` or `com.unity3d.kharma:hook/`
___

#### Formatted()
Delegate for when a URL with hook data is intercepted securely and checked against exceptions.<br>
The URL is passed as a string array of data already formatted.

This callback will only work if both `Allow Formatting` and `Allow Intercepting` are enabled.<br>
Minimum required URL format: `com.unity3d.kharma:hook/key=XXX/` or `com.unity3d.kharma:hook/`.<br>
Must not be on the exceptions list.
___

Examples
----
The `Examples` folder from the source code (also included in the [Asset Package](https://github.com/AdamCarballo/HookInterceptor/releases)) includes an example script using all the provided attributes the default parser and formatter supports.<br>
Remember to attach the `HookExample.cs` component to a GameObject on a scene to initialize it, otherwise the test URLs won't trigger anything.

The example script has hooks to the following URLs. You can experiment changing or adding parameters, secure keys, data groups, etcetera:
```
com.unity3d.kharma:hook/debug
com.unity3d.kharma:hook/debug/settings
com.unity3d.kharma:hook/debug/settings/testing
com.unity3d.kharma:hook/debug/settings/field/param=0
com.unity3d.kharma:hook/debug/settings/property/param=0
```
The URLs listed above don't include any [Secure Key](#secure-key), so either add it, or disable secure hooks.

History
----
Created by Àdam Carballo<br>
Check other works on [F10DEV](https://f10.dev)<br>

[![ko-fi](https://www.ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/X8X4XHCE)

License
---
All files remain under the [MIT](LICENSE) License.
