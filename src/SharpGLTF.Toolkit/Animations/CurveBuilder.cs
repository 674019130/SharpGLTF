﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Represents an editable curve of <typeparamref name="T"/> elements.
    /// </summary>
    /// <typeparam name="T">An element of the curve.</typeparam>
    public abstract class CurveBuilder<T>
        : ICurveSampler<T>,
        IConvertibleCurve<T>
    {
        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        internal SortedDictionary<float, _CurveNode<T>> _Keys = new SortedDictionary<float, _CurveNode<T>>();

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public IReadOnlyCollection<float> Keys => _Keys.Keys;

        public int MaxDegree => _Keys.Count == 0 ? 0 : _Keys.Values.Max(item => item.Degree);

        #endregion

        #region abstract API

        protected abstract bool CheckValue(T value);

        protected abstract T CreateValue(params float[] values);

        public abstract T GetPoint(float offset);

        protected abstract T GetTangent(T fromValue, T toValue);

        #endregion

        #region API

        public void RemoveKey(float offset) { _Keys.Remove(offset); }

        public void SetPoint(float offset, T value, bool isLinear = true)
        {
            Guard.IsTrue(CheckValue(value), nameof(value));

            _Keys[offset] = new _CurveNode<T>(value, isLinear);
        }

        /// <summary>
        /// Sets the incoming tangent to an existing point.
        /// </summary>
        /// <param name="offset">The offset of the existing point.</param>
        /// <param name="tangent">The tangent value.</param>
        public void SetIncomingTangent(float offset, T tangent)
        {
            Guard.IsTrue(_Keys.ContainsKey(offset), nameof(offset));
            Guard.IsTrue(CheckValue(tangent), nameof(tangent));

            offset -= float.Epsilon;

            var offsets = SamplerFactory.FindPairContainingOffset(_Keys.Keys, offset);

            var a = _Keys[offsets.Item1];
            var b = _Keys[offsets.Item2];

            if (a.Degree == 1) a.OutgoingTangent = GetTangent(a.Point, b.Point);

            a.Degree = 3;
            b.IncomingTangent = tangent;

            _Keys[offsets.Item1] = a;
            _Keys[offsets.Item2] = b;
        }

        /// <summary>
        /// Sets the outgoing tangent to an existing point.
        /// </summary>
        /// <param name="offset">The offset of the existing point.</param>
        /// <param name="tangent">The tangent value.</param>
        public void SetOutgoingTangent(float offset, T tangent)
        {
            Guard.IsTrue(_Keys.ContainsKey(offset), nameof(offset));
            Guard.IsTrue(CheckValue(tangent), nameof(tangent));

            var offsets = SamplerFactory.FindPairContainingOffset(_Keys.Keys, offset);

            var a = _Keys[offsets.Item1];
            var b = _Keys[offsets.Item2];

            if (offsets.Item1 != offsets.Item2)
            {
                if (a.Degree == 1) b.IncomingTangent = GetTangent(a.Point, b.Point);
                _Keys[offsets.Item2] = b;
            }

            a.Degree = 3;
            a.OutgoingTangent = tangent;

            _Keys[offsets.Item1] = a;
        }

        private protected (_CurveNode<T>, _CurveNode<T>, float) FindSample(float offset)
        {
            if (_Keys.Count == 0) return (default(_CurveNode<T>), default(_CurveNode<T>), 0);

            var offsets = SamplerFactory.FindPairContainingOffset(_Keys.Keys, offset);

            return (_Keys[offsets.Item1], _Keys[offsets.Item2], offsets.Item3);
        }

        #endregion

        #region With* API

        public CurveBuilder<T> WithPoint(float offset, T value, bool isLinear = true)
        {
            SetPoint(offset, value, isLinear);
            return this;
        }

        public CurveBuilder<T> WithIncomingTangent(float offset, T tangent)
        {
            SetIncomingTangent(offset, tangent);
            return this;
        }

        public CurveBuilder<T> WithOutgoingTangent(float offset, T tangent)
        {
            SetOutgoingTangent(offset, tangent);
            return this;
        }

        public CurveBuilder<T> WithPoint(float offset, params float[] values)
        {
            return WithPoint(offset, CreateValue(values));
        }

        public CurveBuilder<T> WithOutgoingTangent(float offset, params float[] values)
        {
            return WithOutgoingTangent(offset, CreateValue(values));
        }

        public CurveBuilder<T> WithIncomingTangent(float offset, params float[] values)
        {
            return WithIncomingTangent(offset, CreateValue(values));
        }

        #endregion

        #region IConvertibleCurve API

        IReadOnlyDictionary<float, T> IConvertibleCurve<T>.ToStepCurve()
        {
            if (MaxDegree != 0) throw new NotSupportedException();

            return _Keys.ToDictionary(item => item.Key, item => item.Value.Point);
        }

        IReadOnlyDictionary<float, T> IConvertibleCurve<T>.ToLinearCurve()
        {
            var d = new Dictionary<float, T>();

            var orderedKeys = _Keys.Keys.ToList();

            for (int i = 0; i < orderedKeys.Count - 1; ++i)
            {
                var a = orderedKeys[i + 0];
                var b = orderedKeys[i + 1];

                var sa = _Keys[a];
                var sb = _Keys[b];

                switch (sa.Degree)
                {
                    case 0: // simulate a step with an extra key
                        d[a] = sa.Point;
                        d[b - float.Epsilon] = sa.Point;
                        d[b] = sb.Point;
                        break;

                    case 1:
                        d[a] = sa.Point;
                        d[b] = sb.Point;
                        break;

                    case 3:
                        var t = a;
                        while (t < b)
                        {
                            d[t] = this.GetPoint(t);
                            t += 1.0f / 30.0f;
                        }

                        break;

                    default: throw new NotImplementedException();
                }
            }

            return d;
        }

        IReadOnlyDictionary<float, (T, T, T)> IConvertibleCurve<T>.ToSplineCurve()
        {
            var d = new Dictionary<float, (T, T, T)>();

            var orderedKeys = _Keys.Keys.ToList();

            for (int i = 0; i < orderedKeys.Count - 1; ++i)
            {
                var a = orderedKeys[i + 0];
                var b = orderedKeys[i + 1];

                var sa = _Keys[a];
                var sb = _Keys[b];

                if (!d.TryGetValue(a, out (T, T, T) da)) da = default;
                if (!d.TryGetValue(b, out (T, T, T) db)) db = default;

                da.Item2 = sa.Point;
                db.Item2 = sb.Point;

                var delta = GetTangent(da.Item2, db.Item2);

                switch (sa.Degree)
                {
                    case 0: // simulate a step with an extra key
                        da.Item3 = default;
                        d[b - float.Epsilon] = (default, sa.Point, delta);
                        db.Item1 = delta;
                        break;

                    case 1: // tangents are the delta between points
                        da.Item3 = db.Item1 = delta;
                        break;

                    case 3: // actual tangents
                        da.Item3 = sa.OutgoingTangent;
                        db.Item1 = sb.IncomingTangent;
                        break;

                    default: throw new NotImplementedException();
                }

                d[a] = da;
                d[b] = db;
            }

            return d;
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{IncomingTangent} -> {Point}[{Degree}] -> {OutgoingTangent}")]
    struct _CurveNode<T>
    {
        public _CurveNode(T value, bool isLinear)
        {
            IncomingTangent = default;
            Point = value;
            OutgoingTangent = default;
            Degree = isLinear ? 1 : 0;
        }

        public _CurveNode(T incoming, T value, T outgoing)
        {
            IncomingTangent = incoming;
            Point = value;
            OutgoingTangent = outgoing;
            Degree = 3;
        }

        public T IncomingTangent;
        public T Point;
        public T OutgoingTangent;
        public int Degree;
    }
}
