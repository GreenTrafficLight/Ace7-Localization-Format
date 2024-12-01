﻿using Ace7LocalizationFormat.Stream;
using Ace7LocalizationFormat.Utils;

namespace Ace7LocalizationFormat.Formats
{
    public class CmnString
    {
        public int StringNumber = -1;
        public string Key = null;
        public string Name = null;
        public CmnString Parent = null;
        public SortedDictionary<string, CmnString> Childrens = new SortedDictionary<string, CmnString>(StringComparer.Ordinal);

        public override string ToString()
        {
            return Name;
        }

        public CmnString(int stringNumber, string key, string name, CmnString parent)
        {
            StringNumber = stringNumber;
            Key = key;
            Name = name;
            Parent = parent;
        }
    }

    public class CmnFile
    {
        public int this[string key]
        {
            get
            {
                CmnString cmnString = GetVariable(key, Root);
                return cmnString.StringNumber;
            }
        }

        public SortedDictionary<string, CmnString> Root = new SortedDictionary<string, CmnString>(StringComparer.Ordinal);
        
        // The highest string number in the CMN
        public int MaxStringNumber = 0;

        public CmnFile(string path)
        {
            Read(path);
        }

        public CmnFile(byte[] data) 
        {
            Read(data);
        }

        /// <summary>
        /// Read a CMN file
        /// </summary>
        /// <param name="path">Path of the CMN file</param>
        public void Read(string path)
        {
            byte[] data = File.ReadAllBytes(path);

            Read(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void Read(byte[] data)
        {
            data = DatFile.Crypt(data, (uint)data.Length);
            data = CompressionHandler.Decompress(data);

            DATBinaryReader br = new DATBinaryReader(data);

            //Root.Childrens = ReadVariables(br, null, Root.Childrens);
            ReadVariables(br, null, Root);
        }

        /// <summary>
        /// Write a CMN file
        /// </summary>
        /// <param name="path">Output path for the written CMN file</param>
        public void Write(string path)
        {
            DATBinaryWriter bw = new DATBinaryWriter();

            bw.WriteInt(Root.Count);
            foreach (var children in Root.Values)
                WriteVariables(bw, children);

            byte[] data = bw.DATBinaryWriterData.ToArray();

            data = CompressionHandler.Compress(data);
            uint size = (uint)data.Length;
            data = DatFile.Crypt(data, size);

            File.WriteAllBytes(path, data);
        }

        /// <summary>
        /// Read the variables inside a binary data
        /// </summary>
        private void ReadVariables(DATBinaryReader br, CmnString parent, SortedDictionary<string, CmnString> node, string fullName = "")
        {
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int nameLength = br.ReadInt32();
                string name = br.ReadString(nameLength);
                int stringNumber = br.ReadInt32();
                // Get the maximum the of the strings contained in the CMN
                if (MaxStringNumber < stringNumber)
                {
                    MaxStringNumber = stringNumber;
                }
                CmnString cmnString = new CmnString(stringNumber, name, fullName + name, parent);
                node.Add(name, cmnString);
                ReadVariables(br, node[name], node[name].Childrens, fullName + name);
            }
        }

        /// <summary>
        /// Write the variables inside a binary data
        /// </summary>
        /// <param name="bw"></param>
        /// <param name="parent"></param>
        private void WriteVariables(DATBinaryWriter bw, CmnString parent)
        {
            bw.WriteInt(parent.Name.Length);
            bw.WriteString(parent.Name);
            bw.WriteInt(parent.StringNumber);
            bw.WriteInt(parent.Childrens.Count);
            foreach (CmnString child in parent.Childrens.Values)
                WriteVariables(bw, child);
        }

        public void AddVariable(string newVariableName, SortedDictionary<string, CmnString> parent)
        {
            MergeVariable(newVariableName, GetVariable(newVariableName, parent));
            MaxStringNumber++;
        }

        public CmnString GetVariable(string variableName, SortedDictionary<string, CmnString> parent)
        {
            CmnString parentCmnString = null;
            while (true)
            {
                string matchingKey = parent.Keys
                    .Where(key => variableName.StartsWith(key))
                    .OrderByDescending(key => key.Length) // Sort by length to get the longest match
                    .FirstOrDefault(); // Take the first match

                if (matchingKey == null)
                {
                    return parentCmnString;
                }
                variableName = variableName.Substring(matchingKey.Length);
                parentCmnString = parent[matchingKey];
                parent = parent[matchingKey].Childrens;
            }
        }

        public void MergeVariable(string newVariableName, CmnString parent)
        {
            foreach (string key in parent.Childrens.Keys)
            {
                int subStringIndex = StringUtils.GetCommonSubstringIndex(key, newVariableName);

                if (subStringIndex != -1)
                {
                    // Merged Node
                    string mergedCmnStringKey = key.Substring(0, subStringIndex + 1); // Merged node name
                    CmnString mergedCmnString = new CmnString(-1, mergedCmnStringKey, parent.Name + mergedCmnStringKey, parent);

                    // Existing Node
                    string existingCmnStringKey = key.Substring(subStringIndex + 1);
                    CmnString existingCmnString = new CmnString(parent.Childrens[key].StringNumber, existingCmnStringKey, mergedCmnString.Name + existingCmnStringKey, parent);
                    existingCmnString.Childrens = new SortedDictionary<string, CmnString>(parent.Childrens[key].Childrens);
                    mergedCmnString.Childrens.Add(existingCmnStringKey, existingCmnString);

                    // New Node
                    string newCmnStringKey = newVariableName.Substring(subStringIndex + 1);
                    CmnString newCmnString = new CmnString(MaxStringNumber, newCmnStringKey, mergedCmnString.Name + newCmnStringKey, parent);
                    mergedCmnString.Childrens.Add(newCmnStringKey, newCmnString);

                    // Remove the existing node from the parent
                    parent.Childrens.Remove(key);

                    // Add the merged node in the parent
                    parent.Childrens.Add(mergedCmnStringKey, mergedCmnString);

                    return;
                }
            }
            parent.Childrens.Add(newVariableName, new CmnString(MaxStringNumber, newVariableName, parent.Name + newVariableName, parent));
        }
    }
}