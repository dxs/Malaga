using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.WinRT;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace Malaga
{
	class Database
	{

		public static int nextId { get; set; }
		public static double CurrentLatitude { get; set; }
		public static double CurrentLongitude { get; set; }
		private static string dbPath = string.Empty;
		private static string DbPath { get { return dbPath; } }


		/// <summary>
		/// Instancie une Database avec le Path donnée en paramètre
		/// </summary>
		/// <param name="path">Dossier de localisation</param>
		public Database(string path)
		{
			if(string.IsNullOrEmpty(dbPath))
				dbPath = System.IO.Path.Combine(path, "MapPoint.sqlite");
			nextId = 0;
			CurrentLatitude = 0.00;
			CurrentLongitude = 0.0;
		}

		/// <summary>
		/// Set the SQLiteConnection
		/// </summary>
		private static SQLiteConnection DbConnection
		{
			get { return new SQLiteConnection(new SQLitePlatformWinRT(), DbPath); }
		}

		/// <summary>
		/// Internal Object repressenting a Point on a Map
		/// </summary>
		internal class MapPoint
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

		}

		/// <summary>
		/// Setup Database and create table if not already exist
		/// </summary>
		public async Task<bool> setDB()
		{
			if(!string.IsNullOrEmpty(dbPath))
			using (var DB = DbConnection)
			{
				var c = DB.CreateTable<MapPoint>();
				var info = DB.GetMapping(typeof(MapPoint));
				var j = 1;
				int count = DB.Table<MapPoint>().Count();
				#region MapPointCreation
				if(count < 1)
				{
					MapPoint point = new MapPoint();//46.532622, 6.590774
					point = await createMapPoint(j++, "Bar fiesta", "Happy hours 22h-22h30", 46.532622, 6.590774, "Bar");
					var i = DB.InsertOrReplace(point);
					point = await createMapPoint(j++, "Bar Pong Pong", "Beer pong party", 46.532821, 6.600000, "Bar");
					i = DB.InsertOrReplace(point);
					point = await createMapPoint(j++, "Club PicNique", "Pool party tonight", 46.529087, 6.587635, "Club");
					i = DB.InsertOrReplace(point);
					point = await createMapPoint(j++, "La fraise", "Chou fractale happy hour", 46.538762, 6.577635, "Restaurant");
					i = DB.InsertOrReplace(point);
					point = await createMapPoint(j++, "Le gigolo", "Viens, on sera bien", 46.5386756, 6.587528, "Club");
					i = DB.InsertOrReplace(point);
					point = await createMapPoint(j++, "Zoo", "Animal crossing 2", 46.5287756, 6.574768, "Visit");
					i = DB.InsertOrReplace(point);
					point = await createMapPoint(j++, "Le gigolo", "Viens, on sera bien", 46.5279756, 6.5862528, "visit");
					i = DB.InsertOrReplace(point);
					point = await createMapPoint(j++, "Bar chicka", "Braaaaa", 46.5287567, 6.587620, "Bar");
					i = DB.InsertOrReplace(point);
					point = await createMapPoint(j++, "Tapas Tacos", "Petage de panse", 46.5387260, 6.5721976, "Restaurant");
					i = DB.InsertOrReplace(point);
					point = await createMapPoint(j++, "Chinese food", "Chez toi ou Chinois?", 46.539678, 6.597290, "Restaurant");
					i = DB.InsertOrReplace(point);
				}
				#endregion
			}
			return true;
		}

		/// <summary>
		/// Delete an entry in Databas
		/// </summary>
		/// <param name="point">MapPoint to Delete</param>
		public void DeleteMapPoint(MapPoint point)
		{
			using (var DB = DbConnection)
				DB.Execute("DELETE FROM MapPoint WHERE Id = ?", point.Id);
		}

		/// <summary>
		/// Save a point (creating or updating existing point)
		/// </summary>
		/// <param name="point"></param>
		public void SaveMapPoint(MapPoint point)
		{
			using (var DB = DbConnection)
			{
				DB.InsertOrReplace(point);
			}
		}

		/// <summary>
		/// Gets all the point as a List
		/// </summary>
		/// <returns>List of MapPoint</returns>
		public ObservableCollection<MapPoint> GetAllPoints()
		{
			ObservableCollection<MapPoint> pointList = new ObservableCollection<MapPoint>();
			List<MapPoint> list;
			using (var DB = DbConnection)
				list = (from m in DB.Table<MapPoint>() select m).ToList();
			foreach (MapPoint item in list)
				pointList.Add(item);
			if (pointList.Count > 0)
				nextId = pointList[pointList.Count - 1].Id + 1;
			return pointList;
		}

		/// <summary>
		/// Get a MapPoint from an Id
		/// </summary>
		/// <param name="_Id"></param>
		/// <returns>MapPoint</returns>
		public MapPoint GetPointById(int _Id)
		{
			using (var DB = DbConnection)
			{
				MapPoint point = (from m in DB.Table<MapPoint>()
								  where m.Id == _Id
								  select m).FirstOrDefault();
				return point;
			}
		}

		/// <summary>
		/// Get a MapPoint from name
		/// </summary>
		/// <param name="Name"></param>
		/// <returns>MapPoint</returns>
		public MapPoint GetPointByName(string Name)
		{
			using (var DB = DbConnection)
			{
				MapPoint point = (from m in DB.Table<MapPoint>()
								  where m.Name == Name
								  select m).FirstOrDefault();
				return point;
			}
		}

		/// <summary>
		/// Retrieve from DB all MapPoint that match a Type
		/// </summary>
		/// <param name="Type"></param>
		/// <returns>List of MapPoint</returns>
		public ObservableCollection<MapPoint> GetPointsByType(string Type)
		{
			List<MapPoint> list = null;
			ObservableCollection<MapPoint> listPoint = new ObservableCollection<MapPoint>();
			if (Type == "All")
				return GetAllPoints();

			using (var DB = DbConnection)
			{
				list = (from m in DB.Table<MapPoint>()
						where m.Type == Type
						select m).ToList();
				foreach (MapPoint item in list)
					listPoint.Add(item);
				return listPoint;
			}
		}

		/// <summary>
		/// Cree un MapPoint selon une latitude et une longitude
		/// </summary>
		/// <param name="_Id">Identifiant</param>
		/// <param name="_Name">Nom</param>
		/// <param name="_Description">Explication brève et remarques</param>
		/// <param name="_Latitude">Coordonnée géographique 1</param>
		/// <param name="_Longitude">Coordonnée géographique 2</param>
		/// <param name="_Type">Type d'endroit</param>
		/// <returns>MapPoint</returns>
		public async Task<MapPoint> createMapPoint(int _Id, string _Name, string _Description, double _Latitude, double _Longitude, string _Type)
		{
			MapPoint _point = new MapPoint();
			_point.Id = _Id;
			_point.Latitude = _Latitude;
			_point.Longitude = _Longitude;
			string address = await GetAdressFromPoint(new Point(_Latitude, _Longitude));
			string[] parse = address.Split(',');
			_point.Street = parse[0];
			_point.Town = parse[1] + "," + parse[2];
			_point.Name = _Name;
			_point.Type = _Type;
			_point.Description = _Description;
			return _point;
		}

		/// <summary>
		/// Cree un MapPoint selon une adresse
		/// </summary>
		/// <param name="_Id">Identifian</param>
		/// <param name="_Name">Nom</param>
		/// <param name="_Description">Explication brève et remarques</param>
		/// <param name="_Street">Rue</param>
		/// <param name="_Town">Code postal + ville</param>
		/// <param name="_Type">Type d'endroits</param>
		/// <returns>MapPoint</returns>
		public async Task<MapPoint> createMapPoint(int _Id, string _Name, string _Description, string _Street, string _Town, string _Type)
		{
			MapPoint _point = new MapPoint();
			_point.Id = _Id;
			_point.Street = _Street;
			_point.Town = _Town;
			Point p = await GetPointFromAddress(_Street + ", " + _Town);
			_point.Latitude = p.X;
			_point.Longitude = p.Y;
			_point.Name = _Name;
			_point.Type = _Type;
			_point.Description = _Description;
			return _point;
		}

		#region coordAdressConverter

		/// <summary>
		/// Convert a lat/lon coordinate to an address
		/// </summary>
		/// <param name="point"></param>
		/// <returns>string address</returns>
		public async Task<string> GetAdressFromPoint(Point point)
		{
			string address = "";
			// The location to reverse geocode.
			BasicGeoposition location = new BasicGeoposition();
			location.Latitude = point.X;
			location.Longitude = point.Y;
			Geopoint pointToReverseGeocode = new Geopoint(location);

			// Reverse geocode the specified geographic location.
			MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

			if (result.Status == MapLocationFinderStatus.Success)
			{
				address += result.Locations[0].Address.Street + " ";
				address += result.Locations[0].Address.StreetNumber + ", ";
				address += result.Locations[0].Address.PostCode + ", ";
				address += result.Locations[0].Address.Town;
			}

			return address;
		}

		/// <summary>
		/// Convert an address to a lat/lon coordinate
		/// </summary>
		/// <param name="address"></param>
		/// <returns>Point latitude = X longitude = y</returns>
		public async Task<Point> GetPointFromAddress(string address)
		{
			Point point = new Point(0.00, 0.00);

			// The nearby location to use as a query hint.
			BasicGeoposition queryHint = new BasicGeoposition();
			queryHint.Latitude = CurrentLatitude;
			queryHint.Longitude = CurrentLongitude;
			Geopoint hintPoint = new Geopoint(queryHint);

			// Geocode the specified address, using the specified reference point
			// as a query hint. Return no more than 3 results.
			MapLocationFinderResult result =
				  await MapLocationFinder.FindLocationsAsync(address, hintPoint, 3);

			if (result.Status == MapLocationFinderStatus.Success)
			{
				point.X = result.Locations[0].Point.Position.Latitude;
				point.Y = result.Locations[0].Point.Position.Longitude;
			}
			return point;
		}
		#endregion

	}
}
