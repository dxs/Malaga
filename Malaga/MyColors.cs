using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Malaga
{
	class MyColors
	{
		public SolidColorBrush RedPlain = new SolidColorBrush(Colors.Red);
		public SolidColorBrush RedFaded = new SolidColorBrush(Colors.Red);
		public SolidColorBrush GreenPlain = new SolidColorBrush(Colors.Green);
		public SolidColorBrush GreenFaded = new SolidColorBrush(Colors.Green);
		public SolidColorBrush BluePlain = new SolidColorBrush(Colors.Blue);
		public SolidColorBrush BlueFaded = new SolidColorBrush(Colors.Blue);
		public SolidColorBrush YellowPlain = new SolidColorBrush(Colors.Yellow);
		public SolidColorBrush YellowFaded = new SolidColorBrush(Colors.Yellow);
		public SolidColorBrush VioletPlain = new SolidColorBrush(Colors.Violet);
		public SolidColorBrush VioletFaded = new SolidColorBrush(Colors.Violet);
		public SolidColorBrush GrayPlain = new SolidColorBrush(Colors.Gray);
		public SolidColorBrush GrayFaded = new SolidColorBrush(Colors.Gray);

		public static int Opacity { get { return Opacity * 100; } set { if (value != 0) Opacity = value / 100; } }

		/// <summary>
		/// Get colors in an easy way
		/// </summary>
		/// <param name="opacity">Percent of faded colors opacity</param>
		public MyColors(double opacity = 5)
		{
			opacity /= 100;
			RedFaded.Opacity = opacity;
			GreenFaded.Opacity = opacity;
			BlueFaded.Opacity = opacity;
			YellowFaded.Opacity = opacity;
			VioletFaded.Opacity = opacity;
			GrayFaded.Opacity = opacity;
		}
	}
}
