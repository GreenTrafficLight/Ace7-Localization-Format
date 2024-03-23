using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ace7LocalizationFormat.Utils;
using Ace7LocalizationFormat.Stream;

namespace Ace7LocalizationFormat.Formats
{
    public class CMN
    {
        public class CmnString
        {
            public int StringNumber = -1;
            public string Name = null;
            public string FullName = null;
            public KeyValuePair<string, CmnString> Parent = new KeyValuePair<string, CmnString>();
            public List<KeyValuePair<string, CmnString>> childrens = new List<KeyValuePair<string, CmnString>>();

            public CmnString(int stringNumber, string name, KeyValuePair<string, CmnString> parent)
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

        public List<KeyValuePair<string, CmnString>> Root = new List<KeyValuePair<string, CmnString>>();
        
        // The highest string number in the CMN
        public int MaxStringNumber = 0;

        public CMN(string path)
        {
            Read(path);
        }

        public CMN(byte[] data) 
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

            data = DAT.Crypt(data, (uint)data.Length);
            data = CompressionHandler.Decompress(data);

            DATBinaryReader br = new DATBinaryReader(data);

            Root = ReadVariables(br, new KeyValuePair<string, CmnString>(null, null), Root);
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

            Root = ReadVariables(br, new KeyValuePair<string, CmnString>(null, null), Root);
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
        /// Add a new variable in the CMN
        /// </summary>
        /// <param name="variable">Variable to be added</param>
        /// <param name="stringNumber">String number tied to this variable</param>
        /// <returns>
        /// Retrun true if the variable didn't exist, false if the variable already exist
        /// </returns>
        public bool AddVariable(string variable, int? stringNumber = null)
        {
            KeyValuePair<string, CmnString> parent = Root.FirstOrDefault(x => variable.StartsWith(x.Key));
            while (parent.Value != null)
            {
                variable = variable.Remove(0, parent.Key.Length);
                KeyValuePair<string, CmnString> child = parent.Value.childrens.FirstOrDefault(x => variable.StartsWith(x.Key));
                if (child.Key == null && child.Value == null)
                {
                    // Variable already exist
                    if (variable == "")
                    {
                        break;
                    }
                    MaxStringNumber++;
                    MergeVariables(parent, variable, stringNumber);
                    return true;
                }
                parent = child;
            }
            return false;
        }

        /// <summary>
        /// Add a new variable in the CMN
        /// </summary>
        /// <param name="variable">Variable to be added</param>
        /// <param name="parent">The parent of the variable that will be added</param>
        /// <param name="stringNumber">String number tied to this variable</param>
        /// <returns>Return true if a variable has been successfully added</returns>
        public bool AddVariable(string variable, KeyValuePair<string, CmnString> parent, int? stringNumber = null)
        {
            while (true)
            {
                //variable = variable.Remove(0, parent.Key.Length);
                KeyValuePair<string, CmnString> child = parent.Value.childrens.FirstOrDefault(x => variable.StartsWith(x.Key));
                if (child.Key == null && child.Value == null)
                {
                    // Variable already exist
                    if (variable == "")
                    {
                        break;
                    }
                    MaxStringNumber++;
                    MergeVariables(parent, variable, stringNumber);
                    return true;
                }
                parent = child;
                variable = variable.Remove(0, parent.Key.Length);
            }
            return false;
        }

