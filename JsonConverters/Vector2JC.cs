using OpenTK.Mathematics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VoxelWorld.JsonConverters
{
    public class Vector2JC : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            float x = (float)jo["X"];
            float y = (float)jo["Y"];
            return new Vector2(x, y);
        }
    }
}