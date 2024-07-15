using static OpenTK.Graphics.OpenGL4.GL;
using static OpenTK.Graphics.OpenGL4.BufferTarget;
using static OpenTK.Graphics.OpenGL4.BufferUsageHint;

namespace VoxelWorld.Graphics
{
    public class EBO : IGraphicsObject
    {
        public int ID { get; set; }

        public EBO(List<uint> data)
        {
            ID = GenBuffer();
            BindBuffer(ElementArrayBuffer, ID);
            BufferData(ElementArrayBuffer, data.Count * sizeof(uint), data.ToArray(), StaticDraw);
        }

        public void Bind() => BindBuffer(ElementArrayBuffer, ID);
        public void Unbind() => BindBuffer(ElementArrayBuffer, 0);
        public void Delete() => DeleteBuffer(ID);
    }
}
