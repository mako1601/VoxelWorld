using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using static OpenTK.Graphics.OpenGL4.GL;

namespace VoxelWorld.Graphics
{
    public class ShaderProgram : IGraphicsObject
    {
        public int ID { get; set; }

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
        public void Delete() => DeleteProgram(ID);

        public void SetBool(string name, bool value) =>
            Uniform1(GetUniformLocation(ID, name), value ? 1 : 0);
        public void SetInt(string name, int value) =>
            Uniform1(GetUniformLocation(ID, name), value);
        public void SetFloat(string name, float value) =>
            Uniform1(GetUniformLocation(ID, name), value);

        public void SetVector2(string name, Vector2 value) =>
            Uniform2(GetUniformLocation(ID, name), value);
        public void SetVector2(string name, float x, float y) =>
            Uniform2(GetUniformLocation(ID, name), x, y);

        public void SetVector3(string name, Vector3 value) =>
            Uniform3(GetUniformLocation(ID, name), value);
        public void SetVector3(string name, float x, float y, float z) =>
            Uniform3(GetUniformLocation(ID, name), x, y, z);

        public void SetVector4(string name, Vector4 value) =>
            Uniform4(GetUniformLocation(ID, name), value);
        public void SetVector4(string name, float x, float y, float z, float w) =>
            Uniform4(GetUniformLocation(ID, name), x, y, z, w);

        public void SetMatrix2(string name, Matrix2 matrix) =>
            UniformMatrix2(GetUniformLocation(ID, name), false, ref matrix);

        public void SetMatrix3(string name, Matrix3 matrix) =>
            UniformMatrix3(GetUniformLocation(ID, name), false, ref matrix);

        public void SetMatrix4(string name, Matrix4 matrix) =>
            UniformMatrix4(GetUniformLocation(ID, name), false, ref matrix);

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
                GetProgram(shader, GetProgramParameterName.LinkStatus, out var success);

                if (success == 0)
                {
                    GetProgramInfoLog(shader, out var infoLog);
                    Console.WriteLine($"[ERROR] Program linking error: {type}\n{infoLog}");
                }
            }
            else
            {
                GetShader(shader, ShaderParameter.CompileStatus, out var success);

                if (success == 0)
                {
                    GetShaderInfoLog(shader, out var infoLog);
                    Console.WriteLine($"[ERROR] Shader compilation error: {type}\n{infoLog}");
                }
            }
        }
    }
}
