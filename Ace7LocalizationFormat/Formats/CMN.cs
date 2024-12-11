using Ace7LocalizationFormat.Stream;
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

            string directoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryPath)){
                Directory.CreateDirectory(directoryPath);
            }
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
                if (!node.ContainsKey(name)){
                    node.Add(name, cmnString);
                }
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
            bw.WriteInt(parent.Key.Length);
            bw.WriteString(parent.Key);
            bw.WriteInt(parent.StringNumber);
            bw.WriteInt(parent.Childrens.Count);
            foreach (CmnString child in parent.Childrens.Values)
                WriteVariables(bw, child);
        }

        /// <summary>
        /// Add a new variable to the Cmn
        /// </summary>
        /// <param name="newVariableName">The variable name that will be added to the parent</param>
        /// <param name="parent">The parent where the new variable will be added</param>
        /// <returns>
        /// If the new variable has been added
        /// </returns>
        public bool AddVariable(string newVariableName, SortedDictionary<string, CmnString> parent)
        {
            CmnString parentCmnString = GetVariable(newVariableName, parent, out bool alreadyExist);
            if (!alreadyExist)
            {
                MaxStringNumber++;
                MergeVariable(newVariableName.Substring(parentCmnString.Name.Length), parentCmnString);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add a new variable to the Cmn
        /// </summary>
        /// <param name="newCmnstring">The variable that will be added to the parent</param>
        /// <param name="parent">The parent where the new variable will be added</param>
        /// <returns>
        /// If the new variable has been added.
        /// </returns>
        public bool AddVariable(CmnString newCmnstring, SortedDictionary<string, CmnString> parent)
        {
            CmnString parentCmnString = GetVariable(newCmnstring.Name, parent, out bool alreadyExist);
            if (!alreadyExist)
            {
                MaxStringNumber++;
                MergeVariable(newCmnstring, parentCmnString);
#if DEBUG
                Console.WriteLine($"Added variable : {newCmnstring.Name}");
#endif
                return true;
            }
            else if (alreadyExist && parentCmnString.StringNumber != newCmnstring.StringNumber && parentCmnString.StringNumber == -1)
            {
                // The parentCmnString is the existing variable name
                // Update the string number of it
                // Brute force way to fix later, will probs create more problems
                MaxStringNumber++;
                parentCmnString.StringNumber = MaxStringNumber;

                Console.WriteLine($"Updated existing variable : {newCmnstring.Name}");

                return true;
            }
#if DEBUG
            Console.WriteLine($"Variable not added : {newCmnstring.Name}");
#endif
            return false;
        }

        /// <summary>
        /// Search a variable inside the parent
        /// </summary>
        /// <param name="variableName">The variable name to be search</param>
        /// <param name="parent">Where the variable name will start to be searched</param>
        /// <param name="alredyExist">If the variable name already exist</param>
        /// <returns>
        /// The parent CmnString of the variable searched
        /// </returns>
        public CmnString GetVariable(string variableName, SortedDictionary<string, CmnString> parent, out bool alredyExist)
        {
            CmnString parentCmnString = null;
            alredyExist = false;

            while (true)
            {
                bool updated = false;
                // Iterate each children of the parent CmnString
                foreach (string key in parent.Keys)
                {
                    // Find the common start string between the childrens Cmnstring and the variable that is going to be added
                    int subStringIndex = StringUtils.GetCommonSubstringIndex(key, variableName);

                    // If there a common start string found
                    if (subStringIndex != -1)
                    {
                        string matchingKey = variableName.Substring(0, subStringIndex + 1);
                        variableName = variableName.Substring(matchingKey.Length);
                        // If the variable doesn't exist
                        if (!parent.ContainsKey(matchingKey)){
                            return parentCmnString;
                        }
                        parentCmnString = parent[matchingKey];
                        parent = parent[matchingKey].Childrens;
                        updated = true;
                        break; // Restart iteration
                    }
                }
                if (!updated) break; // Exit if no updates
            }
            // If there are no common string found in the childrens
            // If the variable already exist
            if (variableName == "")
            {
                alredyExist = true;
            }
            return parentCmnString;
        }

        /// <summary>
        /// Search a variable inside the parent
        /// </summary>
        /// <param name="variableName">The variable name to be search</param>
        /// <param name="parent">Where the variable name will start to be searched</param>
        /// <returns>
        /// The parent CmnString of the variable searched
        /// </returns>
        public CmnString GetVariable(string variableName, SortedDictionary<string, CmnString> parent)
        {
            CmnString parentCmnString = null;

            while (true)
            {
                bool updated = false;
                foreach (string key in parent.Keys)
                {
                    int subStringIndex = StringUtils.GetCommonSubstringIndex(key, variableName);

                    if (subStringIndex != -1)
                    {
                        string matchingKey = variableName.Substring(0, subStringIndex + 1);
                        variableName = variableName.Substring(matchingKey.Length);
                        if (!parent.ContainsKey(matchingKey))
                        {
                            return parentCmnString;
                        }
                        parentCmnString = parent[matchingKey];
                        parent = parent[matchingKey].Childrens;
                        updated = true;
                        break; // Restart iteration
                    }
                }
                if (!updated) break; // Exit if no updates
            }
            return parentCmnString;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newVariableName"></param>
        /// <param name="parent"></param>
        public void MergeVariable(string newVariableName, CmnString parent)
        {
            foreach (string key in parent.Childrens.Keys)
            {
                int subStringIndex = StringUtils.GetCommonSubstringIndex(key, newVariableName);

                // Merge the nodes
                if (subStringIndex != -1)
                {
                    // Merged Node
                    string mergedCmnStringKey = key.Substring(0, subStringIndex + 1); // Merged node name
                    CmnString mergedCmnString = new CmnString(-1, mergedCmnStringKey, parent.Name + mergedCmnStringKey, parent);

                    // Existing Node (going to be added to the merged node
                    string existingCmnStringKey = key.Substring(subStringIndex + 1);
                    CmnString existingCmnString = new CmnString(parent.Childrens[key].StringNumber, existingCmnStringKey, mergedCmnString.Name + existingCmnStringKey, parent);
                    // Copy the children of the existing node to this one
                    existingCmnString.Childrens = new SortedDictionary<string, CmnString>(parent.Childrens[key].Childrens);
                    mergedCmnString.Childrens.Add(existingCmnStringKey, existingCmnString);

                    // New Node
                    string newCmnStringKey = newVariableName.Substring(subStringIndex + 1);
                    if (newCmnStringKey != "")
                    {
                        CmnString newCmnString = new CmnString(MaxStringNumber, newCmnStringKey, mergedCmnString.Name + newCmnStringKey, parent);
                        mergedCmnString.Childrens.Add(newCmnStringKey, newCmnString);
                    }


                    // Remove the existing node from the parent
                    parent.Childrens.Remove(key);

                    // Add the merged node in the parent
                    parent.Childrens.Add(mergedCmnStringKey, mergedCmnString);

                    return;
                }
            }
            // Add the new node
            parent.Childrens.Add(newVariableName, new CmnString(MaxStringNumber, newVariableName, parent.Name + newVariableName, parent));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addCmnString"></param>
        /// <param name="parent"></param>
        public void MergeVariable(CmnString addCmnString, CmnString parent)
        {
            string newVariableName = addCmnString.Name.Substring(parent.Name.Length);
            foreach (string key in parent.Childrens.Keys)
            {
                int subStringIndex = StringUtils.GetCommonSubstringIndex(key, newVariableName);

                // Merge the nodes
                if (subStringIndex != -1)
                {
                    // Merged Node
                    string mergedCmnStringKey = key.Substring(0, subStringIndex + 1); // Merged node name
                    CmnString mergedCmnString = new CmnString(-1, mergedCmnStringKey, parent.Name + mergedCmnStringKey, parent);

                    // Existing Node (going to be added to the merged node
                    string existingCmnStringKey = key.Substring(subStringIndex + 1);
                    CmnString existingCmnString = new CmnString(parent.Childrens[key].StringNumber, existingCmnStringKey, mergedCmnString.Name + existingCmnStringKey, parent);
                    // Copy the children of the existing node to this one
                    existingCmnString.Childrens = new SortedDictionary<string, CmnString>(parent.Childrens[key].Childrens);
                    mergedCmnString.Childrens.Add(existingCmnStringKey, existingCmnString);

                    // New Node
                    string newCmnStringKey = newVariableName.Substring(subStringIndex + 1);
                    if (newCmnStringKey != "")
                    {
                        CmnString newCmnString = new CmnString(MaxStringNumber, newCmnStringKey, mergedCmnString.Name + newCmnStringKey, parent);
                        mergedCmnString.Childrens.Add(newCmnStringKey, newCmnString);
                    }
                    else if (addCmnString.StringNumber != -1)
                    {
                        // Merged node that contains a string
                        mergedCmnString.StringNumber = MaxStringNumber;
#if DEBUG
                        Console.WriteLine($"Merged node that contains a string : {addCmnString.Name}");
#endif
                    }

                    // Remove the existing node from the parent
                    parent.Childrens.Remove(key);

                    // Add the merged node in the parent
                    parent.Childrens.Add(mergedCmnStringKey, mergedCmnString);

                    return;
                }
            }
            // Add the new node
            parent.Childrens.Add(newVariableName, new CmnString(MaxStringNumber, newVariableName, parent.Name + newVariableName, parent));
        }
    }
}