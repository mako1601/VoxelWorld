using System.Drawing;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

using FontStashSharp.Interfaces;

namespace VoxelWorld.Graphics.Renderer
{
    // This code was taken from the FontStashSharp library page on Github: 
    // https://github.com/FontStashSharp/FontStashSharp/tree/main/samples/FontStashSharp.Samples.OpenTK
    public class Text : IFontStashRenderer2, IDisposable
    {
        private const int MAX_SPRITES = 2048;
        private const int MAX_VERTICES = MAX_SPRITES * 4;
        private const int MAX_INDICES = MAX_SPRITES * 6;

        private readonly ShaderProgram _shader;
        private readonly BufferObject<VertexPositionColorTexture> _vertexBuffer;
        private readonly BufferObject<short> _indexBuffer;
        private readonly VertexArrayObject _vao;
        private readonly VertexPositionColorTexture[] _vertexData = new VertexPositionColorTexture[MAX_VERTICES];
        private object? _lastTexture;
        private int _vertexIndex = 0;

        private readonly Texture2DManager _textureManager;

        public ITexture2DManager TextureManager => _textureManager;

        private static readonly short[] indexData = GenerateIndexArray();

        public unsafe Text()
        {
            _textureManager = new Texture2DManager();

            _vertexBuffer = new BufferObject<VertexPositionColorTexture>(BufferTarget.ArrayBuffer, MAX_VERTICES, true);
            _indexBuffer = new BufferObject<short>(BufferTarget.ElementArrayBuffer, indexData.Length, false);
            _indexBuffer.SetData(indexData, 0, indexData.Length);

            _shader = new ShaderProgram("text.glslv", "text.glslf");
            _shader.Use();

            _vao = new VertexArrayObject(sizeof(VertexPositionColorTexture));
            _vao.Bind();

            var location = _shader.GetAttribLocation("a_position");
            _vao.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, 0);

            location = _shader.GetAttribLocation("a_color");
            _vao.VertexAttribPointer(location, 4, VertexAttribPointerType.UnsignedByte, true, 3 * sizeof(float));

            location = _shader.GetAttribLocation("a_texCoords0");
            _vao.VertexAttribPointer(location, 2, VertexAttribPointerType.Float, false, 3 * sizeof(float) + 4 * sizeof(byte));
        }

        ~Text() => Dispose(false);
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _vao.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _shader.Dispose();
        }

        public void Begin(Vector2i windowSize)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            _shader.Use();
            _shader.SetInt("TextureSampler", 0);

            var transform = Matrix4.CreateOrthographicOffCenter(0f, windowSize.X, windowSize.Y, 0f, 0f, -1f);
            _shader.SetMatrix4("MatrixTransform", transform);

            _vao.Bind();
            _indexBuffer.Bind();
            _vertexBuffer.Bind();
        }

        public void DrawQuad(object texture, ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight, ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
        {
            if (_lastTexture != texture)
            {
                FlushBuffer();
            }

            _vertexData[_vertexIndex++] = topLeft;
            _vertexData[_vertexIndex++] = topRight;
            _vertexData[_vertexIndex++] = bottomLeft;
            _vertexData[_vertexIndex++] = bottomRight;

            _lastTexture = texture;
        }

        public void End()
        {
            FlushBuffer();
        }

        private unsafe void FlushBuffer()
        {
            if (_vertexIndex == 0 || _lastTexture == null)
            {
                return;
            }

            _vertexBuffer.SetData(_vertexData, 0, _vertexIndex);

            var texture = (TextTexture)_lastTexture;
            texture.Bind();

            GL.DrawElements(PrimitiveType.Triangles, _vertexIndex * 6 / 4, DrawElementsType.UnsignedShort, IntPtr.Zero);

            _vertexIndex = 0;
        }

        private static short[] GenerateIndexArray()
        {
            short[] result = new short[MAX_INDICES];
            for (int i = 0, j = 0; i < MAX_INDICES; i += 6, j += 4)
            {
                result[i + 0] = (short)(j + 0);
                result[i + 1] = (short)(j + 1);
                result[i + 2] = (short)(j + 2);
                result[i + 3] = (short)(j + 3);
                result[i + 4] = (short)(j + 2);
                result[i + 5] = (short)(j + 1);
            }
            return result;
        }
    }

    internal class Texture2DManager : ITexture2DManager
    {
        public Texture2DManager()
        {
        }

        public object CreateTexture(int width, int height) => new TextTexture(width, height);

        public Point GetTextureSize(object texture)
        {
            var t = (TextTexture)texture;
            return new Point(t.Width, t.Height);
        }

        public void SetTextureData(object texture, Rectangle bounds, byte[] data)
        {
            var t = (TextTexture)texture;
            t.SetData(bounds, data);
        }
    }

    public unsafe class TextTexture : IDisposable
    {
        private readonly int _handle;

        public readonly int Width;
        public readonly int Height;

        public TextTexture(int width, int height)
        {
            Width = width;
            Height = height;

            _handle = GL.GenTexture();
            Bind();

            //Reserve enough memory from the gpu for the whole image
            GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            SetParameters();
        }

        private void SetParameters()
        {
            //Setting some texture perameters so the texture behaves as expected.
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);

            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);

            //Generating mipmaps.
            GL.GenerateMipmap(TextureTarget.Texture2d);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            //When we bind a texture we can choose which textureslot we can bind it to.
            GL.ActiveTexture(textureSlot);

            GL.BindTexture(TextureTarget.Texture2d, _handle);
        }

        public void Dispose()
        {
            //In order to dispose we need to delete the opengl handle for the texure.
            GL.DeleteTexture(_handle);
        }

        public void SetData(Rectangle bounds, byte[] data)
        {
            Bind();
            fixed (byte* ptr = data)
            {
                GL.TexSubImage2D(
                    target: TextureTarget.Texture2d,
                    level: 0,
                    xoffset: bounds.Left,
                    yoffset: bounds.Top,
                    width: bounds.Width,
                    height: bounds.Height,
                    format: PixelFormat.Rgba,
                    type: PixelType.UnsignedByte,
                    pixels: new IntPtr(ptr)
                );
            }
        }
    }
}