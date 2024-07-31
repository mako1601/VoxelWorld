using OpenTK.Graphics.OpenGL4;
using static OpenTK.Graphics.OpenGL4.GL;
using static OpenTK.Graphics.OpenGL4.TextureTarget;
using static OpenTK.Graphics.OpenGL4.TextureWrapMode;
using static OpenTK.Graphics.OpenGL4.TextureMinFilter;
using static OpenTK.Graphics.OpenGL4.TextureParameterName;

using StbImageSharp;

namespace VoxelWorld.Graphics
{
    public class Texture : IGraphicsObject
    {
        public int ID { get; set; }

        public Texture(string filename)
        {
            ID = GenTexture();
            BindTexture(Texture2D, ID);
            TexParameter(Texture2D, TextureWrapS, (int)Repeat);
            TexParameter(Texture2D, TextureWrapT, (int)Repeat);
            TexParameter(Texture2D, TextureParameterName.TextureMinFilter, (int)NearestMipmapNearest);
            TexParameter(Texture2D, TextureParameterName.TextureMagFilter, (int)Nearest);
            TexParameter(Texture2D, TextureMaxLevel, 4);
            StbImage.stbi_set_flip_vertically_on_load(1);
            
            ImageResult texture;

            try
            {
                texture = ImageResult.FromStream(File.OpenRead($"resources/textures/{filename}"),
                    ColorComponents.RedGreenBlueAlpha);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Failed to load texture file '{ex.FileName}'");
                texture = ImageResult.FromStream(File.OpenRead($"resources/textures/utilities/missing_texture.png"),
                    ColorComponents.RedGreenBlueAlpha);
            }

            TexImage2D(Texture2D, 0, PixelInternalFormat.Rgba,
                texture.Width, texture.Height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, texture.Data);

            GenerateTextureMipmap(ID);
        }

        public void Bind() => BindTexture(Texture2D, ID);
        public void Unbind() => BindTexture(Texture2D, 0);
        public void Delete() => DeleteTexture(ID);
    }
}
