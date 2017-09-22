using System;
using System.Collections.Generic;
using FFImageLoading.Work;
using FFImageLoading.Transformations;

namespace FFImageLoading.Forms.Sample
{
    
    public class TransformationsSelectorPageModel : BaseTransformationPageModel
    {
        public TransformationsSelectorPageModel()
        {
            Transformations = new List<ITransformation>()
            {
                new CustomTransformationSelector(),
            };
        }

        public List<ITransformation> Transformations { get; set;  }

        public class CustomTransformationSelector : ITransformation
        {
            readonly ITransformation PlaceholderTransformation = new CircleTransformation(5d, "#EEEEEE");
            readonly ITransformation ImageTransformation = new GrayscaleTransformation();

            public string Key
            {
                get
                {
                    return "CustomTransformationSelector";
                }
            }

            public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
            {
                if (isPlaceholder)
                {
                    return PlaceholderTransformation.Transform(sourceBitmap, path, source, isPlaceholder, key);
                }

                return ImageTransformation.Transform(sourceBitmap, path, source, isPlaceholder, key);
            }
        }
    }
}
