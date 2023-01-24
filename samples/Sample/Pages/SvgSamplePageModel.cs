using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample
{
    public partial class SvgSamplePageModel : ObservableObject
    {
        public SvgSamplePageModel()
        {
        }

        [ObservableProperty]
        string source = "sample.svg";
    }
}
