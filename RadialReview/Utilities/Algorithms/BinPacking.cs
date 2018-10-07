using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
/// <summary>
/// https://github.com/davidmchapman/3DContainerPacking
/// </summary>
namespace RadialReview.Utilities.BinPacking {

	public class RectangleBinPack<T> {

		public class Node {
			internal Node left;
			internal Node right;
			internal double width;
			internal double height;
			internal double x;
			internal double y;
			private double w;
			private double h;
			public T Object { get; set; }
			public Node() {
			}
			public Node(T obj, double w, double h) {
				this.w = w;
				this.h = h;
				Object = obj;
			}
		}

		public double binWidth { get; set; }
		public double binHeight { get; set; }
		public Node root { get; set; }

		/** Restarts the packing process, clearing all previously packed rectangles and
			sets up a new bin of a given initial size. These bin dimensions stay fixed during
			the whole packing process, i.e. to change the bin size, the packing must be
			restarted again with a new call to Init(). */
		public RectangleBinPack(int width, int height) {
			binWidth = width;
			binHeight = height;
			root = new Node();
			root.left = null;
			root.right = null;
			root.x = 0;
			root.y = 0;
		}

		/** @return A value [0, 1] denoting the ratio of total surface area that is in use.
			0.0f - the bin is totally empty, 1.0f - the bin is full. */
		public double Occupancy() {
			double totalSurfaceArea = binWidth * binHeight;
			double usedSurfaceArea = UsedSurfaceArea(root);

			return usedSurfaceArea / totalSurfaceArea;
		}

		/** Recursively calls itself. */
		public double UsedSurfaceArea(Node node) {
			if (node.left!=null || node.right != null) {
				double usedSurfaceArea = node.width * node.height;
				if (node.left != null)
					usedSurfaceArea += UsedSurfaceArea(node.left);
				if (node.right != null)
					usedSurfaceArea += UsedSurfaceArea(node.right);

				return usedSurfaceArea;
			}

			// This is a leaf node, it doesn't constitute to the total surface area.
			return 0;
		}

		/** Running time is linear to the number of rectangles already packed. Recursively calls itself.
			@return 0null If the insertion didn't succeed. */
		public Node Insert(Node node, double width, double height) {
			// If this node is an internal node, try both leaves for possible space.
			// (The rectangle in an internal node stores used space, the leaves store free space)
			if (node.left!=null || node.right!=null) {
				if (node.left != null) {
					var newNode = Insert(node.left, width, height);
					if (newNode != null)
						return newNode;
				}
				if (node.right != null) {
					var newNode = Insert(node.right, width, height);
					if (newNode!=null)
						return newNode;
				}
				return null; // Didn't fit into either subtree!
			}

			// This node is a leaf, but can we fit the new rectangle here?
			if (width > node.width || height > node.height)
				return null; // Too bad, no space.

			// The new cell will fit, split the remaining space along the shorter axis,
			// that is probably more optimal.
			double w = node.width - width;
			double h = node.height - height;
			node.left = new Node();
			node.right = new Node();
			if (w <= h) // Split the remaining space in horizontal direction.
			{
				node.left.x = node.x + width;
				node.left.y = node.y;
				node.left.width = w;
				node.left.height = height;

				node.right.x = node.x;
				node.right.y = node.y + height;
				node.right.width = node.width;
				node.right.height = h;
			} else {// Split the remaining space in vertical direction.

				node.left.x = node.x;
				node.left.y = node.y + height;
				node.left.width = width;
				node.left.height = h;

				node.right.x = node.x + width;
				node.right.y = node.y;
				node.right.width = w;
				node.right.height = node.height;
			}
			// Note that as a result of the above, it can happen that node->left or node->right
			// is now a degenerate (zero area) rectangle. No need to do anything about it,
			// like remove the nodes as "unnecessary" since they need to exist as children of
			// this node (this node can't be a leaf anymore).

			// This node is now a non-leaf, so shrink its area - it now denotes
			// *occupied* space instead of free space. Its children spawn the resulting
			// area of free space.
			node.width = width;
			node.height = height;
			return node;
		}

		public List<Node> Contents() {
			return Contents(root);
		}

		private List<Node> Contents(Node n) {
			var contents = new List<Node>();
			if (n != null) {
				if (n.left != null) {
					contents.Add(n.left);
					contents.AddRange(Contents(n.left));
				}
				if (n.right != null) {
					contents.Add(n.right);
					contents.AddRange(Contents(n.right));
				}
			}
			return contents;
		}
	}

		//public interface IPackingAlgorithm {
		//	/// <summary>
		//	/// Runs the algorithm on the specified container and items.
		//	/// </summary>
		//	/// <param name="container">The container.</param>
		//	/// <param name="items">The items to pack.</param>
		//	/// <returns>The algorithm packing result.</returns>
		//	AlgorithmPackingResult Run(Container container, List<Item> items);
		//}
		//public abstract class AlgorithmBase {
		//	public abstract ContainerPackingResult Run(Container container, List<Item> items);
		//}
		///// <summary>
		///// An item to be packed. Also used to hold post-packing details for the item.
		///// </summary>
		//[DataContract]
		//public class Item {
		//	#region Private Variables

		//	private decimal volume;

		//	#endregion Private Variables

		//	#region Constructors

		//	/// <summary>
		//	/// Initializes a new instance of the Item class.
		//	/// </summary>
		//	/// <param name="id">The item ID.</param>
		//	/// <param name="dim1">The length of one of the three item dimensions.</param>
		//	/// <param name="dim2">The length of another of the three item dimensions.</param>
		//	/// <param name="dim3">The length of the other of the three item dimensions.</param>
		//	/// <param name="itemQuantity">The item quantity.</param>
		//	public Item(int id, decimal dim1, decimal dim2, decimal dim3, int quantity) {
		//		this.ID = id;
		//		this.Dim1 = dim1;
		//		this.Dim2 = dim2;
		//		this.Dim3 = dim3;
		//		this.volume = dim1 * dim2 * dim3;
		//		this.Quantity = quantity;
		//	}

		//	#endregion Constructors

		//	#region Public Properties

		//	/// <summary>
		//	/// Gets or sets the item ID.
		//	/// </summary>
		//	/// <value>
		//	/// The item ID.
		//	/// </value>
		//	[DataMember]
		//	public int ID { get; set; }

		//	/// <summary>
		//	/// Gets or sets a value indicating whether this item has already been packed.
		//	/// </summary>
		//	/// <value>
		//	///   True if the item has already been packed; otherwise, false.
		//	/// </value>
		//	[DataMember]
		//	public bool IsPacked { get; set; }

		//	/// <summary>
		//	/// Gets or sets the length of one of the item dimensions.
		//	/// </summary>
		//	/// <value>
		//	/// The first item dimension.
		//	/// </value>
		//	[DataMember]
		//	public decimal Dim1 { get; set; }

		//	/// <summary>
		//	/// Gets or sets the length another of the item dimensions.
		//	/// </summary>
		//	/// <value>
		//	/// The second item dimension.
		//	/// </value>
		//	[DataMember]
		//	public decimal Dim2 { get; set; }

		//	/// <summary>
		//	/// Gets or sets the third of the item dimensions.
		//	/// </summary>
		//	/// <value>
		//	/// The third item dimension.
		//	/// </value>
		//	[DataMember]
		//	public decimal Dim3 { get; set; }

		//	/// <summary>
		//	/// Gets or sets the x coordinate of the location of the packed item within the container.
		//	/// </summary>
		//	/// <value>
		//	/// The x coordinate of the location of the packed item within the container.
		//	/// </value>
		//	[DataMember]
		//	public decimal CoordX { get; set; }

