using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
    public interface ILazyCollection : ICollection{
        bool IsResolved();
    }

    public class LazyCollection<T> : ILazyCollection {
        private ICollection Resolved {
            get {
                if (!IsResolved())
                    _Resolved = Backing.ToList();
                return _Resolved;
            }
        }
        private ICollection _Resolved;
        private IEnumerable<T> Backing;

        public bool IsResolved() {
            return _Resolved != null;
        }

        public LazyCollection(IEnumerable<T> backing) {
            Backing = backing;
        }

        public int Count { get { return Resolved.Count; } }
        public object SyncRoot { get { return Resolved.SyncRoot; } }
        public bool IsSynchronized { get { return Resolved.IsSynchronized; } }

        public void CopyTo(Array array, int index) {
            Resolved.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator() {
            if (!IsResolved())
                return Backing.GetEnumerator();
            return Resolved.GetEnumerator();
        }
    }
}