using OpenTK.Mathematics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VoxelWorld.JsonConverters
{
    public class Vector3iJC : JsonConverter<Vector3i>
    {
        public override void WriteJson(JsonWriter writer, Vector3i value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("Z");
            writer.WriteValue(value.Z);
            writer.WriteEndObject();
        }

        public override Vector3i ReadJson(JsonReader reader, Type objectType, Vector3i existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            int x = (int)jo["X"];
            int y = (int)jo["Y"];
            int z = (int)jo["Z"];
            return new Vector3i(x, y, z);
        }
    }
}
