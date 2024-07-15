using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using static OpenTK.Graphics.OpenGL4.GL;
using static OpenTK.Graphics.OpenGL4.TextureTarget;
using static OpenTK.Graphics.OpenGL4.TextureWrapMode;
using static OpenTK.Graphics.OpenGL4.TextureMinFilter;

using StbImageSharp;

using VoxelWorld.Window;

namespace VoxelWorld.Graphics.Renderer
{
    public class Skybox
    {
        private readonly ShaderProgram _shader;
        private readonly VAO _vao;
        private readonly VBO _vbo;
        private readonly EBO _ebo;
        private readonly int _texture;

        public Skybox()
        {
            _shader = new ShaderProgram("skybox.glslv", "skybox.glslf");

            _vao = new VAO();
            _vbo = new VBO(_skyboxVertices);
            VAO.LinkToVAO(0, 3);
            _ebo = new EBO(_skyboxIndices);

            _texture = GenTexture();
            BindTexture(TextureCubeMap, _texture);
            TexParameter(TextureCubeMap, TextureParameterName.TextureMinFilter, (int)Linear);
            TexParameter(TextureCubeMap, TextureParameterName.TextureMagFilter, (int)Linear);
            TexParameter(TextureCubeMap, TextureParameterName.TextureWrapS, (int)ClampToEdge);
            TexParameter(TextureCubeMap, TextureParameterName.TextureWrapT, (int)ClampToEdge);
            TexParameter(TextureCubeMap, TextureParameterName.TextureWrapR, (int)ClampToEdge);
            StbImage.stbi_set_flip_vertically_on_load(0);

            ImageResult texture = new ImageResult();

            for (int i = 0; i < _skyboxPaths.Count; i++)
            {
                try
                {
                    texture = ImageResult.FromStream(File.OpenRead($"resources/textures/{_skyboxPaths[i]}"),
                        ColorComponents.RedGreenBlue);
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"Failed to load texture file '{ex.FileName}'");
                }

                TexImage2D(TextureCubeMapPositiveX + i, 0,
                    PixelInternalFormat.Rgb, texture.Width, texture.Height,
                    0, PixelFormat.Rgb, PixelType.UnsignedByte, texture.Data);
            }

            _shader.Bind();
            _shader.SetInt("skybox", 0);
        }

        public void Draw(Matrixes matrix)
        {
            DepthFunc(DepthFunction.Lequal);

            _shader.Bind();
            _shader.SetMatrix4("view", new Matrix4(new Matrix3(matrix.View)));
            _shader.SetMatrix4("projection", matrix.Projection);

            ActiveTexture(TextureUnit.Texture0);
            BindTexture(TextureCubeMap, _texture);

            _vao.Bind();
            DrawElements(PrimitiveType.Triangles, _skyboxIndices.Count, DrawElementsType.UnsignedInt, 0);

            DepthFunc(DepthFunction.Less);
        }

        public void Delete()
        {
            DeleteTexture(_texture);
            _ebo.Delete();
            _vbo.Delete();
            _vao.Delete();
            _shader.Delete();
        }

        private static readonly List<string> _skyboxPaths = new List<string>
        {
            "skybox/right.png",
            "skybox/left.png",
            "skybox/top.png",
            "skybox/bottom.png",
            "skybox/front.png",
            "skybox/back.png"
        };
        private static readonly List<Vector3> _skyboxVertices = new List<Vector3>
        {
            (-1f, -1f,  1f),
            ( 1f, -1f,  1f),
            ( 1f,  1f,  1f),
            (-1f,  1f,  1f),

            (-1f, -1f, -1f),
            (-1f,  1f, -1f),
            ( 1f,  1f, -1f),
            ( 1f, -1f, -1f),

            (-1f, -1f, -1f),
            (-1f, -1f,  1f),
            (-1f,  1f,  1f),
            (-1f,  1f, -1f),

            ( 1f, -1f, -1f),
            ( 1f,  1f, -1f),
            ( 1f,  1f,  1f),
            ( 1f, -1f,  1f),

            (-1f,  1f, -1f),
            (-1f,  1f,  1f),
            ( 1f,  1f,  1f),
            ( 1f,  1f, -1f),

            (-1f, -1f, -1f),
            ( 1f, -1f, -1f),
            ( 1f, -1f,  1f),
            (-1f, -1f,  1f)
        };
        private static readonly List<uint> _skyboxIndices = new List<uint>
        {
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8,
            12, 13, 14, 14, 15, 12,
            16, 17, 18, 18, 19, 16,
            20, 21, 22, 22, 23, 20
        };
    }
}
