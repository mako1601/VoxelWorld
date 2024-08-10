using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;
using static OpenTK.Graphics.OpenGL.TextureTarget;
using static OpenTK.Graphics.OpenGL.TextureWrapMode;
using static OpenTK.Graphics.OpenGL.TextureMinFilter;
using static OpenTK.Graphics.OpenGL.TextureParameterName;

using StbImageSharp;

namespace VoxelWorld.Graphics
{
    public class TextureArray
    {
        public int ID { get; set; }

        public TextureArray(List<string> filepaths)
        {
            ID = GenTexture();
            BindTexture(Texture2dArray, ID);
            TexParameterf(Texture2dArray, TextureWrapS, (int)Repeat);
            TexParameterf(Texture2dArray, TextureWrapT, (int)Repeat);
            TexParameterf(Texture2dArray, TextureParameterName.TextureMinFilter, (int)NearestMipmapNearest);
            TexParameterf(Texture2dArray, TextureParameterName.TextureMagFilter, (int)Nearest);
            TexParameterf(Texture2dArray, TextureMaxLevel, 4);
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

            TexImage3D(Texture2dArray, 0, InternalFormat.Rgba, textures[0].Width,
                textures[0].Height, 6, 0, PixelFormat.Rgba, PixelType.UnsignedByte, 0);

            for (int i = 0; i < filepaths.Count; i++)
            {
                TexSubImage3D(Texture2dArray, 0, 0, 0, i, textures[0].Width, textures[0].Height, 
                    1, PixelFormat.Rgba, PixelType.UnsignedByte, textures[i].Data);
            }

            GenerateTextureMipmap(ID);
        }

        public void Bind() => BindTexture(Texture2dArray, ID);
        public static void Unbind() => BindTexture(Texture2dArray, 0);
        public void Delete() => DeleteTexture(ID);
    }
}
