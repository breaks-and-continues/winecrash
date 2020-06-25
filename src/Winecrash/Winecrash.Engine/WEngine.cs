﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

namespace Winecrash.Engine
{
    public static class WEngine
    {
        public static OSPlatform OS { get; private set; }

        private static OSPlatform[] SupportedOS = new OSPlatform[] { OSPlatform.Windows, OSPlatform.Linux };

        public delegate void StopDelegate();
        public static StopDelegate OnStop;

        public static WObject EngineObject;

        public static void TraceLayers()
        {
            Debug.Log(Layer.GetTrace());
        }

        public static Thread Run()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            Thread winThread = Viewport.ThreadRunner = new Thread(ShowWindow)
            {
                IsBackground = false,
                Priority = ThreadPriority.Highest
            };
            winThread.Start();

            Load();

            return winThread;
        }

        private static void ShowWindow()
        {
            Icon icon = null;

            try
            {
               icon = new Icon("icon.ico");
            }

            catch (Exception e)
            {
                MessageBox.Show("Unable to load icon : " + e.Message);
            }

            finally
            {
                using (Viewport vp = new Viewport(1024, 1024, "Winecraft Viewport", icon))
                {
                    vp.Run();
                }
            }
        }

        public static void Load()
        {
            Initializer.InitializeEngine();
        }
        internal static void Stop(object sender)
        {
            try
            {
                OnStop?.Invoke();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message + "\n" + e.StackTrace, "Error when stopping engine", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (!(sender is Viewport))
            {
                Viewport.ThreadRunner?.Abort();
            }

            foreach(Layer layer in Layer._Layers)
            {
                foreach(Group group in layer._Groups)
                {
                    group.Thread.Abort();
                }
            }

            //Updater.UpdateThread?.Abort();
            //Updater.FixedUpdateThread?.Abort();
            Debug.PrintThread?.Abort();
        }
        public static void Stop()
        {
            Stop(null);
        }

        [Initializer(Int32.MinValue + 10)]
        private static void Initialize()
        {
            CheckPlateform();

            if(!SupportedOS.Contains(OS))
            {
                string errorMessage = "Sorry, but Winecrash is not compatible with system " + OS.ToString();

                try
                {
                    MessageBox.Show(errorMessage, "Fatal error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                    throw new Exception(errorMessage);
                }
            }

            CreateEngineWObject();
        }

        private static WObject CreateEngineWObject()
        {
            WObject wobj = new WObject("Engine Logic")
            {
                Undeletable = true
            };

            wobj.AddModule<Input>().ExecutionOrder = Int32.MinValue;

            Layer.CreateOrGetLayer(0).Name = "Default Layer";
            Group.CreateOrGetGroup(-1, "3D Logic");

            EngineObject = wobj;
            return wobj;
        }

        private static void CheckPlateform()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OS = OSPlatform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                OS = OSPlatform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OS = OSPlatform.OSX;
            }
        }
    }
}