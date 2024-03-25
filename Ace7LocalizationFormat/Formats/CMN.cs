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
            public CmnString Parent = null;
            public List<CmnString> Childrens = new List<CmnString>();
            public int Index = -1;

            public CmnString(int stringNumber, string name, CmnString parent, int index)
            {
                StringNumber = stringNumber;
                Name = name;
                FullName = name;
                Parent = parent;
                if (parent != null)
                {
                    FullName = GetVariable(parent) + FullName;
                }
                Index = index;
            }
        }

        public List<CmnString> Root = new List<CmnString>();
        
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

            Root = ReadVariables(br, null, Root);
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

            Root = ReadVariables(br, null, Root);
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
        public bool AddVariable(string variable)
        {
            CmnString parent = Root.FirstOrDefault(x => variable.StartsWith(x.Name));
            while (parent != null)
            {
                variable = variable.Remove(0, parent.Name.Length);
                CmnString child = parent.Childrens.FirstOrDefault(x => variable.StartsWith(x.Name));
                if (child == null)
                {
                    // Variable already exist
                    if (variable == "")
                    {
                        break;
                    }
                    MaxStringNumber++;
                    MergeVariables(parent, variable);
                    return true;
                }
                parent = child;
            }
            return false;
        }

        /// <summary>
        /// Add a new variable in the CMN
        /// </summary>
        /// <param name="newVariableName">Variable to be added</param>
        /// <param name="parent">The parent of the variable that will be added</param>
        /// <param name="stringNumber">String number tied to this variable</param>
        /// <returns>Return true if a variable has been successfully added</returns>
        public bool AddVariable(string newVariableName, CmnString parent)
        {
            while (true)
            {
                //variable = variable.Remove(0, parent.Key.Length);
                CmnString child = parent.Childrens.FirstOrDefault(x => newVariableName.StartsWith(x.Name));
                if (child == null)
                {
                    // Variable already exist
                    if (newVariableName == "")
                    {
                        break;
                    }
                    MaxStringNumber++;
                    MergeVariables(parent, newVariableName);
                    return true;
                }
                parent = child;
                newVariableName = newVariableName.Remove(0, parent.Name.Length);
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void RenameVariable(string newVariableName, CmnString cmnNode)
        {
            foreach (var children in cmnNode.Childrens)
            {

            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public List<int> DeleteVariable(CmnString cmnNode) 
        {
            List<CmnString> childrens = cmnNode.Parent != null ? cmnNode.Parent.Childrens : Root;

            List<int> variableStringNumbers = GetChildrensStringNumber(cmnNode, new List<int>());

            childrens.RemoveAt(cmnNode.Index);
            // Update index
            for (int i = cmnNode.Index; i < childrens.Count; i++)
            {
                childrens[i].Index = i;
            }

            // To do, update string numbers

            return variableStringNumbers;
        }

        /// <summary>
        /// Get the full variable string from a child CMN
        /// </summary>
        /// <param name="child">The child CMN that we want the full string</param>
        /// <returns>
        /// Return the full variable string of a child CMN
        /// </returns>
        public static string GetVariable(CmnString child)
        {
            string variable = child.Name;
            while (child.Parent != null)
            {
                variable = string.Concat(child.Parent.Name, variable);
                child = child.Parent;
            }
            return variable;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        public int GetVariableStringNumber(string variable)
        {
            CmnString parent = Root.FirstOrDefault(x => variable.StartsWith(x.Name));
            int variableStringNumber = -1;
            while (parent != null)
            {
                variable = variable.Remove(0, parent.Name.Length);
                CmnString child = parent.Childrens.FirstOrDefault(x => variable.StartsWith(x.Name));
                if (child != null)
                {
                    variableStringNumber = child.StringNumber;
                }
                parent = child;
            }
            return variableStringNumber;
        }

        /// <summary>
        /// </summary>
        /// 
        public List<int> GetChildrensStringNumber(CmnString cmnNode, List<int> variableStringNumbers)
        {
            foreach (var children in cmnNode.Childrens)
            {
                variableStringNumbers = GetChildrensStringNumber(children, variableStringNumbers);
                variableStringNumbers.Add(children.StringNumber);
            }
            return variableStringNumbers;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public bool CheckVariableExist(string variable)
        {
            CmnString parent = Root.FirstOrDefault(x => variable.StartsWith(x.Name));
            while (parent != null)
            {
                variable = variable.Remove(0, parent.Name.Length);
                CmnString child = parent.Childrens.FirstOrDefault(x => variable.StartsWith(x.Name));
                if (child == null)
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
        private void MergeVariables(CmnString parent, string value)
        {
            // Index where the added variable will be placed in the parent childrens
            int sortNodeIndex = 0;
            foreach (CmnString child in parent.Childrens)
            {
                int subStringIndex = StringUtils.GetCommonSubstringIndex(child.Name, value);
                
                // If there is a node to merge
                if (subStringIndex != -1)
                {
                    // Merged node name
                    string mergedCmnStringName = value.Substring(0, subStringIndex + 1);
                    // New existing node name
                    string existingCmnStringName = child.Name.Substring(subStringIndex + 1);
                    // New node name
                    string newCmnStringName = value.Substring(subStringIndex + 1);

                    // Compare the casing and number with the existing node
                    var comparisonResult = string.Compare(newCmnStringName, existingCmnStringName, StringComparison.Ordinal);

                    // Make a new node for the merged variable
                    CmnString mergedCmnString = new CmnString(-1, mergedCmnStringName, parent, sortNodeIndex);

                    /// Existing node
                    // Substring the variable of the existing node
                    CmnString existingCmnString = new CmnString(child.StringNumber, existingCmnStringName, mergedCmnString, comparisonResult < 0 ? 1 : 0);
                    // Add the childrens of the existing node
                    foreach (CmnString existingCMNStringChild in child.Childrens){
                        existingCmnString.Childrens.Add(existingCMNStringChild);
                    }
                    // Add the existing node in the new merged variable
                    mergedCmnString.Childrens.Add(existingCmnString);

                    /// New node
                    // Add the new node in the new merged variable
                    CmnString newCmnString = new CmnString(MaxStringNumber, newCmnStringName, mergedCmnString, comparisonResult < 0 ? 0 : mergedCmnString.Childrens.Count);
                    mergedCmnString.Childrens.Insert(comparisonResult < 0 ? 0 : mergedCmnString.Childrens.Count, newCmnString);

                    /// Parent node
                    // Insert the merged variable in the parent node
                    parent.Childrens.Insert(sortNodeIndex, mergedCmnString);
                    // Remove the existing node from the parent node
                    parent.Childrens.Remove(child);

                    return;
                }

                // Compare the casing and number of the added variable with the existing childrens
                if (string.Compare(value, child.Name, StringComparison.Ordinal) < 0) {
                    break; // Break the loop if the added variable has a inferior order
                }

                // Increase the index where the added variable will be placed in the parent childrens
                sortNodeIndex++;
            }
            // If there isn't any node to merge
            parent.Childrens.Insert(sortNodeIndex, new CmnString(MaxStringNumber, value, parent, sortNodeIndex));
            for (int i = sortNodeIndex + 1; i < parent.Childrens.Count; i++)
            {
                parent.Childrens[i].Index++;
            }
        }

        /// <summary>
        /// Read the variables inside a binary data
        /// </summary>
        private List<CmnString> ReadVariables(DATBinaryReader br, CmnString parent, List<CmnString> node)
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
                CmnString cmnString = new CmnString(stringNumber, name, parent, i);
                node.Add(cmnString);
                ReadVariables(br, node.FirstOrDefault(x => x.Name == name), node.FirstOrDefault(x => x.Name == name).Childrens);
            }
            return node;
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
            foreach (CmnString child in parent.Childrens)
                WriteVariables(bw, child);
        }
    }
}