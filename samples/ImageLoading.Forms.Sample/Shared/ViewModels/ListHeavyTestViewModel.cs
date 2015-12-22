using System;
using System.Collections.ObjectModel;
using FFImageLoading.Forms.Sample.Models;

namespace FFImageLoading.Forms.Sample.ViewModels
{
	public class ListHeavyTestViewModel : BaseExampleViewModel
	{
		public ListHeavyTestViewModel()
		{
		}

		public override void PageFactoryMessageReceived(string message, object sender, object arg)
		{
			if (message == "Reload")
				Items = GenerateSampleData();
		}

		public ObservableCollection<ListHeavyItem> Items {
			get { return GetField<ObservableCollection<ListHeavyItem>>(); }
			set { SetField(value); }
		}

		ObservableCollection<ListHeavyItem> GenerateSampleData()
		{
			var list = new ObservableCollection<ListHeavyItem>();

			string[] images = {
				"https://farm8.staticflickr.com/7292/8730172244_fc908366de_z_d.jpg",
				"https://farm8.staticflickr.com/7236/7171671407_1671ccd763_z_d.jpg",
				"https://farm4.staticflickr.com/3725/13839923294_d583bb8c21_z_d.jpg",
				"https://farm7.staticflickr.com/6135/5952249358_72202c3d82_z_d.jpg",
				"https://farm6.staticflickr.com/5827/20828815605_dd57ee2575_z_d.jpg",
				"https://farm3.staticflickr.com/2560/4181767596_0d0f971143_z_d.jpg",
				"https://farm6.staticflickr.com/5240/14213434134_25c913e7c7_z_d.jpg",
				"https://farm8.staticflickr.com/7635/16823997277_9455dc4df1_z_d.jpg",
				"https://farm9.staticflickr.com/8369/8529775981_e18941218e_z_d.jpg",
				"https://farm6.staticflickr.com/5337/9624202634_bb6fd9cf8b_z_d.jpg",
				"https://farm4.staticflickr.com/3826/13304885523_f9fd599673_z_d.jpg",
				"https://farm8.staticflickr.com/7421/13976697616_6fe78de2a2_z_d.jpg",
				"https://farm9.staticflickr.com/8612/15473104983_a7b807577a_z_d.jpg",
				"https://farm6.staticflickr.com/5211/5384756951_7a9465de30_z_d.jpg",
				"https://farm5.staticflickr.com/4141/4796759387_ebeec9a22e_z_d.jpg",
				"https://farm8.staticflickr.com/7304/13408556934_a462fc3056_z_d.jpg",
				"https://farm8.staticflickr.com/7472/15946946349_e9ae0cf37a_z_d.jpg",
				"https://farm3.staticflickr.com/2902/14357636976_2b3a93f86a_z_d.jpg",
				"https://farm6.staticflickr.com/5534/11476526956_3afb34122f_z_d.jpg",
				"https://farm6.staticflickr.com/5328/8979116312_cdb493d348_z_d.jpg",
				"https://farm4.staticflickr.com/3825/10522948753_9859b365a7_z_d.jpg",
				"https://farm1.staticflickr.com/342/18427359388_758337aa67_z_d.jpg",
				"https://farm5.staticflickr.com/4011/4308181244_5ac3f8239b.jpg",
				"https://farm1.staticflickr.com/66/158583580_79e1c5f121_z_d.jpg",
				"https://farm9.staticflickr.com/8625/15806486058_7005d77438.jpg",
				"https://farm7.staticflickr.com/6129/5923256699_d7af1a373b_z_d.jpg",
				"https://farm6.staticflickr.com/5607/15519760371_da01e1042f_z_d.jpg",
				"https://farm8.staticflickr.com/7442/14220177913_1a4fcc0aa8_z_d.jpg",
				"https://farm9.staticflickr.com/8190/8097504059_ebb845d404_z_d.jpg",
				"https://farm8.staticflickr.com/7450/14178722235_f208ef6353_z_d.jpg",
				"https://farm4.staticflickr.com/3822/11134920205_db415e3d7a_z_d.jpg",
				"https://farm1.staticflickr.com/681/21141103820_32064b9818_z_d.jpg",
				"https://farm1.staticflickr.com/44/128608095_67f0721c6d_z_d.jpg",
				"https://farm9.staticflickr.com/8596/16729860256_672d463e92_z_d.jpg",
				"https://farm9.staticflickr.com/8058/8213531652_00c55f5fb6_z_d.jpg",
				"https://farm4.staticflickr.com/3791/18729017954_f55fb8d092_z_d.jpg",
				"https://farm6.staticflickr.com/5829/22451304710_be02cde686_z_d.jpg",
				"https://farm6.staticflickr.com/5591/15112444938_886f6160d7_z_d.jpg",
				"https://farm1.staticflickr.com/732/21559098478_9c4a822d09_z_d.jpg",
				"https://farm9.staticflickr.com/8877/18600339506_7c440f9928_z_d.jpg",
			};

			for (int j = 0; j < 10; j++)
			{
				for (int i = 0; i < images.Length; i++ )
				{
					var item = new ListHeavyItem() {
						Image1Url = images[i],
						Image2Url = images[i],
						Image3Url = images[i],
						Image4Url = images[i],
					};

					list.Add(item);
				}
			}

			return list;
		}
	}
}

