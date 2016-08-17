using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace Malaga
{
	class Settings
	{
		private static ApplicationDataCompositeValue compositeResults;
		private static Windows.Storage.ApplicationDataContainer RoamingSettings;

		private static bool firstBoot = true;
		public static bool FirstBoot { get; }
		private static Point lastPosition = new Point(0.00,0.00);
		public static Point LastPosition { get { return lastPosition; } set { lastPosition = value; WriteGPS(); } }
		/*[Food,Drinks,Restaurant,Museum,Pub,Shopping,LocalFlavour,IceCream,Sport,Beauty,Education]*/
		private static string yelpCode = "11010110011"; 
		public static string YelpCode { get { return yelpCode; } set { yelpCode = value;  WriteYelp(); } }

		/// <summary>
		/// Dispatcher to write to memory
		/// </summary>
		/// <param name="value"></param>
		private static void WriteSettings(string value = null)
		{
			switch (value)
			{
				case null:
					WriteAll();
					break;
				case "GPS":
					WriteGPS();
					break;
				case "YELP":
					WriteYelp();
					break;
			}
		}

		/// <summary>
		/// Write Yelp settings to memory
		/// </summary>
		private static void WriteYelp()
		{
			compositeResults = new Windows.Storage.ApplicationDataCompositeValue();
			compositeResults["Items"] = yelpCode;
			RoamingSettings.Values["Yelp"] = compositeResults;
		}

		/// <summary>
		/// Write GPS settings to memory
		/// </summary>
		private static void WriteGPS()
		{
			compositeResults = new Windows.Storage.ApplicationDataCompositeValue();
			if (lastPosition == null)
				lastPosition = new Point(0, 0);
			compositeResults["LastLat"] = lastPosition.X;
			compositeResults["LastLon"] = lastPosition.Y;
			RoamingSettings.Values["GPS"] = compositeResults;
		}

		/// <summary>
		/// Write to Memory all settings
		/// </summary>
		private static void WriteAll()
		{
			WriteGPS();
			WriteYelp();
			RoamingSettings.Values["FirstBoot"] = true;
		}

		/// <summary>
		/// Load settings into Memory, if not existing create it
		/// </summary>
		public static void ReadSettings()
		{
			RoamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

			compositeResults = (Windows.Storage.ApplicationDataCompositeValue)RoamingSettings.Values["GPS"];
			if (compositeResults == null)
				WriteAll();
			else
			{
				if (compositeResults.Count() < 2)
					WriteGPS();
				else
				{
					lastPosition.X = (double)compositeResults["LastLat"];
					lastPosition.Y = (double)compositeResults["LastLon"];
				}
			}

			compositeResults = (Windows.Storage.ApplicationDataCompositeValue)RoamingSettings.Values["Yelp"];
			if (compositeResults == null)
				WriteAll();
			else
			{
				if (compositeResults.Count < 1)
					WriteYelp();
				else
					yelpCode = (string)compositeResults["Items"];
			}

			firstBoot = (bool)RoamingSettings.Values["FirstBoot"];
			if (firstBoot)
				RoamingSettings.Values["FirstBott"] = false;
		}
	}
}
