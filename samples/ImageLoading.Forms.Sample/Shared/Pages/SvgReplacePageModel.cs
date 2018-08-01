using System;
using System.Collections.Generic;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
    public class SvgReplacePageModel : BasePageModel
    {
        private Random _random = new Random();

        public SvgReplacePageModel()
        {
            ReplaceCommand = new BaseCommand((arg) =>
            {

                var r = _random.Next(256);
                var g = _random.Next(256);
                var b = _random.Next(256);

                ReplaceMap = new Dictionary<string, string>()
                {
                    { "#TEXT", Guid.NewGuid().ToString() },
                    { "#FILLCOLOR", $"#{r:X2}{g:X2}{b:X2}" }
                };
            });

            ReplaceCommand.Execute(null);
        }

        public Dictionary<string, string> ReplaceMap { get; set; }

        public IBaseCommand ReplaceCommand { get; set; }
    }
}
