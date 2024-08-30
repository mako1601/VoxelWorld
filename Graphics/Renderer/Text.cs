using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using SharpFont;

namespace VoxelWorld.Graphics.Renderer
{
    public class Text
    {
        public struct Character
        {
            public int TextureID { get; set; }
            public Vector2 Size { get; set; }
            public Vector2 Bearing { get; set; }
            public int Advance { get; set; }
        }

        public ShaderProgram Shader { get; private set; }
        public VAO VAO { get; private set; }
        public VBO VBO { get; private set; }
        public Dictionary<uint, Character> Characters { get; private set; } = [];

        public Text(uint height)
        {
            Shader = new ShaderProgram("text.glslv", "text.glslf");
            var library = new Library();
            var face = new Face(library, "Resources/Fonts/BitmapMc.ttf");
            face.SetPixelSizes(0, height);
            PixelStoref(PixelStoreParameter.UnpackAlignment, 1);
            ActiveTexture(TextureUnit.Texture0);

            for (uint c = 0; c < 128; c++)
            {
                try
                {
                    face.LoadChar(c, LoadFlags.Render, LoadTarget.Normal);
                    GlyphSlot glyph = face.Glyph;
                    FTBitmap bitmap = glyph.Bitmap;

                    int texture = GenTexture();
                    BindTexture(TextureTarget.Texture2d, texture);
                    TexImage2D(TextureTarget.Texture2d, 0,
                        InternalFormat.R8, bitmap.Width, bitmap.Rows, 0,
                        PixelFormat.Red, PixelType.UnsignedByte, bitmap.Buffer);

                    TextureParameterf(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    TextureParameterf(texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    TextureParameterf(texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    TextureParameterf(texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                    var character = new Character
                    {
                        TextureID = texture,
                        Size = new Vector2(bitmap.Width, bitmap.Rows),
                        Bearing = new Vector2(glyph.BitmapLeft, glyph.BitmapTop),
                        Advance = glyph.Advance.X.Value
                    };
                    Characters.Add(c, character);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            BindTexture(TextureTarget.Texture2d, 0);
            PixelStoref(PixelStoreParameter.UnpackAlignment, 4);

            VBO = new VBO(_vquad);

            VAO = new VAO();
            VAO.LinkToVAO(0, 2, 4 * 4);
            VAO.LinkToVAO(1, 2, 4 * 4, 2 * 4);

            VBO.Unbind();
            VAO.Unbind();
        }

        public void Delete()
        {
            VBO.Dispose();
            VAO.Dispose();
            Shader.Dispose();
        }

        private static readonly List<float> _vquad =
        [
        //   x    y   u   v
            0f, -1f, 0f, 0f,
            0f,  0f, 0f, 1f,
            1f,  0f, 1f, 1f,
            0f, -1f, 0f, 0f,
            1f,  0f, 1f, 1f,
            1f, -1f, 1f, 0f
        ];
    }
}