		//	/// <summary>
		//	/// Gets or sets the y coordinate of the location of the packed item within the container.
		//	/// </summary>
		//	/// <value>
		//	/// The y coordinate of the location of the packed item within the container.
		//	/// </value>
		//	[DataMember]
		//	public decimal CoordY { get; set; }

		//	/// <summary>
		//	/// Gets or sets the z coordinate of the location of the packed item within the container.
		//	/// </summary>
		//	/// <value>
		//	/// The z coordinate of the location of the packed item within the container.
		//	/// </value>
		//	[DataMember]
		//	public decimal CoordZ { get; set; }

		//	/// <summary>
		//	/// Gets or sets the item quantity.
		//	/// </summary>
		//	/// <value>
		//	/// The item quantity.
		//	/// </value>
		//	public int Quantity { get; set; }

		//	/// <summary>
		//	/// Gets or sets the x dimension of the orientation of the item as it has been packed.
		//	/// </summary>
		//	/// <value>
		//	/// The x dimension of the orientation of the item as it has been packed.
		//	/// </value>
		//	[DataMember]
		//	public decimal PackDimX { get; set; }

		//	/// <summary>
		//	/// Gets or sets the y dimension of the orientation of the item as it has been packed.
		//	/// </summary>
		//	/// <value>
		//	/// The y dimension of the orientation of the item as it has been packed.
		//	/// </value>
		//	[DataMember]
		//	public decimal PackDimY { get; set; }

		//	/// <summary>
		//	/// Gets or sets the z dimension of the orientation of the item as it has been packed.
		//	/// </summary>
		//	/// <value>
		//	/// The z dimension of the orientation of the item as it has been packed.
		//	/// </value>
		//	[DataMember]
		//	public decimal PackDimZ { get; set; }

		//	/// <summary>
		//	/// Gets the item volume.
		//	/// </summary>
		//	/// <value>
		//	/// The item volume.
		//	/// </value>
		//	[DataMember]
		//	public decimal Volume {
		//		get {
		//			return volume;
		//		}
		//	}

		//	#endregion Public Properties
		//}
		//[DataContract]
		//public enum AlgorithmType {
		//	/// <summary>
		//	/// The EB-AFIT packing algorithm type.
		//	/// </summary>
		//	[DataMember]
		//	EB_AFIT = 1
		//}
		///// <summary>
		///// The container packing result.
		///// </summary>
		//[DataContract]
		//public class ContainerPackingResult {
		//	#region Constructors

		//	public ContainerPackingResult() {
		//		this.AlgorithmPackingResults = new List<AlgorithmPackingResult>();
		//	}

		//	#endregion Constructors

		//	#region Public Properties

		//	/// <summary>
		//	/// Gets or sets the container ID.
		//	/// </summary>
		//	/// <value>
		//	/// The container ID.
		//	/// </value>
		//	[DataMember]
		//	public int ContainerID { get; set; }

		//	[DataMember]
		//	public List<AlgorithmPackingResult> AlgorithmPackingResults { get; set; }

		//	#endregion Public Properties
		//}
		///// <summary>
		///// The container to pack items into.
		///// </summary>
		//public class Container {
		//	#region Private Variables

		//	private decimal volume;

		//	#endregion Private Variables

		//	#region Constructors

		//	/// <summary>
		//	/// Initializes a new instance of the Container class.
		//	/// </summary>
		//	/// <param name="id">The container ID.</param>
		//	/// <param name="length">The container length.</param>
		//	/// <param name="width">The container width.</param>
		//	/// <param name="height">The container height.</param>
		//	public Container(int id, decimal length, decimal width, decimal height) {
		//		this.ID = id;
		//		this.Length = length;
		//		this.Width = width;
		//		this.Height = height;
		//		this.Volume = length * width * height;
		//	}

		//	#endregion Constructors

		//	#region Public Properties
		//	/// <summary>
		//	/// Gets or sets the container ID.
		//	/// </summary>
		//	/// <value>
		//	/// The container ID.
		//	/// </value>
		//	public int ID { get; set; }

		//	/// <summary>
		//	/// Gets or sets the container length.
		//	/// </summary>
		//	/// <value>
		//	/// The container length.
		//	/// </value>
		//	public decimal Length { get; set; }

		//	/// <summary>
		//	/// Gets or sets the container width.
		//	/// </summary>
		//	/// <value>
		//	/// The container width.
		//	/// </value>
		//	public decimal Width { get; set; }

		//	/// <summary>
		//	/// Gets or sets the container height.
		//	/// </summary>
		//	/// <value>
		//	/// The container height.
		//	/// </value>
		//	public decimal Height { get; set; }

		//	/// <summary>
		//	/// Gets or sets the volume of the container.
		//	/// </summary>
		//	/// <value>
		//	/// The volume of the container.
		//	/// </value>
		//	public decimal Volume {
		//		get {
		//			return this.volume;
		//		}
		//		set {
		//			this.volume = value;
		//		}
		//	}

		//	#endregion Public Properties
		//}

		//[DataContract]
		//public class AlgorithmPackingResult {
		//	#region Constructors

		//	public AlgorithmPackingResult() {
		//		this.PackedItems = new List<Item>();
		//		this.UnpackedItems = new List<Item>();
		//	}

		//	#endregion Constructors

		//	#region Public Properties

		//	[DataMember]
		//	public int AlgorithmID { get; set; }

		//	[DataMember]
		//	public string AlgorithmName { get; set; }

		//	/// <summary>
		//	/// Gets or sets a value indicating whether all of the items are packed in the container.
		//	/// </summary>
		//	/// <value>
		//	/// True if all the items are packed in the container; otherwise, false.
		//	/// </value>
		//	[DataMember]
		//	public bool IsCompletePack { get; set; }

		//	/// <summary>
		//	/// Gets or sets the list of packed items.
		//	/// </summary>
		//	/// <value>
		//	/// The list of packed items.
		//	/// </value>
		//	[DataMember]
		//	public List<Item> PackedItems { get; set; }

		//	/// <summary>
		//	/// Gets or sets the elapsed pack time in milliseconds.
		//	/// </summary>
		//	/// <value>
		//	/// The elapsed pack time in milliseconds.
		//	/// </value>
		//	[DataMember]
		//	public long PackTimeInMilliseconds { get; set; }

		//	/// <summary>
		//	/// Gets or sets the percent of container volume packed.
		//	/// </summary>
		//	/// <value>
		//	/// The percent of container volume packed.
		//	/// </value>
		//	[DataMember]
		//	public decimal PercentContainerVolumePacked { get; set; }

		//	/// <summary>
		//	/// Gets or sets the percent of item volume packed.
		//	/// </summary>
		//	/// <value>
		//	/// The percent of item volume packed.
		//	/// </value>
		//	[DataMember]
		//	public decimal PercentItemVolumePacked { get; set; }

		//	/// <summary>
		//	/// Gets or sets the list of unpacked items.
		//	/// </summary>
		//	/// <value>
		//	/// The list of unpacked items.
		//	/// </value>
		//	[DataMember]
		//	public List<Item> UnpackedItems { get; set; }

		//	#endregion Public Properties
		//}

		///// <summary>
		///// A 3D bin packing algorithm originally ported from https://github.com/keremdemirer/3dbinpackingjs,
		///// which itself was a JavaScript port of https://github.com/wknechtel/3d-bin-pack/, which is a C reconstruction 
		///// of a novel algorithm developed in a U.S. Air Force master's thesis by Erhan Baltacioglu in 2001.
		///// </summary>
		//public class EB_AFIT : IPackingAlgorithm {
		//	#region Public Methods

