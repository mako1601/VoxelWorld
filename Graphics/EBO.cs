using static OpenTK.Graphics.OpenGL.GL;
using static OpenTK.Graphics.OpenGL.BufferUsage;
using static OpenTK.Graphics.OpenGL.BufferTarget;

namespace VoxelWorld.Graphics
{
    public class EBO : IGraphicsObject, IDisposable
    {
        private bool _disposed = false;
        public int ID { get; private set; }

        public EBO(List<uint> data)
        {
            ID = GenBuffer();
            BindBuffer(ElementArrayBuffer, ID);
            BufferData(ElementArrayBuffer, data.Count * sizeof(uint), data.ToArray(), StaticDraw);
        }

        public void Bind() => BindBuffer(ElementArrayBuffer, ID);
        public void Unbind() => BindBuffer(ElementArrayBuffer, 0);
        private void Delete()
        {
            if (ID != 0)
            {
                DeleteBuffer(ID);
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

        ~EBO() => Dispose(false);
    }
}
