using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Optimization
{
    /// <summary>
    /// Converts a gene object to and from JSON.
    /// </summary>
    public class GeneConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(GeneConfiguration);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            // get JObject for json
            var json = JObject.Load(reader);

            // values that are indespensible for both int and decimal
            var key = json["key"].Value<string>();

            // step
            var step = json["step"]?.Value<decimal>();

            // string representation for min-max
            var min = json["min"].Value<string>();
            var max = json["max"].Value<string>();

            // try parse for an int - if succesful - then we are working in int notation
            if (int.TryParse(min, out var minOutputIntResult) && int.TryParse(max, out var maxOuptputIntResult))
            {
                // step if existent must be also of an int kind
                if (step.HasValue)
                {
                    if (step % 1 != 0)
                    {
                        throw new Exception("Gene step must be Int; the same as min and max values");
                    }
                }

                // return an object
                return new GeneConfiguration
                {
                    Key = key,
                    MinInt = minOutputIntResult,
                    MaxInt = maxOuptputIntResult,
                    Step = step
                };
            }

            // else these are decimals
            decimal.TryParse(min, out var minOutputDecimalResult);
            decimal.TryParse(max, out var maxOutputDecimalResult);

            return new GeneConfiguration
            {
                Key = key,
                MinDecimal = minOutputDecimalResult,
                MaxDecimal = maxOutputDecimalResult,
                Step = step
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var gene = (GeneConfiguration)value;
            
            writer.WriteStartObject();

            // Key ->
            writer.WritePropertyName("key");
            writer.WriteValue(gene.Key);

            if (gene.MinDecimal.HasValue)
            {
                writer.WritePropertyName("min");
                writer.WriteValue(gene.MinDecimal);
                writer.WritePropertyName("max");
                writer.WriteValue(gene.MaxDecimal);
            }

            if (gene.MinInt.HasValue)
            {
                writer.WritePropertyName("min");
                writer.WriteValue(gene.MinInt);
                writer.WritePropertyName("max");
                writer.WriteValue(gene.MaxInt);
            }
            
            if (gene.Step.HasValue)
            {
                writer.WritePropertyName("step");
                writer.WriteValue(gene.Step);
            }

            writer.WriteEndObject();
        }

    }
}