        /// <summary>
        /// Get the full variable string from a child CMN
        /// </summary>
        /// <param name="child">The child CMN that we want the full string</param>
        /// <returns>
        /// Return the full variable string of a child CMN
        /// </returns>
        public static string GetVariable(KeyValuePair<string, CmnString> child)
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
        /// 
        /// </summary>
        /// <param name="variable"></param>
        public int GetVariableStringNumber(string variable)
        {
            KeyValuePair<string, CmnString> parent = Root.FirstOrDefault(x => variable.StartsWith(x.Key));
            int variableStringNumber = -1;
            while (parent.Value != null)
            {
                variable = variable.Remove(0, parent.Key.Length);
                KeyValuePair<string, CmnString> child = parent.Value.childrens.FirstOrDefault(x => variable.StartsWith(x.Key));
                if (child.Key != null && child.Value != null)
                {
                    variableStringNumber = child.Value.StringNumber;
                }
                parent = child;
            }
            return variableStringNumber;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public bool CheckVariableExist(string variable)
        {
            KeyValuePair<string, CmnString> parent = Root.FirstOrDefault(x => variable.StartsWith(x.Key));
            while (parent.Value != null)
            {
                variable = variable.Remove(0, parent.Key.Length);
                KeyValuePair<string, CmnString> child = parent.Value.childrens.FirstOrDefault(x => variable.StartsWith(x.Key));
                if (child.Key == null && child.Value == null)
                {
                    // If the variable doesn't exist
                    if (variable == "")
                    {
                        break;
                    }
                    return true;
                }
                parent = child;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="value"></param>
        /// <param name="stringNumber"></param>
        private void MergeVariables(KeyValuePair<string, CmnString> parent, string value, int? stringNumber = null)
        {
            // Index where the added variable will be placed in the parent childrens
            int sortIndex = 0;
            foreach (KeyValuePair<string, CmnString> child in parent.Value.childrens)
            {
                int index = StringUtils.GetCommonSubstringIndex(child.Key, value);
                
                // If there is a node to merge
                if (index != -1)
                {
                    string subString = value.Substring(0, index + 1);

                    // Make a new node for the merged variable
                    KeyValuePair<string, CmnString> mergedCMNString = new KeyValuePair<string, CmnString>(subString, new CmnString(-1, subString, parent));

                    /// Existing node
                    // Substring the variable of the existing node
                    KeyValuePair<string, CmnString> existingCMNString = new KeyValuePair<string, CmnString>(child.Key.Substring(index + 1), new CmnString(child.Value.StringNumber, child.Key.Substring(index + 1), mergedCMNString));
                    // Add the childrens of the existing node
                    foreach (KeyValuePair<string, CmnString> existingCMNStringChild in child.Value.childrens){
                        existingCMNString.Value.childrens.Add(existingCMNStringChild);
                    }
                    // Add the existing node in the new merged variable
                    mergedCMNString.Value.childrens.Add(existingCMNString);
                    
                    /// New node
                    // Add the new node in the new merged variable
                    CmnString newCMNString = stringNumber == null ? new CmnString(MaxStringNumber, value.Substring(index + 1), mergedCMNString) : new CmnString(stringNumber.Value, value.Substring(index + 1), mergedCMNString);
                    // Compare the casing and number with the existing node
                    var comparisonResult = string.Compare(value.Substring(index + 1), child.Key.Substring(index + 1), StringComparison.Ordinal);
                    mergedCMNString.Value.childrens.Insert(comparisonResult < 0 ? 0 : mergedCMNString.Value.childrens.Count, new KeyValuePair<string, CmnString>(value.Substring(index + 1), newCMNString));

                    /// Parent node
                    // Insert the merged variable in the parent node
                    parent.Value.childrens.Insert(sortIndex, new KeyValuePair<string, CmnString>(mergedCMNString.Key, mergedCMNString.Value));
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
            parent.Value.childrens.Insert(sortIndex, new KeyValuePair<string, CmnString>(value, stringNumber == null ? new CmnString(MaxStringNumber, value, parent) : new CmnString(stringNumber.Value, value, parent)));
        }

        /// <summary>
        /// Read the variables inside a binary data
        /// </summary>
        private List<KeyValuePair<string, CmnString>> ReadVariables(DATBinaryReader br, KeyValuePair<string, CmnString> parent, List<KeyValuePair<string, CmnString>> node)
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
                KeyValuePair<string, CmnString> cmnString = new KeyValuePair<string, CmnString>(name, new CmnString(stringNumber, name, parent));
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
        private void WriteVariables(DATBinaryWriter bw, KeyValuePair<string, CmnString> parent)
        {
            bw.WriteInt(parent.Key.Length);
            bw.WriteString(parent.Key);
            bw.WriteInt(parent.Value.StringNumber);
            bw.WriteInt(parent.Value.childrens.Count);
            foreach (KeyValuePair<string, CmnString> child in parent.Value.childrens)
                WriteVariables(bw, child);
        }
    }
}