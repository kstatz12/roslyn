﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// For completion and quickinfo in linked files, we compare symbols from different documents
    /// to determine if they are similar enough for us to suppress the platform dependence
    /// warning icon. We consider symbols equivalent if they have the same name.
    /// </summary>
    internal sealed class LinkedFilesSymbolEquivalenceComparer : IEqualityComparer<ISymbol>
    {
        public static readonly LinkedFilesSymbolEquivalenceComparer Instance = new();

        bool IEqualityComparer<ISymbol>.Equals(ISymbol? x, ISymbol? y)
        {
            if (x == y)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.Name == y.Name;
        }

        int IEqualityComparer<ISymbol>.GetHashCode(ISymbol symbol)
            => symbol.Name.GetHashCode();
    }
}
