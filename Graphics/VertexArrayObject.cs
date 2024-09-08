using OpenTK.Graphics.OpenGL;

namespace VoxelWorld.Graphics
{
    public class VertexArrayObject : IGraphicsObject, IDisposable
    {
        public int ID { get; private set; }
        private readonly int _stride;

        public VertexArrayObject(int stride)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(stride);

            _stride = stride;
            ID = GL.GenVertexArray();
        }

        public unsafe void VertexAttribPointer(uint location, int size, VertexAttribPointerType type, bool normalized, int offset)
        {
            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, size, type, normalized, _stride, new IntPtr(offset));
        }

        public void Bind() => GL.BindVertexArray(ID);
        public void Unbind() => GL.BindVertexArray(0);
        public void Dispose() => GL.DeleteVertexArray(ID);
    }
}
