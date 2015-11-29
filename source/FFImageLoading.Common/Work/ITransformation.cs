using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFImageLoading.Work
{
    public interface ITransformation
    {
        string Key { get; }
		IBitmap Transform(IBitmap source);
    }
}