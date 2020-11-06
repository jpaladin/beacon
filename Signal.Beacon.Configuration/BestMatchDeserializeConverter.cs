using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Signal.Beacon.Configuration
{
    public class BestMatchDeserializeConverter<TContract> : JsonConverter
    {
        private readonly IImmutableDictionary<Type, IReadOnlyList<string>> supportedTypesMapping;

        public BestMatchDeserializeConverter(params Type[] supportedTypes) => 
            this.supportedTypesMapping = supportedTypes.ToImmutableDictionary(t => t, this.ExtractProperties);

        private IReadOnlyList<string> ExtractProperties(Type arg) => 
            arg.GetProperties().Select(p => p.Name).ToList();

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => 
            throw new NotSupportedException();

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var bestMatch = this.supportedTypesMapping
                .Select(stm => new
                {
                    Type = stm.Key,
                    MatchedPropertiesCount = stm.Value.Count(stmProperty => obj.ContainsKey(stmProperty)),
                    TotalProperties = stm.Value.Count
                })
                .OrderByDescending(stmMatchCount => stmMatchCount.MatchedPropertiesCount)
                .ThenByDescending(stmMatchCount => stmMatchCount.TotalProperties - stmMatchCount.MatchedPropertiesCount);
             var matchedType = bestMatch.FirstOrDefault()?.Type ??
                              throw new Exception($"No types matched for deserialization of {objectType.FullName}.");
            return obj.ToObject(matchedType, serializer);
        }

        public override bool CanConvert(Type objectType) =>
            typeof(TContract) == objectType;
    }
}