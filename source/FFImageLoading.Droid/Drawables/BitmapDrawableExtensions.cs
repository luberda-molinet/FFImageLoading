//
// BitmapDrawableExtensions.cs
//
// Author:
//   Brett Duncavage <brett.duncavage@rd.io>
//
// Copyright 2013 Rdio, Inc.
//

using System;
using Android.Graphics.Drawables;

namespace FFImageLoading.Drawables
{
    public static class BitmapDrawableExtensions
    {
        public static void TypeCheckedSetIsDisplayed(this BitmapDrawable drawable, bool displayed)
        {
            if (drawable is SelfDisposingBitmapDrawable) {
                ((SelfDisposingBitmapDrawable)drawable).SetIsDisplayed(displayed);
            }
        }
    }
}

