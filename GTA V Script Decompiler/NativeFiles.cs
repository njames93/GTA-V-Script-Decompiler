using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decompiler
{
    class x64NativeFile : Dictionary<ulong, Native>
    {
        public x64NativeFile(Stream Nativefile) : base()
        {
            using (StreamReader sr = new StreamReader(Nativefile))
            {
                RootObject o = (RootObject)new JsonSerializer().Deserialize(sr, typeof(RootObject));
                foreach (KeyValuePair<string, JToken> ns in o.Values)
                {
                    foreach (KeyValuePair<string, JToken> natives in ns.Value.ToObject<RootObject>().Values)
                    {
                        Native native = natives.Value.ToObject<Native>();
                        native.Namespace = ns.Key;
                        if (Program.IsBit32)
                        {
                            uint jhash;
                            if (native.Joaat != "" && uint.TryParse(native.Joaat.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out jhash))
                            {
                                native.Hash = jhash;
                                this[jhash] = native;
                            }
                        }
                        else
                        {
                            ulong hash;
                            if (ulong.TryParse(natives.Key.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hash))
                            {
                                native.Hash = hash;
                                this[hash] = native;
                                if (native.Hashes != null)
                                {
                                    foreach (string s in native.Hashes)
                                    {
                                        if (ulong.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hash) && hash != 0 && !ContainsKey(hash))
                                            Add(hash, native);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public string GetNativeInfo(ulong hash)
        {
            Native native;
            if (!TryGetValue(hash, out native))
                throw new Exception("Native not found");

            string dec = native.ReturnParam.StackType.ReturnType() + native.Display + "(";
            if (native.Params.Count == 0)
                return dec + ");";
            for (int i = 0; i < native.Params.Count; i++)
            {
                if (native.Params[i].Vardiac)
                {
                    dec += "...";
                    break;
                }
                else
                    dec += native.Params[i].StackType.VarDeclaration() + i + ", ";
            }
            return dec.Remove(dec.Length - 2) + ");";
        }

        public Stack.DataType GetReturnType(ulong hash)
        {
            Native native;
            return TryGetValue(hash, out native) ? native.ReturnParam.StackType : Stack.DataType.Unk;
        }

        public void UpdateParam(ulong hash, Stack.DataType type, int index)
        {
            Native native;
            if (TryGetValue(hash, out native))
            {
                if (index < native.Params.Count && !native.Vardiac)
                {
                    Param p = native.Params[index];
                    if (p.StackType.IsUnknown())
                        p.Type = type.LongName();
                }
            }
        }

        public void UpdateRetType(ulong hash, Stack.DataType returns, bool over = false)
        {
            Native native;
            if (TryGetValue(hash, out native))
            {
                if (native.ReturnParam.StackType.IsUnknown())
                    native.ReturnParam.Type = returns.LongName();
            }
        }

        public bool FetchNativeCall(ulong hash, string name, int pcount, int rcount, out Native native)
        {
            lock (Program.ThreadLock)
            {
                if (TryGetValue(hash, out native))
                {
                    /**
                    * Remove natives and make all types "Any", since we cannot
                    * be confident in the current type information
                    */
                    if (native.Params.Count != pcount && !native.Vardiac)
                    {
                        Console.WriteLine("Native Argument Mismatch: " + name + " " + pcount + "/" + native.Params.Count);
                        native.Params.Clear();
                        for (int i = 0; i < pcount; ++i)
                            native.Params.Add(new Param("Any", "Param" + i.ToString()));
                    }
                }
                else
                {
                    native = new Native();
                    native.Hash = hash;
                    native.Name = name;
                    native.Joaat = "";
                    native.Comment = "";
                    native.Build = "";
                    native.Namespace = "";

                    native.Return = "Any";
                    native.Params = new List<Param>();
                    for (int i = 0; i < pcount; ++i)
                        native.Params.Add(new Param("Any", "Param" + i.ToString()));
                    Add(hash, native);
                }
            }
            return true;
        }

        public void UpdateNative(ulong hash, Stack.DataType returns, params Stack.DataType[] param)
        {
            Native native;
            lock (Program.ThreadLock)
            {
                if (!TryGetValue(hash, out native)) throw new Exception("Unknown Native: " + hash.ToString("X"));

                native.Return = returns.LongName();
                for (int i = 0; i < param.Length; ++i)
                {
                    if (native.Params[i].Vardiac)
                        break;
                    if (native.Params[i].StackType.IsUnknown())
                        native.Params[i].Type = param[i].LongName();
                }
            }
        }

        /// <summary>
        /// JSON Types to StackTypes.
        /// </summary>
        public static readonly Dictionary<string, Stack.DataType> TypeMap = new Dictionary<string, Stack.DataType> {
            /* Stack Types */
            { "var", Stack.DataType.Unk },
            { "var*", Stack.DataType.UnkPtr },
            { "bool", Stack.DataType.Bool },
            { "bool*", Stack.DataType.BoolPtr },

            /* Codegen Types */
            { "Any", Stack.DataType.Unk },
            { "Any*", Stack.DataType.UnkPtr },
            { "void", Stack.DataType.None },
            { "char[]", Stack.DataType.String },
            { "char*", Stack.DataType.StringPtr },
            { "const char*", Stack.DataType.StringPtr },
            { "Vector3", Stack.DataType.Vector3 },
            { "Vector3*", Stack.DataType.Vector3Ptr },
            { "BOOL", Stack.DataType.Bool },
            { "BOOL*", Stack.DataType.BoolPtr },
            { "float", Stack.DataType.Float },
            { "float*",  Stack.DataType.FloatPtr },
            { "uint", Stack.DataType.Int },
            { "Hash", Stack.DataType.Int },
            { "Entity", Stack.DataType.Int },
            { "Player", Stack.DataType.Int },
            { "FireId", Stack.DataType.Int },
            { "Ped", Stack.DataType.Int },
            { "Vehicle", Stack.DataType.Int },
            { "Cam", Stack.DataType.Int },
            { "CarGenerator", Stack.DataType.Int },
            { "Group", Stack.DataType.Int },
            { "Train", Stack.DataType.Int },
            { "Pickup", Stack.DataType.Int },
            { "Object", Stack.DataType.Int },
            { "Weapon", Stack.DataType.Int },
            { "Interior", Stack.DataType.Int },
            { "Blip", Stack.DataType.Int },
            { "Texture", Stack.DataType.Int },
            { "TextureDict", Stack.DataType.Int },
            { "CoverPoint", Stack.DataType.Int },
            { "Camera", Stack.DataType.Int },
            { "TaskSequence", Stack.DataType.Int },
            { "ColourIndex", Stack.DataType.Int },
            { "Sphere", Stack.DataType.Int },
            { "ScrHandle", Stack.DataType.Int },
            { "int", Stack.DataType.Int },
            { "long", Stack.DataType.Int },
            { "Hash*", Stack.DataType.IntPtr },
            { "Entity*", Stack.DataType.IntPtr },
            { "Player*", Stack.DataType.IntPtr },
            { "FireId*", Stack.DataType.IntPtr },
            { "Ped*", Stack.DataType.IntPtr },
            { "Vehicle*", Stack.DataType.IntPtr },
            { "Cam*", Stack.DataType.IntPtr },
            { "CarGenerator*", Stack.DataType.IntPtr },
            { "Group*", Stack.DataType.IntPtr },
            { "Train*", Stack.DataType.IntPtr },
            { "Pickup*", Stack.DataType.IntPtr },
            { "Object*", Stack.DataType.IntPtr },
            { "Weapon*", Stack.DataType.IntPtr },
            { "Interior*", Stack.DataType.IntPtr },
            { "Blip*", Stack.DataType.IntPtr },
            { "Texture*", Stack.DataType.IntPtr },
            { "TextureDict*", Stack.DataType.IntPtr },
            { "CoverPoint*", Stack.DataType.IntPtr },
            { "Camera*", Stack.DataType.IntPtr },
            { "TaskSequence*", Stack.DataType.IntPtr },
            { "ColourIndex*", Stack.DataType.IntPtr },
            { "Sphere*", Stack.DataType.IntPtr },
            { "ScrHandle*", Stack.DataType.IntPtr },
            { "int*", Stack.DataType.IntPtr },

            // RDR Extended
            { "Itemset", Stack.DataType.Int },
            { "Prompt", Stack.DataType.Int },
            { "Volume", Stack.DataType.Int },
            { "AnimScene", Stack.DataType.Int },
            { "PopZone", Stack.DataType.Int },
            { "PropSet", Stack.DataType.Int },
            { "ItemSet", Stack.DataType.Int },
        };
    }

    internal class RootObject
    {
        [JsonExtensionData]
        public IDictionary<string, JToken> Values { get; set; }
    }

    public class Native
    {
        public static readonly Param VardiacParam = new Param("const char*", "Parameter");
        public static readonly string UnkPrefix = "unk_";

        private bool _dirty = false;
        private bool _vardiac = false;
        private string _return;
        private string _name;
        private string _namespace;
        private string _displayName;
        private IList<Param> _params;

        public Native() { }

        public ulong Hash { get; set; }

        public string Namespace
        {
            get => _namespace;
            set { _dirty = true; _namespace = value; }
        }

        [Newtonsoft.Json.JsonProperty("name")]
        public string Name
        {
            get => _name;
            set { _dirty = true; _name = value; }
        }

        [Newtonsoft.Json.JsonProperty("jhash")]
        public string Joaat { get; set; }

        [Newtonsoft.Json.JsonProperty("comment")]
        public string Comment { get; set; }

        [Newtonsoft.Json.JsonProperty("params")]
        public IList<Param> Params
        {
            get => _params;
            set
            {
                _params = value;
                _vardiac = false; // Update vardiac condition
                foreach (Param p in _params) _vardiac = _vardiac | p.Vardiac;
            }
        }

        public Param GetParam(int index) => (index >= Params.Count && Vardiac) ? Native.VardiacParam : Params[index];

        [Newtonsoft.Json.JsonProperty("hashes")]
        public IList<string> Hashes { get; set; }

        [Newtonsoft.Json.JsonProperty("return_type")]
        public string Return
        {
            get => _return;
            set { _return = value; ReturnParam = new Param(_return, "Return"); }
        }

        /// <summary>
        /// Return type converted into a parameter object.
        /// </summary>
        public Param ReturnParam { get; set; }

        [Newtonsoft.Json.JsonProperty("build")]
        public string Build { get; set; }

        /**
         * Cached Fields
         */
        public bool Vardiac => _vardiac;
        public string Display { get { UpdateDirty(); return _displayName; } }

        private void UpdateDirty()
        {
            if (!_dirty) return;

            string dispStr = Program.NativeName((Program.Show_Nat_Namespace && Namespace != "") ? (Namespace + "::") : "");
            if (Name.StartsWith("_0x"))
                dispStr += Name.Remove(3) + Program.NativeName(Name.Substring(3));
            else if (Name.StartsWith(Program.NativeName(Native.UnkPrefix)))
                dispStr += Name;
            else
                dispStr += Program.NativeName(Name);
            _displayName = dispStr;
        }

        public static string CreateNativeHash(ulong hash)
        {
            string hashStr = hash.ToString("X");
            while (hashStr.Length < (Program.IsBit32 ? 8 : 16))
                hashStr = "0" + hashStr;
            return "0x" + Program.NativeName(hashStr);
        }
    }

    public class Param
    {
        private string _type;
        private bool _vardiac; // Temporary fix for 0xFA925AC00EB830B9.
        private Stack.DataType _sType;

        public Param() { }

        public Param(string type, string name) { Name = name; Type = type; }

        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; set; }

        [Newtonsoft.Json.JsonProperty("type")]
        public string Type
        {
            get => _type;
            set
            {
                _vardiac = value == "";
                _type = _vardiac ? "const char*" : value;
                _sType = x64NativeFile.TypeMap[_type];
            }
        }

        /// <summary>
        ///
        /// </summary>
        public Stack.DataType StackType => _sType;

        /// <summary>
        ///
        /// </summary>
        public bool Vardiac => _vardiac;
    }
}