		//	/// <summary>
		//	/// Runs the packing algorithm.
		//	/// </summary>
		//	/// <param name="container">The container to pack items into.</param>
		//	/// <param name="items">The items to pack.</param>
		//	/// <returns>The bin packing result.</returns>
		//	public AlgorithmPackingResult Run(Container container, List<Item> items) {
		//		Initialize(container, items);
		//		ExecuteIterations(container);
		//		Report(container);

		//		AlgorithmPackingResult result = new AlgorithmPackingResult();
		//		result.AlgorithmID = (int)AlgorithmType.EB_AFIT;
		//		result.AlgorithmName = "EB-AFIT";

		//		for (int i = 1; i <= itemsToPackCount; i++) {
		//			itemsToPack[i].Quantity = 1;

		//			if (!itemsToPack[i].IsPacked) {
		//				result.UnpackedItems.Add(itemsToPack[i]);
		//			}
		//		}

		//		result.PackedItems = itemsPackedInOrder;



		//		if (result.UnpackedItems.Count == 0) {
		//			result.IsCompletePack = true;
		//		}

		//		return result;
		//	}

		//	#endregion Public Methods

		//	#region Private Variables

		//	private List<Item> itemsToPack;
		//	private List<Item> itemsPackedInOrder;
		//	private List<Layer> layers;
		//	private ContainerPackingResult result;

		//	private ScrapPad scrapfirst;
		//	private ScrapPad smallestZ;
		//	private ScrapPad trash;

		//	private bool evened;
		//	private bool hundredPercentPacked = false;
		//	private bool layerDone;
		//	private bool packing;
		//	private bool packingBest = false;
		//	private bool quit = false;

		//	private int bboxi;
		//	private int bestIteration;
		//	private int bestVariant;
		//	private int boxi;
		//	private int cboxi;
		//	private int layerListLen;
		//	private int packedItemCount;
		//	private int x;

		//	private decimal bbfx;
		//	private decimal bbfy;
		//	private decimal bbfz;
		//	private decimal bboxx;
		//	private decimal bboxy;
		//	private decimal bboxz;
		//	private decimal bfx;
		//	private decimal bfy;
		//	private decimal bfz;
		//	private decimal boxx;
		//	private decimal boxy;
		//	private decimal boxz;
		//	private decimal cboxx;
		//	private decimal cboxy;
		//	private decimal cboxz;
		//	private decimal layerinlayer;
		//	private decimal layerThickness;
		//	private decimal lilz;
		//	private decimal packedVolume;
		//	private decimal packedy;
		//	private decimal prelayer;
		//	private decimal prepackedy;
		//	private decimal preremainpy;
		//	private decimal px;
		//	private decimal py;
		//	private decimal pz;
		//	private decimal remainpy;
		//	private decimal remainpz;
		//	private decimal itemsToPackCount;
		//	private decimal totalItemVolume;
		//	private decimal totalContainerVolume;

		//	#endregion Private Variables

		//	#region Private Methods

		//	/// <summary>
		//	/// Analyzes each unpacked box to find the best fitting one to the empty space given.
		//	/// </summary>
		//	private void AnalyzeBox(decimal hmx, decimal hy, decimal hmy, decimal hz, decimal hmz, decimal dim1, decimal dim2, decimal dim3) {
		//		if (dim1 <= hmx && dim2 <= hmy && dim3 <= hmz) {
		//			if (dim2 <= hy) {
		//				if (hy - dim2 < bfy) {
		//					boxx = dim1;
		//					boxy = dim2;
		//					boxz = dim3;
		//					bfx = hmx - dim1;
		//					bfy = hy - dim2;
		//					bfz = Math.Abs(hz - dim3);
		//					boxi = x;
		//				} else if (hy - dim2 == bfy && hmx - dim1 < bfx) {
		//					boxx = dim1;
		//					boxy = dim2;
		//					boxz = dim3;
		//					bfx = hmx - dim1;
		//					bfy = hy - dim2;
		//					bfz = Math.Abs(hz - dim3);
		//					boxi = x;
		//				} else if (hy - dim2 == bfy && hmx - dim1 == bfx && Math.Abs(hz - dim3) < bfz) {
		//					boxx = dim1;
		//					boxy = dim2;
		//					boxz = dim3;
		//					bfx = hmx - dim1;
		//					bfy = hy - dim2;
		//					bfz = Math.Abs(hz - dim3);
		//					boxi = x;
		//				}
		//			} else {
		//				if (dim2 - hy < bbfy) {
		//					bboxx = dim1;
		//					bboxy = dim2;
		//					bboxz = dim3;
		//					bbfx = hmx - dim1;
		//					bbfy = dim2 - hy;
		//					bbfz = Math.Abs(hz - dim3);
		//					bboxi = x;
		//				} else if (dim2 - hy == bbfy && hmx - dim1 < bbfx) {
		//					bboxx = dim1;
		//					bboxy = dim2;
		//					bboxz = dim3;
		//					bbfx = hmx - dim1;
		//					bbfy = dim2 - hy;
		//					bbfz = Math.Abs(hz - dim3);
		//					bboxi = x;
		//				} else if (dim2 - hy == bbfy && hmx - dim1 == bbfx && Math.Abs(hz - dim3) < bbfz) {
		//					bboxx = dim1;
		//					bboxy = dim2;
		//					bboxz = dim3;
		//					bbfx = hmx - dim1;
		//					bbfy = dim2 - hy;
		//					bbfz = Math.Abs(hz - dim3);
		//					bboxi = x;
		//				}
		//			}
		//		}
		//	}

		//	/// <summary>
		//	/// After finding each box, the candidate boxes and the condition of the layer are examined.
		//	/// </summary>
		//	private void CheckFound() {
		//		evened = false;

		//		if (boxi != 0) {
		//			cboxi = boxi;
		//			cboxx = boxx;
		//			cboxy = boxy;
		//			cboxz = boxz;
		//		} else {
		//			if ((bboxi > 0) && (layerinlayer != 0 || (smallestZ.Pre == null && smallestZ.Post == null))) {
		//				if (layerinlayer == 0) {
		//					prelayer = layerThickness;
		//					lilz = smallestZ.CumZ;
		//				}

		//				cboxi = bboxi;
		//				cboxx = bboxx;
		//				cboxy = bboxy;
		//				cboxz = bboxz;
		//				layerinlayer = layerinlayer + bboxy - layerThickness;
		//				layerThickness = bboxy;
		//			} else {
		//				if (smallestZ.Pre == null && smallestZ.Post == null) {
		//					layerDone = true;
		//				} else {
		//					evened = true;

		//					if (smallestZ.Pre == null) {
		//						trash = smallestZ.Post;
		//						smallestZ.CumX = smallestZ.Post.CumX;
		//						smallestZ.CumZ = smallestZ.Post.CumZ;
		//						smallestZ.Post = smallestZ.Post.Post;
		//						if (smallestZ.Post != null) {
		//							smallestZ.Post.Pre = smallestZ;
		//						}
		//					} else if (smallestZ.Post == null) {
		//						smallestZ.Pre.Post = null;
		//						smallestZ.Pre.CumX = smallestZ.CumX;
		//					} else {
		//						if (smallestZ.Pre.CumZ == smallestZ.Post.CumZ) {
		//							smallestZ.Pre.Post = smallestZ.Post.Post;

