using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ace7LocalizationFormat.Utils;
using Ace7LocalizationFormat.Stream;
using static Ace7LocalizationFormat.Formats.CMN;

namespace Ace7LocalizationFormat.Formats
{
    public class CMN
    {
        public class CMNString
        {
            public int StringNumber = -1;
            public string Name = null;
            public string FullName = null;
            public KeyValuePair<string, CMNString> Parent = new KeyValuePair<string, CMNString>();
            public List<KeyValuePair<string, CMNString>> childrens = new List<KeyValuePair<string, CMNString>>();

            public CMNString(int stringNumber, string name, KeyValuePair<string, CMNString> parent)
            {
                StringNumber = stringNumber;
                Name = name;
                FullName = name;
                Parent = parent;
                if (parent.Key != null && parent.Value != null)
                {
                    FullName = GetVariable(parent) + FullName;
                }
            }
        }

        public CMN(string path)
        {
            Read(path);
        }

        public CMN(byte[] data) 
        {
            Read(data);
        }

        public List<KeyValuePair<string, CMNString>> Root = new List<KeyValuePair<string, CMNString>>();
        // The highest string number in the CMN
        public int MaxStringNumber = 0;

        /// <summary>
        /// Read a CMN file
        /// </summary>
        /// <param name="path">Path of the CMN file</param>
        public void Read(string path)
        {
            byte[] data = File.ReadAllBytes(path);

            data = DAT.Crypt(data, (uint)data.Length);
            data = CompressionHandler.Decompress(data);

            DATBinaryReader br = new DATBinaryReader(data);

            Root = ReadVariables(br, new KeyValuePair<string, CMNString>(null, null), Root);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void Read(byte[] data)
        {
            data = DAT.Crypt(data, (uint)data.Length);
            data = CompressionHandler.Decompress(data);

            DATBinaryReader br = new DATBinaryReader(data);

            Root = ReadVariables(br, new KeyValuePair<string, CMNString>(null, null), Root);
        }

        /// <summary>
        /// Write a CMN file
        /// </summary>
        /// <param name="path">Output path for the written CMN file</param>
        public void Write(string path)
        {
            DATBinaryWriter bw = new DATBinaryWriter();

            bw.WriteInt(Root.Count);
            foreach (var children in Root)
                WriteVariables(bw, children);

            byte[] data = bw.DATBinaryWriterData.ToArray();

            data = CompressionHandler.Compress(data);
            uint size = (uint)data.Length;
            data = DAT.Crypt(data, size);

            File.WriteAllBytes(path, data);
        }

        /// <summary>
        /// Get the full variable string from a child CMN
        /// </summary>
        /// <param name="child">The child CMN that we want the full string</param>
        /// <returns>
        /// Return the full variable string of a child CMN
        /// </returns>
        public static string GetVariable(KeyValuePair<string, CMNString> child)
        {
            string variable = child.Key;
            while (child.Value.Parent.Key != null && child.Value.Parent.Value != null)
            {
                variable = string.Concat(child.Value.Parent.Key, variable);
                child = child.Value.Parent;
            }
            return variable;
        }

        /// <summary>
        /// Add a new variable in the CMN
        /// </summary>
        /// <param name="variable">Variable to be added</param>
        /// <param name="stringNumber">String number tied to this variable</param>
        /// <returns>
        /// Retrun true if the variable didn't exist, false if the variable already exist
        /// </returns>
        public bool AddVariable(string variable, out int variableStringNumber, int? stringNumber = null)
        {
            KeyValuePair<string, CMNString> parent = Root.FirstOrDefault(x => variable.StartsWith(x.Key));
            variableStringNumber = -1;
            while (parent.Value != null)
            {
                variable = variable.Remove(0, parent.Key.Length);
                KeyValuePair<string, CMNString> child = parent.Value.childrens.FirstOrDefault(x => variable.StartsWith(x.Key));
                if (child.Key == null && child.Value == null)
                {
                    // If the variable doesn't exist
                    if (variable == ""){
                        break;
                    }
                    MaxStringNumber++;
                    MergeVariables(parent, variable, stringNumber);
                    return true;
                }
                else
                {
                    variableStringNumber = child.Value.StringNumber;
                }
                parent = child;
            }
            return false;
        }

        /// <summary>
        /// Add a new variable in the CMN
        /// </summary>
        /// <param name="value">Variable to be added</param>
        /// <param name="parent">The parent of the variable that will be added</param>
        /// <param name="stringNumber">String number tied to this variable</param>
        public void AddVariable(string value, KeyValuePair<string, CMNString> parent, int? stringNumber = null)
        {
            MergeVariables(parent, value, stringNumber);
        }
        
        private void MergeVariables(KeyValuePair<string, CMNString> parent, string value, int? stringNumber = null)
        {
            // Index where the added variable will be placed in the parent childrens
            int sortIndex = 0;
            foreach (KeyValuePair<string, CMNString> child in parent.Value.childrens)
            {
                int index = StringUtils.GetCommonSubstringIndex(child.Key, value);
                
                // If there is a node to merge
                if (index != -1)
                {
                    string subString = value.Substring(0, index + 1);

                    // Make a new node for the merged variable
                    KeyValuePair<string, CMNString> mergedCMNString = new KeyValuePair<string, CMNString>(subString, new CMNString(-1, subString, parent));

                    /// Existing node
                    // Substring the variable of the existing node
                    KeyValuePair<string, CMNString> existingCMNString = new KeyValuePair<string, CMNString>(child.Key.Substring(index + 1), new CMNString(child.Value.StringNumber, child.Key.Substring(index + 1), mergedCMNString));
                    // Add the childrens of the existing node
                    foreach (KeyValuePair<string, CMNString> existingCMNStringChild in child.Value.childrens){
                        existingCMNString.Value.childrens.Add(existingCMNStringChild);
                    }
                    // Add the existing node in the new merged variable
                    mergedCMNString.Value.childrens.Add(existingCMNString);
                    
                    /// New node
                    // Add the new node in the new merged variable
                    CMNString newCMNString = stringNumber == null ? new CMNString(MaxStringNumber, subString, mergedCMNString) : new CMNString(stringNumber.Value, subString, mergedCMNString);
                    // Compare the casing and number with the existing node
                    var comparisonResult = string.Compare(value.Substring(index + 1), child.Key.Substring(index + 1), StringComparison.Ordinal);
                    mergedCMNString.Value.childrens.Insert(comparisonResult < 0 ? 0 : mergedCMNString.Value.childrens.Count, new KeyValuePair<string, CMNString>(value.Substring(index + 1), newCMNString));

                    /// Parent node
                    // Insert the merged variable in the parent node
                    parent.Value.childrens.Insert(sortIndex, new KeyValuePair<string, CMNString>(mergedCMNString.Key, mergedCMNString.Value));
                    // Remove the existing node from the parent node
                    parent.Value.childrens.Remove(child);

                    return;
                }

                // Compare the casing and number of the added variable with the existing childrens
                if (string.Compare(value, child.Key, StringComparison.Ordinal) < 0) {
                    break; // Break the loop if the added variable has a inferior order
                }

                // Increase the index where the added variable will be placed in the parent childrens
                sortIndex++;
            }
            // If there isn't any node to merge
            parent.Value.childrens.Insert(sortIndex, new KeyValuePair<string, CMNString>(value, stringNumber == null ? new CMNString(MaxStringNumber, value, parent) : new CMNString(stringNumber.Value, value, parent)));
        }

        /// <summary>
        /// Read the variables inside a binary data
        /// </summary>
        private List<KeyValuePair<string, CMNString>> ReadVariables(DATBinaryReader br, KeyValuePair<string, CMNString> parent, List<KeyValuePair<string, CMNString>> node)
        {
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int nameLength = br.ReadInt32();
                string name = br.ReadString(nameLength);
                int stringNumber = br.ReadInt32();
                // Get the maximum the of the strings contained in the CMN
                if (MaxStringNumber < stringNumber) {
                    MaxStringNumber = stringNumber;
                }
                KeyValuePair<string, CMNString> cmnString = new KeyValuePair<string, CMNString>(name, new CMNString(stringNumber, name, parent));
                node.Add(cmnString);
                ReadVariables(br, node.FirstOrDefault(x => x.Key == name), node.FirstOrDefault(x => x.Key == name).Value.childrens);
            }
            return node;
        }

        /// <summary>
        /// Write the variables inside a binary data
        /// </summary>
        /// <param name="bw"></param>
        /// <param name="parent"></param>
        private void WriteVariables(DATBinaryWriter bw, KeyValuePair<string, CMNString> parent)
        {
            bw.WriteInt(parent.Key.Length);
            bw.WriteString(parent.Key);
            bw.WriteInt(parent.Value.StringNumber);
            bw.WriteInt(parent.Value.childrens.Count);
            foreach (KeyValuePair<string, CMNString> child in parent.Value.childrens)
                WriteVariables(bw, child);
        }
    }
}