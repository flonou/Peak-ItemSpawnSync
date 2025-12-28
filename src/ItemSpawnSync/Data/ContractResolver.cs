using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

/// <summary>
/// Custom contract resolver that only serializes public fields or fields marked with [SerializeField]
/// Also handles properties for anonymous types
/// </summary>
public class ContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        property.Writable = true;
        property.Readable = true;
        return property;
    }

    protected override List<MemberInfo> GetSerializableMembers(Type objectType)
    {
        // For anonymous types or types without [Serializable], include all properties
        bool isAnonymousType = objectType.Name.Contains("AnonymousType");

        if (isAnonymousType)
        {
            // Include all public properties for anonymous types and non-Unity types
            List<MemberInfo> properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Cast<MemberInfo>()
                .ToList();
            return properties;
        }

        // For Unity types, include public fields or fields marked with [SerializeField]
        List<MemberInfo> fields = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
            .Cast<MemberInfo>()
            .ToList();

        return fields;
    }
}