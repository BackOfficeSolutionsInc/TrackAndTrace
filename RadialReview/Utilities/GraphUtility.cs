using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
	public class GraphUtility
	{

		public class Node {
			public long Id { get; set; }
			public long? ParentId { get; set; }
		}

		/// <summary>
		/// https://en.wikipedia.org/wiki/Topological_sorting
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static bool HasCircularDependency<T>(List<T> nodes,Func<T,long> idFunc, Func<T,long?> parentIdFunc)
		{
			var nodeIds = nodes.Select(x => idFunc(x)).Distinct().ToList();
			var edges = nodes.Where(x => parentIdFunc(x).HasValue).Select(x => Tuple.Create(idFunc(x), parentIdFunc(x).Value)).ToList();
			return TopologicalSort(new HashSet<long>(nodeIds), new HashSet<Tuple<long,long>>(edges)) == null;
		}


		public static bool HasCircularDependency(List<Node> nodes){
			return HasCircularDependency(nodes, x => x.Id, x => x.ParentId);
		}

		/// <summary>
		/// https://gist.github.com/Sup3rc4l1fr4g1l1571c3xp14l1d0c10u5/3341dba6a53d7171fe3397d13d00ee3f/
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="nodes"></param>
		/// <param name="edges"></param>
		/// <returns></returns>
		public static List<T> TopologicalSort<T>(HashSet<T> nodes, HashSet<Tuple<T, T>> edges) where T : IEquatable<T>
		{
			// Empty list that will contain the sorted elements
			var L = new List<T>();

			// Set of all nodes with no incoming edges
			var S = new HashSet<T>(nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false)));

			// while S is non-empty do
			while (S.Any())
			{

				//  remove a node n from S
				var n = S.First();
				S.Remove(n);

				// add n to tail of L
				L.Add(n);
				// for each node m with an edge e from n to m do
				foreach (var e in edges.Where(e => e.Item1.Equals(n)).ToList())
				{
					var m = e.Item2;
					// remove edge e from the graph
					edges.Remove(e);

					// if m has no other incoming edges then
					if (edges.All(me => me.Item2.Equals(m) == false))
					{
						// insert m into S
						S.Add(m);
					}
				}
			}

			// if graph has edges then
			if (edges.Any())
			{
				// return error (graph has at least one cycle)
				return null;
			}
			else
			{
				// return L (a topologically sorted order)
				return L;
			}
		}
	}
}