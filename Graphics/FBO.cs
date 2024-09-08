using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

namespace VoxelWorld.Graphics
{
    public class FBO : IGraphicsObject, IDisposable
    {
        public int ID { get; set; }
        public int Texture { get; set; }

        public FBO(Vector2i textureSize)
        {
            ID = GenFramebuffer();
            BindFramebuffer(FramebufferTarget.Framebuffer, ID);

            Texture = GenTexture();
            BindTexture(TextureTarget.Texture2d, Texture);
            TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, textureSize.X, textureSize.Y, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, Texture, 0);
        
            if (CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
            {
                Console.WriteLine("FBO ERROR!");
            }

            BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Bind() => BindFramebuffer(FramebufferTarget.Framebuffer, ID);
        public void Unbind() => BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        public void Dispose()
        {
            DeleteFramebuffer(ID);
            DeleteTexture(Texture);
        }
    }
}
