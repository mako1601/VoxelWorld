using static OpenTK.Graphics.OpenGL.GL;
using static OpenTK.Graphics.OpenGL.VertexAttribPointerType;

namespace VoxelWorld.Graphics
{
    public class VAO : IGraphicsObject, IDisposable
    {
        private bool _disposed = false;
        public int ID { get; private set; }

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
        private void Delete()
        {
            if (ID != 0)
            {
                DeleteVertexArray(ID);
                ID = 0;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
            }

            Delete();

            _disposed = true;
        }

        ~VAO() => Dispose(false);
    }
}
