/*
 * HookExample.cs
 * Example showing how to use HookAttributes.
 * 
 * by Adam Carballo under MIT license.
 * https://github.com/AdamCarballo/HookInterceptor
 * https://f10.dev
 */

using UnityEngine;

namespace F10.Hooks {
	public class HookExample : MonoBehaviour {
		[HookField(new[] {"debug", "settings", "field"})]
		public int _field = 0;

		[HookProperty(new[] {"debug", "settings", "property"})]
		public int Property { get; set; }
		
		private HookExample() {
			HookAttributesParser.Add(this);
		}

		[HookMethod(new[] {"debug", "settings", "testing"})]
		public void Testing() {
			Debug.Log("Called Testing.");
		}

		[HookMethod(new[] {"debug", "settings"})]
		private void Settings() {
			Debug.Log("Called Settings.");
		}

		[HookMethod(new[] {"debug", "settings"})]
		public static void SettingsWithParamsInt(int param) {
			Debug.Log("Called SettingsWithParamsInt: " + param);
		}
	}
}