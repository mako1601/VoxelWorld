using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using static OpenTK.Graphics.OpenGL4.GL;

namespace VoxelWorld.Graphics
{
    public class FBO : IGraphicsObject
    {
        public int ID { get; set; }
        public int Texture { get; set; }

        public FBO(Vector2i textureSize)
        {
            ID = GenFramebuffer();
            BindFramebuffer(FramebufferTarget.Framebuffer, ID);

            Texture = GenTexture();
            BindTexture(TextureTarget.Texture2D, Texture);
            TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, textureSize.X, textureSize.Y, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture, 0);
        
            if (CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("FBO ERROR!");
            }

            BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Bind() => BindFramebuffer(FramebufferTarget.Framebuffer, ID);
        public void Unbind() => BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        public void Delete() => DeleteFramebuffer(ID);
    }
}
