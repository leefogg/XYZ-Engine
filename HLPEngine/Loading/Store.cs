using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using static System.Diagnostics.Debug;

namespace HPLEngine.Loading
{
    public class Store
    {
        private readonly Dictionary<int, string> LUT = new Dictionary<int, string>();

        public readonly string Extension;

        private bool IsInitialized => LUT.Count > 0;
        private string CacheFileName => Extension + "Store.cache";

        public Store(string exension)
        {
            Extension = exension;
        }

        public bool TryGetValue(string name, out string path)
        {
            if (string.IsNullOrEmpty(name)) 
            { 
                path = null; 
                return false;
            }

            Assert(string.Compare(Path.GetExtension(name), "." + Extension, true) == 0, "Mismatching extension.");
            Assert(IsInitialized, "Attempting to retreive value before initializing store.");

            var hash = GetInt32HashCode(name.ToLower());
            var found = LUT.TryGetValue(hash, out path);
            return found;
        }

        public void Init(string folderPath)
        {
            Assert(!IsInitialized, "Re-Initializing Store");

            try
            {
                if (File.Exists(CacheFileName))
                    ReadCache();
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Failed to parse cache file {CacheFileName}");
                Console.WriteLine(ex);
            }

            if (!IsInitialized)
                LoadFromSearch(folderPath);
        }

        private void LoadFromSearch(string folderPath)
        {
            var options = new EnumerationOptions()
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false
            };
            foreach (var filePath in Directory.GetFiles(folderPath, "*." + Extension, options))
                TryAddKey(Path.GetFileName(filePath), filePath);

            Console.WriteLine($"Located {LUT.Count} files with extension {Extension}.");
        }

        public void WriteCache()
        {
            var sb = new StringBuilder(100000);

            foreach (var pair in LUT)
                sb.AppendLine(pair.Key + "=" + pair.Value);

            var outputPath = CacheFileName;
            File.WriteAllText(outputPath, sb.ToString());
            Console.WriteLine($"Wrote cache file {outputPath}");
        }

        public void ReadCache()
        {
            using var fileData = File.OpenText(CacheFileName);

            var numEntries = 0;
            while (!fileData.EndOfStream)
            {
                var line = fileData.ReadLine();
                var parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
                LUT.Add(int.Parse(parts[0]), parts[1]);
                numEntries++;
            }

            Console.WriteLine($"Loaded {numEntries} entries from cache file {CacheFileName}.");
        }

        private bool TryAddKey(string name, string path)
        {
            name = name.ToLower();

            var hash = GetInt32HashCode(name);

            if (LUT.ContainsKey(hash))
                return false;

            LUT.Add(hash, path);
            return true;
        }

        private static int GetInt32HashCode(string strText)
        {
            if (string.IsNullOrEmpty(strText))
                return 0;

            byte[] byteContents = Encoding.Unicode.GetBytes(strText);
            var hasher = new SHA256CryptoServiceProvider();
            byte[] hashedText = hasher.ComputeHash(byteContents);

            int seed = 0;
            int hash = seed;

            for (int i = 0; i < hashedText.Length; i += 4)
                hash ^= BitConverter.ToInt32(hashedText, i);

            return hash;
        }
    }
}
