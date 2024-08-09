using OpenTK.Mathematics;
using static OpenTK.Graphics.OpenGL4.GL;
using static OpenTK.Graphics.OpenGL4.BufferTarget;
using static OpenTK.Graphics.OpenGL4.BufferUsageHint;

namespace VoxelWorld.Graphics
{
    public class VBO : IGraphicsObject
    {
        public int ID { get; set; }

        public VBO(List<float> data)
        {
            ID = GenBuffer();
            BindBuffer(ArrayBuffer, ID);
            BufferData(ArrayBuffer, data.Count * sizeof(float), data.ToArray(), StaticDraw);
        }

        public VBO(List<Vector2> data)
        {
            ID = GenBuffer();
            BindBuffer(ArrayBuffer, ID);
            BufferData(ArrayBuffer, data.Count * Vector2.SizeInBytes, data.ToArray(), StaticDraw);
        }

        public VBO(List<Vector3> data)
        {
            ID = GenBuffer();
            BindBuffer(ArrayBuffer, ID);
            BufferData(ArrayBuffer, data.Count * Vector3.SizeInBytes, data.ToArray(), StaticDraw);
        }

        public VBO(List<Vector4> data)
        {
            ID = GenBuffer();
            BindBuffer(ArrayBuffer, ID);
            BufferData(ArrayBuffer, data.Count * Vector4.SizeInBytes, data.ToArray(), StaticDraw);
        }

        public void Bind() => BindBuffer(ArrayBuffer, ID);
        public void Unbind() => BindBuffer(ArrayBuffer, 0);
        public void Delete() => DeleteBuffer(ID);
    }
}
