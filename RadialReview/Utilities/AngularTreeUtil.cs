using RadialReview.Models.Angular;
using RadialReview.Models.Angular.Accountability;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities {
	public class AngularTreeUtil {

		public static List<AngularAccountabilityNode> GetAllNodes(AngularAccountabilityNode root) {
			return DiveAllNodes(root);
		}

		private static List<AngularAccountabilityNode> DiveAllNodes(AngularAccountabilityNode current) {
			var output = new List<AngularAccountabilityNode>();
			if (current == null)
				return output;
			var children = current.GetDirectChildren();
			output.AddRange(children);
			foreach (var child in children) {
				var found = DiveAllNodes(child);
				output.AddRange(found);
			}
			return output;
		}

		public static List<AngularAccountabilityNode> FindUsersNodes(AngularAccountabilityNode root, long userId) {
			return DiveFindAllUsersNodes(root, userId);
		}

		public static T FindNode<T>(T root, long nodeId) where T : AngularTreeNode<T> {
			return DiveFindNode(root, nodeId);
		}
		public static List<T> GetDirectChildren<T>(T root, long nodeId) where T : AngularTreeNode<T> {
			return DiveDirectChildren(root, nodeId) ?? new List<T>();
		}

		public static T GetDirectParent<T>(T root, long nodeId) where T : AngularTreeNode<T> {
			return DiveDirectParent(root, nodeId, null);
		}

		public static List<T> GetDirectPeers<T>(T root, long nodeId) where T : AngularTreeNode<T> {
			var node=FindNode(root, nodeId);
			if (node.GetDirectChildren().Any()) {
				return new List<T>();
			}
			var parent = GetDirectParent(root, nodeId);
			if (parent == null)
				return new List<T>();
			//Everyone except Self and people with direct reports
			return parent.GetDirectChildren()
						.Where(x => x.Id != nodeId && !x.GetDirectChildren().Any())
						.ToList();
		}

		private static T DiveFindNode<T>(T current, long nodeId) where T : AngularTreeNode<T> {
			if (current == null)
				return null;
			if (current.Id == nodeId)
				return current;
			var children = current.GetDirectChildren();
			foreach (var child in children) {
				var found = DiveFindNode(child, nodeId);
				if (found != null)
					return found;
			}
			return null;
		}

		private static T DiveDirectParent<T>(T current, long nodeId, T parent) where T : AngularTreeNode<T> {
			if (current == null)
				return null;
			if (current.Id == nodeId)
				return parent;
			var children = current.GetDirectChildren();
			foreach (var child in children) {
				var found = DiveDirectParent(child, nodeId, current);
				if (found != null)
					return found;
			}
			return null;
		}

		private static List<T> DiveDirectChildren<T>(T current, long nodeId) where T : AngularTreeNode<T> {
			if (current == null)
				return null;
			var children = current.GetDirectChildren();
			if (current.Id == nodeId)
				return children;
			foreach (var child in children) {
				var found = DiveDirectChildren(child, nodeId);
				if (found != null)
					return found;
			}
			return null;
		}

		private static List<AngularAccountabilityNode> DiveFindAllUsersNodes(AngularAccountabilityNode current, long userId) {
			var output = new List<AngularAccountabilityNode>();
			if (current == null)
				return output;
			if (current.User!=null && current.User.Id == userId)
				output.Add(current);
			var children = current.GetDirectChildren();
			foreach (var child in children) {
				var found = DiveFindAllUsersNodes(child, userId);
				output.AddRange(found);
			}
			return output;
		}

	}
}