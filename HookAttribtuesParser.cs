/*
 * HookAttributesParser.cs
 * Finds and stores all members using a HookAttribute derived attribute.
 * 
 * by Adam Carballo under MIT license.
 * https://github.com/AdamCarballo/HookInterceptor
 * https://f10.dev
 */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace F10.Hooks {
    [ExecuteAlways]
    public static class HookAttributesParser {

#if UNITY_EDITOR
        public static readonly Dictionary<string[], KeyValuePair<object, MemberInfo>> HookAttributes = new Dictionary<string[], KeyValuePair<object, MemberInfo>>();
#endif

        public static void Add(object obj) {
#if UNITY_EDITOR
            if (HookAttributes.Count(x => x.Value.Key == obj) > 0) {
                // Already in the list, ignore.
                return;
            }

            var members = new List<MemberInfo>();

            members.AddRange(obj.GetType().GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(x => x.GetCustomAttributes(typeof(HookAttribute), false).Length > 0).ToList());

            foreach (var member in members) {
                if (HookAttributes.Any(x => x.Value.Value == member)) continue;

                var attribute = member.GetCustomAttribute(typeof(HookAttribute), false) as HookAttribute;
                var hookMethodPair = new KeyValuePair<object, MemberInfo>(obj, member);
                HookAttributes.Add(attribute.DataGroups, hookMethodPair);
            }
#endif
        }

    }
}