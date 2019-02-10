﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace glTF2Sharp.Memory
{
    /// <summary>
    /// Special accessor to wrap over a base accessor and a sparse accessor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Diagnostics.DebuggerDisplay("Sparse {typeof(T).Name} Accessor {Count}")]
    public struct SparseAccessor<T> : IAccessor<T>
        where T : unmanaged
    {
        #region lifecycle

        public SparseAccessor(IAccessor<T> bottom, IAccessor<T> top, IntegerAccessor topMapping)
        {
            _BottomItems = bottom;
            _TopItems = top;
            _Mapping = new Dictionary<int, int>();

            for (int val = 0; val < topMapping.Count; ++val)
            {
                var key = (int)topMapping[val];
                _Mapping[key] = val;
            }
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IAccessor<T> _BottomItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IAccessor<T> _TopItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Dictionary<int, int> _Mapping;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private T[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _BottomItems.Count;

        public T this[int index]
        {
            get => _Mapping.TryGetValue(index, out int topIndex) ? _TopItems[topIndex] : _BottomItems[index];
            set
            {
                if (_Mapping.TryGetValue(index, out int topIndex)) _TopItems[topIndex] = value;
            }
        }        

        public void CopyTo(ArraySegment<T> dst) { AccessorsUtils.Copy(this, dst); }

        public (T, T) GetBounds()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator() { return new AccessorEnumerator<T>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<T>(this); }

        #endregion
    }
}