		//							if (smallestZ.Post.Post != null) {
		//								smallestZ.Post.Post.Pre = smallestZ.Pre;
		//							}

		//							smallestZ.Pre.CumX = smallestZ.Post.CumX;
		//						} else {
		//							smallestZ.Pre.Post = smallestZ.Post;
		//							smallestZ.Post.Pre = smallestZ.Pre;

		//							if (smallestZ.Pre.CumZ < smallestZ.Post.CumZ) {
		//								smallestZ.Pre.CumX = smallestZ.CumX;
		//							}
		//						}
		//					}
		//				}
		//			}
		//		}
		//	}

		//	/// <summary>
		//	/// Executes the packing algorithm variants.
		//	/// </summary>
		//	private void ExecuteIterations(Container container) {
		//		int itelayer;
		//		int layersIndex;
		//		decimal bestVolume = 0.0M;

		//		for (int containerOrientationVariant = 1; (containerOrientationVariant <= 6) && !quit; containerOrientationVariant++) {
		//			switch (containerOrientationVariant) {
		//				case 1:
		//					px = container.Length;
		//					py = container.Height;
		//					pz = container.Width;
		//					break;

		//				case 2:
		//					px = container.Width;
		//					py = container.Height;
		//					pz = container.Length;
		//					break;

		//				case 3:
		//					px = container.Width;
		//					py = container.Length;
		//					pz = container.Height;
		//					break;

		//				case 4:
		//					px = container.Height;
		//					py = container.Length;
		//					pz = container.Width;
		//					break;

		//				case 5:
		//					px = container.Length;
		//					py = container.Width;
		//					pz = container.Height;
		//					break;

		//				case 6:
		//					px = container.Height;
		//					py = container.Width;
		//					pz = container.Length;
		//					break;
		//			}

		//			layers.Add(new Layer { LayerEval = -1 });
		//			ListCanditLayers();
		//			layers = layers.OrderBy(l => l.LayerEval).ToList();

		//			for (layersIndex = 1; (layersIndex <= layerListLen) && !quit; layersIndex++) {
		//				packedVolume = 0.0M;
		//				packedy = 0;
		//				packing = true;
		//				layerThickness = layers[layersIndex].LayerDim;
		//				itelayer = layersIndex;
		//				remainpy = py;
		//				remainpz = pz;
		//				packedItemCount = 0;

		//				for (x = 1; x <= itemsToPackCount; x++) {
		//					itemsToPack[x].IsPacked = false;
		//				}

		//				do {
		//					layerinlayer = 0;
		//					layerDone = false;

		//					PackLayer();

		//					packedy = packedy + layerThickness;
		//					remainpy = py - packedy;

		//					if (layerinlayer != 0 && !quit) {
		//						prepackedy = packedy;
		//						preremainpy = remainpy;
		//						remainpy = layerThickness - prelayer;
		//						packedy = packedy - layerThickness + prelayer;
		//						remainpz = lilz;
		//						layerThickness = layerinlayer;
		//						layerDone = false;

		//						PackLayer();

		//						packedy = prepackedy;
		//						remainpy = preremainpy;
		//						remainpz = pz;
		//					}

		//					FindLayer(remainpy);
		//				} while (packing && !quit);

		//				if ((packedVolume > bestVolume) && !quit) {
		//					bestVolume = packedVolume;
		//					bestVariant = containerOrientationVariant;
		//					bestIteration = itelayer;
		//				}

		//				if (hundredPercentPacked)
		//					break;
		//			}

		//			if (hundredPercentPacked)
		//				break;

		//			if ((container.Length == container.Height) && (container.Height == container.Width))
		//				containerOrientationVariant = 6;

		//			layers = new List<Layer>();
		//		}
		//	}

		//	/// <summary>
		//	/// Finds the most proper boxes by looking at all six possible orientations,
		//	/// empty space given, adjacent boxes, and pallet limits.
		//	/// </summary>
		//	private void FindBox(decimal hmx, decimal hy, decimal hmy, decimal hz, decimal hmz) {
		//		int y;
		//		bfx = 32767;
		//		bfy = 32767;
		//		bfz = 32767;
		//		bbfx = 32767;
		//		bbfy = 32767;
		//		bbfz = 32767;
		//		boxi = 0;
		//		bboxi = 0;

		//		for (y = 1; y <= itemsToPackCount; y = y + itemsToPack[y].Quantity) {
		//			for (x = y; x < x + itemsToPack[y].Quantity - 1; x++) {
		//				if (!itemsToPack[x].IsPacked)
		//					break;
		//			}

		//			if (itemsToPack[x].IsPacked)
		//				continue;

		//			if (x > itemsToPackCount)
		//				return;

		//			AnalyzeBox(hmx, hy, hmy, hz, hmz, itemsToPack[x].Dim1, itemsToPack[x].Dim2, itemsToPack[x].Dim3);

		//			if ((itemsToPack[x].Dim1 == itemsToPack[x].Dim3) && (itemsToPack[x].Dim3 == itemsToPack[x].Dim2))
		//				continue;

		//			AnalyzeBox(hmx, hy, hmy, hz, hmz, itemsToPack[x].Dim1, itemsToPack[x].Dim3, itemsToPack[x].Dim2);
		//			AnalyzeBox(hmx, hy, hmy, hz, hmz, itemsToPack[x].Dim2, itemsToPack[x].Dim1, itemsToPack[x].Dim3);
		//			AnalyzeBox(hmx, hy, hmy, hz, hmz, itemsToPack[x].Dim2, itemsToPack[x].Dim3, itemsToPack[x].Dim1);
		//			AnalyzeBox(hmx, hy, hmy, hz, hmz, itemsToPack[x].Dim3, itemsToPack[x].Dim1, itemsToPack[x].Dim2);
		//			AnalyzeBox(hmx, hy, hmy, hz, hmz, itemsToPack[x].Dim3, itemsToPack[x].Dim2, itemsToPack[x].Dim1);
		//		}
		//	}

		//	/// <summary>
		//	/// Finds the most proper layer height by looking at the unpacked boxes and the remaining empty space available.
		//	/// </summary>
		//	private void FindLayer(decimal thickness) {
		//		decimal exdim = 0;
		//		decimal dimdif;
		//		decimal dimen2 = 0;
		//		decimal dimen3 = 0;
		//		int y;
		//		int z;
		//		decimal layereval;
		//		decimal eval;
		//		layerThickness = 0;
		//		eval = 1000000;

		//		for (x = 1; x <= itemsToPackCount; x++) {
		//			if (itemsToPack[x].IsPacked)
		//				continue;

		//			for (y = 1; y <= 3; y++) {
		//				switch (y) {
		//					case 1:
		//						exdim = itemsToPack[x].Dim1;
		//						dimen2 = itemsToPack[x].Dim2;
		//						dimen3 = itemsToPack[x].Dim3;
		//						break;

		//					case 2:
		//						exdim = itemsToPack[x].Dim2;
		//						dimen2 = itemsToPack[x].Dim1;
		//						dimen3 = itemsToPack[x].Dim3;
		//						break;

		//					case 3:
		//						exdim = itemsToPack[x].Dim3;
		//						dimen2 = itemsToPack[x].Dim1;
		//						dimen3 = itemsToPack[x].Dim2;
		//						break;
		//				}

		//				layereval = 0;

		//				if ((exdim <= thickness) && (((dimen2 <= px) && (dimen3 <= pz)) || ((dimen3 <= px) && (dimen2 <= pz)))) {
		//					for (z = 1; z <= itemsToPackCount; z++) {
		//						if (!(x == z) && !(itemsToPack[z].IsPacked)) {
		//							dimdif = Math.Abs(exdim - itemsToPack[z].Dim1);

