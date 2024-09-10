using OpenTK.Graphics.OpenGL;

namespace VoxelWorld.Graphics
{
    public class BufferObject<T> : IGraphicsObject, IDisposable where T : unmanaged
    {
        public int ID { get; private set; }
        private BufferTarget _type;

        public unsafe BufferObject(BufferTarget type, int size, bool isDynamic)
        {
            _type = type;
            ID = GL.GenBuffer();
            GL.BindBuffer(type, ID);
            GL.BufferData(type, size * Marshal.SizeOf<T>(), IntPtr.Zero, isDynamic ? BufferUsage.StreamDraw : BufferUsage.StaticDraw);
        }

        public unsafe BufferObject(BufferTarget type, T[] data, bool isDynamic)
        {
            _type = type;
            ID = GL.GenBuffer();
            GL.BindBuffer(type, ID);
            GL.BufferData(type, data.Length * Marshal.SizeOf<T>(), data, isDynamic ? BufferUsage.StreamDraw : BufferUsage.StaticDraw);
        }

        public void Bind() => GL.BindBuffer(_type, ID);
        public void Unbind() => GL.BindBuffer(_type, 0);
        public void Dispose() => GL.DeleteBuffer(ID);

        public unsafe void SetData(T[] data, int startIndex, int elementCount)
        {
            GL.BindBuffer(_type, ID);
            fixed (T* dataPtr = &data[startIndex])
            {
                GL.BufferSubData(_type, IntPtr.Zero, elementCount * Marshal.SizeOf<T>(), new IntPtr(dataPtr));
            }
        }
    }
}
