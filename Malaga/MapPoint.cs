
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malaga
{
	/// <summary>
	/// Internal Object repressenting a Point on a Map
	/// </summary>
	public class MapPoint
	{
		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		[MaxLength(64)]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the description.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the latitude
		/// </summary>
		public double Latitude { get; set; }

		/// <summary>
		/// Gets or sets the Longitude
		/// </summary>
		public double Longitude { get; set; }

		/// <summary>
		/// Gets or sets the type
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Gets or sets the Street address
		/// </summary>
		public string Street { get; set; }

		/// <summary>
		/// Gets or sets the Town
		/// </summary>
		public string Town { get; set; }

		/// <summary>
		/// Gets or sets the url of the image
		/// </summary>
		public string PhotoUrl { get; set; }
	}
}
