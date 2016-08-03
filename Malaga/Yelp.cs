using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Malaga
{
	class Yelp
	{
		/// <summary>
		/// Consumer key used for OAuth authentication.
		/// This must be set by the user.
		/// </summary>
		private const string CONSUMER_KEY = "AU5CWpU8pobr6QWx1VYVlg;

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


		public Yelp()
		{

		}

		private async JObject PerformRequest(string baseURL, Dictionary<string, string> queryParams = null)
		{
			
		}

	}
}
