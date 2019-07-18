﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Utility class to create samplers from curve collections.
    /// </summary>
    public static class SamplerFactory
    {
        #region sampler utils

        public static Vector3 CreateTangent(Vector3 fromValue, Vector3 toValue, Single scale = 1)
        {
            return (toValue - fromValue) * scale;
        }

        public static Quaternion CreateTangent(Quaternion fromValue, Quaternion toValue, Single scale = 1)
        {
            var tangent = Quaternion.Concatenate(toValue, Quaternion.Inverse(fromValue));

            if (scale == 1) return tangent;

            // decompose into Axis - Angle pair
            var axis = Vector3.Normalize(new Vector3(tangent.X, tangent.Y, tangent.Z));
            var angle = Math.Acos(tangent.W) * 2;

            return Quaternion.CreateFromAxisAngle(axis, scale * (float)angle);
        }

        public static Single[] CreateTangent(Single[] fromValue, Single[] toValue, Single scale = 1)
        {
            var r = new float[fromValue.Length];

            for (int i = 0; i < r.Length; ++i)
            {
                r[i] = (toValue[i] - fromValue[i]) * scale;
            }

            return r;
        }

        /// <summary>
        /// Calculates the Hermite point weights for a given <paramref name="amount"/>
        /// </summary>
        /// <param name="amount">The input amount (must be between 0 and 1)</param>
        /// <returns>
        /// The output weights.
        /// - Item1: Weight for Start point
        /// - Item2: Weight for End point
        /// - Item3: Weight for Start Outgoing Tangent
        /// - Item4: Weight for End Incoming Tangent
        /// </returns>
        public static (float, float, float, float) CreateHermitePointWeights(float amount)
        {
            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1, nameof(amount));

            // http://mathworld.wolfram.com/HermitePolynomial.html

            // https://www.cubic.org/docs/hermite.htm

            var squared = amount * amount;
            var cubed = amount * squared;

            /*
            var part1 = (2.0f * cubed) - (3.0f * squared) + 1.0f;
            var part2 = (-2.0f * cubed) + (3.0f * squared);
            var part3 = cubed - (2.0f * squared) + amount;
            var part4 = cubed - squared;
            */

            var part2 = (3.0f * squared) - (2.0f * cubed);
            var part1 = 1 - part2;
            var part4 = cubed - squared;
            var part3 = part4 - squared + amount;

            return (part1, part2, part3, part4);
        }

        /// <summary>
        /// Calculates the Hermite tangent weights for a given <paramref name="amount"/>
        /// </summary>
        /// <param name="amount">The input amount (must be between 0 and 1)</param>
        /// <returns>
        /// The output weights.
        /// - Item1: Weight for Start point
        /// - Item2: Weight for End point
        /// - Item3: Weight for Start Outgoing Tangent
        /// - Item4: Weight for End Incoming Tangent
        /// </returns>
        public static (float, float, float, float) CreateHermiteTangentWeights(float amount)
        {
            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1, nameof(amount));

            // https://math.stackexchange.com/questions/1270776/how-to-find-tangent-at-any-point-along-a-cubic-hermite-spline

            var squared = amount * amount;

            /*
            var part1 = (6 * squared) - (6 * amount);
            var part2 = -(6 * squared) + (6 * amount);
            var part3 = (3 * squared) - (4 * amount) + 1;
            var part4 = (3 * squared) - (2 * amount);
            */

            var part1 = (6 * squared) - (6 * amount);
            var part2 = -part1;
            var part3 = (3 * squared) - (4 * amount) + 1;
            var part4 = (3 * squared) - (2 * amount);

            return (part1, part2, part3, part4);
        }

        /// <summary>
        /// Given a <paramref name="sequence"/> of float+<typeparamref name="T"/> pairs and an <paramref name="offset"/>,
        /// it finds two consecutive values that contain <paramref name="offset"/> between them.
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="sequence">A sequence of float+<typeparamref name="T"/> pairs sorted in ascending order.</param>
        /// <param name="offset">the offset to look for in the sequence.</param>
        /// <returns>Two consecutive <typeparamref name="T"/> values and a float amount to LERP amount.</returns>
        public static (T, T, float) FindPairContainingOffset<T>(this IEnumerable<(float, T)> sequence, float offset)
        {
            if (!sequence.Any()) return (default(T), default(T), 0);

            (float, T)? left = null;
            (float, T)? right = null;
            (float, T)? prev = null;

            var first = sequence.First();
            if (offset < first.Item1) offset = first.Item1;

            foreach (var item in sequence)
            {
                System.Diagnostics.Debug.Assert(!prev.HasValue || prev.Value.Item1 < item.Item1, "Values in the sequence must be sorted ascending.");

                if (item.Item1 == offset)
                {
                    left = item; continue;
                }

                if (item.Item1 > offset)
                {
                    if (left == null) left = prev;
                    right = item;
                    break;
                }

                prev = item;
            }

            if (left == null && right == null) return (default(T), default(T), 0);
            if (left == null) return (right.Value.Item2, right.Value.Item2, 0);
            if (right == null) return (left.Value.Item2, left.Value.Item2, 0);

            var delta = right.Value.Item1 - left.Value.Item1;

            System.Diagnostics.Debug.Assert(delta > 0);

            var amount = (offset - left.Value.Item1) / delta;

            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1);

            return (left.Value.Item2, right.Value.Item2, amount);
        }

        /// <summary>
        /// Given a <paramref name="sequence"/> of offsets and an <paramref name="offset"/>,
        /// it finds two consecutive offsets that contain <paramref name="offset"/> between them.
        /// </summary>
        /// <param name="sequence">A sequence of offsets sorted in ascending order.</param>
        /// <param name="offset">the offset to look for in the sequence.</param>
        /// <returns>Two consecutive offsets and a LERP amount.</returns>
        public static (float, float, float) FindPairContainingOffset(IEnumerable<float> sequence, float offset)
        {
            if (!sequence.Any()) return (0, 0, 0);

            float? left = null;
            float? right = null;
            float? prev = null;

            var first = sequence.First();
            if (offset < first) offset = first;

            foreach (var item in sequence)
            {
                System.Diagnostics.Debug.Assert(!prev.HasValue || prev.Value < item, "Values in the sequence must be sorted ascending.");

                if (item == offset)
                {
                    left = item; continue;
                }

                if (item > offset)
                {
                    if (left == null) left = prev;
                    right = item;
                    break;
                }

                prev = item;
            }

            if (left == null && right == null) return (0, 0, 0);
            if (left == null) return (right.Value, right.Value, 0);
            if (right == null) return (left.Value, left.Value, 0);

            var delta = right.Value - left.Value;

            System.Diagnostics.Debug.Assert(delta > 0);

            var amount = (offset - left.Value) / delta;

            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1);

            return (left.Value, right.Value, amount);
        }

        #endregion

        #region interpolation utils

        public static Single[] Lerp(Single[] start, Single[] end, Single amount)
        {
            var startW = 1 - amount;
            var endW = amount;

            var result = new float[start.Length];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (start[i] * startW) + (end[i] * endW);
            }

            return result;
        }

        public static Vector3 CubicLerp(Vector3 start, Vector3 outgoingTangent, Vector3 end, Vector3 incomingTangent, Single amount)
        {
            var hermite = SamplerFactory.CreateHermitePointWeights(amount);

            return (start * hermite.Item1) + (end * hermite.Item2) + (outgoingTangent * hermite.Item3) + (incomingTangent * hermite.Item4);
        }

        public static Quaternion CubicLerp(Quaternion start, Quaternion outgoingTangent, Quaternion end, Quaternion incomingTangent, Single amount)
        {
            var hermite = CreateHermitePointWeights(amount);

            return Quaternion.Normalize((start * hermite.Item1) + (end * hermite.Item2) + (outgoingTangent * hermite.Item3) + (incomingTangent * hermite.Item4));
        }

        public static Single[] CubicLerp(Single[] start, Single[] outgoingTangent, Single[] end, Single[] incomingTangent, Single amount)
        {
            var hermite = CreateHermitePointWeights(amount);

            var result = new float[start.Length];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (start[i] * hermite.Item1) + (end[i] * hermite.Item2) + (outgoingTangent[i] * hermite.Item3) + (incomingTangent[i] * hermite.Item4);
            }

            return result;
        }

        #endregion

        #region sampler creation

        public static ICurveSampler<Vector3> CreateSampler(this IEnumerable<(Single, Vector3)> collection, bool isLinear = true)
        {
            if (collection == null) return null;

            return new Vector3LinearSampler(collection, isLinear);
        }

        public static ICurveSampler<Quaternion> CreateSampler(this IEnumerable<(Single, Quaternion)> collection, bool isLinear = true)
        {
            if (collection == null) return null;

            return new QuaternionLinearSampler(collection, isLinear);
        }

        public static ICurveSampler<Single[]> CreateSampler(this IEnumerable<(Single, Single[])> collection, bool isLinear = true)
        {
            if (collection == null) return null;

            return new ArrayLinearSampler(collection, isLinear);
        }

        public static ICurveSampler<Vector3> CreateSampler(this IEnumerable<(Single, (Vector3, Vector3, Vector3))> collection)
        {
            if (collection == null) return null;

            return new Vector3CubicSampler(collection);
        }

        public static ICurveSampler<Quaternion> CreateSampler(this IEnumerable<(Single, (Quaternion, Quaternion, Quaternion))> collection)
        {
            if (collection == null) return null;

            return new QuaternionCubicSampler(collection);
        }

        public static ICurveSampler<Single[]> CreateSampler(this IEnumerable<(Single, (Single[], Single[], Single[]))> collection)
        {
            if (collection == null) return null;

            return new ArrayCubicSampler(collection);
        }

        #endregion
    }
}
