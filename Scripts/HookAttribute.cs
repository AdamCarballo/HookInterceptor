/*
 * HookAttribute.cs
 * Attributes used by the default formatter.
 * 
 * by Adam Carballo under MIT license.
 * https://github.com/AdamCarballo/HookInterceptor
 * https://f10.dev
 */

using System;

namespace F10.Hooks {
	public class HookAttribute : Attribute {
		public string[] DataGroups { get; }

		protected HookAttribute(string[] dataGroups) {
			DataGroups = dataGroups;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class HookField : HookAttribute {
		public HookField(string[] dataGroups) : base(dataGroups) { }
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class HookProperty : HookAttribute {
		public HookProperty(string[] dataGroups) : base(dataGroups) { }
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class HookMethod : HookAttribute {
		public HookMethod(string[] dataGroups) : base(dataGroups) { }
	}
}