		//							if (Math.Abs(exdim - itemsToPack[z].Dim2) < dimdif) {
		//								dimdif = Math.Abs(exdim - itemsToPack[z].Dim2);
		//							}

		//							if (Math.Abs(exdim - itemsToPack[z].Dim3) < dimdif) {
		//								dimdif = Math.Abs(exdim - itemsToPack[z].Dim3);
		//							}

		//							layereval = layereval + dimdif;
		//						}
		//					}

		//					if (layereval < eval) {
		//						eval = layereval;
		//						layerThickness = exdim;
		//					}
		//				}
		//			}
		//		}

		//		if (layerThickness == 0 || layerThickness > remainpy)
		//			packing = false;
		//	}

		//	/// <summary>
		//	/// Finds the first to be packed gap in the layer edge.
		//	/// </summary>
		//	private void FindSmallestZ() {
		//		ScrapPad scrapmemb = scrapfirst;
		//		smallestZ = scrapmemb;

		//		while (scrapmemb.Post != null) {
		//			if (scrapmemb.Post.CumZ < smallestZ.CumZ) {
		//				smallestZ = scrapmemb.Post;
		//			}

		//			scrapmemb = scrapmemb.Post;
		//		}
		//	}

		//	/// <summary>
		//	/// Initializes everything.
		//	/// </summary>
		//	private void Initialize(Container container, List<Item> items) {
		//		itemsToPack = new List<Item>();
		//		itemsPackedInOrder = new List<Item>();
		//		result = new ContainerPackingResult();

		//		// The original code uses 1-based indexing everywhere. This fake entry is added to the beginning
		//		// of the list to make that possible.
		//		itemsToPack.Add(new Item(0, 0, 0, 0, 0));

		//		layers = new List<Layer>();
		//		itemsToPackCount = 0;

		//		foreach (Item item in items) {
		//			for (int i = 1; i <= item.Quantity; i++) {
		//				Item newItem = new Item(item.ID, item.Dim1, item.Dim2, item.Dim3, item.Quantity);
		//				itemsToPack.Add(newItem);
		//			}

		//			itemsToPackCount += item.Quantity;
		//		}

		//		itemsToPack.Add(new Item(0, 0, 0, 0, 0));

		//		totalContainerVolume = container.Length * container.Height * container.Width;
		//		totalItemVolume = 0.0M;

		//		for (x = 1; x <= itemsToPackCount; x++) {
		//			totalItemVolume = totalItemVolume + itemsToPack[x].Volume;
		//		}

		//		scrapfirst = new ScrapPad();

		//		scrapfirst.Pre = null;
		//		scrapfirst.Post = null;
		//		packingBest = false;
		//		hundredPercentPacked = false;
		//		quit = false;
		//	}

		//	/// <summary>
		//	/// Lists all possible layer heights by giving a weight value to each of them.
		//	/// </summary>
		//	private void ListCanditLayers() {
		//		bool same;
		//		decimal exdim = 0;
		//		decimal dimdif;
		//		decimal dimen2 = 0;
		//		decimal dimen3 = 0;
		//		int y;
		//		int z;
		//		int k;
		//		decimal layereval;

		//		layerListLen = 0;

		//		for (x = 1; x <= itemsToPackCount; x++) {
		//			for (y = 1; y <= 3; y++) {
		//				switch (y) {
		//					case 1:
		//						exdim = itemsToPack[x].Dim1;
		//						dimen2 = itemsToPack[x].Dim2;
		//						dimen3 = itemsToPack[x].Dim3;
		//						break;

		//					case 2:
		//						exdim = itemsToPack[x].Dim2;
		//						dimen2 = itemsToPack[x].Dim1;
		//						dimen3 = itemsToPack[x].Dim3;
		//						break;

		//					case 3:
		//						exdim = itemsToPack[x].Dim3;
		//						dimen2 = itemsToPack[x].Dim1;
		//						dimen3 = itemsToPack[x].Dim2;
		//						break;
		//				}

		//				if ((exdim > py) || (((dimen2 > px) || (dimen3 > pz)) && ((dimen3 > px) || (dimen2 > pz))))
		//					continue;

		//				same = false;

		//				for (k = 1; k <= layerListLen; k++) {
		//					if (exdim == layers[k].LayerDim) {
		//						same = true;
		//						continue;
		//					}
		//				}

		//				if (same)
		//					continue;

		//				layereval = 0;

		//				for (z = 1; z <= itemsToPackCount; z++) {
		//					if (!(x == z)) {
		//						dimdif = Math.Abs(exdim - itemsToPack[z].Dim1);

		//						if (Math.Abs(exdim - itemsToPack[z].Dim2) < dimdif) {
		//							dimdif = Math.Abs(exdim - itemsToPack[z].Dim2);
		//						}
		//						if (Math.Abs(exdim - itemsToPack[z].Dim3) < dimdif) {
		//							dimdif = Math.Abs(exdim - itemsToPack[z].Dim3);
		//						}
		//						layereval = layereval + dimdif;
		//					}
		//				}

		//				layerListLen++;

		//				layers.Add(new Layer());
		//				layers[layerListLen].LayerEval = layereval;
		//				layers[layerListLen].LayerDim = exdim;
		//			}
		//		}
		//	}

		//	/// <summary>
		//	/// Transforms the found coordinate system to the one entered by the user and writes them
		//	/// to the report file.
		//	/// </summary>
		//	private void OutputBoxList() {
		//		decimal packCoordX = 0;
		//		decimal packCoordY = 0;
		//		decimal packCoordZ = 0;
		//		dynamic packDimX = 0;
		//		dynamic packDimY = 0;
		//		dynamic packDimZ = 0;

		//		switch (bestVariant) {
		//			case 1:
		//				packCoordX = itemsToPack[cboxi].CoordX;
		//				packCoordY = itemsToPack[cboxi].CoordY;
		//				packCoordZ = itemsToPack[cboxi].CoordZ;
		//				packDimX = itemsToPack[cboxi].PackDimX;
		//				packDimY = itemsToPack[cboxi].PackDimY;
		//				packDimZ = itemsToPack[cboxi].PackDimZ;
		//				break;

		//			case 2:
		//				packCoordX = itemsToPack[cboxi].CoordZ;
		//				packCoordY = itemsToPack[cboxi].CoordY;
		//				packCoordZ = itemsToPack[cboxi].CoordX;
		//				packDimX = itemsToPack[cboxi].PackDimZ;
		//				packDimY = itemsToPack[cboxi].PackDimY;
		//				packDimZ = itemsToPack[cboxi].PackDimX;
		//				break;

		//			case 3:
		//				packCoordX = itemsToPack[cboxi].CoordY;
		//				packCoordY = itemsToPack[cboxi].CoordZ;
		//				packCoordZ = itemsToPack[cboxi].CoordX;
		//				packDimX = itemsToPack[cboxi].PackDimY;
		//				packDimY = itemsToPack[cboxi].PackDimZ;
		//				packDimZ = itemsToPack[cboxi].PackDimX;
		//				break;

		//			case 4:
		//				packCoordX = itemsToPack[cboxi].CoordY;
		//				packCoordY = itemsToPack[cboxi].CoordX;
		//				packCoordZ = itemsToPack[cboxi].CoordZ;
		//				packDimX = itemsToPack[cboxi].PackDimY;
		//				packDimY = itemsToPack[cboxi].PackDimX;
		//				packDimZ = itemsToPack[cboxi].PackDimZ;
		//				break;

