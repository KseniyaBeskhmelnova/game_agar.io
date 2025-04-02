using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using System.Drawing;
using System.Reflection;
using static System.Formats.Asn1.AsnWriter;
using System.Reflection.Metadata;


namespace game_agar.io
{
    internal class Game : GameWindow
    {

        public List<Vector3> vertices = new List<Vector3>();
        public List<uint> indices = new List<uint>();
        public List<Vector2> texCoords = new List<Vector2>();

        int width, height;

        int shaderProgram;
        int vao;
        int vbo;
        int ebo;
        int textureVBO;
        int textureID;

        Matrix4 rotate = Matrix4.Identity;

        static float sphereRad = 0.1f;
        float sphereZoom = 2.0f;
        float sphereZoomMax = 5.0f;
        float sphereZoomStep = 0.1f;
        float sphereIntersectAnimTimer = 0.0f;
        static float sphereRotStep = 0.05f;

        static int sphereListCount = 30;
        public Vector3[] sphereList = new Vector3[sphereListCount];

        float sphereListDx = 0;
        float sphereListDy = 0;
        float sphereListDxDyStep = 0.00025f;

        static float sphereListRepeatDist = 8.0f;

        Random rnd = new Random();

        void createSphereList()
        {
            for (int i = 0; i < sphereListCount; i++)
            {
                sphereList[i] = new Vector3(sphereListRepeatDist * (float)rnd.NextDouble(), sphereListRepeatDist * (float)rnd.NextDouble(), 0.0f);
            }
        }

        void createSphere()
        {
            uint ix, iy;
            double x, y, z;
            float piDiv180 = 3.14f / 180.0f;
            float piDiv360 = 3.14f / 360.0f;
            float a, b;
            uint nx = 16;
            uint ny = 16;
            for (iy = 0; iy <= ny; iy++)
            {
                b = iy * 360.0f / ny;
                for (ix = 0; ix <= nx; ix++)
                {
                    a = ix * 360.0f / nx;
                    // calc vertices
                    x = sphereRad * Math.Cos(a * piDiv180) * Math.Sin(b * piDiv360);
                    y = sphereRad * Math.Sin(a * piDiv180) * Math.Sin(b * piDiv360);
                    z = sphereRad * Math.Cos(b * piDiv360);
                    this.vertices.Add(new Vector3((float)x, (float)y, (float)z));
                    // calc indices
                    this.indices.Add(iy * (nx + 1) + ix);
                    this.indices.Add(iy * (nx + 1) + ix + 1);
                    this.indices.Add((iy + 1) * (nx + 1) + ix + 1);
                    this.indices.Add(iy * (nx + 1) + ix);
                    this.indices.Add((iy + 1) * (nx + 1) + ix + 1);
                    this.indices.Add((iy + 1) * (nx + 1) + ix);
                    // calc texture
                    this.texCoords.Add(new Vector2((float)ix / nx, (float)iy / ny));
                }
            }
        }

        public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.width = width;
            this.height = height;
            CenterWindow(new Vector2i(width, height));
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            this.width = e.Width;
            this.height = e.Height;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            createSphere();
            createSphereList();

