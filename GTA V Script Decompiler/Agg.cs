using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Decompiler
{
    public struct AggregateData
    {
        public string ScriptName { get; private set; }
        public string FunctionName { get; private set; }
        public string FunctionString { get; private set; }

        public string AggregateString { get; private set; }
        public string AggregateName { get; private set; }
        public string Hash { get; private set; }
        public List<string> Hits;

        public AggregateData(Function function, string aggName, string hash)
        {
            Hits = new List<string>();
            Hash = hash;
            AggregateName = aggName;
            AggregateString = function.ToString();

            ScriptName = function.Scriptfile.name;
            FunctionName =  function.Scriptfile.name + "." + function.Name;
            FunctionString = function.BaseFunction.ToString();
        }

        public void AddFunction(Function function)
        {
            string addedName = function.Scriptfile.name + "." + function.Name;
            if (String.Compare(FunctionName, addedName, comparisonType: StringComparison.OrdinalIgnoreCase) > 0)
            {
                Hits.Add("// Hit: " + FunctionName);
                ScriptName = function.Scriptfile.name;
                FunctionName = addedName;
                FunctionString = function.BaseFunction.ToString();
            }
            else
            {
                Hits.Add("// Hit: " + addedName);
            }
        }
    }

    public sealed class Agg
    {
        private readonly object pushLock = new object();
        private readonly object countLock = new object();

        static SHA256Managed crypt = new SHA256Managed();
        static StringBuilder Sb = new StringBuilder();

        private static int CountLines(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            if (str == string.Empty)
                return 0;

            int index = -1, count = 0;
            while (-1 != (index = str.IndexOf(Environment.NewLine, index + 1)))
                count++;
            return count + 1;
        }

        public static string SHA256(string value)
        {
            Sb.Clear();
            foreach (Byte b in crypt.ComputeHash(Encoding.UTF8.GetBytes(value)))
                Sb.Append(b.ToString("x2"));
            return Sb.ToString();
        }

        public static Agg Instance { get { return Nested.instance; } }

        private Dictionary<string, AggregateData> FunctionLoc;
        private Dictionary<string, ulong> nativeRefCount;

        private Agg()
        {
            FunctionLoc = new Dictionary<string, AggregateData>();
            nativeRefCount = new Dictionary<string, ulong>();
        }

        public bool CanAggregateLiteral(string lit)
        {
            return !lit.StartsWith("Global");
        }

        public bool IsAggregate(string decomp)
        {
            return FunctionLoc.ContainsKey(SHA256(decomp));
        }

        public AggregateData FetchAggregate(string decomp)
        {
            return FunctionLoc[SHA256(decomp)];
        }

        public void Count(string hash)
        {
            lock (pushLock)
            {
                ulong value = 0;
                nativeRefCount[hash] = (nativeRefCount.TryGetValue(hash, out value) ? value : 0) + 1;
            }
        }

        public void PushAggregate(ScriptFile script, Function function, string decomp)
        {
            lock (pushLock)
            {
                if (function.NativeCount > 0 && CountLines(decomp) >= Program.AggregateMinLines)
                {
                    string hash = SHA256(decomp);
                    if (FunctionLoc.ContainsKey(hash))
                        FunctionLoc[hash].AddFunction(function);
                    else
                        FunctionLoc.Add(hash, new AggregateData(function, "Aggregate_" + FunctionLoc.Count, hash));
                }
            }
        }

        public void SaveAggregate(string SaveDirectory)
        {
            using (Stream stream = File.Create(Path.Combine(SaveDirectory, "aggregate.c")))
            {
                StreamWriter savestream = new StreamWriter(stream);
                List<KeyValuePair<string, AggregateData>> list = FunctionLoc.ToList();
                list.Sort(delegate (KeyValuePair<string, AggregateData> pair1, KeyValuePair<string, AggregateData> pair2) {
                    if (pair2.Value.Hits.Count == pair1.Value.Hits.Count)
                        return pair1.Value.AggregateName.CompareTo(pair2.Value.AggregateName);
                    return pair2.Value.Hits.Count.CompareTo(pair1.Value.Hits.Count);
                });

                foreach (KeyValuePair<string, AggregateData> entry in list)
                {
                    if (entry.Value.Hits.Count >= Program.AggregateMinHits)
                    {
                        savestream.WriteLine("// " + entry.Key);
                        savestream.WriteLine("// Base: " + entry.Value.FunctionName);

                        entry.Value.Hits.Sort();
                        foreach (string c in entry.Value.Hits)
                            savestream.WriteLine(c);
                        savestream.WriteLine(entry.Value.FunctionString);
                    }
                }
            }
        }

        public void SaveFrequency(string SaveDirectory)
        {
            using (Stream stream = File.Create(Path.Combine(SaveDirectory, "freq.csv")))
            {
                using (StreamWriter savestream = new StreamWriter(stream))
                {
                    List<KeyValuePair<string, ulong>> list = nativeRefCount.ToList();
                    list.Sort(delegate (KeyValuePair<string, ulong> pair1, KeyValuePair<string, ulong> pair2) {
                        int comp = pair2.Value.CompareTo(pair1.Value);
                        return comp == 0 ? string.Compare(pair1.Key, pair2.Key) : comp;
                    });

                    foreach (KeyValuePair<string, ulong> entry in list)
                        savestream.WriteLine(entry.Key + ", " + entry.Value);
                }
            }
        }

        public void SaveAggregateDefinitions(string SaveDirectory)
        {
            using (Stream stream = File.Create(Path.Combine(SaveDirectory, "aggregatedefns.c")))
            {
                StreamWriter savestream = new StreamWriter(stream);
                List<KeyValuePair<string, AggregateData>> list = FunctionLoc.ToList();
                list.Sort(delegate (KeyValuePair<string, AggregateData> pair1, KeyValuePair<string, AggregateData> pair2) {
                    if (pair2.Value.Hits.Count == pair1.Value.Hits.Count)
                        return pair1.Value.AggregateName.CompareTo(pair2.Value.AggregateName);
                    return pair2.Value.Hits.Count.CompareTo(pair1.Value.Hits.Count);
                });

                foreach (KeyValuePair<string, AggregateData> entry in list)
                {
                    if (entry.Value.Hits.Count >= Program.AggregateMinHits)
                    {
                        savestream.WriteLine("// " + entry.Key);
                        savestream.WriteLine("// Base: " + entry.Value.FunctionName);
                        savestream.WriteLine(entry.Value.AggregateString);
                    }
                }
            }
        }

        private class Nested
        {
            static Nested() { }
            internal static readonly Agg instance = new Agg();
        }
    }
}
