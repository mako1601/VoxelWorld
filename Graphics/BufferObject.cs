using OpenTK.Graphics.OpenGL;

namespace VoxelWorld.Graphics
{
    public class BufferObject<T> : IGraphicsObject, IDisposable where T : unmanaged
    {
        public int ID { get; private set; }
        public BufferTarget Type { get; private set; }

        public unsafe BufferObject(BufferTarget type, int size, bool isDynamic)
        {
            Type = type;
            ID = GL.GenBuffer();
            GL.BindBuffer(type, ID);
            GL.BufferData(type, size * Marshal.SizeOf<T>(), IntPtr.Zero, isDynamic ? BufferUsage.DynamicDraw : BufferUsage.StaticDraw);
        }

        public unsafe BufferObject(BufferTarget type, T[] data, bool isDynamic)
        {
            Type = type;
            ID = GL.GenBuffer();
            GL.BindBuffer(type, ID);
            GL.BufferData(type, data.Length * Marshal.SizeOf<T>(), data, isDynamic ? BufferUsage.DynamicDraw : BufferUsage.StaticDraw);
        }

        public void Bind() => GL.BindBuffer(Type, ID);
        public void Unbind() => GL.BindBuffer(Type, 0);
        public void Dispose() => GL.DeleteBuffer(ID);

        public unsafe void SetData(T[] data, int startIndex, int elementCount)
        {
            GL.BindBuffer(Type, ID);
            fixed (T* dataPtr = &data[startIndex])
            {
                GL.BufferSubData(Type, IntPtr.Zero, elementCount * Marshal.SizeOf<T>(), new IntPtr(dataPtr));
            }
        }
    }
}
