using OpenTK.Mathematics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VoxelWorld.JsonConverters
{
    public class Vector2iJC : JsonConverter<Vector2i>
    {
        public override void WriteJson(JsonWriter writer, Vector2i value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WriteEndObject();
        }

        public override Vector2i ReadJson(JsonReader reader, Type objectType, Vector2i existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            int x = (int)jo["X"];
            int y = (int)jo["Y"];
            return new Vector2i(x, y);
        }
    }
}