		//			case 5:
		//				packCoordX = itemsToPack[cboxi].CoordX;
		//				packCoordY = itemsToPack[cboxi].CoordZ;
		//				packCoordZ = itemsToPack[cboxi].CoordY;
		//				packDimX = itemsToPack[cboxi].PackDimX;
		//				packDimY = itemsToPack[cboxi].PackDimZ;
		//				packDimZ = itemsToPack[cboxi].PackDimY;
		//				break;

		//			case 6:
		//				packCoordX = itemsToPack[cboxi].CoordZ;
		//				packCoordY = itemsToPack[cboxi].CoordX;
		//				packCoordZ = itemsToPack[cboxi].CoordY;
		//				packDimX = itemsToPack[cboxi].PackDimZ;
		//				packDimY = itemsToPack[cboxi].PackDimX;
		//				packDimZ = itemsToPack[cboxi].PackDimY;
		//				break;
		//		}

		//		itemsToPack[cboxi].CoordX = packCoordX;
		//		itemsToPack[cboxi].CoordY = packCoordY;
		//		itemsToPack[cboxi].CoordZ = packCoordZ;
		//		itemsToPack[cboxi].PackDimX = packDimX;
		//		itemsToPack[cboxi].PackDimY = packDimY;
		//		itemsToPack[cboxi].PackDimZ = packDimZ;

		//		itemsPackedInOrder.Add(itemsToPack[cboxi]);
		//	}

		//	/// <summary>
		//	/// Packs the boxes found and arranges all variables and records properly.
		//	/// </summary>
		//	private void PackLayer() {
		//		decimal lenx;
		//		decimal lenz;
		//		decimal lpz;

		//		if (layerThickness == 0) {
		//			packing = false;
		//			return;
		//		}

		//		scrapfirst.CumX = px;
		//		scrapfirst.CumZ = 0;

		//		for (; !quit;) {
		//			FindSmallestZ();

		//			if ((smallestZ.Pre == null) && (smallestZ.Post == null)) {
		//				//*** SITUATION-1: NO BOXES ON THE RIGHT AND LEFT SIDES ***

		//				lenx = smallestZ.CumX;
		//				lpz = remainpz - smallestZ.CumZ;
		//				FindBox(lenx, layerThickness, remainpy, lpz, lpz);
		//				CheckFound();

		//				if (layerDone)
		//					break;
		//				if (evened)
		//					continue;

		//				itemsToPack[cboxi].CoordX = 0;
		//				itemsToPack[cboxi].CoordY = packedy;
		//				itemsToPack[cboxi].CoordZ = smallestZ.CumZ;
		//				if (cboxx == smallestZ.CumX) {
		//					smallestZ.CumZ = smallestZ.CumZ + cboxz;
		//				} else {
		//					smallestZ.Post = new ScrapPad();

		//					smallestZ.Post.Post = null;
		//					smallestZ.Post.Pre = smallestZ;
		//					smallestZ.Post.CumX = smallestZ.CumX;
		//					smallestZ.Post.CumZ = smallestZ.CumZ;
		//					smallestZ.CumX = cboxx;
		//					smallestZ.CumZ = smallestZ.CumZ + cboxz;
		//				}
		//			} else if (smallestZ.Pre == null) {
		//				//*** SITUATION-2: NO BOXES ON THE LEFT SIDE ***

		//				lenx = smallestZ.CumX;
		//				lenz = smallestZ.Post.CumZ - smallestZ.CumZ;
		//				lpz = remainpz - smallestZ.CumZ;
		//				FindBox(lenx, layerThickness, remainpy, lenz, lpz);
		//				CheckFound();

		//				if (layerDone)
		//					break;
		//				if (evened)
		//					continue;

		//				itemsToPack[cboxi].CoordY = packedy;
		//				itemsToPack[cboxi].CoordZ = smallestZ.CumZ;
		//				if (cboxx == smallestZ.CumX) {
		//					itemsToPack[cboxi].CoordX = 0;

		//					if (smallestZ.CumZ + cboxz == smallestZ.Post.CumZ) {
		//						smallestZ.CumZ = smallestZ.Post.CumZ;
		//						smallestZ.CumX = smallestZ.Post.CumX;
		//						trash = smallestZ.Post;
		//						smallestZ.Post = smallestZ.Post.Post;

		//						if (smallestZ.Post != null) {
		//							smallestZ.Post.Pre = smallestZ;
		//						}
		//					} else {
		//						smallestZ.CumZ = smallestZ.CumZ + cboxz;
		//					}
		//				} else {
		//					itemsToPack[cboxi].CoordX = smallestZ.CumX - cboxx;

		//					if (smallestZ.CumZ + cboxz == smallestZ.Post.CumZ) {
		//						smallestZ.CumX = smallestZ.CumX - cboxx;
		//					} else {
		//						smallestZ.Post.Pre = new ScrapPad();

		//						smallestZ.Post.Pre.Post = smallestZ.Post;
		//						smallestZ.Post.Pre.Pre = smallestZ;
		//						smallestZ.Post = smallestZ.Post.Pre;
		//						smallestZ.Post.CumX = smallestZ.CumX;
		//						smallestZ.CumX = smallestZ.CumX - cboxx;
		//						smallestZ.Post.CumZ = smallestZ.CumZ + cboxz;
		//					}
		//				}
		//			} else if (smallestZ.Post == null) {
		//				//*** SITUATION-3: NO BOXES ON THE RIGHT SIDE ***

		//				lenx = smallestZ.CumX - smallestZ.Pre.CumX;
		//				lenz = smallestZ.Pre.CumZ - smallestZ.CumZ;
		//				lpz = remainpz - smallestZ.CumZ;
		//				FindBox(lenx, layerThickness, remainpy, lenz, lpz);
		//				CheckFound();

		//				if (layerDone)
		//					break;
		//				if (evened)
		//					continue;

		//				itemsToPack[cboxi].CoordY = packedy;
		//				itemsToPack[cboxi].CoordZ = smallestZ.CumZ;
		//				itemsToPack[cboxi].CoordX = smallestZ.Pre.CumX;

		//				if (cboxx == smallestZ.CumX - smallestZ.Pre.CumX) {
		//					if (smallestZ.CumZ + cboxz == smallestZ.Pre.CumZ) {
		//						smallestZ.Pre.CumX = smallestZ.CumX;
		//						smallestZ.Pre.Post = null;
		//					} else {
		//						smallestZ.CumZ = smallestZ.CumZ + cboxz;
		//					}
		//				} else {
		//					if (smallestZ.CumZ + cboxz == smallestZ.Pre.CumZ) {
		//						smallestZ.Pre.CumX = smallestZ.Pre.CumX + cboxx;
		//					} else {
		//						smallestZ.Pre.Post = new ScrapPad();

		//						smallestZ.Pre.Post.Pre = smallestZ.Pre;
		//						smallestZ.Pre.Post.Post = smallestZ;
		//						smallestZ.Pre = smallestZ.Pre.Post;
		//						smallestZ.Pre.CumX = smallestZ.Pre.Pre.CumX + cboxx;
		//						smallestZ.Pre.CumZ = smallestZ.CumZ + cboxz;
		//					}
		//				}
		//			} else if (smallestZ.Pre.CumZ == smallestZ.Post.CumZ) {
		//				//*** SITUATION-4: THERE ARE BOXES ON BOTH OF THE SIDES ***

