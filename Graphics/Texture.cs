using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;
using static OpenTK.Graphics.OpenGL.TextureTarget;
using static OpenTK.Graphics.OpenGL.TextureWrapMode;
using static OpenTK.Graphics.OpenGL.TextureMinFilter;
using static OpenTK.Graphics.OpenGL.TextureParameterName;

using StbImageSharp;

namespace VoxelWorld.Graphics
{
    public class Texture : IGraphicsObject
    {
        public int ID { get; set; }

        public Texture(string filename)
        {
            ID = GenTexture();
            BindTexture(Texture2d, ID);
            TexParameterf(Texture2d, TextureWrapS, (int)Repeat);
            TexParameterf(Texture2d, TextureWrapT, (int)Repeat);
            TexParameterf(Texture2d, TextureParameterName.TextureMinFilter, (int)NearestMipmapNearest);
            TexParameterf(Texture2d, TextureParameterName.TextureMagFilter, (int)Nearest);
            TexParameterf(Texture2d, TextureMaxLevel, 4);
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

            TexImage2D(Texture2d, 0, InternalFormat.Rgba,
                texture.Width, texture.Height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, texture.Data);

            GenerateTextureMipmap(ID);
        }

        public void Bind() => BindTexture(Texture2d, ID);
        public void Unbind() => BindTexture(Texture2d, 0);
        public void Delete() => DeleteTexture(ID);
    }
}
