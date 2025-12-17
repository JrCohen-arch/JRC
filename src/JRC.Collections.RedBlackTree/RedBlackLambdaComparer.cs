// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using System;
using System.Collections.Generic;

namespace JRC.Collections.RedBlackTree
{
    internal sealed class RedBlackLambdaComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;

        public RedBlackLambdaComparer(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }

        public int Compare(T x, T y)
        {
            return comparison(x, y);
        }
    }
}
