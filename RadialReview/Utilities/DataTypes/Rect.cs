using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {

    public enum RectType {
        Row,
        Column,
        Rectangle
    }
    public class Rect : IEnumerable<Tuple<int, int>> {
        private List<int> rect { get; set; }

        public int MinX { get { return rect[0]; } }
        public int MinY { get { return rect[1]; } }
        public int MaxX { get { return rect[2]; } }
        public int MaxY { get { return rect[3]; } }

        public Rect(IEnumerable<int> rect)
        {
            this.rect = rect.ToList();
        }

        public Rect(int x1, int y1, int x2, int y2)
        {
            var realMinx = Math.Min(x1, x2);
            var realMaxx = Math.Max(x1, x2);
            var realMiny = Math.Min(y1, y2);
            var realMaxy = Math.Max(y1, y2);

            rect = new List<int> { realMinx, realMiny, realMaxx, realMaxy };
        }

        public override string ToString()
        {
            return string.Join(",", rect);
        }

		public bool IsCell() {
			return (rect[0]==rect[2]) && (rect[1]==rect[3]);
		}

        public RectType GetRectType()
        {
			var col = rect[0].CompareTo(rect[2]) == 0;
			var row = rect[1].CompareTo(rect[3]) == 0;

			if (col && !row)
                return RectType.Column;
            else if (!col && row)
                return RectType.Row;
            return RectType.Rectangle;
        }
        public List<T> GetArray1D<T>(List<List<T>> rowData)
        {
            return GetArray1D(rowData, x => x);
        }
        public List<U> GetArray1D<T, U>(List<List<T>> rowData, Func<T, U> transform)
        {
            return GetArray1D(rowData, (x, c) => transform(x));
        }
        public List<U> GetArray1D<T,U>(List<List<T>> rowData, Func<T, Tuple<int, int>, U> transform){
            return GetArray(rowData,transform).SelectMany(c=>c).ToList();
        }

        public List<List<T>> GetArray<T>(List<List<T>> rowData)
        {
            return GetArray(rowData, x => x);
        }
        public List<List<U>> GetArray<T, U>(List<List<T>> rowData, Func<T, U> transform)
        {
            return GetArray(rowData, (x, c) => transform(x));
        }
        public List<List<U>> GetArray<T, U>(List<List<T>> rowData, Func<T, Tuple<int, int>, U> transform)
        {
            return GetCoordinates2D().Select(
                y => y.Select(x =>
                    transform(rowData[x.Item2][x.Item1], x)
                ).ToList()
            ).ToList();
        }

        public List<Tuple<int, int>> GetCoordinates1D()
        {
            //if (rect[0] == rect[2]) {
            //    return Enumerable.Range(rect[1], rect[3] - rect[1] + 1).Select(x => func(csv[x][rect[0]])).ToList();
            //} else if (rect[1] == rect[3]) {
            //    return Enumerable.Range(rect[0], rect[2] - rect[0] + 1).Select(x => func(csv[rect[1]][x])).ToList();
            //}
            var o = new List<Tuple<int, int>>();
            for (var j = rect[1]; j < rect[3]  + 1; j++) {
                for (var i = rect[0]; i < rect[2] + 1; i++) {
                    o.Add(Tuple.Create(i, j));
                }
            }
            return o;
        }


        /// <summary>
        ///  List<List<Tuple<x,y>>>
        ///  List<List<Tuple<col,row>>>
        ///  Y<X<Tuple>>
        ///  row<col<Tuple>>
        /// </summary>
        /// <returns></returns>
        public List<List<Tuple<int, int>>> GetCoordinates2D()
        {
            return GetCoordinates1D().GroupBy(x => x.Item2).Select(x => x.ToList()).ToList();
        }

        public RectType EnsureRowOrColumn()
        {
            var type = GetRectType();
            if (type == RectType.Rectangle)
                throw new ArgumentOutOfRangeException("rect", "Must be a row or column");
            return type;
        }
        
        public void EnsureSameRangeAs(Rect other)
        {
            var type =this.GetRectType();
            if (type == other.GetRectType()) {
                if (type == RectType.Column) {
                    if (rect[1] != other.rect[1] || rect[3] != other.rect[3])
                        throw new ArgumentOutOfRangeException("rect", "Must have the same row start and end");
                } else if (type == RectType.Row) {
                    if (rect[0] != other.rect[0] || rect[2] != other.rect[2])
                        throw new ArgumentOutOfRangeException("rect", "Must have the same column start and end");
                } else if (type == RectType.Rectangle) {
                    if (rect[0]-rect[1] != other.rect[0]-other.rect[1] || rect[2]-rect[3] != other.rect[2]-other.rect[3])
                        throw new ArgumentOutOfRangeException("rect", "Rectangles must have the same range");
                }

            } else {
                throw new ArgumentOutOfRangeException("rect", "Must both be rows, columns, or rectangles of the same range.");
            }
        }

        public IEnumerator<Tuple<int, int>> GetEnumerator()
        {
            return GetCoordinates1D().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetCoordinates1D().GetEnumerator();
        }
    }
}