		//				//*** SUBSITUATION-4A: SIDES ARE EQUAL TO EACH OTHER ***

		//				lenx = smallestZ.CumX - smallestZ.Pre.CumX;
		//				lenz = smallestZ.Pre.CumZ - smallestZ.CumZ;
		//				lpz = remainpz - smallestZ.CumZ;

		//				FindBox(lenx, layerThickness, remainpy, lenz, lpz);
		//				CheckFound();

		//				if (layerDone)
		//					break;
		//				if (evened)
		//					continue;

		//				itemsToPack[cboxi].CoordY = packedy;
		//				itemsToPack[cboxi].CoordZ = smallestZ.CumZ;

		//				if (cboxx == smallestZ.CumX - smallestZ.Pre.CumX) {
		//					itemsToPack[cboxi].CoordX = smallestZ.Pre.CumX;

		//					if (smallestZ.CumZ + cboxz == smallestZ.Post.CumZ) {
		//						smallestZ.Pre.CumX = smallestZ.Post.CumX;

		//						if (smallestZ.Post.Post != null) {
		//							smallestZ.Pre.Post = smallestZ.Post.Post;
		//							smallestZ.Post.Post.Pre = smallestZ.Pre;
		//						} else {
		//							smallestZ.Pre.Post = null;
		//						}
		//					} else {
		//						smallestZ.CumZ = smallestZ.CumZ + cboxz;
		//					}
		//				} else if (smallestZ.Pre.CumX < px - smallestZ.CumX) {
		//					if (smallestZ.CumZ + cboxz == smallestZ.Pre.CumZ) {
		//						smallestZ.CumX = smallestZ.CumX - cboxx;
		//						itemsToPack[cboxi].CoordX = smallestZ.CumX - cboxx;
		//					} else {
		//						itemsToPack[cboxi].CoordX = smallestZ.Pre.CumX;
		//						smallestZ.Pre.Post = new ScrapPad();

		//						smallestZ.Pre.Post.Pre = smallestZ.Pre;
		//						smallestZ.Pre.Post.Post = smallestZ;
		//						smallestZ.Pre = smallestZ.Pre.Post;
		//						smallestZ.Pre.CumX = smallestZ.Pre.Pre.CumX + cboxx;
		//						smallestZ.Pre.CumZ = smallestZ.CumZ + cboxz;
		//					}
		//				} else {
		//					if (smallestZ.CumZ + cboxz == smallestZ.Pre.CumZ) {
		//						smallestZ.Pre.CumX = smallestZ.Pre.CumX + cboxx;
		//						itemsToPack[cboxi].CoordX = smallestZ.Pre.CumX;
		//					} else {
		//						itemsToPack[cboxi].CoordX = smallestZ.CumX - cboxx;
		//						smallestZ.Post.Pre = new ScrapPad();

		//						smallestZ.Post.Pre.Post = smallestZ.Post;
		//						smallestZ.Post.Pre.Pre = smallestZ;
		//						smallestZ.Post = smallestZ.Post.Pre;
		//						smallestZ.Post.CumX = smallestZ.CumX;
		//						smallestZ.Post.CumZ = smallestZ.CumZ + cboxz;
		//						smallestZ.CumX = smallestZ.CumX - cboxx;
		//					}
		//				}
		//			} else {
		//				//*** SUBSITUATION-4B: SIDES ARE NOT EQUAL TO EACH OTHER ***

		//				lenx = smallestZ.CumX - smallestZ.Pre.CumX;
		//				lenz = smallestZ.Pre.CumZ - smallestZ.CumZ;
		//				lpz = remainpz - smallestZ.CumZ;
		//				FindBox(lenx, layerThickness, remainpy, lenz, lpz);
		//				CheckFound();

		//				if (layerDone)
		//					break;
		//				if (evened)
		//					continue;

		//				itemsToPack[cboxi].CoordY = packedy;
		//				itemsToPack[cboxi].CoordZ = smallestZ.CumZ;
		//				itemsToPack[cboxi].CoordX = smallestZ.Pre.CumX;

		//				if (cboxx == (smallestZ.CumX - smallestZ.Pre.CumX)) {
		//					if ((smallestZ.CumZ + cboxz) == smallestZ.Pre.CumZ) {
		//						smallestZ.Pre.CumX = smallestZ.CumX;
		//						smallestZ.Pre.Post = smallestZ.Post;
		//						smallestZ.Post.Pre = smallestZ.Pre;
		//					} else {
		//						smallestZ.CumZ = smallestZ.CumZ + cboxz;
		//					}
		//				} else {
		//					if ((smallestZ.CumZ + cboxz) == smallestZ.Pre.CumZ) {
		//						smallestZ.Pre.CumX = smallestZ.Pre.CumX + cboxx;
		//					} else if (smallestZ.CumZ + cboxz == smallestZ.Post.CumZ) {
		//						itemsToPack[cboxi].CoordX = smallestZ.CumX - cboxx;
		//						smallestZ.CumX = smallestZ.CumX - cboxx;
		//					} else {
		//						smallestZ.Pre.Post = new ScrapPad();

		//						smallestZ.Pre.Post.Pre = smallestZ.Pre;
		//						smallestZ.Pre.Post.Post = smallestZ;
		//						smallestZ.Pre = smallestZ.Pre.Post;
		//						smallestZ.Pre.CumX = smallestZ.Pre.Pre.CumX + cboxx;
		//						smallestZ.Pre.CumZ = smallestZ.CumZ + cboxz;
		//					}
		//				}
		//			}

		//			VolumeCheck();
		//		}
		//	}

		//	/// <summary>
		//	/// Using the parameters found, packs the best solution found and
		//	/// reports to the console.
		//	/// </summary>
		//	private void Report(Container container) {
		//		quit = false;

		//		switch (bestVariant) {
		//			case 1:
		//				px = container.Length;
		//				py = container.Height;
		//				pz = container.Width;
		//				break;

		//			case 2:
		//				px = container.Width;
		//				py = container.Height;
		//				pz = container.Length;
		//				break;

		//			case 3:
		//				px = container.Width;
		//				py = container.Length;
		//				pz = container.Height;
		//				break;

		//			case 4:
		//				px = container.Height;
		//				py = container.Length;
		//				pz = container.Width;
		//				break;

		//			case 5:
		//				px = container.Length;
		//				py = container.Width;
		//				pz = container.Height;
		//				break;

		//			case 6:
		//				px = container.Height;
		//				py = container.Width;
		//				pz = container.Length;
		//				break;
		//		}

		//		packingBest = true;

		//		//Print("BEST SOLUTION FOUND AT ITERATION                      :", bestIteration, "OF VARIANT", bestVariant);
		//		//Print("TOTAL ITEMS TO PACK                                   :", itemsToPackCount);
		//		//Print("TOTAL VOLUME OF ALL ITEMS                             :", totalItemVolume);
		//		//Print("WHILE CONTAINER ORIENTATION X - Y - Z                 :", px, py, pz);

		//		layers.Clear();
		//		layers.Add(new Layer { LayerEval = -1 });
		//		ListCanditLayers();
		//		layers = layers.OrderBy(l => l.LayerEval).ToList();
		//		packedVolume = 0;
		//		packedy = 0;
		//		packing = true;
		//		layerThickness = layers[bestIteration].LayerDim;
		//		remainpy = py;
		//		remainpz = pz;

		//		for (x = 1; x <= itemsToPackCount; x++) {
		//			itemsToPack[x].IsPacked = false;
		//		}

