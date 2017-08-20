using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileParser
{
    class LOD
    {
        public int NodeCount        // The number of Nodes for this LOD
        { get; private set; }
        public int[] NodesTypes     // List of the NodeTypes
        { get; private set; }
        public List<IBSPNode> Nodes // Collection of the Nodes
        { get; private set; }

        public LOD()
        {
            NodeCount = 0;
            NodesTypes = new int[0];
            Nodes = new List<IBSPNode>();            
        }

    }

    /// <summary>
    /// Node Interface
    /// Provides a common interface for all the different types of Nodes.
    /// Required for combining different node types in the same list.
    /// </summary>
    public interface IBSPNode
    {
        BSPNode.ENodeType NodeType
        { get; set; }
        int FillNode(BinaryReader reader);
        string ToString();
        string Print();
        string DebugPrint();
        
    }

    /// <summary>
    /// Container Base Class for all the Nodes to inherit from. 
    /// </summary>
    public abstract class BSPNode
    {
        public enum ENodeType
        { BNode, SubTreeNode, RootNode, SlotNode, DOFNode, SwitchNode, SplitterNode, PrimitiveNode, LitPrimitiveNode, CulledPrimitiveNode, SpecialXFormNode, LightStringNode};

        // Properties
        public ENodeType NodeType
        { get; set; }// Type of Node we are dealing with

        // Fields
        protected long RootPtr;             // Start point of the LOD
        protected long NodePtr;
        protected int lineCount;            // Line Counters to help with Debug Outout

        // Constructor
        protected BSPNode() { }

        // Wrapper class to turn a Point3D into a Normal
        public class Normal
        {
            public Double I
            { get { return pData.X; }
              set { pData.X = value; }
            }
            public Double J
            {
                get { return pData.Y; }
                set { pData.Y = value; }
            }
            public Double K
            {
                get { return pData.Z; }
                set { pData.Z = value; }
            }

            private System.Windows.Media.Media3D.Point3D pData;

            public Normal(double i, double j, double k)
            {
                pData = new System.Windows.Media.Media3D.Point3D(i, j, k);
            }
        }
    }

    /// <summary>
    /// The generic base Node which contains the Sibling and Child pointers.
    /// Inherited in Every Node: 
    /// Indirect inheritance in BRootNode and BDOFNode through BSubTreeNode.
    /// Indirect inheritance in BLightStringNode through BPrimitiveNode.
    /// </summary>
    public class BNode : BSPNode, IBSPNode
    {

        // Public Properties
        public int Sibling          // ptr to Sibling
        { get; private set; }   
        public int Children         // ptr to Child Nodes
        { get; private set; }

        // Empty Constructor
        public BNode()
        {
            NodePtr = 0;
            NodeType = BSPNode.ENodeType.BNode;
            Sibling = 0;
            Children = 0;
        }

        // Constructor with Pointers passed
        public BNode(int sibling, int children)
        {
            NodePtr = 0;
            NodeType = BSPNode.ENodeType.BNode;
            Sibling = sibling;
            Children = children;
        }

        // Constructor with a File passed
        public BNode(BinaryReader reader)
        {
            NodePtr = reader.BaseStream.Position;
            FillNode(reader);
        }

        /// <summary>
        /// Reads the Node data from the provided BinaryReader
        /// </summary>
        /// <remarks>reader must already be open and at the correct Position in the stream</remarks>
        /// <param name="reader">BinaryReader</param>
        public int FillNode(BinaryReader reader)
        {
            Sibling = reader.ReadInt32();
            Children = reader.ReadInt32();
            return 1;
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns>
        /// Sibling Pointer: INT
        /// Children Pointer: INT
        /// </returns> 
        public string Print()
        {
            return "Sibling Pointer: " + Sibling + "\nChildren Pointer: " + Children;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public string DebugPrint()
        {
            // Initialize the Line Counters
            lineCount = (int)RootPtr + 8;
            return "(" + RootPtr + ") Sibling Pointer: " + Sibling + "\n("+ (RootPtr +4) + ") Children Pointer: " + Children;
        }
    }

    /// <summary>
    /// Node to identify a SubTree structure in the LOD.
    /// Inherits BNode. 
    /// Inherited By BRootNode.  
    /// Inherited by BDOFNode. 
    /// </summary>
    public class BSubTreeNode : BNode, IBSPNode
    {
        //Properties
        public List<System.Windows.Media.Media3D.Point3D> Coords
        { get; private set; }
        public List<System.Windows.Media.Media3D.Point3D> DynamicCoords
        { get; private set; }
        public List<Normal> Normals
        { get; private set; }
        public int SubTree
        { get; private set; }

        private long _CoordsPtr;        // File Position for the Coords
        private int _nCoords;           // Number of Coords
        private long _DynamicsPtr;      // File Position of Dynamic Coords
        private int _nDynamics;         // Number of Dynamics        
        private long _NormalsPtr;       // File position of the Normals
        private int _nNormals;          // Number of Normals

        // Empty constructor
        public BSubTreeNode()
        {
            // Initialize the Root ptr
            NodePtr = 0;
            // Set the NodeType
            NodeType = ENodeType.SubTreeNode;

            // Initialize the Lists
            Coords = new List<System.Windows.Media.Media3D.Point3D>();
            DynamicCoords = new List<System.Windows.Media.Media3D.Point3D>();
            Normals = new List<Normal>();
        }

        // Constructor with File Reader passed
        public BSubTreeNode(BinaryReader reader)
        {
            // Initialize the Root ptr
            NodePtr = reader.BaseStream.Position;

            // Set the NodeType
            NodeType = ENodeType.SubTreeNode;

            
            // Fill the SubTree specific Data
            FillNode(reader);            
        }

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <remarks>reader must already be open and at the correct Position in the stream</remarks>
        /// <param name="reader">BinaryReader</param>
        public new int FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);

            // Add the SubTree data
            _CoordsPtr = (reader.ReadInt32() + RootPtr);
            _nCoords = reader.ReadInt32();
            _nDynamics = reader.ReadInt32();
            _DynamicsPtr = (reader.ReadInt32() + RootPtr);
            _NormalsPtr = (reader.ReadInt32() + RootPtr);
            _nNormals = reader.ReadInt32();
            SubTree = reader.ReadInt32();

            // Fill the Lists            
            Coords = FillCoords(reader, _nCoords, reader.BaseStream.Position, _CoordsPtr);
            DynamicCoords = FillCoords(reader, _nDynamics, reader.BaseStream.Position, _DynamicsPtr);
            Normals = FillNormals(reader, _nNormals, reader.BaseStream.Position, _NormalsPtr);

            return 1;
        }

        /// <summary>
        /// Helper function to read the Coordinates and Dynamic Coordinates for this node
        /// </summary>        
        /// <param name="reader">BinaryReader</param>
        /// /// <remarks>reader must already be open and at the correct Position in the stream</remarks>
        /// <param name="count">Nmber of Coordinates to read</param>
        /// <param name="returnAddress">The position of the File Stream to return to after the read is complete</param>
        /// <param name="dataAddress">The position in the file where the data is located</param>
        /// <returns>List of Point3Ds to represent either the Coordinates or the Dynamic Coordinates (x,y,z)</returns>
        private List<System.Windows.Media.Media3D.Point3D> FillCoords(BinaryReader reader, int count, long returnAddress, long dataAddress)
        {
            // Initialize a new list
            List<System.Windows.Media.Media3D.Point3D> PointList = new List<System.Windows.Media.Media3D.Point3D>();

            // Seek to the File position for the data
            reader.BaseStream.Position = dataAddress;

            // Fill the list
            for (int i =0;i<count;i++)
            {
                PointList.Add(new System.Windows.Media.Media3D.Point3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }

            reader.BaseStream.Position = returnAddress;
            return PointList;
        }

        /// <summary>
        /// Helper function to read the Normal data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>reader must already be open and at the correct Position in the stream</remarks>
        /// <param name="count">Number of Normals to read</param>
        /// <param name="returnAddress">The position of the File Stream to return to after the read is complete</param>
        /// <param name="dataAddress">The position in the file where the data is located</param>
        /// <returns>List of Normal Data (I,J,K) as a Normal Class Object</returns>
        private List<Normal> FillNormals(BinaryReader reader, int count, long returnAddress, long dataAddress)
        {
            // Initialize a new list
            List<Normal> PointList = new List<Normal>();

            // Seek to the File position for the data
            reader.BaseStream.Position = dataAddress;

            // Fill the list
            for (int i = 0; i < count; i++)
            {
                PointList.Add(new Normal(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }

            reader.BaseStream.Position = returnAddress;
            return PointList;
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns>
        /// Sibling Pointer: INT
        /// Children Pointer: INT
        /// </returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";
            retText += "Number of Coords: " + _nCoords + "\n";
            for (int i = 0; i < _nCoords; i++)
                retText += "    (" + Coords[i].X + ", " + Coords[i].Y + ", " + Coords[i].Z + ")\n";
            retText += "Number of Dynamic Coords: " + _nDynamics +"\n";
            for (int i = 0; i < _nDynamics; i++)
                retText += "    (" + DynamicCoords[i].X + ", " + DynamicCoords[i].Y + ", " + DynamicCoords[i].Z + ")\n";
            retText += "Number of Normals: " + _nNormals + "\n";
            for (int i = 0; i < _nNormals; i++)
                retText += "    (" + Normals[i].I + ", " + Normals[i].J + ", " + Normals[i].K + ")\nSubTree: " + SubTree;
            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            
            string retText = base.DebugPrint() + "\n";
            retText += "(" + lineCount + ") Coord Pointer: " + _CoordsPtr + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") Number of Coords: " + _nCoords + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") Number of Dynamic Coords: " + _nDynamics + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") Dynamic Coords Pointer: " + _DynamicsPtr + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") Normals Pointer: " + _NormalsPtr + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") Number of Normals: " + _nNormals + "\n\n"; lineCount += 4;
            retText += "Coords: \n";
            for (int i = 0; i < _nCoords; i++)
                retText += "(" + (_CoordsPtr + (4 * i)) + ")     (" + Coords[i].X + ", " + Coords[i].Y + ", " + Coords[i].Z + ")\n";
            retText += "\nDynamic Coords: \n";
            for (int i = 0; i < _nDynamics; i++)
                retText += "(" + (_DynamicsPtr + (4 * i)) + ")     (" + DynamicCoords[i].X + ", " + DynamicCoords[i].Y + ", " + DynamicCoords[i].Z + ")\n";
            retText += "\nNormals: \n";
            for (int i = 0; i < _nNormals; i++)
                retText += "(" + (_NormalsPtr + (4 * i)) + ")     (" + Normals[i].I + ", " + Normals[i].J + ", " + Normals[i].K + ")\nSubTree: " + SubTree;
            return retText;
        }
        
    }
    
    /// <summary>
    /// Root node found at the beginning of each LOD.
    /// Inherits BSubTree.
    /// </summary>
    public class BRootNode : BSubTreeNode, IBSPNode
    {
        //Properties
        public int ScriptID
        { get {return _ScriptID; }}
        public int TextureCount
        { get { return _nTextures; }}
        public int[] Textures
        { get; private set; }

        //Fields
        private long _TexturePtr;
        private int _nTextures;
        private int _ScriptID;

        // Empty Constructor
        public BRootNode()
        {
            // Initialize the Root ptr
            RootPtr = 0;
            NodePtr = 0;

            // Set the NodeType
            NodeType = ENodeType.RootNode;            
        }

        // Constructor with a file passed
        public BRootNode(BinaryReader reader)
        {
            // Initialize the Root ptr
            RootPtr = NodePtr = reader.BaseStream.Position;

            // Set the NodeType
            NodeType = ENodeType.RootNode;

            FillNode(reader);
        }

        /// <summary>
        /// Helper function to read the Node from the file
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>reader must already be open and at the correct Position in the stream</remarks>
        public new int FillNode(BinaryReader reader)
        {
            // Fill the SubTree data
            base.FillNode(reader);

            //Read the Root Specific Data
            _TexturePtr = (reader.ReadInt32() + RootPtr);
            _nTextures = reader.ReadInt32();
            _ScriptID = reader.ReadInt32();

            // Fill the TextureID Array
            Textures = GetTextureIDs(reader, _nTextures, reader.BaseStream.Position, _TexturePtr);

            return 1;
        }

        /// <summary>
        /// Helper Function to get the TextureIDs for this Root Node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>reader must already be open and at the correct Position in the stream</remarks>
        /// <param name="count">The Number of Textures to read</param>
        /// <param name="returnAddress">The position of the File Stream to return to after the read is complete</param>
        /// <param name="dataAddress">The position in the file where the data is located</param>
        /// <returns>int[] Filled with Texture IDs</returns>
        private int[] GetTextureIDs(BinaryReader reader, int count, long returnAddress, long dataAddress)
        {
            // Initialize a new Array to hold our pointers
            int[] aTextures = new int[count];

            // Seek to the File position for the data
            reader.BaseStream.Position = dataAddress;

            // Fill the list
            for (int i = 0; i < count; i++)
            {
                aTextures[i] = reader.ReadInt32();
            }

            reader.BaseStream.Position = returnAddress;
            return aTextures;
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";
            retText += "Number of Textures: " + _nTextures + "\n";
            for (int i = 0; i < _nTextures; i++)
                retText += "    " + Textures[i] + "\n";
            retText += "Script ID: " + _ScriptID + "\n";
            
            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {

            string retText = base.DebugPrint() + "\n";
            retText += "(" + lineCount + ") Texture IDs Pointer: " + _TexturePtr + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") Number of Textures: " + _nTextures + "\n"; lineCount += 4;            
            retText += "(" + lineCount + ") Script ID: " + _ScriptID + "\n"; lineCount += 4;

            for (int i = 0; i < _nTextures; i++)
                retText += "(" + (_TexturePtr + (i * 4)) + ")     " + Textures[i] + "\n";

            return retText;
        }
    }

    /// <summary>
    /// Node to identify a Slot on the LOD.
    /// Inherits BNode.
    /// </summary>
    public class BSlotNode : BNode, IBSPNode
    {
        //Properties
        public float[][] RotationMatrix
        { get; private set; }
        public System.Windows.Media.Media3D.Point3D RotationPoint
        { get; private set; }
        public int SlotID
        { get; private set; }

        
        //Fields
        //NA All Values need to be exposed

        //Empty Constructor
        public BSlotNode()
        {
            // Initialize the Root ptr
            NodePtr = 0;

            // Set the NodeType
            NodeType = ENodeType.SlotNode;
            
        }

        //Constructor with file passed
        public BSlotNode(BinaryReader reader)
        {
            // Initialize the Root ptr
            NodePtr = reader.BaseStream.Position;

            // Set the NodeType
            NodeType = ENodeType.SlotNode;

            FillNode(reader);
        }

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>Binary Reader must be open and at the correct position</remarks>
        public new int FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);

            // Fill the 3x3 Matrix
            for (int i =0;i<3;i++)
            {
                for (int j=0;j<3;j++)
                {
                    RotationMatrix[i][j] = reader.ReadSingle();
                }                
            }

            // Get the Rotation Point
            RotationPoint = new System.Windows.Media.Media3D.Point3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            // Read the Slot ID
            SlotID = reader.ReadInt32();

            return 1;
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\nRotation Matrix: \n";
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    retText += RotationMatrix[i][j] + "  ";
                }
                retText += "\n";
            }
            retText += "Rotation Point: (" + RotationPoint.X + ", " + RotationPoint.Y + ", " + RotationPoint.Z + ")\n";
            retText += "Slot Number: " + SlotID;
            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            string retText = base.DebugPrint() + "\n("+lineCount + "Rotation Matrix: \n";   lineCount += 4;         
            for (int i = 0; i < 3; i++)
            {
                retText += "(" + lineCount + ") ";
                for (int j = 0; j < 3; j++)
                {
                    retText += RotationMatrix[i][j] + "  "; lineCount += 4;
                }
                retText += "\n";
            }
            retText += "(" + lineCount + ") Rotation Point: " + RotationPoint.X + ", " + RotationPoint.Y + ", " + RotationPoint.Z + ")\n"; lineCount += 4;
            retText += "(" + lineCount + ") Slot ID: " + SlotID; lineCount += 4;
            return retText;
        }
    }

    /// <summary>
    /// Node to identify a DegreeOfFreedom (DOF) on the LOD.
    /// Inherits BSubtree.
    /// </summary>
    public class BDOFNode : BSubTreeNode, IBSPNode
    {
        //Properties
        public int DOFID
        { get; private set; }
        public float[][] RotationMatrix
        { get; private set; }
        public System.Windows.Media.Media3D.Point3D RotationPoint
        { get; private set; }

        //Fields
        //NA all values need to be exposed

        //Empty Constructor
        public BDOFNode()
        {
            // Initialize the Root ptr
            NodePtr = 0;

            // Set the NodeType
            NodeType = ENodeType.DOFNode;
        }

        //Constructor with file passed
        public BDOFNode(BinaryReader reader)
        {
            // Initialize the Root ptr
            NodePtr = reader.BaseStream.Position;

            // Set the NodeType
            NodeType = ENodeType.DOFNode;

            // Fill the Node Data
            FillNode(reader);
        }

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>Binary Reader must be open and at the correct position</remarks>
        public new int FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);

            // Fill the DOF specific data
            // Read the DOF ID
            DOFID = reader.ReadInt32();
            // Fill the 3x3 Matrix
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    RotationMatrix[i][j] = reader.ReadSingle();
                }
            }

            // Get the Rotation Point
            RotationPoint = new System.Windows.Media.Media3D.Point3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            return 1;
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";
            retText += "DOF ID: " + DOFID + "\nRotation Matrix:\n";
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    retText += RotationMatrix[i][j] + "  ";
                }
                retText += "\n";
            }
            retText += "Rotation Point: (" + RotationPoint.X + ", " + RotationPoint.Y + ", " + RotationPoint.Z + ")\n";
            

            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            string retText = base.DebugPrint() + "\n";
            retText += "(" + lineCount + ") DOF ID: " + DOFID + "\nRotation Matrix:\n"; lineCount += 4;
            for (int i = 0; i < 3; i++)
            {
                retText += "(" + lineCount + ")";
                for (int j = 0; j < 3; j++)
                {
                    retText += RotationMatrix[i][j] + "  "; lineCount += 4;
                }
                retText += "\n";
            }
            retText += "(" + lineCount + ") Rotation Point: (" + RotationPoint.X + ", " + RotationPoint.Y + ", " + RotationPoint.Z + ")\n"; lineCount += 4;
            return retText;
        }
    }

    /// <summary>
    /// Node to identify a Switch Node in the LOD.
    /// Inherits BNode.
    /// </summary>
    public class BSwitchNode : BNode, IBSPNode
    {
        //Properties
        public int Switch
        { get; private set; }

        //Fields
        private int _nChildren;
        private long _SubtreePtr;

        //Empty Constructor
        public BSwitchNode()
        {
            // Initialize the Root ptr
            NodePtr = 0;

            // Set the NodeType
            NodeType = ENodeType.SwitchNode;
        }

        //Constructor with file passed
        public BSwitchNode(BinaryReader reader)
        {
            // Initialize the Root ptr
            NodePtr = reader.BaseStream.Position;

            // Set the NodeType
            NodeType = ENodeType.SwitchNode;

            // Fill the Node
            FillNode(reader);
        }

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>Binary Reader must be open and at the correct position</remarks>
        public new int FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);

            // Fill the Switch specific data
            Switch = reader.ReadInt32();
            _nChildren = reader.ReadInt32();
            _SubtreePtr = (RootPtr + reader.ReadInt32());

            return 1;
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";
            retText += "Switch Number: " + Switch + "\n";            
            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            string retText = base.DebugPrint() + "\n";
            retText += "(" + lineCount + ") Switch Number: " + Switch + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") Number of Children: " + _nChildren + "\n";lineCount += 4;
            retText += "(" + lineCount + ") SubTree Pointer: " + _SubtreePtr + "\n"; lineCount += 4;
            return retText;
        }
    }

    /// <summary>
    /// Node to identify a Splitter Node in the LOD.
    /// Inherits BNode.
    /// </summary>
    public class BSplitterNode : BNode, IBSPNode
    {
        //Properties
        public float A
        { get; private set; }
        public float B
        { get; private set; }
        public float C
        { get; private set; }
        public float D
        { get; private set; }

        //Fields
        private long _frntBNodePtr;
        private long _backBNodePtr;

        //Empty Constructor
        public BSplitterNode()
        {
            // Initialize the Root ptr
            NodePtr = 0;

            // Set the NodeType
            NodeType = ENodeType.SplitterNode;
        }

        //Constructor with file passed
        public BSplitterNode(BinaryReader reader)
        {
            // Initialize the Root ptr
            NodePtr = reader.BaseStream.Position;

            // Set the NodeType
            NodeType = ENodeType.SplitterNode;

            // Fill the node
            FillNode(reader);
        }

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>Binary Reader must be open and at the correct position</remarks>
        public new int FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);

            // Fill the Splitter specific data
            A = reader.ReadSingle();
            B = reader.ReadSingle();
            C = reader.ReadSingle();
            D = reader.ReadSingle();
            _frntBNodePtr = reader.ReadInt32();
            _backBNodePtr = reader.ReadInt32();

            return 1;
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";
            retText += "A: " + A + "\n";
            retText += "B: " + B + "\n";
            retText += "C: " + C + "\n";
            retText += "D: " + D + "\n";
            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            string retText = base.DebugPrint() + "\n";
            retText += "(" + lineCount + ") A: " + A + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") B: " + B + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") C: " + C + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") D: " + D + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") Front BNode Pointer: " + _frntBNodePtr + "\n"; lineCount += 4;
            retText += "(" + lineCount + ") Back BNode Pointer: " + _backBNodePtr + "\n";lineCount += 4;
            return retText;
        }
    }

    /// <summary>
    /// Node to identify a Primitive in the LOD.
    /// Inherits BNode.
    /// </summary>
    public class BPrimitiveNode : BNode, IBSPNode
    {
        //Properties
        public Primitive.EPrimType PrimitiveType
        { get { return _Prim.PrimitiveType; } }        
        public IPrimitive Primitive
        { get { return _Prim; } }

        //Fields
        long _PrimPtr;
        IPrimitive _Prim;

        //Empty Constructor
        public BPrimitiveNode()
        {
            // Initialize the Root ptr
            NodePtr = 0;

            // Set the NodeType
            NodeType = ENodeType.PrimitiveNode;
        }

        //Constructor with file passed
        public BPrimitiveNode(BinaryReader reader)
        {
            // Initialize the Root ptr
            NodePtr = reader.BaseStream.Position;

            // Set the NodeType
            NodeType = ENodeType.PrimitiveNode;

            // Fill the node
            FillNode(reader);
        }

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>Binary Reader must be open and at the correct position</remarks>
        public new int FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);
            
            // Fill the Prim Data
            _PrimPtr = (RootPtr + reader.ReadInt32());

            // Get the Prim information
            _Prim = GetPrim(reader, _PrimPtr, reader.BaseStream.Position);
            
            return 1;
        }

        // Get the Poly
        private IPrimitive GetPrim(BinaryReader reader, long dataAddress, long returnAddress)
        {
            IPrimitive prim = new Primitive();
            reader.BaseStream.Position = dataAddress;
            switch ((int)reader.PeekChar())
            {
                // Prim Point
                case 0:
                    prim = new PrimitivePoint(reader);
                    break;

                // Prim Line
                case 1:
                    prim = new PrimitiveLine(reader);
                    break;

                // Poly Flat
                case 2:

                    break;

                // Poly Flat Lit
                case 3:

                    break;

                // Poly Gourad
                case 4:

                    break;

                // Poly Gourad Lit
                case 5:

                    break;
                case 6:

                    break;
                case 7:

                    break;
                case 8:

                    break;

            }
            


            reader.BaseStream.Position = returnAddress;
            return prim;
        }
        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";

            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            string retText = base.DebugPrint() + "\n";

            return retText;
        }
    }

    public class BLitPrimitiveNode : BNode, IBSPNode
    {
        //Properties

        //Fields

        //Empty Constructor

        //Constructor with file passed

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>Binary Reader must be open and at the correct position</remarks>
        public new int FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);

            return 1;
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";

            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            string retText = base.DebugPrint() + "\n";

            return retText;
        }
    }

    public class BCulledPrimitiveNode : BNode, IBSPNode
    {
        //Properties

        //Fields

        //Empty Constructor

        //Constructor with file passed

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>Binary Reader must be open and at the correct position</remarks>
        public new void FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";

            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            string retText = base.DebugPrint() + "\n";

            return retText;
        }
    }

    public class BSpecialXFormNode : BNode, IBSPNode
    {
        //Properties

        //Fields

        //Empty Constructor

        //Constructor with file passed

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>Binary Reader must be open and at the correct position</remarks>
        public new void FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";

            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            string retText = base.DebugPrint() + "\n";

            return retText;
        }
    }

    public class BLightStringNode : BPrimitiveNode, IBSPNode
    {
        //Properties

        //Fields

        //Empty Constructor

        //Constructor with file passed

        /// <summary>
        /// Helper function to read the data for this node
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <remarks>Binary Reader must be open and at the correct position</remarks>
        public new void FillNode(BinaryReader reader)
        {
            // Fill the BNode data
            base.FillNode(reader);
        }

        /// <summary>
        /// Print the properties for this BNode in a multi-line format
        /// </summary>
        /// <returns></returns> 
        public new string Print()
        {
            string retText = base.Print() + "\n";

            return retText;
        }

        /// <summary>
        /// Debug Printing used to help map the files.  Prints private properties in
        /// addition to the public properties, when they exist.  Will print in the order
        /// in which the data is found in the LOD file.
        /// </summary>
        /// <returns></returns>
        public new string DebugPrint()
        {
            string retText = base.DebugPrint() + "\n";

            return retText;
        }
    }


    // Primitive Class Structure
    public interface IPrimitive
    {
        Primitive.EPrimType PrimitiveType
        { get; set; }
        int FillPrim(BinaryReader reader);
    }

    public class Primitive : IPrimitive
    {
        // Enum Dec
        public enum EPrimType
        {
            Primitive = 0,
            PrimitivePoint,
            PrimitiveLine,
            PrimitiveLightString
        }

        // Properties
        public EPrimType PrimitiveType
        { get; set; }
        public Poly.EPolyType PolyType;

        // Fields
        protected int _nVerts;
        protected long _xyzPtr;

        // Empty constructor
        public Primitive()
        {
            PrimitiveType = EPrimType.Primitive;
        }

        // Constructor with File
        public Primitive(BinaryReader reader)
        {
            // Set the PrimType
            PrimitiveType = EPrimType.Primitive;

            // Fill the Prim
            FillPrim(reader);
        }

        /// <summary>
        /// Helper function to load the Prim Data from the File Stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <remarks>Reader must be open and at the correct position</remarks>
        /// <returns></returns>
        public int FillPrim(BinaryReader reader)
        {
            // Read the Prim Data
            PolyType = (Poly.EPolyType)reader.ReadInt32();
            _nVerts = reader.ReadInt32();
            _xyzPtr = reader.ReadInt32();

            return 1;
        }        

        // GetPoly???
    }

    /// <summary>
    /// PrimitivePoint Class.
    /// Inherits Prim.
    /// </summary>
    public class PrimitivePoint : Primitive, IPrimitive
    {
        // Properties
        public int RGBA
        { get; private set; }                       // Front Color Index

        // Fields
        //NA

        // Empty Constructor
        public PrimitivePoint()
        {
            // Primitive Type
            PrimitiveType = EPrimType.PrimitivePoint; 
        }

        // Constructor with File passed
        public PrimitivePoint(BinaryReader reader)
        {
            // Primitive Type
            PrimitiveType = EPrimType.PrimitivePoint;            

            // Fill the PointData
            FillPrim(reader);
        }
                
        /// <summary>
        /// Helper function to load the Prim Data from the File Stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public new int FillPrim(BinaryReader reader)
        {
            // Fill the Prim
            base.FillPrim(reader);

            RGBA = reader.ReadInt32();

            return 1;
        }

    }

    /// <summary>
    /// PrimitiveLine Class.
    /// Inherits Prim.
    /// </summary>
    public class PrimitiveLine : Primitive, IPrimitive
    {
        // Properties
        public int RGBA
        { get; private set; }                       // Front Color Index

        // Fields
        //NA

        // Empty Constructor
        public PrimitiveLine()
        {
            // Primitive Type
            PrimitiveType = EPrimType.PrimitiveLine;
        }

        // Constructor with File passed
        public PrimitiveLine(BinaryReader reader)
        {
            // Primitive Type
            PrimitiveType = EPrimType.PrimitiveLine;

            // Fill the PointData
            FillPrim(reader);
        }

        /// <summary>
        /// Helper function to load the Prim Data from the File Stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public new int FillPrim(BinaryReader reader)
        {
            // Fill the Prim
            base.FillPrim(reader);

            RGBA = reader.ReadInt32();

            return 1;
        }

    }

    /// <summary>
    /// PrimitiveLightString Class.
    /// Inherits Prim.
    /// </summary>
    public class PrimitiveLightString : Primitive, IPrimitive
    {
        // Properties
        public int RGBA
        { get; private set; }                       // Front Color Index
        public int RGBABack
        { get; private set; }                   // Back Color INDEX
        public BSPNode.Normal LightVector
        { get; private set; }     // I,J,K Vector
                
        // Fields
        //NA

        // Empty Constructor
        public PrimitiveLightString()
        {
            // Primitive Type
            PrimitiveType = EPrimType.PrimitiveLightString;
            
        }

        // Constructor with File passed
        public PrimitiveLightString(BinaryReader reader)
        {
            // Primitive Type
            PrimitiveType = EPrimType.PrimitiveLightString;

            // Fill the PointData
            FillPrim(reader);
        }

        /// <summary>
        /// Helper function to load the Prim Data from the File Stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public new int FillPrim(BinaryReader reader)
        {
            // Fill the Prim
            base.FillPrim(reader);

            RGBA = reader.ReadInt32();
            RGBABack = reader.ReadInt32();
            LightVector = new BSPNode.Normal(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            return 1;
        }
    }



    // Poly Class Structure
    public class Poly
    {
        public enum EPolyType { Poly = 0, PolyPoint}
        public Poly()
        {

        }
    }
}
