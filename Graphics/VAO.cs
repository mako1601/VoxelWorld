using static OpenTK.Graphics.OpenGL.GL;
using static OpenTK.Graphics.OpenGL.VertexAttribPointerType;

namespace VoxelWorld.Graphics
{
    public class VAO : IGraphicsObject
    {
        public int ID { get; set; }

        public VAO()
        {
            ID = GenVertexArray();
            BindVertexArray(ID);
        }

        public static void LinkToVAO(uint index, int size, int stride = 0, int offset = 0)
        {
            VertexAttribPointer(index, size, Float, false, stride, offset);
            EnableVertexAttribArray(index);
        }

        public void Bind() => BindVertexArray(ID);
        public void Unbind() => BindVertexArray(0);
        public void Delete() => DeleteVertexArray(ID);
    }
}
