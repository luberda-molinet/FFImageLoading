using FFImageLoading.Work;
using Xamarin.Forms

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class TintTransformation : ITransformation
    {
        public TintTransformation() : this(0, 165, 0, 128)
        {
        }

        public TintTransformation(int r, int g, int b, int a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public TintTransformation(string hexColor)
        {
            HexColor = hexColor;
        }
		
        public static readonly BindableProperty EnableSolidColorProperty = 
			BindableProperty.Create(nameof(EnableSolidColor), 
									typeof(bool), 
									typeof(TintTransformation), 
									false, 
									BindingMode.OneWay);
									
        public bool EnableSolidColor 
		{ 
			get => (bool)GetValue(EnableSolidColorProperty); 
			set => SetValue(EnableSolidColorProperty, value);
		}

		public static readonly BindableProperty HexColorProperty = 
			BindableProperty.Create(nameof(HexColor), 
									typeof(string), 
									typeof(TintTransformation), 
									"#000000", 
									BindingMode.OneWay);

        public string HexColor
		{ 
			get => (string)GetValue(HexColorProperty); 
			set => SetValue(HexColorProperty, value);
		}
		
        public static readonly BindableProperty RProperty = 
			BindableProperty.Create(nameof(R), 
									typeof(int), 
									typeof(TintTransformation), 
									0, 
									BindingMode.OneWay);
        public int R 
		{ 
			get => (int)GetValue(RProperty); 
			set => SetValue(RProperty, value);
		}
		
        public static readonly BindableProperty GProperty = 
			BindableProperty.Create(nameof(G), 
									typeof(int), 
									typeof(TintTransformation), 
									false, 
									BindingMode.OneWay);
        public int G 
		{ 
			get => (int)GetValue(GProperty); 
			set => SetValue(GProperty, value);
		}
		
        public static readonly BindableProperty BProperty = 
			BindableProperty.Create(nameof(B), 
									typeof(int), 
									typeof(TintTransformation), 
									false, 
									BindingMode.OneWay);

        public int B 
		{ 
			get => (int)GetValue(BProperty); 
			set => SetValue(BProperty, value);
		}
		
        public static readonly BindableProperty AProperty = 
			BindableProperty.Create(nameof(A), 
									typeof(int), 
									typeof(TintTransformation), 
									false, 
									BindingMode.OneWay);

        public int A 
		{ 
			get => (int)GetValue(AProperty); 
			set => SetValue(AProperty, value);
		}

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public string Key => $"TintTransformation,R={R},G={G},B={B},A={A},HexColor={HexColor},EnableSolidColor={EnableSolidColor}";
    }
}
