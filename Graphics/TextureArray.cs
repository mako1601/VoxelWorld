using OpenTK.Graphics.OpenGL4;
using static OpenTK.Graphics.OpenGL4.GL;
using static OpenTK.Graphics.OpenGL4.TextureTarget;
using static OpenTK.Graphics.OpenGL4.TextureWrapMode;
using static OpenTK.Graphics.OpenGL4.TextureMinFilter;
using static OpenTK.Graphics.OpenGL4.TextureParameterName;

using StbImageSharp;

namespace VoxelWorld.Graphics
{
    public class TextureArray
    {
        public int ID { get; set; }

        public TextureArray(List<string> filepaths)
        {
            ID = GenTexture();
            BindTexture(Texture2DArray, ID);
            TexParameter(Texture2DArray, TextureWrapS, (int)Repeat);
            TexParameter(Texture2DArray, TextureWrapT, (int)Repeat);
            TexParameter(Texture2DArray, TextureParameterName.TextureMinFilter, (int)NearestMipmapNearest);
            TexParameter(Texture2DArray, TextureParameterName.TextureMagFilter, (int)Nearest);
            TexParameter(Texture2DArray, TextureMaxLevel, 4);
            StbImage.stbi_set_flip_vertically_on_load(1);

            var textures = new List<ImageResult>();

            for (int i = 0; i < filepaths.Count; i++)
            {
                try
                {
                    textures.Add(ImageResult.FromStream(File.OpenRead($"resources/textures/{filepaths[i]}"),
                        ColorComponents.RedGreenBlueAlpha));
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"[WARNING] Failed to load texture file '{ex.FileName}'");
                    textures.Add(ImageResult.FromStream(File.OpenRead($"resources/textures/utilities/missing_texture.png"),
                        ColorComponents.RedGreenBlueAlpha));
                }
            }

            TexImage3D(Texture2DArray, 0, PixelInternalFormat.Rgba, textures[0].Width,
                textures[0].Height, 256, 0, PixelFormat.Rgba, PixelType.UnsignedByte, 0);

            for (int i = 0; i < filepaths.Count; i++)
            {
                TexSubImage3D(Texture2DArray, 0, 0, 0, i, textures[0].Width, textures[0].Height, 
                    1, PixelFormat.Rgba, PixelType.UnsignedByte, textures[i].Data);
            }

            GenerateTextureMipmap(ID);
        }

        public void Bind() => BindTexture(Texture2DArray, ID);
        public static void Unbind() => BindTexture(Texture2DArray, 0);
        public void Delete() => DeleteTexture(ID);
    }
}
