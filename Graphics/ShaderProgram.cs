using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

namespace VoxelWorld.Graphics
{
    public class ShaderProgram : IGraphicsObject, IDisposable
    {
        private bool _disposed = false;
        public int ID { get; private set; }

        public ShaderProgram(string vertexShaderFilename, string fragmentShaderFilename)
        {
            ID = CreateProgram();

            int vertexShader = CreateShader(ShaderType.VertexShader);
            ShaderSource(vertexShader, LoadShaderSource(vertexShaderFilename));
            CompileShader(vertexShader);
            CheckCompileErrors(vertexShader, "VERTEX");

            int fragmentShader = CreateShader(ShaderType.FragmentShader);
            ShaderSource(fragmentShader, LoadShaderSource(fragmentShaderFilename));
            CompileShader(fragmentShader);
            CheckCompileErrors(fragmentShader, "FRAGMENT");

            AttachShader(ID, vertexShader);
            AttachShader(ID, fragmentShader);

            LinkProgram(ID);
            CheckCompileErrors(ID, "PROGRAM");

            DeleteShader(vertexShader);
            DeleteShader(fragmentShader);
        }

        public void Bind() => UseProgram(ID);
        public void Unbind() => UseProgram(0);
        private void Delete()
        {
            if (ID != 0)
            {
                DeleteProgram(ID);
                ID = 0;
            }
        }

        public void SetBool(string name, bool value) =>
            Uniform1i(GetUniformLocation(ID, name), value ? 1 : 0);
        public void SetInt(string name, int value) =>
            Uniform1i(GetUniformLocation(ID, name), value);
        public void SetFloat(string name, float value) =>
            Uniform1f(GetUniformLocation(ID, name), value);

        public void SetVector2(string name, Vector2 value) =>
            Uniform2f(GetUniformLocation(ID, name), value.X, value.Y);
        public void SetVector2(string name, float x, float y) =>
            Uniform2f(GetUniformLocation(ID, name), x, y);

        public void SetVector3(string name, Vector3 value) =>
            Uniform3f(GetUniformLocation(ID, name), value.X, value.Y, value.Z);
        public void SetVector3(string name, Color3<Rgb> value) =>
            Uniform3f(GetUniformLocation(ID, name), value.X, value.Y, value.Z);
        public void SetVector3(string name, float x, float y, float z) =>
            Uniform3f(GetUniformLocation(ID, name), x, y, z);

        public void SetVector4(string name, Vector4 value) =>
            Uniform4f(GetUniformLocation(ID, name), value.X, value.Y, value.Z, value.W);
        public void SetVector4(string name, Color4<Rgba> value) =>
            Uniform4f(GetUniformLocation(ID, name), value.X, value.Y, value.Z, value.W);
        public void SetVector4(string name, float x, float y, float z, float w) =>
            Uniform4f(GetUniformLocation(ID, name), x, y, z, w);

        public void SetMatrix2(string name, Matrix2 matrix) =>
            UniformMatrix2f(GetUniformLocation(ID, name), 1, false, matrix);

        public void SetMatrix3(string name, Matrix3 matrix) =>
            UniformMatrix3f(GetUniformLocation(ID, name), 1, false, matrix);

        public void SetMatrix4(string name, Matrix4 matrix) =>
            UniformMatrix4f(GetUniformLocation(ID, name), 1, false, matrix);

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
                GetProgrami(shader, ProgramProperty.LinkStatus, out var success);

                if (success == 0)
                {
                    GetProgramInfoLog(shader, out var infoLog);
                    Console.WriteLine($"[ERROR] Program linking error: {type}\n{infoLog}");
                }
            }
            else
            {
                GetShaderi(shader, ShaderParameterName.CompileStatus, out var success);

                if (success == 0)
                {
                    GetShaderInfoLog(shader, out var infoLog);
                    Console.WriteLine($"[ERROR] Shader compilation error: {type}\n{infoLog}");
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
            }

            Delete();

            _disposed = true;
        }

        ~ShaderProgram() => Dispose(false);
    }
}
