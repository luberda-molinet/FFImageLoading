using System;
using Xamvvm;

namespace Sample
{
    
    public class SvgSamplePageModel : BasePageModel
    {
        public SvgSamplePageModel()
        {
        }

        public string Source { get; set; } = "sample.svg";
    }
}
