using OpenTK.Mathematics;
using static OpenTK.Graphics.OpenGL.GL;
using static OpenTK.Graphics.OpenGL.BufferUsage;
using static OpenTK.Graphics.OpenGL.BufferTarget;

namespace VoxelWorld.Graphics
{
    public class VBO : IGraphicsObject, IDisposable
    {
        private bool _disposed = false;
        public int ID { get; private set; }

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

        ~VBO() => Dispose(false);
    }
}
