/*
 * HookInterceptor.cs
 * Main class that intercepts, formats and manages url schemes and payloads.
 * 
 * by Adam Carballo under MIT license.
 * https://github.com/AdamCarballo/HookInterceptor
 * https://f10.dev
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace F10.Hooks {
	[InitializeOnLoad, ExecuteAlways]
	public static class HookInterceptor {
		private const string Tag = "HookInterceptor";

		public enum LogLevel {
			None = 0,
			Essential = 1,
			Debug = 2,
			All = 3
		}

		private const string AssetStoreWindowTitle = "Asset Store";

		private const string BaseUrl = "com.unity3d.kharma:";
		private const string UrlScheme = "hook/";
		private const string UrlSecurity = "key=";
		private const string UrlParam = "param=";

		private static readonly string _assembledUrl = BaseUrl + UrlScheme;

		private static readonly HookInterceptorPreferences _preferences = null;

		/// <summary>
		/// Delegate for when a url is intercepted with hook data.
		/// <para>The url still contains a key (if any) and hasn't been checked.</para>
		/// <para>You are strongly recommended to use InterceptedSecurely instead.</para>
		/// </summary>
		public static Action<string> Intercepted;

		/// <summary>
		/// Delegate for when a url is intercepted securely with hook data.
		/// <para>The url has been checked against a local key (if any).</para>
		/// <para>Use Intercepted if you need to access the url before is checked (not recommended).</para>
		/// </summary>
		public static Action<string> InterceptedSecurely;

		/// <summary>
		/// Delegate for when a url with hook data is intercepted securely and checked against exceptions.
		/// <para>The url is passed as an string array of data already formatted.</para>
		/// <para>Use InterceptedSecurely if you need to access the entire url, or if you want to parse an exception.</para>
		/// </summary>
		public static Action<List<string>> Formatted;

		private static readonly Assembly _assembly = Assembly.GetAssembly(typeof(Editor));

		private static bool WasWindowOpen = false;

		static HookInterceptor() {
			_preferences = HookInterceptorPreferences.GetOrCreatePreferences();

			EditorApplication.update += OnUpdate;
			Intercepted += OnIntercept;
			Formatted += OnFormatted;
		}

		private static void OnUpdate() {
			var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			if (windows == null || windows.Length <= 0) return;

			foreach (var window in windows) {
				if (window.titleContent.text != AssetStoreWindowTitle) continue;

				WasWindowOpen = !OnPreIntercept(window);
				return;
			}

			// No Asset Store found, mark as closed
			WasWindowOpen = false;
		}

		private static bool OnPreIntercept(EditorWindow assetStoreWindow) {
			LogVerbose("Asset Store Window found!");

			// Obtain the url data using reflection
			var type = _assembly.GetType("UnityEditor.AssetStoreContext");
			var instance = type.GetMethod("GetInstance").Invoke(null, null);
			string url = (string) instance.GetType().GetMethod("GetInitialOpenURL").Invoke(instance, null);

			// Check if there is a url
			if (string.IsNullOrEmpty(url)) {
				LogVerbose("URL is empty, no payload");
				return false;
			}

			LogVerbose($"Initial Open URL: {url}");

			// Check if the passed url is a hook or something else
			if (!url.StartsWith(_assembledUrl)) {
				LogDebug("URL is not a Hook url, ignoring...");
				return false;
			}

			// Close the Asset Store window
			if (WasWindowOpen) {
				LogVerbose("Asset Store window was opened previously, blocking close...");
			} else {
				LogVerbose("Closing Asset Store window");
				assetStoreWindow.Close();
			}

			// Subtract the payload from the url
			var payload = url.Replace(_assembledUrl, string.Empty);
			LogVerbose($"URL payload: {payload}");
			
			if (!_preferences.AllowIntercepting) {
				LogDebug("Intercepting is disabled on settings");
				return true;
			}

			Intercepted?.Invoke(payload);

			return true;
		}

		private static void OnIntercept(string payload) {
			var data = payload.Split('/').ToList();
			// Trim empty splits (when a url payload ends with / this will avoid issues where it thinks there are multiple splits)
			data = data.Where(x => !string.IsNullOrEmpty(x)).ToList();

			if (!IsSecure(ref data)) {
				LogEssential($"Payload sent with an incorrect or empty key!\nDisable 'Use secure hooks' if you want to use hooks without security keys");
				return;
			}

			InterceptedSecurely?.Invoke(string.Join("/", data.ToArray()));

			if (!_preferences.AllowFormatting) {
				LogDebug("Formatting is disabled on settings");
				return;
			}
			
			if (_preferences.Exceptions.Contains(data[0])) {
				LogDebug("Payload is part of exceptions list. Stopping formatting...");
				return;
			}

			Formatted?.Invoke(data);
		}

		private static bool IsSecure(ref List<string> payload) {
			var secured = true;
			if (payload[0].StartsWith(UrlSecurity)) {
				// Remove secure key scheme from payload
				payload[0] = payload[0].Replace(UrlSecurity, string.Empty);

				if (!_preferences.UseSecureHooks) {
					LogVerbose("Payload contains a security key, but 'Use secure hooks' is disabled, ignoring key...");
				} else {
					LogVerbose("Payload contains a security key, checking...");
					secured = payload[0] == _preferences.SecureKey;
				}

				// Remove secure key from payload
				payload.RemoveAt(0);
			} else {
				if (_preferences.UseSecureHooks) {
					LogDebug("Payload does not contain a security key, but 'Use secure hooks' is enabled");
					secured = false;
				} else {
					LogVerbose("No security key found in payload");
				}
			}

			return secured;
		}

		private static void OnFormatted(List<string> data) {
			var filteredMethods = HookAttributesParser.HookAttributes.ToArray();
			object param = null;
			for (int i = 0; i < data.Count; i++) {
				if (data[i].StartsWith(UrlParam)) {
					param = data[i].Replace(UrlParam, string.Empty);
				} else {
					filteredMethods = filteredMethods.Where(x => x.Key.Length > i && x.Key[i] == data[i]).ToArray();
					LogVerbose(string.Format("Number of attributes found in {0} group: {1}", data[i], filteredMethods.Length));
				}
			}

			for (int i = 0; i < filteredMethods.Length; i++) {
				LogVerbose("Reflection calling: " + filteredMethods[i].Value.Value.Name);

				var attributeType = filteredMethods[i].Value.Value.GetCustomAttribute(typeof(HookAttribute));
				if (attributeType is HookField) {
					if (param == null) return;
					
					var info = filteredMethods[i].Value.Value as FieldInfo;
					var parsedParam = GetParsedParameter(info.FieldType, param);

					info.SetValue(filteredMethods[i].Value.Key, parsedParam);
				} else if (attributeType is HookProperty) {
					if (param == null) return;

					var info = filteredMethods[i].Value.Value as PropertyInfo;
					var parsedParam = GetParsedParameter(info.PropertyType, param);

					info.SetValue(filteredMethods[i].Value.Key, parsedParam);
				} else if (attributeType is HookMethod) {
					var info = filteredMethods[i].Value.Value as MethodInfo;
					
					// Check if method has parameters
					var parameters = info.GetParameters();

					if (parameters.Length <= 0) {
						info.Invoke(filteredMethods[i].Value.Key, null);
					} else {
						var parsedParam = GetParsedParameter(parameters[0].ParameterType, param);
						info.Invoke(filteredMethods[i].Value.Key, new[] {parsedParam});
					}
				}
			}
		}

		/// <summary>
		/// Helper to parse string-based (or other objects) url hook params to be used as values.
		/// </summary>
		/// <param name="type">Type to convert to</param>
		/// <param name="param">Parameter to convert</param>
		/// <returns></returns>
		public static object GetParsedParameter(Type type, object param) {
			object parsedParam = null;

			// Ignore object as it doesn't need parsing
			if (type == typeof(bool)) {
				parsedParam = Convert.ToBoolean(param);
			} else if (type == typeof(int)) {
				parsedParam = Convert.ToInt32(param);
			} else if (type == typeof(float)) {
				parsedParam = Convert.ToSingle(param);
			} else if (type == typeof(string)) {
				parsedParam = Convert.ToString(param);
			}

			return parsedParam;
		}

		#region Logging

		public static void LogVerbose(string message) {
			Log(message, LogLevel.All);
		}

		public static void LogDebug(string message) {
			Log(message, LogLevel.Debug);
		}

		public static void LogEssential(string message) {
			Log(message, LogLevel.Essential);
		}

		private static void Log(string message, LogLevel level) {
			if (level > _preferences.Logging) return;

			if (level <= LogLevel.Essential) {
				Debug.LogWarning($"{Tag} - {message}");
			} else {
				Debug.Log($"{Tag} - {message}");
			}
		}

		#endregion
	}
}