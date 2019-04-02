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
            var scale = json["scale"]?.Value<int>();
            var step = json["step"]?.Value<decimal>();
            var fibo = json["fibonacci"]?.Value<bool>() ?? false;

            // determine if we are working in decimal or int notaion?
            var minConvertedToDecimal = json["min"].Value<decimal>();
            var maxConvertedToDecimal = json["max"].Value<decimal>();
            var minScale = GeneFactory.DecimalScale(minConvertedToDecimal);
            var maxScale = GeneFactory.DecimalScale(maxConvertedToDecimal);

            // if any of three scales is above 0 we are working with decimals
            if (scale > 0 || Math.Max(minScale, maxScale) > 0)
            {
                return new GeneConfiguration
                {
                    Key = key,
                    MinDecimal = minConvertedToDecimal,
                    MaxDecimal = maxConvertedToDecimal,
                    Scale = scale,
                    Step = step,
                    Fibonacci = fibo
                };
            }

            // else this is int
            return new GeneConfiguration
            {
                Key = key,
                MinInt = (int)minConvertedToDecimal,
                MaxInt = (int)maxConvertedToDecimal,
                Scale = scale,
                Step = step,
                Fibonacci = fibo
            };

            /*
            if (json["actual"] != null)
            {
                int parsed;
                string raw = json["actual"].Value<string>();
                if (int.TryParse(raw, out parsed))
                {
                    gene.ActualInt = parsed;
                }

                if (!gene.ActualInt.HasValue)
                {
                    decimal decimalParsed;
                    if (decimal.TryParse(raw, out decimalParsed))
                    {
                        gene.ActualDecimal = decimalParsed;
                    }
                    if (decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out decimalParsed))
                    {
                        gene.ActualDecimal = decimalParsed;
                    }
                }
            }
            */
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var gene = (GeneConfiguration)value;
            
            writer.WriteStartObject();

            // key
            writer.WritePropertyName("key");
            writer.WriteValue(gene.Key);

            // fibo
            writer.WritePropertyName("fibonacci");
            writer.WriteValue(gene.Fibonacci);

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

            if (gene.Scale.HasValue)
            {
                writer.WritePropertyName("scale");
                writer.WriteValue(gene.Scale);
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