﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace WEngine
{
    /// <summary>
    /// WObject are at the base of the game logic, they can hold multiple <see cref="Module"/> allowing scripting.
    /// <br>To add a module, use <see cref="WObject.AddModule{T}"/>. Remove and find also have their own methods.</br>
    /// </summary>
    public class WObject : BaseObject
    {
        internal static object WobjectsLocker = new object();
        // all the loaded wobjects
        internal static List<WObject> WObjects { get; set; } = new List<WObject>(1);
        // modules linked to this wobject
        internal List<Module> _Modules { get; set; } = new List<Module>(1);

        /// <summary>
        /// Create an empty <see cref="WObject"/>
        /// </summary>
        public WObject() : base()
        {
            lock(WobjectsLocker)
                WObjects.Add(this);

            if (Engine.DoGUI)
                Graphics.Window.OnRender += (e) => _RendersForward = this.Forward;
        }

        /// <summary>
        /// Create an empty <see cref="WObject"/>
        /// </summary>
        /// <param name="name">The name of the WObject.</param>
        public WObject(string name) : base(name) 
        {
            lock(WobjectsLocker)
                WObjects.Add(this);

            if(Engine.DoGUI)
                Graphics.Window.OnRender += (e) => _RendersForward = this.Forward;
        }

        /// <summary>
        /// Render layer of the WObject. The render layer
        /// 0 = none
        /// Int64.MaxValue = everything
        /// 
        /// ex: 1<<32 = skybox
        /// </summary>
        public Int64 Layer { get; set; } = 1L; // Default layer = 1;

        

        private WObject _Parent = null;
        public WObject Parent
        {
            get
            {
                return this._Parent;
            }

            set
            {
                if(_Parent != null)
                {
                    _Parent._Children.Remove(this);
                    _Parent = null;
                }

                if(value != null)
                {
                    Vector3F oldGlobalPosition = this.Position;
                    Quaternion oldGlobalRotation = this.Rotation;
                    Vector3F oldGlobalScale = this.Scale;

                    this._Parent = value;
                    value._Children.Add(this);

                    this.Position = oldGlobalPosition;
                    this.Rotation = oldGlobalRotation;
                    this.Scale = oldGlobalScale;
                }
            }
        }



        private List<WObject> _Children { get; set; } = new List<WObject>();
        private object _ChildrenLocker = new object();
        public WObject[] Children
        {
            get
            {
                return _Children.ToArray();
            }
        }

        public Vector3D Position
        {
            get
            {   //                                      first set to scale                                     then rotate                          then add parent's position
                return this._Parent ? (this.LocalPosition * this._Parent.Scale).RotateAround(Vector3D.Zero, this._Parent.Rotation) + this._Parent.Position : this.LocalPosition;
            }

            set
            {   //                                      first get relative position                              then rotate by invert                                 then scale
                this.LocalPosition = this._Parent ? (value - this._Parent.Position).RotateAround(Vector3D.Zero, this._Parent.Rotation) * (Vector3D.One / this._Parent.Scale) : value;
            }
        }
        public Vector3D LocalPosition { get; set; } = Vector3D.Zero;

        public Quaternion Rotation
        {
            get
            {
                return this._Parent ? this._Parent.Rotation * this.LocalRotation : this.LocalRotation;
            }

            set
            {
                this.LocalRotation = this._Parent ? this._Parent.Rotation.Inverted * value : value;
            }
        }
        public Quaternion LocalRotation { get; set; } = Quaternion.Identity;

        public Vector3D Scale
        {
            get
            {
                return this._Parent ? this._Parent.Scale * this.LocalScale : this.LocalScale;
            }

            set
            {
                this.LocalScale = this._Parent ? value / this._Parent.Scale : value;
            }
        }
        public Vector3D LocalScale { get; set; } = Vector3D.One;

        public Vector3D Right
        {
            get
            {
                return this.Rotation * Vector3D.Right;
            }
        }
        public Vector3D Left
        {
            get
            {
                return this.Rotation * Vector3D.Left;
            }
        }
        public Vector3D Up
        {
            get
            {
                return this.Rotation * Vector3D.Up;
            }

            set
            {
                this.Rotation = Quaternion.Identity;

                this.Rotation *= new Quaternion(Vector3D.Up, Vector2D.Angle(Vector2D.Right, value.XZ) * WMath.RadToDeg);

                this.Rotation *= new Quaternion(Vector3D.Right, Vector2D.Angle(Vector2D.Up, value.YZ) * WMath.RadToDeg);

                this.Rotation *= new Quaternion(Vector3D.Forward, Vector2D.Angle(Vector2D.Up, value.XY) * WMath.RadToDeg);
            }
        }
        public Vector3D Down
        {
            get
            {
                return this.Rotation * Vector3D.Down;
            }
        }
        public Vector3D Forward
        {
            get
            {
                return this.Rotation * Vector3D.Forward;
            }
        }
        internal Vector3D _RendersForward;
        public Vector3D Backward
        {
            get
            {
                return this.Rotation * Vector3D.Backward;
            }
        }

        internal Matrix4D _RendersTransformMatrix;
        internal protected virtual Matrix4D TransformMatrix
        {
            get
            {
                Matrix4D scaD =
                    new Matrix4D(this.Scale, true);

                Matrix4D rotD =
                    new Matrix4D(this.Rotation);

                Matrix4D posD =
                    new Matrix4D(this.Position, false);


                return scaD * rotD * posD * Matrix4D.Identity;
            }
        }

        internal protected virtual void TransformMatrixRef(out Matrix4D result)
        {
            Matrix4D scaD =
                     new Matrix4D(this.Scale, true);

            Matrix4D rotD =
                new Matrix4D(this.Rotation);

            Matrix4D posD =
                new Matrix4D(this.Position, false);

            Matrix4D.Mult(in scaD, in rotD, out result);
            Matrix4D.Mult(in result, in posD, out result);
            Matrix4D.Mult(in result, Matrix4D.Identity, out result);
        }

        public static WObject Find(string name)
        {
            List<WObject> wobjs;
            lock (WobjectsLocker)
                wobjs = WObjects.ToList(); 
            return wobjs.FirstOrDefault(w => w.Name == name);
        }

        public WObject FindChild(string childName)
        {
            WObject[] children;
            lock (_ChildrenLocker)
                children = _Children.ToArray();
            return children.FirstOrDefault(c => c.Name == childName);
        }

        public T AddModule<T>() where T : Module
        {
            T mod = (T)Activator.CreateInstance(typeof(T));

            if(mod.Undeletable && !this.Undeletable)
            {
                Debug.LogError("Cannot add undeletable module on deletable WObject !");

                Group.GetGroup(0).RemoveModule(mod);
                return null;
            }

            mod.Name = mod.GetType().Name;
            mod.WObject = this;

            this._Modules.Add(mod);

            mod.Creation();

            if(this.Enabled)
            {
                mod.OnEnable();
            }
            else
            {
                mod.OnDisable();
            }

            return mod;
        }

        public T GetModule<T>() where T : Module
        {
            foreach (Module mod in this._Modules) if (mod is T module) return module;
            return null;
        }

        public List<T> GetModules<T>() where T : Module
        {
            List<T> modules = new List<T>();

            foreach (Module mod in _Modules) if (mod is T module) modules.Add(module);

            if (modules.Count == 0) return null;
            else return modules;
        }

        public T AddOrGetModule<T>() where T : Module
        {
            return this.GetModule<T>() ?? this.AddModule<T>();
        }


        public override bool Enabled 
        { 
            get
            {
                return ThisAndParentEnabled();
            }
            set
            {
                this._Enabled = value;
            }
        }

        private bool ThisAndParentEnabled()
        {
            return this._Enabled && (!this.Parent || this.Parent.Enabled);
        }

        internal sealed override void ForcedDelete()
        {
            for (int i = 0; i < _Modules.Count; i++)
            {
                _Modules[i].ForcedDelete();
            }

            for (int i = 0; i < _Children.Count; i++)
            {
                _Children[i].ForcedDelete();
            }

            this.Parent = null;
            _Children.Clear();
            _Children = null;

            _Modules.Clear();
            _Modules = null;
            lock (WobjectsLocker)
                WObjects.Remove(this);

            base.Delete();
        }
        public sealed override void Delete()
        {
            if (this.Undeletable)
            {
                Debug.LogWarning("Unable to delete " + this + " : wobject undeletable.");
                return;
            }

            if(this._Modules != null)
                for (int i = 0; i < _Modules.Count; i++)
                    _Modules[i].Delete();

            if(this._Children != null)
                for (int i = 0; i < _Children.Count; i++)
                    _Children[i].Delete();

            this.Parent = null;
            _Children?.Clear();
            _Children = null;

            _Modules?.Clear();
            _Modules = null;
            lock (WobjectsLocker)
                WObjects.Remove(this);

            base.Delete();
        }

        public static void TraceHierarchy()
        {
            Debug.Log("All WObjects: \n" + GetHierachy(null, 0));
        }

        private static string GetHierachy(WObject parent, int recursiveCount)
        {
            string str = parent == null ? "Scene" : parent.Name;
            
            WObject[] children = null;

            if (parent)
            {
                children = parent.Children;
            }
            else
            {
                recursiveCount++;
                children = WObjects.Where(w => w._Parent == null).ToArray();
            }
            
            for (int i = 0; i < children.Length; i++)
            {
                str += "\n";

                
                for (int j = 1; j < recursiveCount; j++)
                {
                    str += "  ";
                }

                if (i == children.Length - 1) str += "└─";
                else str += "├─";
                
                
                
                str += GetHierachy(children[i], recursiveCount + 1);
            }

            return str;
        }
    }
}
