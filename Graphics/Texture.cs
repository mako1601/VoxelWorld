using OpenTK.Graphics.OpenGL;

using StbImageSharp;

namespace VoxelWorld.Graphics
{
    public class Texture : IDisposable
    {
        public int ID { get; private set; }

        public Texture(string filename, bool mipmap = true, int mipmapLevels = 4)
        {
            ID = GL.GenTexture();
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, ID);

            StbImage.stbi_set_flip_vertically_on_load(1);

            ImageResult texture;
            try
            {
                texture = ImageResult.FromStream(File.OpenRead($"resources/textures/{filename}"), ColorComponents.RedGreenBlueAlpha);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"[WARNING] Failed to load texture file '{ex.FileName}'");
                texture = ImageResult.FromStream(File.OpenRead($"resources/textures/utilities/missing_texture.png"), ColorComponents.RedGreenBlueAlpha);
            }

            GL.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba,
                texture.Width,
                texture.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                texture.Data
            );

            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, mipmap ? (int)TextureMinFilter.NearestMipmapNearest : (int)TextureMinFilter.Nearest);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

            if (mipmap)
            {
                GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, mipmapLevels);
                GL.GenerateTextureMipmap(ID);
            }
        }

        public Texture(ImageResult texture, bool mipmap = true, int mipmapLevels = 4)
        {
            ID = GL.GenTexture();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, ID);

            GL.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba,
                texture.Width,
                texture.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                texture.Data
            );

            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, mipmap ? (int)TextureMinFilter.NearestMipmapNearest : (int)TextureMinFilter.Nearest);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

            if (mipmap)
            {
                GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, mipmapLevels);
                GL.GenerateTextureMipmap(ID);
            }
        }

        public void Use(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2d, ID);
        }
        public void Dispose() => GL.DeleteTexture(ID);
    }
}
