using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;
using static OpenTK.Graphics.OpenGL.TextureTarget;

using StbImageSharp;

namespace VoxelWorld.Graphics
{
    public class Texture : IGraphicsObject, IDisposable
    {
        private bool _disposed = false;
        public int ID { get; private set; }

        public Texture(string filename, bool mipmap = true, int mipmapLevels = 4)
        {
            ID = GenTexture();
            BindTexture(Texture2d, ID);

            TexParameterf(Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            TexParameterf(Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            TexParameterf(Texture2d, TextureParameterName.TextureMinFilter, mipmap ? (int)TextureMinFilter.NearestMipmapNearest : (int)TextureMinFilter.Nearest);
            TexParameterf(Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            if (mipmap)
            {
                TexParameterf(Texture2d, TextureParameterName.TextureMaxLevel, mipmapLevels);
            }

            StbImage.stbi_set_flip_vertically_on_load(1);
            
            ImageResult texture;
            try
            {
                texture = ImageResult.FromStream(File.OpenRead($"resources/textures/{filename}"),
                    ColorComponents.RedGreenBlueAlpha);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"[WARNING] Failed to load texture file '{ex.FileName}'");
                texture = ImageResult.FromStream(File.OpenRead($"resources/textures/utilities/missing_texture.png"),
                    ColorComponents.RedGreenBlueAlpha);
            }

            TexImage2D(
                Texture2d,
                0,
                InternalFormat.Rgba,
                texture.Width,
                texture.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                texture.Data
            );

            GenerateTextureMipmap(ID);
        }

        public Texture(ImageResult texture, bool mipmap = true, int mipmapLevels = 4)
        {
            ID = GenTexture();
            BindTexture(Texture2d, ID);

            TexParameterf(Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            TexParameterf(Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            TexParameterf(Texture2d, TextureParameterName.TextureMinFilter, mipmap ? (int)TextureMinFilter.NearestMipmapNearest : (int)TextureMinFilter.Nearest);
            TexParameterf(Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

            if (mipmap)
            {
                TexParameterf(Texture2d, TextureParameterName.TextureMaxLevel, mipmapLevels);
            }

            StbImage.stbi_set_flip_vertically_on_load(1);

            TexImage2D(
                Texture2d,
                0,
                InternalFormat.Rgba,
                texture.Width,
                texture.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                texture.Data
            );

            GenerateTextureMipmap(ID);
        }

        public void Bind() => BindTexture(Texture2d, ID);
        public void Unbind() => BindTexture(Texture2d, 0);
        private void Delete()
        {
            if (ID != 0)
            {
                DeleteTexture(ID);
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

        ~Texture() => Dispose(false);
    }
}
