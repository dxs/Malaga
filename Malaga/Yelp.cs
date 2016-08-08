using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using YelpSharp;

namespace Malaga
{
	class Yelp
	{
		int id = 0;

		/// <summary>
		/// Consumer key used for OAuth authentication.
		/// This must be set by the user.
		/// </summary>
		private const string CONSUMER_KEY = "AU5CWpU8pobr6QWx1VYVlg";

		/// <summary>
		/// Consumer secret used for OAuth authentication.
		/// This must be set by the user.
		/// </summary>
		private const string CONSUMER_SECRET = "1U1Hilhoxgcs0PL2RtY4mOHaIrM";

		/// <summary>
		/// Token used for OAuth authentication.
		/// This must be set by the user.
		/// </summary>
		private const string TOKEN = "zJjl6XBiSI0RfqUevkBAZVUQxaQESUIh";

		/// <summary>
		/// Token secret used for OAuth authentication.
		/// This must be set by the user.
		/// </summary>
		private const string TOKEN_SECRET = "tvbGsk5UkLdQFwUAMhwJkiuMYZI";

		/// <summary>
		/// Host of the API.
		/// </summary>
		private const string API_HOST = "https://api.yelp.com";

		/// <summary>
		/// Relative path for the Search API.
		/// </summary>
		private const string SEARCH_PATH = "/v2/search/";

		/// <summary>
		/// Relative path for the Business API.
		/// </summary>
		private const string BUSINESS_PATH = "/v2/business/";

		/// <summary>
		/// Search limit that dictates the number of businesses returned.
		/// </summary>
		private const int SEARCH_LIMIT = 3;

		YelpSharp.YelpClient client;
		static private ObservableCollection<Business> collectionBusiness;

		public Yelp()
		{
			client = new YelpSharp.YelpClient(TOKEN, TOKEN_SECRET, CONSUMER_KEY, CONSUMER_SECRET);
			collectionBusiness = new ObservableCollection<Business>();
		}

		/// <summary>
		/// Get data from yelp API
		/// </summary>
		/// <param name="coordinate">X for Latitude, Y for Longitude</param>
		/// <param name="query">What is the request</param>
		/// <param name="radius">Distance starting from the coordinate</param>
		/// <param name="nbToLoad">Numer of place to load</param>
		/// <param name="offset">Offset</param>
		/// <param name="sortBy">Sort results by [BestMatched, Distance, HighestRated]</param>
		/// <param name="town">Additionnal parameter for town</param>
		public async Task<bool> GetData(Point coordinate, string query, int radius, int nbToLoad, int offset, int sortBy, string town)
		{
			YelpSharp.YelpSortMode sortMode = new YelpSharp.YelpSortMode();
			switch(sortBy)
			{
				case 0:
					sortMode = YelpSharp.YelpSortMode.BestMatched;
					break;
				case 1:
					sortMode = YelpSharp.YelpSortMode.Distance;
					break;
				case 2:
					sortMode = YelpSharp.YelpSortMode.HighestRated;
					break;
				default:
					sortMode = YelpSharp.YelpSortMode.BestMatched;
					break;
			}

			YelpSearchOptionsGeneral genOptions = new YelpSharp.YelpSearchOptionsGeneral(query, nbToLoad, offset, sortMode, null, radius, null);
			YelpCoordinates coord = new YelpSharp.YelpCoordinates() { Latitude = coordinate.X, Longitude = coordinate.Y };
			YelpSearchOptionsLocation location = new YelpSharp.YelpSearchOptionsLocation(town, coord);
			YelpSearchOptions options = new YelpSharp.YelpSearchOptions(genOptions, null, location);
			YelpSearchResults result = await client.SearchWithOptions(options);

			List<string> list = new List<string>();
			foreach (var business in result.businesses)
				AddBusinessToList(business);	
			return true;
		}

		/// <summary>
		/// Add a business to the List
		/// </summary>
		/// <param name="business"></param>
		private void AddBusinessToList(YelpBusiness business)
		{
			if (!isValid(business))
				return;
			if (business.image_url == null)
				business.image_url = "ms-appx:///Assets/barBig.png";

			collectionBusiness.Add(new Business()
			{
				ID = business.id,
				Name = business.name,
				Description = business.categories[0][0],
				Distance = business.distance,
				Latitude = business.location.coordinate.Latitude,
				Longitude = business.location.coordinate.Longitude,
				PhotoUrl = business.image_url,
				Rating = business.rating
			});
		}

		private bool isValid(YelpBusiness business)
		{
			if (business.name == null)
				return false;
			if (business.id == null)
				return false;
			if (business.location.coordinate.Latitude == 0)
				return false;
			if (business.location.coordinate.Longitude == 0)
				return false;
			return true;
		}

		/// <summary>
		/// Return the next business information
		/// </summary>
		/// <returns>Return anonymous type represented as [Id, Name, Distance, Photo url, url, latitude, longitude, rating]</returns>
		public Business GetNextBusiness()
		{
			if (id >= collectionBusiness.Count)
				return new Business();
			Business bus = collectionBusiness[id];
			id++;
			return bus;
		}

		public ObservableCollection<Business> GetAllBusiness()
		{
			return collectionBusiness;
		}

		internal static Business FindBusinessById(string Id)
		{
			foreach (Business business in collectionBusiness)
				if (business.ID == Id)
					return business;
			return new Business();
		}
	}
}
