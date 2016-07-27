using SQLite.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SQLite;
using SQLite.Net.Platform;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.WinRT;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;
using Windows.UI;

namespace Malaga
{

	/*36.718715, -4.421998 la taberna del pinxto larios*/
	/// <summary>
	/// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
	/// </summary>
	public sealed partial class MainPage : Page
    {
		private static string dbPath = string.Empty;
		List<MapPoint> ListMapPoint = null;
		private static string DbPath
		{
			get
			{
				if (string.IsNullOrEmpty(dbPath))
				{
					dbPath = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "MapPoint.sqlite");
				}
				return dbPath;
			}
		}

		private static SQLiteConnection DbConnection
		{
			get
			{
				return new SQLiteConnection(new SQLitePlatformWinRT(), DbPath);
			}
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

		}

		/// <summary>
		/// Setup Database and create table if not already exist
		/// </summary>
		private static void setDB()
		{
			using (var DB = DbConnection)
			{

				var c = DB.CreateTable<MapPoint>();
				var info = DB.GetMapping(typeof(MapPoint));
				var j = 1;

				#region MapPointCreation
				MapPoint point = new MapPoint();//46.532622, 6.590774
				point = createMapPoint(j++, "Bar fiesta", "Happy hours 22h-22h30", 46.532622, 6.590774, "Bar");
				var i = DB.InsertOrReplace(point);
				point = createMapPoint(j++, "Bar Pong Pong", "Beer pong party", 46.532821, 6.600000, "Bar");
				i = DB.InsertOrReplace(point);
				point = createMapPoint(j++, "Club PicNique", "Pool party tonight", 46.529087, 6.587635, "Club");
				i = DB.InsertOrReplace(point);
				point = createMapPoint(j++, "La fraise", "Chou fractale happy hour", 46.538762, 6.577635, "Restaurant");
				i = DB.InsertOrReplace(point);
				point = createMapPoint(j++, "Le gigolo", "Viens, on sera bien", 46.5386756, 6.587528, "Club");
				i = DB.InsertOrReplace(point);
				point = createMapPoint(j++, "Bar chicka", "Braaaaa", 46.5287567, 6.587620, "Bar");
				i = DB.InsertOrReplace(point);
				point = createMapPoint(j++, "Tapas Tacos", "Petage de panse", 46.5387260, 6.5721976, "Restaurant");
				i = DB.InsertOrReplace(point);
				point = createMapPoint(j++, "Chinese food", "Chez toi ou Chinois?", 46.539678, 6.597290, "Restaurant");
				i = DB.InsertOrReplace(point);
				#endregion
			}
		}

		/// <summary>
		/// Delete an entry in Database
		/// </summary>
		/// <param name="point"></param>
		private static void DeleteMapPoint(MapPoint point)
		{
			using (var DB = DbConnection)
				DB.Execute("DELETE FROM MapPoint WHERE Id = ?", point.Id);
		}

		/// <summary>
		/// Gets all the point as a List
		/// </summary>
		/// <returns>List of MapPoint</returns>
		private static List<MapPoint> GetAllPoints()
		{
			List<MapPoint> pointList;
			using (var DB = DbConnection)
				pointList = (from m in DB.Table<MapPoint>() select m).ToList();
			return pointList;
		}

		/// <summary>
		/// Get a MapPoint from an Id
		/// </summary>
		/// <param name="_Id"></param>
		/// <returns></returns>
		private static MapPoint GetPointById(int _Id)
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
		/// Save a point (creating or updating existing point)
		/// </summary>
		/// <param name="point"></param>
		private static void SaveMapPoint(MapPoint point)
		{
			using (var DB = DbConnection)
			{
				if (point.Id == 0)
					DB.Insert(point);
				else
					DB.Update(point);
			}
		}

		/// <summary>
		/// Cree un MapPoint selon des paramètres et le retourne
		/// </summary>
		/// <param name="_Id">Identifiant</param>
		/// <param name="_Name">Nom</param>
		/// <param name="_Description">Explication brève et remarques</param>
		/// <param name="_Latitude">Coordonnée géographique 1</param>
		/// <param name="_Longitude">Coordonnée géographique 2</param>
		/// <param name="_Type">Type d'endroit</param>
		/// <returns>MapPoint</returns>
		private static MapPoint createMapPoint(int _Id, string _Name, string _Description, double _Latitude, double _Longitude, string _Type)
		{
			MapPoint _point = new MapPoint();
			_point.Id = _Id;
			_point.Latitude = _Latitude;
			_point.Longitude = _Longitude;
			_point.Name = _Name;
			_point.Type = _Type;
			_point.Description = _Description;
			return _point;
		}
		/// <summary>
		/// 
		/// </summary>
		public MainPage()
        {
            this.InitializeComponent();

			setDB();
			ListMapPoint = GetAllPoints();
			setPOI();
			setGridView();
			setMap();

        }

