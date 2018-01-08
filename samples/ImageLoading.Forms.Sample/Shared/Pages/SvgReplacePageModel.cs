using System;
using System.Collections.Generic;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
    public class SvgReplacePageModel : BasePageModel
    {
        public SvgReplacePageModel()
        {
            ReplaceCommand = new BaseCommand((arg) =>
            {
                ReplaceMap = new Dictionary<string, string>()
                {
                    { "Hello", Guid.NewGuid().ToString() }
                };
            });
        }

        public Dictionary<string, string> ReplaceMap { get; set; }

        public IBaseCommand ReplaceCommand { get; set; }
    }
}
