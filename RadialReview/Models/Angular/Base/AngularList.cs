using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Base
{
    public enum AngularListType {
        ReplaceAll,
        Add,
        ReplaceIfNewer,
        Remove,
    }

	public class AngularList
	{
		public static BaseAngularList<T> Create<T>(AngularListType updateMethod, IEnumerable<T> list)
		{
			return new BaseAngularList<T>(updateMethod, list);
		}

        public static BaseAngularList<T> CreateFrom<T>(AngularListType updateMethod, T item)
        {
            return Create(updateMethod, new[] { item });
        }
	}


	public interface IAngularList
	{
		AngularListType UpdateMethod { get; }
	}

	public class BaseAngularList<T> : IEnumerable<T>, IEnumerable, IAngularList
	{
		public List<T> AngularList { get; protected set; }
		public AngularListType UpdateMethod { get; set; }

		public BaseAngularList(AngularListType updateMethod, IEnumerable<T> list)
		{
			AngularList = list.ToList();
			UpdateMethod = updateMethod;
		} 

		public IEnumerator<T> GetEnumerator(){
			return AngularList.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator(){
			return AngularList.GetEnumerator();
		}
	}
}