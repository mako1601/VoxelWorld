using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace VoxelWorld.Graphics
{
    public class ShaderProgram : IDisposable
    {
        public int ID { get; private set; }

        public ShaderProgram(string vertexShaderFilename, string fragmentShaderFilename)
        {
            ID = GL.CreateProgram();

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, LoadShaderSource(vertexShaderFilename));
            GL.CompileShader(vertexShader);
            CheckCompileErrors(vertexShader, "VERTEX");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadShaderSource(fragmentShaderFilename));
            GL.CompileShader(fragmentShader);
            CheckCompileErrors(fragmentShader, "FRAGMENT");

            GL.AttachShader(ID, vertexShader);
            GL.AttachShader(ID, fragmentShader);

            GL.LinkProgram(ID);
            CheckCompileErrors(ID, "PROGRAM");

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        public void Use() => GL.UseProgram(ID);
        public void Dispose() => GL.DeleteProgram(ID);

        public uint GetAttribLocation(string name) => (uint)GL.GetAttribLocation(ID, name);

        public void SetBool(string name, bool value) =>
            GL.Uniform1i(GL.GetUniformLocation(ID, name), value ? 1 : 0);
        public void SetInt(string name, int value) =>
            GL.Uniform1i(GL.GetUniformLocation(ID, name), value);
        public void SetFloat(string name, float value) =>
            GL.Uniform1f(GL.GetUniformLocation(ID, name), value);

        public void SetVector2(string name, Vector2 value) =>
            GL.Uniform2f(GL.GetUniformLocation(ID, name), value.X, value.Y);
        public void SetVector2(string name, float x, float y) =>
            GL.Uniform2f(GL.GetUniformLocation(ID, name), x, y);

        public void SetVector3(string name, Vector3 value) =>
            GL.Uniform3f(GL.GetUniformLocation(ID, name), value.X, value.Y, value.Z);
        public void SetVector3(string name, Color3<Rgb> value) =>
            GL.Uniform3f(GL.GetUniformLocation(ID, name), value.X, value.Y, value.Z);
        public void SetVector3(string name, float x, float y, float z) =>
            GL.Uniform3f(GL.GetUniformLocation(ID, name), x, y, z);

        public void SetVector4(string name, Vector4 value) =>
            GL.Uniform4f(GL.GetUniformLocation(ID, name), value.X, value.Y, value.Z, value.W);
        public void SetVector4(string name, Color4<Rgba> value) =>
            GL.Uniform4f(GL.GetUniformLocation(ID, name), value.X, value.Y, value.Z, value.W);
        public void SetVector4(string name, float x, float y, float z, float w) =>
            GL.Uniform4f(GL.GetUniformLocation(ID, name), x, y, z, w);

        public void SetMatrix2(string name, Matrix2 matrix) =>
            GL.UniformMatrix2f(GL.GetUniformLocation(ID, name), 1, false, matrix);

        public void SetMatrix3(string name, Matrix3 matrix) =>
            GL.UniformMatrix3f(GL.GetUniformLocation(ID, name), 1, false, matrix);

        public void SetMatrix4(string name, Matrix4 matrix) =>
            GL.UniformMatrix4f(GL.GetUniformLocation(ID, name), 1, false, matrix);

        private static string LoadShaderSource(string filePath)
        {
            try
            {
                using var sr = new StreamReader($"resources/shaders/{filePath}");
                string shaderSource = sr.ReadToEnd();
                Console.WriteLine($"[INFO] The shader source file 'resources/shaders/{filePath}' was loaded successfully");
                return shaderSource;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"[WARNING] Failed to load shader source file '{ex.FileName}'");
                return string.Empty;
            }
        }

        private static void CheckCompileErrors(int shader, string type)
        {
            if (type.Equals("PROGRAM"))
            {
                GL.GetProgrami(shader, ProgramProperty.LinkStatus, out var success);

                if (success == 0)
                {
                    GL.GetProgramInfoLog(shader, out var infoLog);
                    Console.WriteLine($"[ERROR] Program linking error: {type}\n{infoLog}");
                }
            }
            else
            {
                GL.GetShaderi(shader, ShaderParameterName.CompileStatus, out var success);

                if (success == 0)
                {
                    GL.GetShaderInfoLog(shader, out var infoLog);
                    Console.WriteLine($"[ERROR] Shader compilation error: {type}\n{infoLog}");
                }
            }
        }
    }
}