		//		do {
		//			layerinlayer = 0;
		//			layerDone = false;
		//			PackLayer();
		//			packedy = packedy + layerThickness;
		//			remainpy = py - packedy;

		//			if (layerinlayer > 0.0001M) {
		//				prepackedy = packedy;
		//				preremainpy = remainpy;
		//				remainpy = layerThickness - prelayer;
		//				packedy = packedy - layerThickness + prelayer;
		//				remainpz = lilz;
		//				layerThickness = layerinlayer;
		//				layerDone = false;
		//				PackLayer();
		//				packedy = prepackedy;
		//				remainpy = preremainpy;
		//				remainpz = pz;
		//			}

		//			if (!quit) {
		//				FindLayer(remainpy);
		//			}
		//		} while (packing && !quit);
		//	}

		//	/// <summary>
		//	/// After packing of each item, the 100% packing condition is checked.
		//	/// </summary>
		//	private void VolumeCheck() {
		//		itemsToPack[cboxi].IsPacked = true;
		//		itemsToPack[cboxi].PackDimX = cboxx;
		//		itemsToPack[cboxi].PackDimY = cboxy;
		//		itemsToPack[cboxi].PackDimZ = cboxz;
		//		packedVolume = packedVolume + itemsToPack[cboxi].Volume;
		//		packedItemCount++;

		//		if (packingBest) {
		//			OutputBoxList();
		//		} else if (packedVolume == totalContainerVolume || packedVolume == totalItemVolume) {
		//			packing = false;
		//			hundredPercentPacked = true;
		//		}
		//	}

		//	#endregion Private Methods

		//	#region Private Classes

		//	/// <summary>
		//	/// A list that stores all the different lengths of all item dimensions.
		//	/// From the master's thesis:
		//	/// "Each Layerdim value in this array represents a different layer thickness
		//	/// value with which each iteration can start packing. Before starting iterations,
		//	/// all different lengths of all box dimensions along with evaluation values are
		//	/// stored in this array" (p. 3-6).
		//	/// </summary>
		//	private class Layer {
		//		/// <summary>
		//		/// Gets or sets the layer dimension value, representing a layer thickness.
		//		/// </summary>
		//		/// <value>
		//		/// The layer dimension value.
		//		/// </value>
		//		public decimal LayerDim { get; set; }

		//		/// <summary>
		//		/// Gets or sets the layer eval value, representing an evaluation weight
		//		/// value for the corresponding LayerDim value.
		//		/// </summary>
		//		/// <value>
		//		/// The layer eval value.
		//		/// </value>
		//		public decimal LayerEval { get; set; }
		//	}

		//	/// <summary>
		//	/// From the master's thesis:
		//	/// "The double linked list we use keeps the topology of the edge of the 
		//	/// current layer under construction. We keep the x and z coordinates of 
		//	/// each gap's right corner. The program looks at those gaps and tries to 
		//	/// fill them with boxes one at a time while trying to keep the edge of the
		//	/// layer even" (p. 3-7).
		//	/// </summary>
		//	private class ScrapPad {
		//		/// <summary>
		//		/// Gets or sets the x coordinate of the gap's right corner.
		//		/// </summary>
		//		/// <value>
		//		/// The x coordinate of the gap's right corner.
		//		/// </value>
		//		public decimal CumX { get; set; }

		//		/// <summary>
		//		/// Gets or sets the z coordinate of the gap's right corner.
		//		/// </summary>
		//		/// <value>
		//		/// The z coordinate of the gap's right corner.
		//		/// </value>
		//		public decimal CumZ { get; set; }

		//		/// <summary>
		//		/// Gets or sets the following entry.
		//		/// </summary>
		//		/// <value>
		//		/// The following entry.
		//		/// </value>
		//		public ScrapPad Post { get; set; }

		//		/// <summary>
		//		/// Gets or sets the previous entry.
		//		/// </summary>
		//		/// <value>
		//		/// The previous entry.
		//		/// </value>
		//		public ScrapPad Pre { get; set; }
		//	}

		//	#endregion Private Classes
		//}
		///// <summary>
		///// The container packing service.
		///// </summary>
		//public static class PackingService {
		//	/// <summary>
		//	/// Attempts to pack the specified containers with the specified items using the specified algorithms.
		//	/// </summary>
		//	/// <param name="containers">The list of containers to pack.</param>
		//	/// <param name="itemsToPack">The items to pack.</param>
		//	/// <param name="algorithmTypeIDs">The list of algorithm type IDs to use for packing.</param>
		//	/// <returns>A container packing result with lists of the packed and unpacked items.</returns>
		//	public static List<ContainerPackingResult> Pack(List<Container> containers, List<Item> itemsToPack, IEnumerable<int> algorithmTypeIDs) {
		//		Object sync = new Object { };
		//		List<ContainerPackingResult> result = new List<ContainerPackingResult>();

		//		Parallel.ForEach(containers, container => {
		//			ContainerPackingResult containerPackingResult = new ContainerPackingResult();
		//			containerPackingResult.ContainerID = container.ID;

		//			Parallel.ForEach(algorithmTypeIDs, algorithmTypeID => {
		//				IPackingAlgorithm algorithm = PackingService.GetPackingAlgorithmFromTypeID(algorithmTypeID);

		//				// Until I rewrite the algorithm with no side effects, we need to clone the item list
		//				// so the parallel updates don't interfere with each other.
		//				List<Item> items = new List<Item>();

		//				itemsToPack.ForEach(item => {
		//					items.Add(new Item(item.ID, item.Dim1, item.Dim2, item.Dim3, item.Quantity));
		//				});

		//				Stopwatch stopwatch = new Stopwatch();
		//				stopwatch.Start();
		//				AlgorithmPackingResult algorithmResult = algorithm.Run(container, items);
		//				stopwatch.Stop();

		//				algorithmResult.PackTimeInMilliseconds = stopwatch.ElapsedMilliseconds;

		//				decimal containerVolume = container.Length * container.Width * container.Height;
		//				decimal itemVolumePacked = algorithmResult.PackedItems.Sum(i => i.Volume);
		//				decimal itemVolumeUnpacked = algorithmResult.UnpackedItems.Sum(i => i.Volume);

		//				algorithmResult.PercentContainerVolumePacked = Math.Round(itemVolumePacked / containerVolume * 100, 2);
		//				algorithmResult.PercentItemVolumePacked = Math.Round(itemVolumePacked / (itemVolumePacked + itemVolumeUnpacked) * 100, 2);

		//				lock (sync) {
		//					containerPackingResult.AlgorithmPackingResults.Add(algorithmResult);
		//				}
		//			});

		//			containerPackingResult.AlgorithmPackingResults = containerPackingResult.AlgorithmPackingResults.OrderBy(r => r.AlgorithmName).ToList();

		//			lock (sync) {
		//				result.Add(containerPackingResult);
		//			}
		//		});

		//		return result;
		//	}

		//	/// <summary>
		//	/// Gets the packing algorithm from the specified algorithm type ID.
		//	/// </summary>
		//	/// <param name="algorithmTypeID">The algorithm type ID.</param>
		//	/// <returns>An instance of a packing algorithm implementing AlgorithmBase.</returns>
		//	/// <exception cref="System.Exception">Invalid algorithm type.</exception>
		//	public static IPackingAlgorithm GetPackingAlgorithmFromTypeID(int algorithmTypeID) {
		//		switch (algorithmTypeID) {
		//			case (int)AlgorithmType.EB_AFIT:
		//				return new EB_AFIT();

		//			default:
		//				throw new Exception("Invalid algorithm type.");
		//		}
		//	}
		//}
	}
