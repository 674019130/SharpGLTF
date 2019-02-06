﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace glTF2Sharp.Debug
{
    /// <summary>
    /// class to visualize collection items during debug
    /// </summary>
    internal sealed class _CollectionDebugView<T>
    {
        // https://referencesource.microsoft.com/#mscorlib/system/collections/generic/debugview.cs,29

        public _CollectionDebugView(ICollection<T> collection)
        {
            _Collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        private readonly ICollection<T> _Collection;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] items = new T[_Collection.Count];
                _Collection.CopyTo(items, 0);
                return items;
            }
        }
    }

    [System.Diagnostics.DebuggerDisplay("BufferView[{_Value.LogicalIndex}] {_Value.Name} {_Value._target} Bytes:{_Value.Buffer1.Count}")]
    internal sealed class _BufferDebugView
    {
        public _BufferDebugView(Schema2.BufferView value) { _Value = value; }

        public int LogicalIndex => _Value.LogicalParent.LogicalBufferViews.IndexOfReference(_Value);

        private readonly Schema2.BufferView _Value;

        public int ByteStride => _Value.ByteStride;

        public int ByteLength => _Value.Data.Count;

        public Schema2.BufferMode? DeviceBufferTarget => _Value.DeviceBufferTarget;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public Schema2.Accessor[] Accessors => _Value.Accessors.ToArray();
    }

    [System.Diagnostics.DebuggerDisplay(" {_Value._target} Bytes:{_Value.Buffer1.Count}")]
    internal sealed class _MemoryAccessorDebugView<T>
        where T:unmanaged
    {
        public _MemoryAccessorDebugView(Memory.IAccessor<T> value) { _Value = value; }        

        private readonly Memory.IAccessor<T> _Value;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public T[] Accessors => _Value.ToArray();
    }
}
