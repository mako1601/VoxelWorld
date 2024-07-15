using static OpenTK.Graphics.OpenGL4.GL;
using static OpenTK.Graphics.OpenGL4.VertexAttribPointerType;

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

        public static void LinkToVAO(int index, int size, int stride = 0, int offset = 0)
        {
            VertexAttribPointer(index, size, Float, false, stride, offset);
            EnableVertexAttribArray(index);
        }

        public void Bind() => BindVertexArray(ID);
        public void Unbind() => BindVertexArray(0);
        public void Delete() => DeleteVertexArray(ID);
    }
}
