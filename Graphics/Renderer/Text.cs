using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using static OpenTK.Graphics.OpenGL4.GL;

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
        public Dictionary<uint, Character> Characters { get; private set; } = new Dictionary<uint, Character>();

        public Text(uint height)
        {
            Shader = new ShaderProgram("text.glslv", "text.glslf");
            Library library = new Library();
            Face face = new Face(library, "Resources/Fonts/BitmapMc.ttf");
            face.SetPixelSizes(0, height);
            PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            ActiveTexture(TextureUnit.Texture0);

            for (uint c = 0; c < 128; c++)
            {
                try
                {
                    face.LoadChar(c, LoadFlags.Render, LoadTarget.Normal);
                    GlyphSlot glyph = face.Glyph;
                    FTBitmap bitmap = glyph.Bitmap;

                    int texture = GenTexture();
                    BindTexture(TextureTarget.Texture2D, texture);
                    TexImage2D(TextureTarget.Texture2D, 0,
                        PixelInternalFormat.R8, bitmap.Width, bitmap.Rows, 0,
                        PixelFormat.Red, PixelType.UnsignedByte, bitmap.Buffer);

                    TextureParameter(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    TextureParameter(texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    TextureParameter(texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    TextureParameter(texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                    Character character = new Character
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

            BindTexture(TextureTarget.Texture2D, 0);
            PixelStore(PixelStoreParameter.UnpackAlignment, 4);

            VBO = new VBO(_vquad);

            VAO = new VAO();
            VAO.LinkToVAO(0, 2, 4 * 4);
            VAO.LinkToVAO(1, 2, 4 * 4, 2 * 4);

            VBO.Unbind();
            VAO.Unbind();
        }

        public void Delete()
        {
            VBO.Delete();
            VAO.Delete();
            Shader.Delete();
        }

        private static List<float> _vquad = new List<float>
        {
        //   x    y   u   v
            0f, -1f, 0f, 0f,
            0f,  0f, 0f, 1f,
            1f,  0f, 1f, 1f,
            0f, -1f, 0f, 0f,
            1f,  0f, 1f, 1f,
            1f, -1f, 1f, 0f
        };
    }
}