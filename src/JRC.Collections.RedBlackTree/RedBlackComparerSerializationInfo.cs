// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace JRC.Collections.RedBlackTree
{
    /// <summary>
    /// Enum that indicates if a comparer is a known type.
    /// </summary>
    public enum RedBlackComparerKnownType
    {
        /// <summary>
        /// comparer type is unknown
        /// </summary>
        None,
        /// <summary>
        /// comparer is Comparer&lt;T>.Default
        /// </summary>
        Default,
        /// <summary>
        /// comparer is StringComparer.CurrentCulture
        /// </summary>
        StringCurrentCulture,
        /// <summary>
        /// comparer is StringComparer.CurrentCultureIgnoreCase
        /// </summary>
        StringCurrentCultureIgnoreCase,
        /// <summary>
        /// comparer is StringComparer.InvariantCulture
        /// </summary>
        StringInvariantCulture,
        /// <summary>
        /// comparer is StringComparer.InvariantCultureIgnoreCase
        /// </summary>
        StringInvariantCultureIgnoreCase,
        /// <summary>
        /// comparer is StringComparer.Ordinal
        /// </summary>
        StringOrdinal,
        /// <summary>
        /// comparer is StringComparer.OrdinalIgnoreCase
        /// </summary>
        StringOrdinalIgnoreCase,
        /// <summary>
        /// comparer is <see cref="RedBlackTreeSetBase{K}.HashCodeComparer"/>
        /// </summary>
        HashCode
    }

    /// <summary>
    /// Provides information about comparer that can help for serialization
    /// </summary>
    public class RedBlackComparerSerializationInfo<T> : RedBlackTypeSerializationInfo
    {
        private static Type StringType = typeof(string);

        private readonly RedBlackComparerKnownType knownType;
      
        /// <summary>
        /// initialize a new instance of RedBlackComparerSerializationInfo from comparer
        /// </summary>        
        public RedBlackComparerSerializationInfo(IComparer<T> comparer) : base(comparer)
        {
            this.knownType = RedBlackComparerKnownType.None;
            if (object.Equals(Comparer<T>.Default, comparer))
            {
                knownType = RedBlackComparerKnownType.Default;
            }
            else if (comparer.GetType() == typeof(RedBlackTreeSetBase<T>.HashCodeComparer))
            {
                knownType = RedBlackComparerKnownType.HashCode;
            }
            else if (typeof(T) == StringType)
            {
                if (object.Equals(StringComparer.CurrentCulture, comparer))
                {
                    knownType = RedBlackComparerKnownType.StringCurrentCulture;
                }
                else if (object.Equals(StringComparer.CurrentCultureIgnoreCase, comparer))
                {
                    knownType = RedBlackComparerKnownType.StringCurrentCultureIgnoreCase;
                }
                else if (object.Equals(StringComparer.InvariantCulture, comparer))
                {
                    knownType = RedBlackComparerKnownType.StringInvariantCulture;
                }
                else if (object.Equals(StringComparer.InvariantCultureIgnoreCase, comparer))
                {
                    knownType = RedBlackComparerKnownType.StringInvariantCultureIgnoreCase;
                }
                else if (object.Equals(StringComparer.Ordinal, comparer))
                {
                    knownType = RedBlackComparerKnownType.StringOrdinal;
                }
                else if (object.Equals(StringComparer.OrdinalIgnoreCase, comparer))
                {
                    knownType = RedBlackComparerKnownType.StringOrdinalIgnoreCase;
                }
            }
        }

        ///// <summary>
        ///// gets the comparer
        ///// </summary>
        //public IComparer<T> Comparer
        //{
        //    get
        //    {
        //        return comparer;
        //    }
        //}

        /// <summary>
        /// Returns known type if comparer is a known type.
        /// </summary>
        /// <returns></returns>
        public string GetKnownType()
        {
            switch (knownType)
            {
                case RedBlackComparerKnownType.None: return null;
                case RedBlackComparerKnownType.StringCurrentCulture:
                case RedBlackComparerKnownType.StringCurrentCultureIgnoreCase: return $"{knownType}:{Thread.CurrentThread.CurrentCulture.Name}";
                default: return knownType.ToString();
            }
        }

     
        #region static      
        /// <summary>
        /// Provides comparer from known type, else null.
        /// </summary>
        public static IComparer<T> GetComparerFromKnownText(string knownType)
        {
            if (knownType == null)
            {
                return null;
            }
            if (knownType == RedBlackComparerKnownType.Default.ToString())
            {
                return Comparer<T>.Default;
            }
            if (knownType == RedBlackComparerKnownType.HashCode.ToString())
            {
                return new RedBlackTreeSetBase<T>.HashCodeComparer();
            }
            if (knownType == RedBlackComparerKnownType.StringInvariantCulture.ToString())
            {
                return (IComparer<T>)StringComparer.InvariantCulture;
            }
            if (knownType == RedBlackComparerKnownType.StringInvariantCultureIgnoreCase.ToString())
            {
                return (IComparer<T>)StringComparer.InvariantCultureIgnoreCase;
            }
            if (knownType == RedBlackComparerKnownType.StringOrdinal.ToString())
            {
                return (IComparer<T>)StringComparer.Ordinal;
            }
            if (knownType == RedBlackComparerKnownType.StringOrdinalIgnoreCase.ToString())
            {
                return (IComparer<T>)StringComparer.OrdinalIgnoreCase;
            }
            int twoDotIndex;
            if (knownType.StartsWith(RedBlackComparerKnownType.StringCurrentCulture.ToString()) && (twoDotIndex = knownType.IndexOf(':')) == RedBlackComparerKnownType.StringCurrentCulture.ToString().Length)
            {
                return (IComparer<T>)StringComparer.Create(new CultureInfo(knownType.Substring(twoDotIndex + 1)), false);
            }
            if (knownType.StartsWith(RedBlackComparerKnownType.StringCurrentCultureIgnoreCase.ToString()) && (twoDotIndex = knownType.IndexOf(':')) == RedBlackComparerKnownType.StringCurrentCultureIgnoreCase.ToString().Length)
            {
                return (IComparer<T>)StringComparer.Create(new CultureInfo(knownType.Substring(twoDotIndex + 1)), true);
            }
            return GetObjFromKnownText<IComparer<T>>(knownType);
        }       
        #endregion


    }
}
