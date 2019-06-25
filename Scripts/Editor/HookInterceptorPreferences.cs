/*
 * HookInterceptorPreferences.cs
 * Preferences holder and renderer for Unity's Preferences window
 * 
 * by Adam Carballo under MIT license.
 * https://github.com/AdamCarballo/HookInterceptor
 * https://f10.dev
 */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace F10.Hooks {
	internal class HookInterceptorPreferences : ScriptableObject {
		private const string PreferencesPath = "Assets/Editor/HookInterceptor/HookInterceptorPreferences.asset";

		[SerializeField]
		private HookInterceptor.LogLevel _logging = HookInterceptor.LogLevel.Essential;

		public HookInterceptor.LogLevel Logging {
			get { return _logging; }
			set { _logging = value; }
		}

		[SerializeField]
		private string _exceptions = string.Empty;

		public string[] Exceptions {
			get { return _exceptions.Replace(" ", "").Split(','); }
			set { _exceptions = string.Join(",", value); }
		}

		[SerializeField]
		private bool _useSecureHooks = true;

		public bool UseSecureHooks {
			get { return _useSecureHooks; }
			set { _useSecureHooks = value; }
		}

		[SerializeField]
		private string _secureKey = null;

		public string SecureKey {
			get { return _secureKey; }
			set {
				if (value.Contains("/")) {
					HookInterceptor.LogEssential("Tried to save a security key with '/' characters in it. Avoid using them, as they are not supported");
					return;
				}
				_secureKey = value;
			}
		}

		[SerializeField]
		private bool _allowFormatting = true;

		public bool AllowFormatting {
			get { return _allowFormatting; }
			set { _allowFormatting = value; }
		}

		private HookInterceptorPreferences() {
			GenerateRandomSecureKey();
		}

		public static HookInterceptorPreferences GetOrCreatePreferences() {
			var allAssets = AssetDatabase.FindAssets("t:HookInterceptorPreferences");
			HookInterceptorPreferences settings = null;
			if (allAssets.Length > 0) {
				var path = AssetDatabase.GUIDToAssetPath(allAssets[0]);
				settings = AssetDatabase.LoadAssetAtPath<HookInterceptorPreferences>(path);
			}

			if (settings != null) return settings;
			
			settings = CreateInstance<HookInterceptorPreferences>();

			AssetDatabase.CreateFolder("Assets", "Editor");
			AssetDatabase.CreateFolder("Assets/Editor", "HookInterceptor");
			AssetDatabase.CreateAsset(settings, PreferencesPath);
			AssetDatabase.SaveAssets();

			return settings;
		}

		internal static SerializedObject GetSerializedPreferences() {
			return new SerializedObject(GetOrCreatePreferences());
		}

		public void GenerateRandomSecureKey() {
			var random = new System.Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			_secureKey = new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
		}
	}

	internal static class HookInterceptorPreferencesRegister {
		
		[SettingsProvider]
		public static SettingsProvider CreateProvider() {
			var provider = new SettingsProvider("Preferences/Hook Interceptor", SettingsScope.User) {
				label = "Hook Interceptor",
				guiHandler = (searchContext) => {
					var settings = HookInterceptorPreferences.GetSerializedPreferences();

					settings.Update();
					EditorGUILayout.PropertyField(settings.FindProperty("_allowFormatting"), new GUIContent("Allow Formatting \u24D8", "If disabled, url hooks won't be formatted and methods using attributes won't be called.\n\nOnly manually formatting will work, and Formatted() won't be called."));

					EditorGUILayout.Space();

					EditorGUILayout.PropertyField(settings.FindProperty("_exceptions"), new GUIContent("Parsing exceptions \u24D8", "Each string listed here will not be parsed, which means Formatted() won't be called. You must parse the exceptions manually yourself.\n\nSeparate each exception with a comma."));
					EditorGUILayout.PropertyField(settings.FindProperty("_useSecureHooks"), new GUIContent("Use secure hooks \u24D8", "If enabled only url hooks with a secure key will be allowed and parsed."));

					GUI.enabled = settings.FindProperty("_useSecureHooks").boolValue;
					EditorGUILayout.PropertyField(settings.FindProperty("_secureKey"), new GUIContent("Secure key \u24D8", "Secure key that allowed url hooks must contain.\n\nAny length and characters are allowed, minus / and \\"));
					GUI.enabled = true;

					EditorGUILayout.Space();

					EditorGUILayout.PropertyField(settings.FindProperty("_logging"), new GUIContent("Logging level \u24D8", "Logging level. Essential is recommended."));

					settings.ApplyModifiedProperties();
				},

				keywords = new HashSet<string>(new[] {"HookInterceptor", "Hook", "Hooks", "Interceptor", "URL", "F10"})
			};

			return provider;
		}
	}
}