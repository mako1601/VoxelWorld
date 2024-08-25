using OpenTK.Mathematics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VoxelWorld.JsonConverters
{
    public class Vector3JC : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
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

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            float x = (float)jo["X"];
            float y = (float)jo["Y"];
            float z = (float)jo["Z"];
            return new Vector3(x, y, z);
        }
    }
}
