using System;
using System.Windows;
using System.IO;


namespace FileParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int[] nodes;                // Array of NodeTypes shared between functions
        int nodeIndex = 0;          // Index pointer for the type of node being processed
        long ptrBasePosition = 0;   // Position of root node for reference
        string _filename;           // File Name used for OpenFileDialogue

        /// <summary>
        /// Window Initialization
        /// </summary>
        public MainWindow()
        {
            // Initialize the window
            InitializeComponent();            
        }

        /// <summary>
        /// Helper function to load files.
        /// </summary>
        /// <returns></returns>
        private string GetFile(string ext)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Check settings for a file location
            if (Properties.Settings.Default.WorkingFolder != "Working Folder")
            {
                dlg.InitialDirectory = Properties.Settings.Default.WorkingFolder;
            }
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ext;
            dlg.Filter = ext.ToUpper() + " Files (*." + ext.ToLower() + ")|*." + ext.ToLower();

            // I left this here for reference
            /*dlg.Filter = "HDR Files (*.hdr)|*.hdr|" +
                "LOD Files (*.lod)|*.lod" +
                "|CT Files (*.ct)|*.ct" +
                "|UCD Files (*.ucd)|*.ucd" +
                "|FED Files (*.fed)|*.fed" +
                "|OCD Files (*.ocd)|*.ocd" +
                "|WCD Files (*.wcd)|*.wcd" +
                "|FCD Files (*.fcd)|*.fcd" +
                "|FED Files (*.fed)|*.fed" +
                "|VCD Files (*.vcd)|*.vcd" +
                "|FED Files (*.fed)|*.fed" +
                "|WLD Files (*.wld)|*.wld" +
                "|PHD Files (*.phd)|*.phd" +
                "|PD Files (*.pd)|*.pd" +
                "|PDX Files (*.pdx)|*.pdx" +
                "|RCD Files (*.rcd)|*.rcd" +
                "|ICD Files (*.icd)|*.icd" +
                "|RWD Files (*.rwd)|*.rwd" +
                "|VSD Files (*.vsd)|*.vsd" +
                "|SWD Files (*.swd)|*.swd" +
                "|SSD Files (*.ssd)|*.ssd" +
                "|RKT Files (*.rkt)|*.rkt";
            */

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable <bool> result = dlg.ShowDialog();

            // If a file was selected, update the default folder and save the settings before we return the filename
            if (result != false)
            {
                FileInfo fi = new FileInfo(dlg.FileName);
                Properties.Settings.Default.WorkingFolder = fi.DirectoryName;
                Properties.Settings.Default.Save();
                return dlg.FileName;
            }

            // No File selected
            return null;            
        }

        /// <summary>
        /// Event Handler for HEADER button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Check if the HDR file is in the Working Directory
            if (!File.Exists(WorkingFolder.Text + "\\KoreaObj.hdr"))
                // If not, open the OpenFileDialogue to find it
                _filename = GetFile("hdr");
            else
                // If the file exists, use it
                _filename = WorkingFolder.Text + "\\KoreaObj.hdr";

            // Check for Cancel Button
            if (_filename == null)
                return;

            // Call the helper function to Parse the HDR file if one was selected
            if (_filename != null && _filename.ToUpper().Contains("HDR"))
            {
                ParseHeader(_filename);
            }                                  
        }   

        /// <summary>
        /// Event handler for LOD button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Verify the text in the input box is actually a number
            Int32.TryParse(LTB1.Text, out int processTB);

            // If the text is not a number or the Details checkbox is unchecked
            // force the program to run from the first entry
            if (processTB == 0 && LTB1.Text != "0")
            {
                LTB1.Text = "1";
            }

            // Pick a File
            if (!File.Exists(WorkingFolder.Text + "\\KoreaObj.lod"))
                _filename = GetFile("lod");
            else
                _filename = WorkingFolder.Text + "\\KoreaObj.lod";

            // Check for Cancel Button
            if (_filename == null)
                return;

            // Start the Parse with the Given File Position and FileName
            try
            {
                ParseLOD(new BinaryReader(File.Open(_filename, FileMode.Open)), Int32.Parse(LTB1.Text));
            }
            catch
            {                 
                MessageBox.Show("Unable to open the File.  The file is already being used by another program.", "Unable to Open File");
            }
               
        }

        /// <summary>
        /// Helper function to read the HDR file
        /// </summary>
        void ParseHeader(string FileName)
        {
            // Verify the text in the input box is actually a number
            Int32.TryParse(TB1.Text, out int processTB);
            
            // If the textbox is non a number force the program
            // to run at least one header entry
            if (processTB== 0)
            {
                TB1.Text = "1";
            }

            // Open the File with a BinaryReader   
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(FileName, FileMode.Open)))
                {
                    long dataAddress = 0;                       // Placeholder for a ptr to data                           
                    long fPos = reader.BaseStream.Position;     // Place holder to keep track of the line where data came from during printing

                    // Clear the text from the textbox
                    Text1.Clear();

                    // File Version
                    Text1.AppendText("(" + reader.BaseStream.Position + ") File Version: " + reader.ReadInt32().ToString() + "\n");

                    // Read and print the Number of Colors
                    // Update the Position where we're about to read from
                    fPos = reader.BaseStream.Position;
                    int nColors = reader.ReadInt32();
                    Text1.AppendText("(" + fPos + ") Number of Colors: " + nColors + "\n");

                    // Read and print the number of Dark Colors
                    fPos = reader.BaseStream.Position;
                    int nDarkColors = reader.ReadInt32();
                    Text1.AppendText("(" + fPos + ") Number of Dark Colors: " + nDarkColors + "\n");

                    // Print the size of the Color Array
                    Text1.AppendText("    Color Array: " + (16 * nColors) + " Bytes\n");

                    // Set the location we need if we're going to skip the Color Array
                    dataAddress = reader.BaseStream.Position + (16 * nColors);

                    // If the Colors CB is checked, output the number of entries entered, 
                    // or the full array, whichever is less
                    if ((bool)CB1.IsChecked && (bool)ColorCB.IsChecked)
                    {
                        Text1.AppendText("    ****Color Array Contents****\n");
                        for (int i = 0; i < Math.Min(Int32.Parse(TB1.Text), nColors); i++)
                        {
                            // Colors are stored ABGR in float format
                            Text1.AppendText("      ****Entry " + i + "****\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Alpha: " + reader.ReadSingle() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Blue: " + reader.ReadSingle() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Green: " + reader.ReadSingle() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Red: " + reader.ReadSingle() + "\n\n");
                        }
                    }
                    // Jump to the correct position, needed if we didn't print the entire Color Array
                    reader.BaseStream.Position = dataAddress;

                    // Palletes
                    fPos = reader.BaseStream.Position;

                    // Get and print the number of Palettes
                    int nPalettes = reader.ReadInt32();
                    Text1.AppendText("(" + fPos + ") Number of Pallettes: " + nPalettes + "\n");

                    // Print the size of the Palette Array
                    Text1.AppendText("    Palette Array: " + 1032 * nPalettes + " Bytes \n");

                    // Set the location we need if we're going to skip the Palette Array
                    dataAddress = reader.BaseStream.Position + (1032 * nPalettes);

                    // If the Palette CB is checked, output the number of entries entered, 
                    // or the full array, whichever is less
                    if ((bool)CB1.IsChecked && (bool)PaletteCB.IsChecked)
                    {
                        Text1.AppendText("    ****Palette Array Contents****\n");
                        for (int i = 0; i < Math.Min(Int32.Parse(TB1.Text), nPalettes); i++)
                        {
                            Text1.AppendText("      ****Entry " + i + "****\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       ARRAY[Int32 x 256] : 1024 Bytes\n");
                            reader.BaseStream.Position += 1024;
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       (Int32) Palette Handle: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       (Int32) Reference Count: " + reader.ReadInt32() + "\n\n");
                        }
                    }
                    // Jump to the correct position, needed if we didn't print the entire Palette Array
                    reader.BaseStream.Position = dataAddress;

                    // Textures
                    fPos = reader.BaseStream.Position;

                    // Get and print the Number of Textures
                    int ntextures = reader.ReadInt32();
                    Text1.AppendText("(" + fPos + ") Number of Textures: " + ntextures + "\n");
                    fPos = reader.BaseStream.Position;

                    // Get and print the Max CompressedSize
                    int maxSize = reader.ReadInt32();
                    Text1.AppendText("(" + fPos + ")  Max Compressed Size: " + maxSize + " Bytes\n");

                    // Set the location we need if we skip the array
                    dataAddress = reader.BaseStream.Position + (40 * ntextures);

                    // Print the Array size
                    Text1.AppendText("    Texture Array: " + (40 * ntextures) + " Bytes\n");

                    // Print the Array elements if the CB is checked
                    if ((bool)CB1.IsChecked && (bool)TextureCB.IsChecked)
                    {
                        Text1.AppendText("    ****Texture Array Contents****\n");
                        for (int i = 0; i < Math.Min(Int32.Parse(TB1.Text), ntextures); i++)
                        {
                            Text1.AppendText("      ****Entry " + i + "****\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       File Offset: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Size: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Dimension: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Image Pointer: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Palette Pointer: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Flag Pointer: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Chroma: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Texture Handle: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Palette ID: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Reference Count: " + reader.ReadInt32() + "\n\n");
                        }
                    }
                    // Jump to the end of the array
                    reader.BaseStream.Position = dataAddress;

                    // Max Element Size
                    fPos = reader.BaseStream.Position;
                    int maxElement = reader.ReadInt32();
                    Text1.AppendText("(" + fPos + ") Max Element Size: " + maxElement + "\n");

                    // Lod Headers
                    fPos = reader.BaseStream.Position;

                    // Get and print the number of LOD Headers and the Array Size
                    int nHeaders = reader.ReadInt32();
                    Text1.AppendText("(" + fPos + ") Number of LOD Headers: " + nHeaders + "\n");
                    Text1.AppendText("    LOD Header Array: " + (20 * nHeaders) + " Bytes \n");

                    // Set the position to jump to after the array
                    dataAddress = reader.BaseStream.Position + (20 * nHeaders);
                    if ((bool)CB1.IsChecked && (bool)LHCB.IsChecked)
                    {
                        Text1.AppendText("    ****LOD Header Array Contents****\n");
                        for (int i = 0; i < Math.Min(Int32.Parse(TB1.Text), nHeaders); i++)
                        {
                            Text1.AppendText("      ****Entry " + i + "****\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Reference Count: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       On Order: " + reader.ReadInt16() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Flags: " + reader.ReadInt16() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Node Pointer: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       File Offset: " + reader.ReadInt32() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       File Size: " + reader.ReadInt32() + "\n\n");
                        }
                    }
                    reader.BaseStream.Position = dataAddress;

                    // Parent Headers
                    fPos = reader.BaseStream.Position;
                    int nParentHeaders = reader.ReadInt32();

                    Text1.AppendText("(" + fPos + ") Number of Parent Headers: " + nParentHeaders + "\n");
                    dataAddress = reader.BaseStream.Position + (48 * nParentHeaders);
                    Text1.AppendText("    Parent Header Array: " + (48 * nParentHeaders) + " Bytes \n");
                    if ((bool)CB1.IsChecked && (bool)PHCB.IsChecked)
                    {
                        int _indexoffset = 0;
                        Text1.AppendText("    ****Parent Header Array Contents****\n");
                        if (HDRIndex.Text != "0")
                        {
                            Int32.TryParse(HDRIndex.Text, out int _index);
                            reader.BaseStream.Position += (_index * 48);
                            _indexoffset = _index;
                        }
                        for (int i = 0; i < Math.Min(Int32.Parse(TB1.Text), nParentHeaders); i++)
                        {
                            Text1.AppendText("      ****Entry " + (i + _indexoffset) + "****\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Radius: " + reader.ReadSingle().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       MinX: " + reader.ReadSingle().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       MaxX: " + reader.ReadSingle().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       MinY: " + reader.ReadSingle().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       MaxY: " + reader.ReadSingle().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       MinZ: " + reader.ReadSingle().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       MaxZ: " + reader.ReadSingle().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       LOD Record Pointer: " + reader.ReadInt32().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Slot Pointer: " + reader.ReadInt32().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Texture Count: " + reader.ReadInt16().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Dynamic Coords Count: " + reader.ReadInt16().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       LOD Count: " + reader.ReadByte().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Switch Count: " + reader.ReadByte().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       DOF Count: " + reader.ReadByte().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       Slot Count: " + reader.ReadByte().ToString() + "\n");
                            Text1.AppendText("(" + reader.BaseStream.Position + ")       RefCount: " + reader.ReadInt32().ToString() + "\n\n");
                        }
                    }
                    reader.BaseStream.Position = dataAddress;

                    // The Slot/DynCoords/LODRef Arrays follow take up the rest of the file
                    // We aren't parsing them here but we will print the structures
                    Text1.AppendText("\nThe Slot/DynCoords/LODRef Arrays take up the rest of the file.\n");
                    Text1.AppendText("There is one entry of each of the following arrays for every\n");
                    Text1.AppendText("Parent Header with at least 1 LOD assigned to it.  The arrays\n");
                    Text1.AppendText("are arranged one each for every entry, not in groups of similar\n");
                    Text1.AppendText("types: Entry 0: Slot/Dynamic/LODRef, Entry 1: Slot/Dynamic/LODRef. ");
                    Text1.AppendText("Slot Array - 12 Bytes * nSlots from Parent Header:\n");
                    Text1.AppendText("Float X - 4 Bytes\n");
                    Text1.AppendText("Float Y - 4 Bytes\n");
                    Text1.AppendText("Float Z - 4 Bytes\n");
                    Text1.AppendText("Dynamic Coordinates - 12 Bytes * nDynamicCoords from Parent Header:\n");
                    Text1.AppendText("Float X - 4 Bytes\n");
                    Text1.AppendText("Float Y - 4 Bytes\n");
                    Text1.AppendText("Float Z - 4 Bytes\n");
                    Text1.AppendText("LOD Reference Structure - 8 Bytes * nLODs from Parent Header:\n");
                    Text1.AppendText("Int32 LOD ID - 4 Bytes\n");
                    Text1.AppendText("Float MaxRange - 4 Bytes\n");

                } // End Using
            }
            catch
            {
                MessageBox.Show("Unable to open the File.  The file is already being used by another program.", "Unable to Open File");
            }
            
        }  

        /// <summary>
        /// Helper Function to read the LOD file.  Prints the Generic LOD data
        /// prior to reaching the ROOT node.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrStart"></param>
        /// <returns></returns>
        long ParseLOD(BinaryReader reader, long ptrStart)
        {
            // Clear the textbox
            Text1.Clear();

            // Initialize the node index counter
            nodeIndex = 0;

            // LOD File
            // Open the File
            using (reader)
            {
                long fPos = ptrStart;                            // Current location of stream
                ptrBasePosition = ptrStart;                      // Starting position of each node

                // Seek to the LOD we want
                reader.BaseStream.Position = ptrStart;

                // Get the number of nodes and print the node count
                int nNodes = reader.ReadInt32();
                Text1.AppendText("Number of Nodes: " + nNodes + "\n");

                // Create an array to hold the Node Types
                nodes = new int[nNodes];

                // Fill the Array with the types of Nodes and print them to the box               
                for (int i = 0; i < nNodes; i++)
                {
                    fPos = reader.BaseStream.Position;
                    nodes[i] = reader.ReadInt32();
                    Text1.AppendText("(" + fPos + ") Entry " + i + ": " + nodes[i].ToString() + "\n");                   
                }       

                // Call the function to start processing the tree, pass in the location to
                // start reading from (ROOT Node)
                ProcessTree(reader, reader.BaseStream.Position);

                // This is for debugging only, we don't need to return anything here
                return reader.BaseStream.Position;
            }   // End Using
        }

        /// <summary>
        /// Main function to parse each LOD TREE.
        /// </summary>
        /// <remarks>Semi-Recursive function</remarks>
        /// <param name="reader">The File Object</param>
        /// <param name="ptrStart">Where the reader will start reading from</param>
        /// <returns></returns>
        long ProcessTree(BinaryReader reader, long ptrStart)
        {
            // Seek to the correct position
            reader.BaseStream.Position = ptrStart;

            // Setup Variables required to track the stream
            long fPos = reader.BaseStream.Position;                 // Current location of stream                         
            long nextNode = 0;                                      // The next Node we will read
            
            // Loop through the Node Types and print the required data to the box
            // -1 means there are no more Nodes in the current TREE (Exit recursion)
            while (nextNode != -1)
            {

                switch (nodes[nodeIndex++])
                {
                    // Fill the text box with the appropriate data based on the type of Node
                    case 0:  //BNode
                        Text1.AppendText("***BNode***\n");
                        PrintBNode(reader, ptrBasePosition);
                        break;

                    case 1:  //BSubTreeNode
                             // Inherited BNode Data
                        Text1.AppendText("***BSubTree***\n");
                        PrintBNode(reader, ptrBasePosition);
                        
                        // SubTree Data
                        nextNode = PrintBSubTree(reader, ptrBasePosition);                        
                        break;

                    case 2:  //BRootNode
                             // File offsets are based from the root node                         
                        ptrBasePosition = reader.BaseStream.Position;

                        // Inherited SubTree->Bnode
                        Text1.AppendText("***BRoot***\n");
                        PrintBNode(reader, ptrBasePosition);

                        // Inherited SubTree Data
                        nextNode = PrintBSubTree(reader, ptrBasePosition);

                        // BRoot Data
                        PrintBRoot(reader, ptrBasePosition);
                        break;

                    case 3:  //BSlotNode
                        Text1.AppendText("***BSlotNode***\n");
                        // Inherited BNode Data                            
                        nextNode = PrintBNode(reader, ptrBasePosition);

                        // BSlotNode Data
                        PrintSlot(reader);
                        break;

                    case 4:  //BDOFNode
                        Text1.AppendText("***BDofNode***\n");
                        // Inherited SubTree->BNode Data                            
                        PrintBNode(reader, ptrBasePosition);

                        // Inherited SubTree Data
                        nextNode = PrintBSubTree(reader, ptrBasePosition);

                        // DOFNode Data
                        PrintDof(reader);
                        break;

                    case 5:  //BSwitchNode
                        Text1.AppendText("***BSwitchNode***\n");
                        // Inherited BNode Data    
                        PrintBNode(reader, ptrBasePosition);

                        // SwitchNode Data
                        nextNode = PrintSwitch(reader, ptrBasePosition);
                        break;
                        // return -1;
                    case 6:  //BSplitterNode
                        Text1.AppendText("***BSplitterNode***\n");
                        // Inherited BNode Data  
                        PrintBNode(reader, ptrBasePosition);

                        // BSplitterNode Data
                        nextNode = PrintSplitter(reader, ptrBasePosition);
                        break;

                    case 7:  //BPrimitiveNode 
                        Text1.AppendText("***BPrimitiveNode***\n");
                        // Inherited BNode Data
                        nextNode = PrintBNode(reader, ptrBasePosition);

                        // PrimitiveNode Data
                        PrintPoly(reader, ptrBasePosition);
                        break;

                    case 8:  //BLitPrimitiveNode
                        Text1.AppendText("***BLitPrimitiveNode***\n");
                        // Inherited BNode Data  
                        nextNode = PrintBNode(reader, ptrBasePosition);

                        // BLitPrimitive Data
                        PrintLitPrimitive(reader, ptrBasePosition);
                        break;

                    case 9:  //BCulledPrimitiveNode
                        Text1.AppendText("***BCulledPrimitiveNode***\n");
                        // Inherited BNode Data  
                        nextNode = PrintBNode(reader, ptrBasePosition);

                        // BCulledPrimitiveNode Data
                        PrintPoly(reader, ptrBasePosition);
                        break;

                    case 10:  //BSpecialXForm
                        Text1.AppendText("***BSpecialXForm***\n");
                        // Inherited BNode Data  
                        nextNode = PrintBNode(reader, ptrBasePosition);

                        // Special XForm Data
                        PrintSpecial(reader, ptrBasePosition);
                        break;

                    case 11:  //BLightStringNode
                        Text1.AppendText("***BLightStringNode***\n");
                        // Inherited BPrimitiveNode->BNode Data  
                        nextNode = PrintBNode(reader, ptrBasePosition);

                        // Inherited BPrimitiveNode data
                        PrintPoly(reader, ptrBasePosition);

                        // BLitPrimitiveNode Data
                        PrintLightString(reader, ptrBasePosition);
                        break;

                    case 12: //BTransNode
                        Text1.AppendText("****BTransNode****\n");
                        //Inherited BSubtree->BNode
                        PrintBNode(reader, ptrBasePosition);

                        // Inherited SubTree
                        nextNode = PrintBSubTree(reader, ptrBasePosition);

                        // TransNode
                        PrintTransNode(reader, ptrBasePosition);
                        break;

                    case 13: //ScalNode
                        Text1.AppendText("****BScaleNode****\n");
                        // Inherited Subtree->Bnode
                        PrintBNode(reader, ptrBasePosition);

                        // Inherited Subtree
                        nextNode = PrintBSubTree(reader, ptrBasePosition);

                        // ScaleNode
                        PrintScalNode(reader, ptrBasePosition);
                        break;

                    case 14: //XDOF Node
                        Text1.AppendText("****BXDofNod****\n");
                        // Inherited Subtre->BNode
                        PrintBNode(reader, ptrBasePosition);

                        // Inherited Subtree
                        nextNode = PrintBSubTree(reader, ptrBasePosition);

                        // XDof Nod
                        PrintXDofNode(reader, ptrBasePosition);
                        break;

                    case 15: // XSwitch Node
                        Text1.AppendText("****BXSwitchNode****\n");
                        //Inherited BNode
                        nextNode = PrintBNode(reader, ptrBasePosition);

                        // XSwitch Node
                        PrintXSwitchNode(reader, ptrBasePosition);
                        break;

                    case 16: // Render Control Node
                        Text1.AppendText("****BRenderControlNode****\n");
                        // Inherited BNode
                        nextNode = PrintBNode(reader, ptrBasePosition);

                        // RenderControlNode
                        PrintRenderControlNode(reader, ptrBasePosition);
                        break;

                    case 17: // Culled Node
                        Text1.AppendText("****BCulledNode****\n");
                        // Inherited BNode
                        nextNode = PrintBNode(reader, ptrBasePosition);

                        // Culled Node
                        PrintCulledNode(reader, ptrBasePosition);
                        break;
                }   // End Switch 

                // Seek to the next node or return the current position for the
                // next node outside this tree
                if (nextNode > 0)
                    reader.BaseStream.Position = nextNode;                               
                else                
                    return reader.BaseStream.Position;
            }   // End For
            return reader.BaseStream.Position;
        }

        /// <summary>
        /// Function to process a BNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        /// <returns></returns>
        long PrintBNode(BinaryReader reader, long ptrBase)
        {     
            // Every node starts with a BNode: VFT Ptr and Sibling Ptr (NextNode)
            Text1.AppendText("(" + reader.BaseStream.Position + ") VFT Ptr: " + reader.ReadInt32().ToString() + "\n");
            long offset = reader.ReadInt32();
            if (offset > -1)
                offset += ptrBase;
            Text1.AppendText("(" + (reader.BaseStream.Position-4) + ") Sibling Ptr:" + offset + "\n");

            // Return the Sibling Ptr for the nextNode
            return offset;
        }

        /// <summary>
        /// Function to process a SubTreeNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        /// <returns>SubTree Ptr</returns>
        long PrintBSubTree (BinaryReader reader, long ptrBase)
        {
            long fPos = reader.BaseStream.Position;                 // Current position of the reader
            long ptrBasePosition = ptrBase;                         // Starting position of each node
            long returnAddress = 0;                                 // Address to return to after reading data from the file
            long dataAddress = 0;                                   // Address to seek to to find referenced data
            int nLoops = 0;                                         // Number of loops to perform for Coords, Normals, etc...            

            /*** Coords ***/
            // Get the current stream position for text output
            fPos = reader.BaseStream.Position;

            // Get the address we need for our Coords data
            dataAddress = reader.ReadInt32();
            // All pointers are OFFSETS so we have to adjust to offset from the ROOT node
                if (dataAddress > 0)
                    dataAddress += ptrBasePosition;        

            // Print Coords Address to screen
            Text1.AppendText("(" + fPos + ") Coords ptr: " + dataAddress + "\n");

            // Get the Number of Coords
            fPos = reader.BaseStream.Position;
            nLoops = reader.ReadInt32();

            // Print Number of Coords to Screen
            Text1.AppendText("(" + fPos + ") Number of Coords: " + nLoops + "\n");

            // Display the Coord data if there are any                            
            if (nLoops > 0)
            {
                // Set the return address after we read the data
                returnAddress = reader.BaseStream.Position;

                // Seek to the position in the file if there is data to read
                if (dataAddress > 0)
                    reader.BaseStream.Position = dataAddress;

                // Read the coords
                for (int j = 0; j < nLoops; j++)
                {
                    // Build the Coord Format and Print to Screen
                    Text1.AppendText("(" + reader.BaseStream.Position + ")      Coord " + j + ": (" + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ")\n");
                }

                // Return to the next line after our seek
                reader.BaseStream.Position = returnAddress;
            }

            /*** Dynamic Coords ***/
            // Number of Dynamic Coords
            fPos = reader.BaseStream.Position;
            nLoops = reader.ReadInt32();
            Text1.AppendText("(" + fPos + ") Number of Dynamic Coords: " + nLoops + "\n");

            // Address of Dynamic Coords
            fPos = reader.BaseStream.Position;
            dataAddress = reader.ReadInt32();
            if (dataAddress > 0)
                dataAddress += ptrBasePosition;

            Text1.AppendText("(" + fPos + ") Dynamic Coords Offset: " + dataAddress + "\n");

            // Print the Dynamic Coords
            if (nLoops > 0)
            {
                returnAddress = reader.BaseStream.Position;
                if (dataAddress > 0)
                    reader.BaseStream.Position = dataAddress;
                // Print the dynamic coords
                for (int j = 0; j < nLoops; j++)
                {
                    Text1.AppendText("(" + reader.BaseStream.Position + ")      Dynamic Coord " + j + ": (" + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ")\n");
                }

                // Return to the next line after our seek
                reader.BaseStream.Position = returnAddress;
            }

            /*** Normals ***/
            // Address of Normals
            fPos = reader.BaseStream.Position;
            dataAddress = reader.ReadInt32();
                if (dataAddress > 0)
                    dataAddress+= ptrBasePosition;          
            Text1.AppendText("(" + fPos + ") Normals Offset: " + dataAddress + "\n");

            // Number of Normals
            fPos = reader.BaseStream.Position;
            nLoops = reader.ReadInt32();
            Text1.AppendText("(" + fPos + ") Number of Normals: " + nLoops + "\n");
            if (nLoops > 0)
            {
                returnAddress = reader.BaseStream.Position;
                if (dataAddress > 0)
                    reader.BaseStream.Position = dataAddress;

                // Print the Normals
                for (int j = 0; j < nLoops; j++)
                {
                    Text1.AppendText("(" + reader.BaseStream.Position + ")      Normal " + j + ": (" + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ")\n");
                }

                // Return to the next line from before our seek
                reader.BaseStream.Position = returnAddress;
            }

            /*** SubTree Pointer ***/
            fPos = reader.BaseStream.Position;
            dataAddress = reader.ReadInt32();

            // Recursively Call Sub Trees
            if (dataAddress > 0)
                dataAddress += ptrBasePosition;
            Text1.AppendText("(" + fPos + ") SubTree ptr: " + dataAddress + "\n");
            return dataAddress;
        }

        /// <summary>
        /// Function to process RootNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintBRoot(BinaryReader reader, long ptrBase)
        {
            long fPos = reader.BaseStream.Position;                 // Current position of the reader            
            long ptrBasePosition = ptrBase;                         // Starting position of each node
            long returnAddress = 0;                                 // Address to return to after reading data from the file
            long dataAddress = 0;                                   // Address to seek to to find referenced data
            int nLoops = 0;                                         // Number of loops to perform for Coords, Normals, etc...    
                        
            /*** Textures ***/
            fPos = reader.BaseStream.Position;
            dataAddress = reader.ReadInt32();
            if (dataAddress >= 0)            
                dataAddress += ptrBasePosition;
                       
            Text1.AppendText("(" + fPos + ") Texture IDs Pointer: " + dataAddress + "\n");
            fPos = reader.BaseStream.Position;
            nLoops = reader.ReadInt32();
            Text1.AppendText("(" + fPos + ") Number of Textures: " + nLoops + "\n");

            if (nLoops > 0)
            {
                returnAddress = reader.BaseStream.Position;
                reader.BaseStream.Position = dataAddress;
                // Read the Texture IDs
                for (int j = 0; j < nLoops; j++)
                {
                    Text1.AppendText("(" + reader.BaseStream.Position + ")      Texture ID " + j + ": " + reader.ReadInt32().ToString() + "\n");
                }
                // Return to the next line from before our seek
                reader.BaseStream.Position = returnAddress;
            }

            Text1.AppendText("(" + reader.BaseStream.Position + ") Script Number: " + reader.ReadInt32().ToString() + "\n");
        }

        /// <summary>
        /// Function to process a SlotNode
        /// </summary>
        /// <param name="reader"></param>
        void PrintSlot(BinaryReader reader)
        {
            Text1.AppendText("(" + reader.BaseStream.Position + ") MATRIX Row 0: " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") MATRIX Row 1: " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") MATRIX Row 2: " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Rotation Point: (" + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Slot Number: " + reader.ReadInt32().ToString() + "\n");
        }

        /// <summary>
        /// Function to process a DOFNode
        /// </summary>
        /// <param name="reader"></param>
        void PrintDof(BinaryReader reader)
        {
            Text1.AppendText("(" + reader.BaseStream.Position + ") DOF Number: " + reader.ReadInt32().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") MATRIX Row 0: " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") MATRIX Row 1: " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") MATRIX Row 2: " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Rotation Point: (" + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
        }

        /// <summary>
        /// Function to process a SwitchNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        /// <returns></returns>
        long PrintSwitch(BinaryReader reader, long ptrBase)
        {
            long ptrBasePosition = ptrBase;      // Starting position of each node

            // Get the Switch Number
            Text1.AppendText("(" + reader.BaseStream.Position + ") Switch Number: " + reader.ReadInt32().ToString() + "\n");
            long fPos = reader.BaseStream.Position;

            // Get the Number of Children
            int nChild = reader.ReadInt32();
            Text1.AppendText("(" + fPos + ") Number of Children: " + nChild + "\n");
            fPos += 4;

            // Get a pointer the to the Children Collection
            long ChildPtr = reader.ReadInt32();
            if (ChildPtr > 0)
                ChildPtr += ptrBasePosition;
            Text1.AppendText("(" + fPos + ") Children pointer: " + ChildPtr + "\n");

            // Get the list of SubNodes from the position
            reader.BaseStream.Position = ChildPtr;
      
            // Recursively process the Child Trees
            for (int i = 0;i<nChild;i++)
            {
                fPos = reader.BaseStream.Position;
                ChildPtr = reader.ReadInt32();
                if (ChildPtr > 0)
                    ChildPtr += ptrBasePosition;
                Text1.AppendText("(" + fPos + ") Child Pointer: " + ChildPtr + "\n");
                ProcessTree(reader, ChildPtr);
            }
            
            // Break out of our current recursion tree when we're done
            return -1;             
        }

        /// <summary>
        /// Function to process a SplitterNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        /// <returns></returns>
        long PrintSplitter(BinaryReader reader, long ptrBase)
        {
            long ptrBasePosition = ptrBase;     // Starting position of each node
            long FrontPtr, BackPtr;             // The pointers to the subtrees we need to process

            // Print the Splitter Data (Normals)
            Text1.AppendText("(" + reader.BaseStream.Position + ") Point A: " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Point B: " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Point C: " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Point D: " + reader.ReadSingle().ToString() + "\n");

            // Print Front Node Tree
            // Store this for the text output
            long fPos = reader.BaseStream.Position; 
            
            // Read the FrontPtr
            FrontPtr = reader.ReadInt32();

            // Recursively process the FRONT tree
            if (FrontPtr > 0)
                FrontPtr += ptrBasePosition;
            Text1.AppendText("(" + fPos + ") Front Node Pointer:" + FrontPtr + "\n");
            ProcessTree(reader, FrontPtr);

            // Return our reader to the correct position
            fPos += 4;
            reader.BaseStream.Position = fPos;

            // Recursively process the BACK tree
            BackPtr = reader.ReadInt32();
            if (BackPtr > 0)
                BackPtr += ptrBasePosition;
            Text1.AppendText("(" + fPos + ") Back Node Pointer:" + BackPtr + "\n");
            ProcessTree(reader, BackPtr);
            
            // Break out of our recursion tree when we're done
            return -1;
        }

        /// <summary>
        /// Function to process LitPrimitiveNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintLitPrimitive(BinaryReader reader, long ptrBase)
        {
            long fPos = reader.BaseStream.Position;                 // Current position of the reader
            long ptrBasePosition = ptrBase;                         // Starting position of each node            
            long dataAddress, dataAddress2 = 0;                     // Address to seek to to find referenced data
            long returnAddress = 0;

            /*** Poly ***/
            fPos = reader.BaseStream.Position;
            
            // Get the Poly Addresses (2x)
            dataAddress = reader.ReadUInt32();
                if (dataAddress>0)
                dataAddress+= ptrBasePosition;
            dataAddress2 = reader.ReadUInt32();
            if (dataAddress2 > 0)
                dataAddress2 += ptrBasePosition;

            // Poly addresses - TODO: Change this to work like Splitter with calls to PrintPoly()
            Text1.AppendText("(" + fPos + ") Primitive Pointer Front: " + dataAddress + "\n");
            Text1.AppendText("(" + fPos + 4 + ") Primitive Pointer Back: " + dataAddress2 + "\n");

            for (int i=0;i<2;i++)
            {
                reader.BaseStream.Position = dataAddress;
                // Get the Poly Type
                fPos = reader.BaseStream.Position;
                int polyType = reader.ReadInt32();
                Text1.AppendText("(" + (fPos) + ")     Poly Type: " + polyType + "\n");

                // Get the VertexCount
                fPos = reader.BaseStream.Position;
                int nVertex = (int)reader.ReadInt32();
                Text1.AppendText("(" + (fPos) + ")     Number of Vertices: " + nVertex + "\n");

                // Get the Vertex Location
                fPos = reader.BaseStream.Position;
                dataAddress = (reader.ReadInt32() + ptrBasePosition);
                Text1.AppendText("(" + (fPos) + ")     Vertex Pointer: " + dataAddress + "\n");

                // Poly Data
                returnAddress = reader.BaseStream.Position;
                switch (polyType)
                {
                    case 0:  // PrimPoint           
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        break;

                    case 1:  // PrimLine                  
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        break;

                    case 2:
                    case 14:  //PolyFC
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        reader.BaseStream.Position = dataAddress;
                        break;

                    case 3:
                    case 15:  // PolyFCN
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Intensity Index: " + reader.ReadInt32().ToString() + "\n");
                        reader.BaseStream.Position = dataAddress;
                        break;

                    case 4:
                    case 16:  //PolyVC
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        reader.BaseStream.Position = dataAddress;
                        break;

                    case 5:
                    case 17:  //PolyVCN
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Intensity Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        reader.BaseStream.Position = dataAddress;
                        break;

                    case 6:
                    case 10:
                    case 18:
                    case 22:
                    case 26:  //PolyTexFC
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Texture Index: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Texture UV Pointer: " + reader.ReadInt32().ToString() + "\n");
                        reader.BaseStream.Position = dataAddress;
                        break;

                    case 7:
                    case 11:
                    case 19:
                    case 23:  //PolyTexFCN
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Intensity Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Texture Index: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Texture UV Pointer: " + reader.ReadInt32().ToString() + "\n");
                        reader.BaseStream.Position = dataAddress;
                        break;

                    case 8:
                    case 12:
                    case 20:
                    case 24:  //PolyTexVC
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Texture Index: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Texture UV Pointer: " + reader.ReadInt32().ToString() + "\n");
                        reader.BaseStream.Position = dataAddress;
                        break;

                    case 9:
                    case 13:
                    case 21:
                    case 25:  //PolyTexVCN
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Intensity Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Texture Index: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ")     Texture UV Pointer: " + reader.ReadInt32().ToString() + "\n");
                        reader.BaseStream.Position = dataAddress;
                        break;

                }

                // Get the Vertex Data
                for (int j = 0; j < nVertex; j++)
                {
                    Text1.AppendText("(" + reader.BaseStream.Position + ")      Vertex " + j + " Vector Index: " + reader.ReadInt32().ToString() + "\n");
                }

                // Update our jumper address to the Second Poly
                dataAddress = dataAddress2;
            }            
        }

        /// <summary>
        /// Function to process a Special Transform (SpecialXFormNode)
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintSpecial(BinaryReader reader, long ptrBase)
        {
            long ptrBasePosition = ptrBase;                         // Starting position of each node
            long fPos = reader.BaseStream.Position;                 // Current position of the reader            
            long returnAddress = 0;                                 // Address to return to after reading data from the file
            long dataAddress = 0;                                   // Address to seek to to find referenced data
            int nLoops = 0;                                         // Number of loops to perform for Coords, Normals, etc...    

            Text1.AppendText("(" + reader.BaseStream.Position + ") Coords ptr: " + (ptrBasePosition + reader.ReadInt32()) + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Number of Coords: " + reader.ReadInt32().ToString() + "\n");

            /*** Coords ***/
            // Get the current stream position for text output
            fPos = reader.BaseStream.Position;

            // Get the address we need for our Coords data
            dataAddress = reader.ReadInt32();
            if (dataAddress >= 0)            
                dataAddress += ptrBasePosition;
            
            Text1.AppendText("(" + fPos + ") Primitive Pointer: " + dataAddress + "\n");

            // Print Coords Address to screen
            Text1.AppendText("(" + fPos + ") Coords ptr: " + dataAddress + "\n");

            // Get the Number of Coords
            fPos = reader.BaseStream.Position;
            nLoops = reader.ReadInt32();

            // Print Number of Coords to Screen
            Text1.AppendText("(" + fPos + ") Number of Coords: " + nLoops + "\n");

            // Display the Coord data:                            
            if (nLoops > 0)
            {
                // Set the return address after we read the data
                returnAddress = reader.BaseStream.Position;

                // Seek to the position in the file if there is data to read
                reader.BaseStream.Position = dataAddress;

                // Read the coords
                for (int j = 0; j < nLoops; j++)
                {
                    // Build the Coord Format and Print to Screen
                    Text1.AppendText("(" + reader.BaseStream.Position + ")      Coord " + j + ": (" + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ")\n");
                }

                // Return to the next line after our seek
                returnAddress = reader.BaseStream.Position;
            }

            Text1.AppendText("(" + reader.BaseStream.Position + ") Transform Type (ENUM): " + reader.ReadInt32().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") SubTree Pointer: " + (ptrBasePosition + reader.ReadInt32()) + "\n");
            reader.BaseStream.Position = returnAddress;
        }

        /// <summary>
        /// Function to process a Poly for PrimitiveNode, CulledPrimitiveNode, and LightStringNode.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintPoly(BinaryReader reader, long ptrBase)
        {
            long fPos = reader.BaseStream.Position;                 // Current position of the reader
            long ptrBasePosition = ptrBase;                         // Starting position of each node            
            long dataAddress, dataAddress2 = 0;                     // Address to seek to to find referenced data
            long returnAddress = 0;

            /*** Poly ***/
            fPos = reader.BaseStream.Position;
            
            // Get the Poly Address
            dataAddress = reader.ReadUInt32() + ptrBasePosition;            
            Text1.AppendText("(" + fPos + ") Primitive Pointer: " + dataAddress + "\n");
            
            // Start of Poly/Prim Object
            // Get the Poly Type
            fPos = reader.BaseStream.Position;
            int polyType = reader.ReadInt32();            
            Text1.AppendText("(" + (fPos) + ")     Poly Type: " + polyType + "\n");

            // Get the VertexCount
            fPos = reader.BaseStream.Position;
            int nVertex = (int)reader.ReadInt32();            
            Text1.AppendText("(" + (fPos) + ")     Number of Vertices: " + nVertex + "\n");
            
            // Get the Vertex Location
            fPos = reader.BaseStream.Position;
            dataAddress2 = (reader.ReadInt32() + ptrBasePosition);            
            Text1.AppendText("(" + (fPos) + ")     Vertex Pointer: " + dataAddress2 + "\n");

            // Poly Data
            returnAddress = reader.BaseStream.Position;
            switch (polyType)
            {
                case 0:  // PrimPoint           
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");                    
                    break;

                case 1:  // PrimLine                  
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    break;

                case 2:
                case 14:  //PolyFC
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    reader.BaseStream.Position = dataAddress2;
                    break;

                case 3:
                case 15:  // PolyFCN
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Intensity Index: " + reader.ReadInt32().ToString() + "\n");
                    reader.BaseStream.Position = dataAddress2;
                    break;

                case 4:
                case 16:  //PolyVC
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    reader.BaseStream.Position = dataAddress2;
                    break;

                case 5:
                case 17:  //PolyVCN
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Intensity Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    reader.BaseStream.Position = dataAddress2;
                    break;

                case 6:
                case 10:
                case 18:               
                case 22:
                case 26:  //PolyTexFC
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Texture Index: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Texture UV Pointer: " + reader.ReadInt32().ToString() + "\n");
                    reader.BaseStream.Position = dataAddress2;
                    break;

                case 7:
                case 11:
                case 19:
                case 23:  //PolyTexFCN
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Intensity Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Texture Index: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Texture UV Pointer: " + reader.ReadInt32().ToString() + "\n");
                    reader.BaseStream.Position = dataAddress2;
                    break;               

                case 8:
                case 12:
                case 20:
                case 24:  //PolyTexVC
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Texture Index: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Texture UV Pointer: " + reader.ReadInt32().ToString() + "\n");
                    reader.BaseStream.Position = dataAddress2;
                    break;                 

                case 9:
                case 13:
                case 21:
                case 25:  //PolyTexVCN
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point A: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point B: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point C: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Point D: " + reader.ReadSingle().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Color Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Intensity Index Pointer: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Poly Texture Index: " + reader.ReadInt32().ToString() + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ")     Texture UV Pointer: " + reader.ReadInt32().ToString() + "\n");
                    reader.BaseStream.Position = dataAddress2;
                    break;

            }

            // Get the Vertex Data
            for (int i = 0; i < nVertex; i++)
            {                                   
                Text1.AppendText("(" + reader.BaseStream.Position + ")      Vertex " + i + " Vector Index: " + reader.ReadInt32().ToString() + "\n");
            }            
        }
        
        /// <summary>
        /// Function to process a LightStringNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintLightString(BinaryReader reader, long ptrBase)
        {
            long ptrBasePosition = ptrBase;                         // Starting position of each node

            Text1.AppendText("(" + reader.BaseStream.Position + ") Point A: " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Point B: " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Point C: " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Point D: " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") RGBA Front:" + (ptrBasePosition + reader.ReadInt32()) + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") RGBA Back:" + (ptrBasePosition + reader.ReadInt32()) + "\n");
        }

        /// <summary>
        /// Function to process a TransNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintTransNode(BinaryReader reader, long ptrBase)
        {
            Text1.AppendText("(" + reader.BaseStream.Position + ") DOF NUmber: " + reader.ReadInt32() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Min: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Max: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Multiplier: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Future: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadInt32() + "\n");            
            Text1.AppendText("(" + reader.BaseStream.Position + ") Translation Point: " + "(" + reader.ReadSingle() + ", " + reader.ReadSingle() + ", " + reader.ReadSingle() + ")");

        }

        /// <summary>
        /// Function to process a ScaleNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintScalNode(BinaryReader reader, long ptrBase)
        {
            Text1.AppendText("(" + reader.BaseStream.Position + ") DOF NUmber: " + reader.ReadInt32() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Min: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Max: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Multiplier: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Future: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadInt32() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Scale Factor: " + "(" + reader.ReadSingle() + ", " + reader.ReadSingle() + ", " + reader.ReadSingle() + ")");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Translation Point: " + "(" + reader.ReadSingle() + ", " + reader.ReadSingle() + ", " + reader.ReadSingle() + ")");
        }

        /// <summary>
        /// Function to print an XDof Node.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintXDofNode(BinaryReader reader, long ptrBase)
        {
            Text1.AppendText("(" + reader.BaseStream.Position + ") DOF NUmber: " + reader.ReadInt32() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Min: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Max: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Multiplier: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Future: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadInt32() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") MATRIX Row 0: " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") MATRIX Row 1: " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") MATRIX Row 2: " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + ", " + reader.ReadSingle().ToString() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Translation Point: " + "(" + reader.ReadSingle() + ", " + reader.ReadSingle() + ", " + reader.ReadSingle() + ")");
        }

        /// <summary>
        /// Function to process an XSwitchNode.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintXSwitchNode(BinaryReader reader, long ptrBase)
        {
            // Get the Switch Number
            Text1.AppendText("(" + reader.BaseStream.Position + ") Switch Number: " + reader.ReadInt32().ToString() + "\n");
            long fPos = reader.BaseStream.Position;
            // Get the flags
            Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadInt32() + "\n");

            // Get the Number of Children
            int nChild = reader.ReadInt32();
            Text1.AppendText("(" + fPos + ") Number of Children: " + nChild + "\n");
            fPos += 4;

            // Get a pointer the to the Children Collection
            long ChildPtr = reader.ReadInt32();
            if (ChildPtr > 0)
                ChildPtr += ptrBasePosition;
            Text1.AppendText("(" + fPos + ") Children pointer: " + ChildPtr + "\n");

            // Get the list of SubNodes from the position
            reader.BaseStream.Position = ChildPtr;

            // Recursively process the Child Trees
            for (int i = 0; i < nChild; i++)
            {
                fPos = reader.BaseStream.Position;
                ChildPtr = reader.ReadInt32();
                if (ChildPtr > 0)
                    ChildPtr += ptrBasePosition;
                Text1.AppendText("(" + fPos + ") Child Pointer: " + ChildPtr + "\n");
                ProcessTree(reader, ChildPtr);
            }
        }

        /// <summary>
        /// Function to process a RenderControlNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintRenderControlNode(BinaryReader reader, long ptrBase)
        {
            int _Control = reader.ReadInt32();

            switch (_Control)
            {
                case 0:
                    for (int i = 0; i < 8; i++)
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Data Context " + i + " :" + reader.ReadInt32() + "\n");
                    break;

                case 1:                    
                    Text1.AppendText("(" + reader.BaseStream.Position + "32 Byte Buffer\n");
                    reader.BaseStream.Position += 32;
                    Text1.AppendText("(" + reader.BaseStream.Position + ") ZBias Value: " + reader.ReadSingle() + "\n");
                    break;

                case 2:
                    Text1.AppendText("(" + reader.BaseStream.Position + ") Math Mode: " + reader.ReadUInt16() + "\n");
                    for (int i = 0; i < 5; i++)
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Arg Type: " + reader.ReadByte() + "\n");
                    byte _result = reader.ReadByte();
                    Text1.AppendText("(" + reader.BaseStream.Position + ") Result Type: " + _result + "\n");
                    Text1.AppendText("(" + reader.BaseStream.Position + ") Result ID: " + reader.ReadUInt32() + "\n");
                    if (_result == 0)
                    {
                        for (int i = 0; i < 5; i++)
                            Text1.AppendText("(" + reader.BaseStream.Position + ") ID: " + reader.ReadInt32() + "\n");                        
                    }
                    else
                    {
                        for (int i = 0; i < 5; i++)
                            Text1.AppendText("(" + reader.BaseStream.Position + ") Value: " + reader.ReadSingle() + "\n");
                    }
                    break;
            }
        }

        /// <summary>
        /// Function to process a CulledNode
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ptrBase"></param>
        void PrintCulledNode(BinaryReader reader, long ptrBase)
        {
            Text1.AppendText("(" + reader.BaseStream.Position + ") A: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") B: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") C: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") D: " + reader.ReadSingle() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Front Node: " + reader.ReadInt32() + "\n");
            Text1.AppendText("(" + reader.BaseStream.Position + ") Back Node: " + reader.ReadInt32() + "\n");
        }

        /// <summary>
        /// Event Handler for Checkbox. Enables/disables Header Options.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)CB1.IsChecked)
            {
                TB1.IsEnabled = true;
                CBPanel.IsEnabled = true;
            }               
            else
            {
                TB1.IsEnabled = false;
                CBPanel.IsEnabled = false;
            }
                
        }

        /// <summary>
        /// Event Handler for Checkbox.  Enables/Disables LOD Options.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox1_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)LCB1.IsChecked)
                LTB1.IsEnabled = true;                
            else
                LTB1.IsEnabled = false;                            
        }

        /// <summary>
        /// Event Handler to process CT Button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Text1.Clear();

            // Pick a File
            if (!File.Exists(WorkingFolder.Text + "\\FALCON4.ct"))
                _filename = GetFile(".ct");
            else
                _filename = WorkingFolder.Text + "\\FALCON4.ct";

            // Check for Cancel Button
            if (_filename == null)
                return;

            Int32.TryParse(CTTB1.Text, out int nIndex);
            Int32.TryParse(CTTB2.Text, out int nOutputEntries);

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(_filename, FileMode.Open)))
                {
                    // Get the total number of entries
                    int nEntries = reader.ReadInt16();

                    // Set the start position
                    reader.BaseStream.Position += nIndex * 81;

                    // If we started at the beginning print the number of entries
                    if (reader.BaseStream.Position == 2)
                    {
                        Text1.AppendText("(0) Entry Count: " + nEntries + "\n");
                    }

                    // Process the entries
                    // nEntries-nIndex = remaining before EOF
                    for (int i = 0; i < Math.Min(nEntries - nIndex, nOutputEntries); i++)
                    {
                        Text1.AppendText("    **** Entry " + (i + nIndex) + " ****\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") ID: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Collision Type: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Collision Radius" + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Class Info[0]: " + (uint)reader.ReadByte() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Class Info[1]: " + (uint)reader.ReadByte() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Class Info[2]: " + (uint)reader.ReadByte() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Class Info[3]: " + (uint)reader.ReadByte() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Class Info[4]: " + (uint)reader.ReadByte() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Class Info[5]: " + (uint)reader.ReadByte() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Class Info[6]: " + (uint)reader.ReadByte() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Class Info[7]: " + (uint)reader.ReadByte() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Update Rate: " + reader.ReadUInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Update Tolerance: " + reader.ReadUInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Fine Update (Bubble): " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Fine Update Force: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Fine Update Multiplier: " + reader.ReadSingle().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Damage Seed: " + reader.ReadUInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Hitpoints: " + reader.ReadInt32().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Major Revision: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Minor Revision: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Create Priority: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Management Domain: " + reader.ReadByte().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Transferrable: " + reader.ReadByte().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Private: " + reader.ReadByte().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Tangible: " + reader.ReadByte().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Collidable: " + reader.ReadByte().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Global: " + reader.ReadByte().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Persistent: " + reader.ReadByte().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Unknown - Buffer 2 Bytes: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Unknown - Buffer 1 Byte: " + reader.ReadByte().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") visType[0]: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") visType[1]: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") visType[2]: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") visType[3]: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") visType[4]: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") visType[5]: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") visType[6]: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Vehicle Data Index: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") DataType: " + reader.ReadByte().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") RecordID: " + reader.ReadUInt16().ToString() + "\n");
                        Text1.AppendText("(" + reader.BaseStream.Position + ") Unknown  - Buffer 2 Bytes: " + reader.ReadInt16().ToString() + "\n\n");
                    }
                }
            }
            catch
            {
                MessageBox.Show("Unable to open the File.  The file is already being used by another program.", "Unable to Open File");
            }
        }

        /// <summary>
        /// Event Handler for Checkbox.  Enables/Disables CT Ooptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CTCB1_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)CTCB1.IsChecked)
                CTTB1.IsEnabled = true;
            else
            {
                CTTB1.Text = "1";
                CTTB1.IsEnabled = false;
            }
                

        }

        /// <summary>
        /// Event Handler for Checkbox.  Enables/Disables *CD Options.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UCDCB_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)UCDCB1.IsChecked)
                UCDTB1.IsEnabled = true;
            else
            {
                UCDTB1.Text = "1";
                UCDTB1.IsEnabled = false;
            }
        }

        /// <summary>
        /// Event Handler to process *CD Files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UCDButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the TB
            Text1.Clear();
            
            // Get the type of file we want to parse
            string extension =  EXTSel.SelectedItem.ToString().Substring(
                EXTSel.SelectedValue.ToString().Length -4, 3);

            // One entry only has two letters
            if (extension.Contains("("))
                extension = "PD";
          
            // Pick a File if it isn't in the working directory
           if (!File.Exists(WorkingFolder.Text + "\\FALCON4." + extension))
                _filename = GetFile(extension);
           else
               _filename = WorkingFolder.Text + "\\FALCON4." + extension;

            // Exit on Cancel
            if (_filename == null)
                return;

           // Setup the number of entries to output and the starting index
           Int32.TryParse(UCDTB1.Text, out int nIndex);
           Int32.TryParse(UCDTB2.Text, out int nOutputEntries);

            // Open the File
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(_filename, FileMode.Open)))
                {
                    int nEntries = Math.Max((short)1, (short)reader.ReadInt16());       // Number of entries

                    // Print the number of entries
                    Text1.AppendText("(0) Entry Count: " + nEntries + "\n");

                    // Process the file based on the selected type
                    switch (extension)
                    {
                        // Unit Class Data
                        case "UCD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 336);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") CT Index ID: " + reader.ReadInt32().ToString() + "\n");
                                for (int i = 0; i < 16; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Number of Children in Element " + i + ": " + reader.ReadInt32().ToString() + "\n");
                                }
                                for (int i = 0; i < 16; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") CT Index of Element " + i + ": " + reader.ReadInt16().ToString() + "\n");
                                }
                                for (int i = 0; i < 16; i++)
                                {
                                    for (int k = 0; k < 8; k++)
                                    {
                                        Text1.AppendText("(" + reader.BaseStream.Position + ") Element " + i + " Vehicle " + k + " Class ID: " + reader.ReadByte().ToString() + "\n");
                                    }
                                }
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadUInt16().ToString() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Name: " + new string(reader.ReadChars(20)).TrimEnd('\0') + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") UNK - 2 Bytes" + reader.ReadInt16().ToString() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Movement Type: " + reader.ReadInt32().ToString() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Movement Speed (km/h): " + reader.ReadInt16().ToString() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Max Range (km): " + reader.ReadInt16().ToString() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Fuel (lbs): " + reader.ReadInt32().ToString() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Fuel Rate (lb/m): " + reader.ReadInt16().ToString() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") UNK - 2 Bytes: " + reader.ReadInt16().ToString() + "\n");
                                for (int i = 0; i < 16; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Role Participation (%): " + reader.ReadByte().ToString() + "\n");
                                }
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Main Role: " + reader.ReadByte().ToString() + "\n");
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Hit Chance (%): " + reader.ReadByte().ToString() + "\n");
                                }
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Strength: " + reader.ReadByte().ToString() + "\n");
                                }
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Range (km): " + reader.ReadByte().ToString() + "\n");
                                }
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Detection Range (km): " + reader.ReadByte().ToString() + "\n");
                                }
                                for (int i = 0; i < 11; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Damage Model (%): " + reader.ReadByte().ToString() + "\n");
                                }
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Radar Vehicle: " + reader.ReadInt16().ToString() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Sq Stores: " + reader.ReadInt16().ToString() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Unit Icon: " + reader.ReadInt32().ToString() + "\n\n");
                            }
                            break;

                        // Aircraft Class Data
                        case "ACD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 52);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Combat Class: " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Airframe Index: " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Signature Index: " + reader.ReadInt32() + "\n");
                                for (int i = 0; i < 5; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Sensor " + i + " Type: " + reader.ReadInt32() + "\n");
                                }
                                for (int i = 0; i < 5; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Sensor " + i + " Index: " + reader.ReadInt32() + "\n");
                                }
                                Text1.AppendText("\n");
                            }
                            break;

                        // Squadron Stores
                        case "SSD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 603);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {

                                Text1.AppendText("**** Entry " + j + " ****\n");
                                for (int i = 0; i < 600; i++)
                                {
                                    Text1.AppendText("Stores Entry " + i + " Count: " + reader.ReadByte() + "\n");
                                }
                                
                                Text1.AppendText("Stores Infinite AG Weapon Type: " + reader.ReadByte() + "\n");
                                Text1.AppendText("Stores Infinite AA Weapon Type: " + reader.ReadByte() + "\n");
                                Text1.AppendText("Stores Infinite Gun Type: " + reader.ReadByte() + "\n");
                                Text1.AppendText("\n");

                            }
                            break;

                        // Rocket Data
                        case "RKT":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 6);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Weapon ID: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") New Weapon ID: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Weapon Count: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // SimWeapons Data
                        case "SWD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 52);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Drag Coefficient: " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Weight (lbs): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Area (sqft): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") X Ejection Component: " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Y Ejection Component: " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Z Ejection Component: " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Mnemonic: " + new string(reader.ReadChars(8)).TrimEnd('\0') + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Weapon Class: " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Domain: " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Weapon Type Index: " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Data Index: " + reader.ReadInt32() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // VisualSensor Data
                        case "VSD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 20);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Nominal Range (ft): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Top (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Bottom (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Left (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Right (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // RWR Data
                        case "RWD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 22);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Nominal Range (ft): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Top (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Bottom (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Left (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Right (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // IR Sensor Data
                        case "ICD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 20);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText(reader.BaseStream.Length + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Nominal Range (ft): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") FOV Half Angle (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Gimbal Limit Half Angle (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Ground Range Factor: " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flare Chance (%): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // Radar Class Data
                        case "RCD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 58);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") RWR Sound: " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") RWR Symbol: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Radar Data Index: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Lethality (High Altitude): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Lethality (Low Altitude): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Nominal Range (ft): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Beam Half Angle (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Scan Half Angle (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Sweep Rate (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Coast Time (ms): " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Look Down Penalty (%): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Jamming Penalty (%): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Notch Penalty (%): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Notch Speed (m/s): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Chaff Chance (%): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags (NCTR): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // Extended Point Data
                        case "PDX":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 28);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                //x-y flip for coords
                                float temp = reader.ReadSingle();
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Offset: (" + reader.ReadSingle() + ", " + temp + ", " + reader.ReadSingle() + ")\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Max Height (m): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Max Width (m): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Max Length (m): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Point Type: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Root Index: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Branch Index: " + reader.ReadByte() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // Point Data
                        case "PD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 12);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                float temp = reader.ReadSingle();
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Offset: (" + reader.ReadSingle() + ", " + temp + ")\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Type: " + reader.ReadUInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadUInt16() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // Point Header
                        case "PHD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 28);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") CT ID: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Type: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Count: " + reader.ReadByte() + "\n");
                                for (int i = 0; i < 5; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Feature " + i + " Type: " + reader.ReadByte() + "\n");
                                }
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Data Index: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Miscellaneous Data: " + reader.ReadUInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") SIN Heading (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") COS Heading (Rads): " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") First Element: " + reader.ReadUInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Texture ID: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Runway Number: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Base Approach C/L/R: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Next Header: " + reader.ReadUInt16() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // WeaponsList Data
                        case "WLD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 208);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Name: " + new string(reader.ReadChars(16)).TrimEnd('\0') + "\n");
                                for (int i = 0; i < 64; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Weapon Entry " + i + "Weapon Index: " + reader.ReadInt16() + "\n");
                                }
                                for (int i = 0; i < 64; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Weapon Entry " + i + "Count: " + reader.ReadByte() + "\n");
                                }
                                Text1.AppendText("\n");

                            }
                            break;

                        // VehicleClassData
                        case "VCD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 160);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") CT Index: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Hit Points: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadUInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Name: " + new string(reader.ReadChars(15)).TrimEnd('\0') + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Cockpit Name: " + new string(reader.ReadChars(5)).TrimEnd('\0') + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Radar Cross Signature: " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Max Weight (lbs): " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Empty Weight (lbs): " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Fuel Weight (lbs): " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Fuel Flow (lbs/m): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Engine Sound: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") High Altitude (100ft): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Low Altitude (100ft): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Cruise Altitude (100ft): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Max Speed (k/h): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Radar Type: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Crew Members: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Hardpoint Rack Flags: " + reader.ReadUInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Hardpoint Visible Flags: " + reader.ReadUInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Callsign Index: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Callsign Slots: " + reader.ReadByte() + "\n");
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Hit Chance " + i + ": " + reader.ReadByte() + "\n");
                                }
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Strength " + i + ": " + reader.ReadByte() + "\n");
                                }
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Range " + i + ": " + reader.ReadByte() + "\n");
                                }
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Detection " + i + ": " + reader.ReadByte() + "\n");
                                }
                                for (int i = 0; i < 16; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Station " + i + " Weapon Index: " + reader.ReadInt16() + "\n");
                                }
                                for (int i = 0; i < 16; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Station " + i + " Weapon Count: " + reader.ReadByte() + "\n");
                                }
                                for (int i = 0; i < 11; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Damage Model " + i + ": " + reader.ReadByte() + "\n");
                                }
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Buffer 1 Byte " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") IR Sig: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Vis Sig: " + reader.ReadByte() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // FeatureClass Data
                        case "FCD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 60);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {

                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") CT Index: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Repair Time: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Priority: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") UNK - 1 Byte" + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Name: " + new string(reader.ReadChars(20)).TrimEnd('\0') + "\n");


                                Text1.AppendText("(" + reader.BaseStream.Position + ") Hit Points: " + reader.ReadUInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Height: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Angle: " + reader.ReadSingle() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Radar Type: " + reader.ReadInt16() + "\n");
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Detection " + i + ": " + reader.ReadByte() + "\n");
                                }
                                for (int i = 0; i < 11; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Damage Model " + i + ": " + reader.ReadByte() + "\n");
                                }
                                Text1.AppendText("(" + reader.BaseStream.Position + ") UNK - 1 Byte" + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") UNK - 1 Byte" + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") UNK - 1 Byte" + reader.ReadByte() + "\n");
                                Text1.AppendText("\n");

                            }
                            break;

                        // Feature Entity Data
                        case "FED":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 32);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Index: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadUInt16() + "\n");
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Entry " + i + " Class ID: " + reader.ReadByte() + "\n");
                                }
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Damage Required to Destroy: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") UNK - 2 Bytes" + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Offset: (Y:" + reader.ReadSingle() + ", X:" + reader.ReadSingle() + ", Z:" + reader.ReadSingle() + ")\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Heading (Deg): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") UNK - 2 Bytes" + reader.ReadInt16() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // WeaponsClass Data
                        case "WCD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 60);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") CT Index: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Strength (ECM, Chaff Count, Fuel Capacity, etc...): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Damage Type: " + reader.ReadInt32() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Range (km): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Flags: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Name: " + new string(reader.ReadChars(20)).TrimEnd('\0') + "\n");
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Hit Chance " + i + ": " + reader.ReadByte() + "\n");
                                }
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Fire Rate: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Rarety: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Guidance Type: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Collective: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Bullet TTL: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Rack Group: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Weight (lbs): " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Drag Index: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Blast Radius or Flare Count: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Radar Type: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Sim Data Index: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Bullet Rounds/s: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Max Altitude (1000ft): " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Bullet Speed: " + reader.ReadByte() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;

                        // ObjectiveClass Data
                        case "OCD":
                            // Seek to the correct position
                            reader.BaseStream.Position += (nIndex * 54);

                            // Loop through the entries
                            for (int j = 0; j < Math.Min(nEntries - nIndex, nOutputEntries); j++)
                            {
                                Text1.AppendText("**** Entry " + j + " ****\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") CT Index: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Name: " + new string(reader.ReadChars(20)).TrimEnd('\0') + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Data Rate: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") DeAg Distance: (ft)" + reader.ReadInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Parent Index: " + reader.ReadInt16() + "\n");
                                for (int i = 0; i < 8; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Detection " + i + " Range: " + reader.ReadByte() + "\n");
                                }
                                for (int i = 0; i < 11; i++)
                                {
                                    Text1.AppendText("(" + reader.BaseStream.Position + ") Damage Model " + i + ": " + reader.ReadByte() + "\n");
                                }
                                Text1.AppendText("(" + reader.BaseStream.Position + ") 1 Byte Buffer" + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Icon Index: " + reader.ReadUInt16() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Feature Count: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") Radar Vehicle: " + reader.ReadByte() + "\n");
                                Text1.AppendText("(" + reader.BaseStream.Position + ") First Feature Index: " + reader.ReadInt16() + "\n");
                                Text1.AppendText("\n");
                            }
                            break;
                    }


                } // End Using
            }
            catch
            {
                MessageBox.Show("Unable to open the File.  The file is already being used by another program.", "Unable to Open File");
            }
        }

        /// <summary>
        /// Function to activate the FileOpenDialogue manually
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            GetFile(".hdr");
        }


    }   //End Class
}   // End NS
