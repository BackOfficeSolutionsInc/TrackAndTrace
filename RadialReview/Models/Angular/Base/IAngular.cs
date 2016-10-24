﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Models.Angular;

namespace RadialReview.Models.Angular.Base
{
	public interface IAngular{
		
	}
	public interface IAngularItem : IAngular
	{
        long Id { get; set; }
		string Type { get; }
		bool Hide { get; }
	}

	public interface IAngularUpdate : IAngular
	{
	}

	public static class IAngularExtensions
	{
		public static string GetKey(this IAngularItem self){	return self.Type + "_" + self.Id;	}
	}

	public interface IAngularIgnore {

	}

	public static class AngularIgnore {
		public static AngularIgnore<T> Create<T>(T item) {
			return new AngularIgnore<T>(item);
		}
	}

	public class AngularIgnore<T> : IAngularIgnore {
		public T Item { get; set; }
		public AngularIgnore(T item) {
			Item = item;
		}
	}
}