﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Winecrash.Engine;
using System.Threading;
using OpenTK;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

using Winecrash.Engine.GUI;

namespace Winecrash.Client
{
    static class Program
    {
        static void Main()
        {
            Task.Run(CreateDebugWindow);

            WEngine.Run();

            Viewport.OnLoaded += Start;
        }

        static void Start()
        {
            new Shader("assets/shaders/cursor/Cursor.vert", "assets/shaders/cursor/Cursor.frag");

            Input.LockMode = CursorLockModes.Lock;
            Input.CursorVisible = false;

            Input.MouseSensivity *= 5.0F;

            Physics.Gravity = new Vector3D(0, -27, 0);

            Viewport.Instance.VSync = OpenTK.VSyncMode.Off;

            WObject playerWobj = new WObject("Player");
            RigidBody rb = playerWobj.AddModule<RigidBody>();
            rb.UseGravity = false;
            BoxCollider bc = playerWobj.AddModule<BoxCollider>();

            bc.Extents = new Vector3D(0.4F, 0.9F, 0.4F);

            playerWobj.AddModule<Player>();

            Camera.Main.WObject.AddModule<FreeCam>();
            Camera.Main.RenderLayers &= ~(1L << 32);
            Camera.Main.RenderLayers &= ~(1L << 48);

            Camera.Main._FarClip = 1000.0F;
            Camera.Main.FOV = 80.0F;

            playerWobj.Position = Vector3F.Up * 80F;

            Database db = Database.Load("assets/items/items.json");

            db.ParseItems();

            new Shader("assets/shaders/chunk/Chunk.vert", "assets/shaders/chunk/Chunk.frag");
            new Shader("assets/shaders/itemUnlit/ItemUnlit.vert", "assets/shaders/itemUnlit/ItemUnlit.frag");

            Chunk.ChunkTexture = ItemCache.BuildChunkTexture(out int xsize, out int ysize);
            Chunk.TexWidth = xsize;
            Chunk.TexHeight = ysize;
            CreateSkybox();

            WObject worldwobj = new WObject("World");
            worldwobj.AddModule<World>();


            new WObject("Debug").AddModule<DebugMenu>();
            new WObject("EscapeMenu").AddModule<EscapeMenu>();

            WObject crosshair = new WObject("Crosshair");
            crosshair.Parent = Engine.GUI.Canvas.Main.WObject;
            Engine.GUI.Image reticule = crosshair.AddModule<Engine.GUI.Image>();
            reticule.Picture = new Texture("assets/textures/crosshair.png");
            reticule.KeepRatio = true;

            reticule.MinAnchor = new Vector2F(0.48F, 0.48F);
            reticule.MaxAnchor = new Vector2F(0.52F, 0.52F);

            reticule.MaxScale = Vector3F.One * 30.0F;

            WObject itembar = new WObject("Item Bar");
            itembar.Parent = Canvas.Main.WObject;
            Image bar = itembar.AddModule<Image>();
            bar.Picture = new Texture("assets/textures/itembar.png");
            bar.KeepRatio = true;
            bar.MinAnchor = new Vector2F(0.35F, 0.05F);
            bar.MaxAnchor = new Vector2F(0.65F, 0.2F);
            bar.Color = new Color256(1.0, 1.0, 1.0, 0.8F);
            bar.MinScale = new Vector3F(500.0F, -float.MaxValue, -float.MaxValue);
            bar.MaxScale = new Vector3F(750.0F, float.MaxValue, float.MaxValue);


            Mesh mesh = Mesh.LoadFile("assets/models/ItemCube.obj", MeshFormats.Wavefront); ;
            WObject cube = new WObject("Cube");
            cube.Parent = itembar;
            Model model = cube.AddModule<Model>();
            Material mat = new Material(Shader.Find("ItemUnlit"));
            mat.SetData<Vector2>("offset", new Vector2D(0, ((double)ItemCache.GetIndex("winecrash:stone") / ItemCache.TotalItems)));
            mat.SetData<Vector2>("tiling", new Vector2D(1.0, 1.0/ ItemCache.TotalItems));
            mat.SetData<Vector3>("lightDir", new Vector3D(0.8,1.0,-0.6));
            mat.SetData<Vector4>("ambiant", new Color256(0.0, 0.0, 0.0, 1.0));
            mat.SetData<Vector4>("lightColor", new Color256(1.75, 1.75, 1.75, 1.0));

            model.Renderer.Mesh = mesh;
            model.Renderer.Material = mat;
            model.KeepRatio = true;

            cube.Rotation = new Engine.Quaternion(-20, 45, -20);

            model.MinAnchor = new Vector2F(0.025F, 0.0F);
            model.MaxAnchor = new Vector2F(0.075F, 1.0F);

            mat.SetData<Texture>("albedo", Texture.Find("Cache"));
            mat.SetData<Vector4>("color", Color256.White);
        }

        static Color256 HorizonColourDay = new Color256(0.82D, 0.92D, 0.98D, 1.0D);
        static Color256 HorizonColourSunset = new Color256(1.0D, 0.48D, 0.0D, 1.0D);
        static Color256 NightColour = new Color256(0.0D, 0.0D, 0.0D, 1.0D);
        static Color256 HighAtmosphereColour = new Color256(0.23D, 0.41D, 0.70D, 1.0D);
        static Color256 GroundAtmosphereColour = new Color256(0.58D, 0.53D, 0.45D, 1.0D);

        static void CreateSkybox()
        {
            new Shader("assets/shaders/skybox/Skybox.vert", "assets/shaders/skybox/Skybox.frag");

            WObject sky = new WObject("Skybox");
            MeshRenderer mr = sky.AddModule<MeshRenderer>();

            sky.AddModule<DayNightCycle>();

            mr.Mesh = Mesh.LoadFile("assets/models/Skysphere.obj", MeshFormats.Wavefront);
            mr.UseMask = false;

            mr.Material = new Material(Shader.Find("Skybox"));
            mr.Material.SetData<Vector4>("horizonColourDay", HorizonColourDay);
            mr.Material.SetData<Vector4>("horizonColourSunset", HorizonColourSunset);
            mr.Material.SetData<Vector4>("nightColour", NightColour);
            mr.Material.SetData<Vector4>("highAtmosphereColour", HighAtmosphereColour);
            mr.Material.SetData<Vector4>("groundAtmosphereColour", GroundAtmosphereColour);

            mr.Material.SetData<float>("sunSize", 0.1F);



            mr.Material.SetData<Vector4>("sunInnerColor", new Color256(1.0D, 1.0D, 1.0D, 1.0D));
            mr.Material.SetData<Vector4>("sunOuterColor", new Color256(245D / 255D, 234D / 255D, 181D / 255D, 1.0D));

            sky.Layer = 1L << 32;

            WObject sun = new WObject("Sun");
            sun.AddModule<DirectionalLight>();
            

            SkyboxCamera skycam = new WObject("Skybox Camera").AddModule<SkyboxCamera>();
            skycam.ReferenceCamera = Camera.Main;
        }

        static void CreateDebugWindow()
        {
            Debug.AddLogger(new Logger(LogVerbose, LogWarning, LogError, LogException));
            Debug.Log("Winecrash Predev 0.2 - (C) Arthur Carré 2020");
        }

        static void LogVerbose(object msg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(msg.ToString());
        }

        static void LogWarning(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg.ToString());
        }

        static void LogError(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg.ToString());
        }

        static void LogException(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg.ToString());
        }
    }
}