		/// <summary>
		/// Set up the position of the map
		/// </summary>
		/// <remarks>Actually only set up the position regarding what is the current localisation of the user</remarks>
		private async void setMap()
		{
			// Set your current location.
			var accessStatus = await Geolocator.RequestAccessAsync();
			switch (accessStatus)
			{
				case GeolocationAccessStatus.Allowed:

					// Get the current location.
					Geolocator geolocator = new Geolocator();
					Geoposition pos = await geolocator.GetGeopositionAsync();
					Geopoint myLocation = pos.Coordinate.Point;

					// Set the map location
					mainMap.LandmarksVisible = true;
					await mainMap.TrySetViewAsync(myLocation, 13, 0, 0, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);
					break;

				case GeolocationAccessStatus.Denied:
					// Handle the case  if access to location is denied.
					break;

				case GeolocationAccessStatus.Unspecified:
					// Handle the case if  an unspecified error occurs.
					break;
			}
		}

		/// <summary>
		/// Set POI (Point Of Interest) on the map
		/// </summary>
		private void setPOI()
		{
			foreach (MapPoint point in ListMapPoint)
			{
				Geopoint loc = new Geopoint(new BasicGeoposition() { Latitude = point.Latitude, Longitude = point.Longitude });
				MapIcon mapIcon1 = new MapIcon();
				mapIcon1.Location = loc;
				mapIcon1.NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 1.0);
				mapIcon1.Title = point.Name;
				mapIcon1.ZIndex = 0;
				Uri picPath = new Uri("ms-appx:///Assets/bar.png");
				switch(point.Type)
				{
					case "Club":
						picPath = new Uri("ms-appx:///Assets/dj.png");
						break;
					case "Restaurant":
						picPath = new Uri("ms-appx:///Assets/restaurant.png"); ;
						break;
					case "Bar":
						picPath = new Uri("ms-appx:///Assets/bar.png");
						break;
					default:
						break;
				}
				mapIcon1.Image = RandomAccessStreamReference.CreateFromUri(picPath);
				// Add the MapIcon to the map.
				mainMap.MapElements.Add(mapIcon1);
			}
		}

		/// <summary>
		/// Set the grid view of points
		/// </summary>
		private void setGridView()
		{
			var i = 0;
			foreach(MapPoint point in ListMapPoint)
			{
				var rd = new RowDefinition();
				rd.MinHeight = 40;
				pointGrid.RowDefinitions.Add(rd);

				if(i % 2 == 0)
				{
					Rectangle background = new Rectangle();
					background.Fill = new SolidColorBrush(Color.FromArgb(Convert.ToByte(90), Convert.ToByte(230), Convert.ToByte(230), Convert.ToByte(230)));
					Grid.SetRow(background, i);
					Grid.SetColumn(background, 1);
					Grid.SetColumnSpan(background, 5);
					pointGrid.Children.Add(background);
				}

				TextBlock tbId = new TextBlock()
				{
					Text = point.Id.ToString(),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(6)
				};

				Grid.SetRow(tbId, i);
				Grid.SetColumn(tbId, 0);
				pointGrid.Children.Add(tbId);

				TextBlock tbName = new TextBlock()
				{
					Text = point.Name,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(6)
		};

				Grid.SetRow(tbName, i);
				Grid.SetColumn(tbName, 1);
				pointGrid.Children.Add(tbName);

				TextBlock tbDescr = new TextBlock()
				{
					Text = point.Description,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(6)
				};

				Grid.SetRow(tbDescr, i);
				Grid.SetColumn(tbDescr, 2);
				pointGrid.Children.Add(tbDescr);

				Button selectButton = new Button()
				{
					Content = new FontIcon()
					{
						FontFamily = new FontFamily("Segoe MDL2 Assets"),
						Glyph = "\uE1D2"
					},
					Padding = new Thickness(6),
					Background = new SolidColorBrush(Colors.Transparent),
					Tag = point.Id
				};
				selectButton.Click += SelectButton_Click;

				Grid.SetRow(selectButton, i);
				Grid.SetColumn(selectButton, 3);
				pointGrid.Children.Add(selectButton);

				Button editButton = new Button()
				{
					Content = new FontIcon()
					{
						FontFamily = new FontFamily("Segoe MDL2 Assets"),
						Glyph = "\uE946"
					},
					Padding = new Thickness(6),
					Background = new SolidColorBrush(Colors.Transparent),
					Tag = point.Id
				};
				editButton.Click += EditButton_Click;

				Grid.SetRow(editButton, i);
				Grid.SetColumn(editButton, 4);
				pointGrid.Children.Add(editButton);

				i++;
			}
		}

		/// <summary>
		/// Center the map to a givent MapPoint
		/// </summary>
		/// <param name="point"></param>
		private async void CenterMap(MapPoint point)
		{
			Geopoint loc = new Geopoint(new BasicGeoposition() { Latitude = point.Latitude, Longitude = point.Longitude });
			await mainMap.TrySetViewAsync(loc, 19, 0, 0, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);
		}

		/// <summary>
		/// Event called when user press select button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SelectButton_Click(object sender, RoutedEventArgs e)
		{
			var select = sender as Button;
			var point = GetPointById(Convert.ToInt32(select.Tag));
			CenterMap(point);
		}

		/// <summary>
		/// Event called when user press edit button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void EditButton_Click(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the new point to save
		/// </summary>
		/// <remarks>not implemented yet</remarks>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void mainMap_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{

		}
	}
}
