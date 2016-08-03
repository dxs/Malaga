using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

		public Yelp()
		{
			client = new YelpSharp.YelpClient(TOKEN, TOKEN_SECRET, CONSUMER_KEY, CONSUMER_SECRET);
		}

		public async Task<List<string>> GetData(Point coordinate, string query, int radius, int nbToLoad, int offset, int sortBy, string town)
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
				

			return list;
		}

	}
}
