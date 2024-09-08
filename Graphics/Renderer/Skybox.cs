using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;
using static OpenTK.Graphics.OpenGL.TextureTarget;
using static OpenTK.Graphics.OpenGL.TextureWrapMode;
using static OpenTK.Graphics.OpenGL.TextureMinFilter;

using StbImageSharp;

using VoxelWorld.Entity;

namespace VoxelWorld.Graphics.Renderer
{
    public class Skybox
    {
        private readonly ShaderProgram _shader;
        private readonly VertexArrayObject _vao;
        private readonly BufferObject<Vector3> _vbo;
        private readonly BufferObject<byte> _ebo;
        private readonly int _texture;

        public Skybox()
        {
            _shader = new ShaderProgram("skybox.glslv", "skybox.glslf");

            _vao = new VertexArrayObject(0);
            _vao.Bind();

            _vbo = new BufferObject<Vector3>(BufferTarget.ArrayBuffer, _skyboxVertices, false);
            _vao.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0);

            _ebo = new BufferObject<byte>(BufferTarget.ElementArrayBuffer, _skyboxIndices, false);

            _texture = GenTexture();
            BindTexture(TextureCubeMap, _texture);

            TexParameterf(TextureCubeMap, TextureParameterName.TextureMinFilter, (int)Linear);
            TexParameterf(TextureCubeMap, TextureParameterName.TextureMagFilter, (int)Linear);
            TexParameterf(TextureCubeMap, TextureParameterName.TextureWrapS, (int)ClampToEdge);
            TexParameterf(TextureCubeMap, TextureParameterName.TextureWrapT, (int)ClampToEdge);
            TexParameterf(TextureCubeMap, TextureParameterName.TextureWrapR, (int)ClampToEdge);

            StbImage.stbi_set_flip_vertically_on_load(0);

            ImageResult texture;
            for (int i = 0; i < _skyboxPaths.Length; i++)
            {
                try
                {
                    texture = ImageResult.FromStream(File.OpenRead($"resources/textures/{_skyboxPaths[i]}"),
                        ColorComponents.RedGreenBlue);
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"[WARNING] Failed to load texture file '{ex.FileName}'");
                    texture = ImageResult.FromStream(File.OpenRead($"resources/textures/utilities/missing_texture.png"),
                        ColorComponents.RedGreenBlue);
                }

                TexImage2D(
                    TextureCubeMapPositiveX + (uint)i,
                    0,
                    InternalFormat.Rgb,
                    texture.Width,
                    texture.Height,
                    0,
                    PixelFormat.Rgb,
                    PixelType.UnsignedByte,
                    texture.Data
                );
            }

            _shader.Use();
            _shader.SetInt("uSkybox", 0);
        }

        public void Draw(Player player)
        {
            DepthFunc(DepthFunction.Lequal);

            _shader.Use();
            _shader.SetMatrix4("uView", new Matrix4(new Matrix3(player.Camera.GetViewMatrix(player.Position))));
            _shader.SetMatrix4("uProjection", player.Camera.GetProjectionMatrix());

            ActiveTexture(TextureUnit.Texture0);
            BindTexture(TextureCubeMap, _texture);
            _vao.Bind();
            DrawElements(PrimitiveType.Triangles, _skyboxIndices.Length, DrawElementsType.UnsignedByte, 0);

            DepthFunc(DepthFunction.Less);
        }

        public void Delete()
        {
            DeleteTexture(_texture);
            _ebo.Dispose();
            _vbo.Dispose();
            _vao.Dispose();
            _shader.Dispose();
        }

        private static readonly string[] _skyboxPaths =
        [
            "skybox/right.png",
            "skybox/left.png",
            "skybox/top.png",
            "skybox/bottom.png",
            "skybox/front.png",
            "skybox/back.png"
        ];
        private static readonly Vector3[] _skyboxVertices =
        [
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
        ];
        private static readonly byte[] _skyboxIndices =
        [
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8,
            12, 13, 14, 14, 15, 12,
            16, 17, 18, 18, 19, 16,
            20, 21, 22, 22, 23, 20
        ];
    }
}