            shaderProgram = GL.CreateProgram();

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, LoadShaderSource("Default.vert"));
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadShaderSource("Default.frag"));
            GL.CompileShader(fragmentShader);

            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);

            GL.LinkProgram(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);


            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes, vertices.ToArray(), BufferUsageHint.StaticDraw);
            var vertexLocation = GL.GetAttribLocation(shaderProgram, "aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 0, 0);

            textureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector2.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
            var texCoordLocation = GL.GetAttribLocation(shaderProgram, "aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 0, 0);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            //Textures
            textureID = GL.GenTexture();
            //activate the texture in unit
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            //texture parametrs
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            //load image
            StbImage.stbi_set_flip_vertically_on_load(1);
            ImageResult dirtTexture = ImageResult.FromStream(File.OpenRead("../../../Textures/dirtTexture.png"), ColorComponents.RedGreenBlueAlpha);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, dirtTexture.Width, dirtTexture.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, dirtTexture.Data);

            GL.Enable(EnableCap.DepthTest);

        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
            GL.DeleteTexture(textureID);
            GL.DeleteProgram(shaderProgram);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(shaderProgram);

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = Matrix4.Identity;
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60.0f), (float)width/(float)height, 0.1f, 100.0f);

            model *= Matrix4.CreateScale(sphereZoom + 0.2f * (float)Math.Sin(Math.PI * sphereIntersectAnimTimer / 90.0f));
            model *= rotate;
            model *= Matrix4.CreateTranslation(0f, 0f, -3f);

            int modellocation = GL.GetUniformLocation(shaderProgram, "model");
            int viewlocation = GL.GetUniformLocation(shaderProgram, "view");
            int projectionlocation = GL.GetUniformLocation(shaderProgram, "projection");

            GL.UniformMatrix4(modellocation, true, ref model);
            GL.UniformMatrix4(viewlocation, true, ref view);
            GL.UniformMatrix4(projectionlocation, true, ref projection);

            GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);

            if (sphereIntersectAnimTimer < 360.0f)
            {
                sphereIntersectAnimTimer += 0.125f;
            }

            float intersectDist = sphereRad + sphereRad * sphereZoom;
            for (int i = 0; i < sphereListCount; i++)
            {
                if (sphereList[i].Z < 1.0f)
                {
                    sphereList[i].Z += 0.0005f;
                }
                float x = (sphereListDx + sphereList[i].X) % sphereListRepeatDist - (sphereListRepeatDist / 2.0f);
                float y = (sphereListDy + sphereList[i].Y) % sphereListRepeatDist - (sphereListRepeatDist / 2.0f);
                model = Matrix4.CreateScale(sphereList[i].Z);
                model *= Matrix4.CreateTranslation(x, y, -3f);
                GL.UniformMatrix4(modellocation, true, ref model);
                GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
                double dist = Math.Sqrt(x * x + y * y);
                if (dist < intersectDist) {
                    if (sphereZoom < sphereZoomMax)
                    {
                        sphereZoom += sphereZoomStep;
                    }
                    sphereIntersectAnimTimer = 0.0f;
                    sphereList[i].X = sphereListRepeatDist * (float)rnd.NextDouble();
                    sphereList[i].Y = sphereListRepeatDist * (float)rnd.NextDouble();
                    sphereList[i].Z = 0.0f;
                }
            }

            Context.SwapBuffers();
            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            int minDistIndex = 0;
            double minDist = 99999;
            for (int i = 0; i < sphereListCount; i++)
            {
                float x = (sphereListDx + sphereList[i].X) % sphereListRepeatDist - (sphereListRepeatDist / 2.0f);
                float y = (sphereListDy + sphereList[i].Y) % sphereListRepeatDist - (sphereListRepeatDist / 2.0f);
                double dist = Math.Sqrt(x * x + y * y);
                if (dist < minDist)
                {
                    minDist = dist;
                    minDistIndex = i;
                }
            }
            float xx = (sphereListDx + sphereList[minDistIndex].X) % sphereListRepeatDist - (sphereListRepeatDist / 2.0f);
            float yy = (sphereListDy + sphereList[minDistIndex].Y) % sphereListRepeatDist - (sphereListRepeatDist / 2.0f);

            if (xx != 0)
            {
                sphereListDx += (xx < 0) ? +sphereListDxDyStep : -sphereListDxDyStep;
                if (sphereListDx >= sphereListRepeatDist)
                {
                    sphereListDx -= sphereListRepeatDist;
                }
                if (sphereListDx < 0.0f)
                {
                    sphereListDx += sphereListRepeatDist;
                }
                Matrix4 rotateLocal = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-sphereRotStep));
                rotate *= rotateLocal;
            }

            if (yy != 0)
            {
                sphereListDy += (yy < 0) ? +sphereListDxDyStep : -sphereListDxDyStep;
                if (sphereListDy >= sphereListRepeatDist)
                {
                    sphereListDy -= sphereListRepeatDist;
                }
                if (sphereListDy < 0.0f)
                {
                    sphereListDy += sphereListRepeatDist;
                }
                Matrix4 rotateLocal = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(sphereRotStep));
                rotate *= rotateLocal;
            }
            base.OnUpdateFrame(e);
        }

        //Function to load a text file and return its contests as a string
        public static string LoadShaderSource(string filePath)
        {
            string shaderSource = "";
            try
            {
                using (StreamReader reader = new StreamReader("../../../Shaders/" + filePath))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load shader source file: " + e.Message);
            }
            return shaderSource;
        }

    }
}
