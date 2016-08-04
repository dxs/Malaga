using Newtonsoft.Json.Linq;
using SQLite.Net;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.WinRT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Malaga
{
	/// <summary>
	/// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		/*Global vars*/
		#region global
		MapIcon mapIconME = null;
		MapIcon tmpIcon = null;
		DispatcherTimer timer;

		MapPoint SelectedPoint = null;
		bool? follow = false;
		static int nextId = 0;
		List<MapPoint> ListMapPoint = null;
		List<Venue> ListVenue = null;

		int numberOfQueryDone = 0;

		string FOURSQUARECLIENTID = @"OZKEEVVRUNALJV5W4JDENEFQLNUBN2SJN10IHFRQM3VOQGGL";
		string FOURSQUARESECRETID = @"WRPOPQHQOVZUES0L3SAY44XUXFINKRXTLUU0AP1WNQS24Q4W";

		#endregion

		#region Database
		private static string dbPath = string.Empty;
		private static string DbPath
		{
			get
			{
				if (string.IsNullOrEmpty(dbPath))
					dbPath = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "MapPoint.sqlite");
				return dbPath;
			}
		}

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
		private async void setDB()
		{
			using (var DB = DbConnection)
			{
				var c = DB.CreateTable<MapPoint>();
				var info = DB.GetMapping(typeof(MapPoint));
				var j = 1;
				var count = DB.Table<MapPoint>().Count();
				#region MapPointCreation
				if (count < 1)
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
			ListMapPoint = GetAllPoints();
			setPOI();
			setGridView();
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
			if(pointList.Count > 0)
				nextId = pointList[pointList.Count - 1].Id + 1;
			return pointList;
		}

		/// <summary>
		/// Save a point (creating or updating existing point)
		/// </summary>
		/// <param name="point"></param>
		private static void SaveMapPoint(MapPoint point)
		{
			using (var DB = DbConnection)
			{
				DB.InsertOrReplace(point);
			}
		}

		/// <summary>
		/// Get a MapPoint from an Id
		/// </summary>
		/// <param name="_Id"></param>
		/// <returns>MapPoint</returns>
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
		/// Get a MapPoint from name
		/// </summary>
		/// <param name="Name"></param>
		/// <returns>MapPoint</returns>
		private static MapPoint GetPointByName(string Name)
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
		private static List<MapPoint> GetPointsByType(string Type)
		{
			List<MapPoint> list = null;
			if (Type == "All")
				return GetAllPoints();

			using (var DB = DbConnection)
			{
				list = (from m in DB.Table<MapPoint>()
						where m.Type == Type
						select m).ToList();
				return list;
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
		private async Task<MapPoint> createMapPoint(int _Id, string _Name, string _Description, double _Latitude, double _Longitude, string _Type)
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
		/// <param name="_Id"></param>
		/// <param name="_Name"></param>
		/// <param name="_Description"></param>
		/// <param name="_Street"></param>
		/// <param name="_Town"></param>
		/// <param name="_Type"></param>
		/// <returns>MapPoint</returns>
		private async Task<MapPoint> createMapPoint(int _Id, string _Name, string _Description, string _Street, string _Town, string _Type)
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

		#endregion

		#region coordAdressConverter

		/// <summary>
		/// Convert a lat/lon coordinate to an address
		/// </summary>
		/// <param name="point"></param>
		/// <returns>string address</returns>
		private async Task<string> GetAdressFromPoint(Point point)
		{
			string address = "";
			// The location to reverse geocode.
			BasicGeoposition location = new BasicGeoposition();
			location.Latitude = point.X;
			location.Longitude = point.Y;
			Geopoint pointToReverseGeocode = new Geopoint(location);

			// Reverse geocode the specified geographic location.
			MapLocationFinderResult result =  await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

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
		private async Task<Point> GetPointFromAddress(string address)
		{
			Point point = new Point(0.00, 0.00);

			// The nearby location to use as a query hint.
			BasicGeoposition queryHint = new BasicGeoposition();
			queryHint.Latitude = mapIconME.Location.Position.Latitude;
			queryHint.Longitude = mapIconME.Location.Position.Longitude;
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

		/// <summary>
		/// 
		/// </summary>
		public MainPage()
		{
			this.InitializeComponent();
			SelectedPoint = new MapPoint();
			timer = new DispatcherTimer();
			timer.Interval = new TimeSpan(0,0,1);
			timer.Tick += Timer_Tick;
			setDB();
			ListMapPoint = GetAllPoints();
			setMap();
			timer.Start();
		}

		private async void Timer_Tick(object sender, object e)
		{
			timer.Stop();
			bool? isConnected = false;
			isConnected = await loadYelp();
			if (isConnected == false)
			{
				timer.Interval = new TimeSpan(0, 0, 1);
				timer.Start();
			}
			if (isConnected == null)
			{
				timer.Interval = new TimeSpan(0, 0, 30);
				timer.Start();
			}
		}


		#region Yelp
		private async Task<bool> loadYelp(int queryNb = 0)
		{
			if (mapIconME == null)
				return false;
			Yelp y = new Yelp();
			Point p = new Point()
			{
				X = mapIconME.Location.Position.Latitude,
				Y = mapIconME.Location.Position.Longitude
			};
			string town = (await GetAdressFromPoint(p)).Split(',')[2];
			int offset = 20;
			ring2.Visibility = Visibility.Visible;
			await y.GetData(p, "", 10000, offset, queryNb * offset, 0, town);
			ring2.Visibility = Visibility.Collapsed;
			ring.Visibility = Visibility.Collapsed;
			DisplayYelp(offset, y);
			return true;
		}

		private void DisplayYelp(int offset, Yelp y)
		{
			for (int i = 0; i < offset; i++)
			{
				Yelp.Business business = new Yelp.Business();
				business = y.GetNextBusiness();
				if (business.Name != null)
				{
					StackPanel stack = new StackPanel();
					if (business.PhotoUrl != null)
						stack.Children.Add(new Image()
						{
							Source = new BitmapImage(new Uri(business.PhotoUrl)),
							MaxWidth = 120
						});
					else
						stack.Children.Add(new Image()
						{
							Source = new BitmapImage(new Uri("ms-appx:///Assets/barBig.png")),
							MaxWidth =120
						});
					stack.Children.Add(new TextBlock()
					{
						Text = business.Name
					});
					stack.Children.Add(new TextBlock()
					{
						Text = "Rating : " + business.Rating.ToString()
					});
					yelpGridView.Items.Add(new GridViewItem()
					{
						Name = business.ID,
						Content = stack,
						Margin = new Thickness(10)
					});
				}
			}
		}

		private void yelpGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (yelpGridView.SelectedItems.Count > 0)
				SaveYelpButton.Visibility = Visibility.Visible;
			else
				SaveYelpButton.Visibility = Visibility.Collapsed;
		}

		#endregion

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
					var myPosition = new Geolocator { ReportInterval = 500 };
					myPosition.DesiredAccuracy = PositionAccuracy.High;
					Geoposition pos = await myPosition.GetGeopositionAsync();
					Geopoint loc = new Geopoint(new BasicGeoposition()
					{ Latitude = pos.Coordinate.Point.Position.Latitude, Longitude = pos.Coordinate.Point.Position.Longitude });
					await mainMap.TrySetViewAsync(loc, 19, 0, 0, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);
					// Subscribe to the PositionChanged event to get location updates.
					myPosition.PositionChanged += MyPosition_PositionChanged;
					break;

				case GeolocationAccessStatus.Denied:
					//_rootPage.NotifyUser("Access to location is denied.", NotifyType.ErrorMessage);
					break;

				case GeolocationAccessStatus.Unspecified:
					//_rootPage.NotifyUser("Unspecificed error!", NotifyType.ErrorMessage);
					break;
			}
		}

		/// <summary>
		/// Update the position and call CenterMap(Point) if necessary
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private async void MyPosition_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
		{
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { setMyPosition(args.Position); });
			Point p = new Point(args.Position.Coordinate.Point.Position.Latitude, args.Position.Coordinate.Point.Position.Longitude);
			if (follow == true)
				CenterMap(p);
		}

		/// <summary>
		/// Set a MapElement on MapControl representing the user position
		/// </summary>
		/// <param name="position"></param>
		private void setMyPosition(Geoposition position)
		{
			if (mapIconME == null)
				mapIconME = new MapIcon();
				
			mapIconME.Location = new Geopoint(new BasicGeoposition()
			{ Latitude = position.Coordinate.Point.Position.Latitude, Longitude = position.Coordinate.Point.Position.Longitude });
			mapIconME.NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 1.0);
			mapIconME.ZIndex = 100;
			mapIconME.CollisionBehaviorDesired = MapElementCollisionBehavior.RemainVisible;
			mapIconME.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/loc.png"));
			mainMap.MapElements.Remove(mapIconME);
			mainMap.MapElements.Add(mapIconME);
		}

		/// <summary>
		/// Set POI (Point Of Interest) on the map
		/// </summary>
		/// <remarks>Use the ZIndex to mark the Id of the point</remarks>
		private void setPOI()
		{
			clearPOI();
			if (ListMapPoint == null)
				return;

			foreach (MapPoint point in ListMapPoint)
			{
				Geopoint loc = new Geopoint(new BasicGeoposition() { Latitude = point.Latitude, Longitude = point.Longitude });
				MapIcon mapIcon1 = new MapIcon();
				mapIcon1.Location = loc;
				mapIcon1.NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 1.0);
				mapIcon1.Title = point.Name;
				mapIcon1.ZIndex = point.Id;
				mapIcon1.CollisionBehaviorDesired = MapElementCollisionBehavior.RemainVisible;
				Uri picPath = new Uri("ms-appx:///Assets/bar.png");
				switch (point.Type)
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
					case "Visit":
						picPath = new Uri("ms-appx:///Assets/visit.png");
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
		/// Clear POI
		/// </summary>
		private void clearPOI() { mainMap.MapElements.Clear(); }

		/// <summary>
		/// Open the editor for a point
		/// </summary>
		/// <param name="point"></param>
		private void EditPointUI(MapPoint point)
		{
			HideEditUI(false);
			/*fill the fields*/
			boxName.Text = point.Name;
			boxDesc.Text = point.Description;
			latBox.Text = point.Latitude.ToString();
			LonBox.Text = point.Longitude.ToString();
			streetBox.Text = point.Street;
			townBox.Text = point.Town;
			switch (point.Type)
			{
				case "Bar":
					typeSelect.SelectedIndex = 0;
					break;
				case "Club":
					typeSelect.SelectedIndex = 1;
					break;
				case "Restaurant":
					typeSelect.SelectedIndex = 2;
					break;
				case "Visit":
					typeSelect.SelectedIndex = 3;
					break;
				default:
					typeSelect.SelectedIndex = 0;
					break;
			}
			SelectedPoint = point;
			latBox.TextChanged += latBox_TextChanged;
			LonBox.TextChanged += latBox_TextChanged;
			streetBox.TextChanged += streetBox_TextChanged;
			townBox.TextChanged += streetBox_TextChanged;
		}

		/// <summary>
		/// Set the grid view of points
		/// </summary>
		private void setGridView()
		{
			pointGrid.Children.Clear();
			var i = 0;
			foreach (MapPoint point in ListMapPoint)
			{
				var rd = new RowDefinition();
				pointGrid.RowDefinitions.Add(rd);

				if (i % 2 == 0)
				{
					Rectangle background = new Rectangle();
					background.Fill = new SolidColorBrush(Color.FromArgb(Convert.ToByte(90), Convert.ToByte(230), Convert.ToByte(230), Convert.ToByte(230)));
					Grid.SetRow(background, i);
					Grid.SetColumn(background, 0);
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
		/// Given a parameter, updates the List and POI and map
		/// </summary>
		/// <param name="type"></param>
		private void selectItemInView(string type)
		{
			ListMapPoint = GetPointsByType(type);
			setPOI();
			setGridView();
		}

		/// <summary>
		/// Hide the UI that allows user to edit fields
		/// </summary>
		/// <param name="state"></param>
		private void HideEditUI(bool state)
		{
			UpdateButton.Content = "Update";

			if (state)
			{
				Grid.SetColumnSpan(scrollview, 2);
				latBox.TextChanged -= latBox_TextChanged;
				LonBox.TextChanged -= latBox_TextChanged;
				streetBox.TextChanged -= streetBox_TextChanged;
				townBox.TextChanged -= streetBox_TextChanged;
			}
			else
			{
				latBox.TextChanged -= latBox_TextChanged;
				LonBox.TextChanged -= latBox_TextChanged;
				streetBox.TextChanged -= streetBox_TextChanged;
				townBox.TextChanged -= streetBox_TextChanged;
				Grid.SetColumnSpan(scrollview, 1);
			}


			ListMapPoint = GetAllPoints();
			setPOI();
			setGridView();
		}

		#region map
		/// <summary>
		/// Center the map to a given MapPoint
		/// </summary>
		/// <param name="point"></param>
		private async void CenterMap(MapPoint point)
		{
			Geopoint loc = new Geopoint(new BasicGeoposition() { Latitude = point.Latitude, Longitude = point.Longitude });
			await mainMap.TrySetViewAsync(loc, 19, 0, 0, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);
		}

		/// <summary>
		/// Center the map to a given Point (Latitude = X Longitude = Y)
		/// </summary>
		/// <param name="point"></param>
		private async void CenterMap(Point point)
		{
			Geopoint loc = new Geopoint(new BasicGeoposition() { Latitude = point.X, Longitude = point.Y });
			await mainMap.TrySetViewAsync(loc, 19, 0, 0, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);
		}

		/// <summary>
		/// Event called when user click on a map element
		/// </summary>
		/// <remarks>not implemented yet</remarks>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void mainMap_MapElementClick(MapControl sender, MapElementClickEventArgs args)
		{
			MapPoint point = GetPointById(args.MapElements[0].ZIndex);
			CenterMap(point);
			UpdateButton.Content = "Update";
			EditPointUI(point);
		}

		/// <summary>
		/// Event called when user click on the map
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void mainMap_Tapped(MapControl sender, MapInputEventArgs e)
		{
			HideEditUI(false);
			SelectedPoint = new MapPoint();
			var tappedGeoPosition = e.Location.Position;
			setTmpPoint(tappedGeoPosition.Latitude, tappedGeoPosition.Longitude);
			SelectedPoint.Latitude = tappedGeoPosition.Latitude;
			SelectedPoint.Longitude = tappedGeoPosition.Longitude;
			latBox.Text = tappedGeoPosition.Latitude.ToString();
			LonBox.Text = tappedGeoPosition.Longitude.ToString();
			string s = await GetAdressFromPoint(new Point(SelectedPoint.Latitude, SelectedPoint.Longitude));
			string[] address = s.Split(',');
			streetBox.Text = SelectedPoint.Street = address[0];
			townBox.Text = SelectedPoint.Town = address[1] + "," + address[2];
			UpdateButton.Content = "Create";
			latBox.TextChanged += latBox_TextChanged;
			LonBox.TextChanged += latBox_TextChanged;
			streetBox.TextChanged += streetBox_TextChanged;
			townBox.TextChanged += streetBox_TextChanged;
		}

		/// <summary>
		/// Set a mappin icon where the user has touch the map
		/// </summary>
		/// <param name="latitude"></param>
		/// <param name="longitude"></param>
		private void setTmpPoint(double latitude, double longitude)
		{
			if (tmpIcon == null)
				tmpIcon = new MapIcon();
			else
				mainMap.MapElements.Remove(tmpIcon);
			tmpIcon.Location = new Geopoint(new BasicGeoposition() { Latitude = latitude, Longitude = longitude });
			tmpIcon.ZIndex = 100;
			tmpIcon.NormalizedAnchorPoint = new Point(0.5, 1.0);
			tmpIcon.CollisionBehaviorDesired = MapElementCollisionBehavior.RemainVisible;
			tmpIcon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/mapPin.png"));
			mainMap.MapElements.Add(tmpIcon);
		}
		#endregion

		#region eventflyout
		/// <summary>
		/// Event called when user want to select restaurants
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FlyoutSelectRest(object sender, RoutedEventArgs e)
		{
			selectItemInView("Restaurant");
		}

		/// <summary>
		/// Event called wen user want to select bar
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FlyoutSelectBar(object sender, RoutedEventArgs e)
		{
			selectItemInView("Bar");
		}

		/// <summary>
		/// Event called when user want to select club
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FlyoutSelectClub(object sender, RoutedEventArgs e)
		{
			selectItemInView("Club");
		}

		/// <summary>
		/// Event called when user want to select All
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FlyoutSelectAll(object sender, RoutedEventArgs e)
		{
			selectItemInView("All");
		}
		#endregion

		#region comboBoxEvent

		#endregion

		#region buttonEvent
		/// <summary>
		/// Event called when user presses DeleteButton
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			DeleteMapPoint(SelectedPoint);
			HideEditUI(true);
		}

		/// <summary>
		/// Event called when user presses UpdateButton
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			MessageDialog error = null;
			/*check if user has miss something*/
			if (boxName.Text == "")
				error = new MessageDialog("Please enter a Name");
			if (boxDesc.Text == "" && error == null)
				error = new MessageDialog("Please enter a Description");
			if (((latBox.Text == "" || LonBox.Text == "")
				&& (streetBox.Text == "" || townBox.Text == ""))
				&& error == null)
				error = new MessageDialog("Please enter a Position");
			if (error != null)
			{
				await error.ShowAsync();
				return;
			}

			UpdateCall();
		}

		private async void UpdateCall()
		{
			Point point = new Point(0, 0);
			string address = "";

			if (latBox.Text == "")
				point = await GetPointFromAddress(streetBox.Text + ", " + townBox);
			else
			{
				SelectedPoint.Latitude = Convert.ToDouble(latBox.Text);
				SelectedPoint.Longitude = Convert.ToDouble(LonBox.Text);
				address = await GetAdressFromPoint(new Point(SelectedPoint.Latitude, SelectedPoint.Longitude));
			}

			SelectedPoint = new MapPoint();
			SelectedPoint.Name = boxName.Text;
			SelectedPoint.Description = boxDesc.Text;

			if (point.X == 0 || point.Y == 0)
			{
				SelectedPoint.Latitude = Convert.ToDouble(latBox.Text);
				SelectedPoint.Longitude = Convert.ToDouble(LonBox.Text);
			}
			else
			{
				SelectedPoint.Latitude = point.X;
				SelectedPoint.Longitude = point.Y;
			}

			if (address == "")
			{
				SelectedPoint.Street = streetBox.Text;
				SelectedPoint.Town = townBox.Text;
			}
			else
			{
				string[] parse = address.Split(',');
				SelectedPoint.Street = parse[0];
				SelectedPoint.Town = parse[1] + ',' + parse[2];
			}

			switch (typeSelect.SelectedIndex)
			{
				case 0:
					SelectedPoint.Type = "Bar";
					break;
				case 1:
					SelectedPoint.Type = "Club";
					break;
				case 2:
					SelectedPoint.Type = "Restaurant";
					break;
				case 3:
					SelectedPoint.Type = "Visit";
					break;
				default:
					SelectedPoint.Type = "Visit";
					break;
			}

			if (UpdateButton.Content.ToString() == "Create")
			{
				SelectedPoint.Id = nextId++;
				mainMap.MapElements.Remove(tmpIcon);
			}
			SaveMapPoint(SelectedPoint);
			HideEditUI(true);
		}

		/// <summary>
		/// Event called when user presses Add Button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			HideEditUI(false);
			latBox.Text = "";
			LonBox.Text = "";
			boxDesc.Text = "";
			boxName.Text = "";
			typeSelect.SelectedIndex = 0;
			UpdateButton.Content = "Create";
		}

		/// <summary>
		/// Event called when user presses Hide Button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void HideButton_Click(object sender, RoutedEventArgs e)
		{
			HideEditUI(true);
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
			var select = sender as Button;
			var point = GetPointById(Convert.ToInt32(select.Tag));
			UpdateButton.Content = "Update";
			EditPointUI(point);
		}
		#endregion

		#region toggleButtonEvent

		/// <summary>
		/// Event called when user want to changes map view
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AppBarToggleButton_Checked(object sender, RoutedEventArgs e)
		{
			var t = sender as AppBarToggleButton;
			if (t.IsChecked == true)
			{
				if (mainMap.Is3DSupported)
					mainMap.Style = MapStyle.Aerial3D;
				else
					toggle.IsChecked = false;
			}
			else
				mainMap.Style = MapStyle.Road;
		}

		/// <summary>
		/// Event called when user want to be followed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void followToggle_Click(object sender, RoutedEventArgs e)
		{
			var toggle = sender as ToggleButton;
			follow = toggle.IsChecked;
		}

		/// <summary>
		/// Event called when user want to display traffic informatiosn
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void trafficToggle_Click(object sender, RoutedEventArgs e)
		{
			var toggle = sender as ToggleButton;
			if (toggle.IsChecked == true)
				mainMap.TrafficFlowVisible = true;
			else
				mainMap.TrafficFlowVisible = false;
		}
		#endregion

		#region scrollviewverEvent
		/// <summary>
		/// Called when user scroll to detect if bottom is reached
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
    		var verticalOffset = yelp_scrollviewer.VerticalOffset;
    		var maxVerticalOffset = yelp_scrollviewer.ScrollableHeight; //sv.ExtentHeight - sv.ViewportHeight;

    		if (maxVerticalOffset < 0 || verticalOffset == maxVerticalOffset)// Scrolled to bottom
			{
				numberOfQueryDone++;
				await loadYelp(numberOfQueryDone);
			}
			else// Not scrolled to bottom
			{
        		
    		}
		}
		#endregion

		#region textBoxEvent
		/// <summary>
		/// Reset latitude and longitude box 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void streetBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			latBox.TextChanged -= latBox_TextChanged;
			LonBox.TextChanged -= latBox_TextChanged;
			latBox.Text = "";
			LonBox.Text = "";
			latBox.TextChanged += latBox_TextChanged;
			LonBox.TextChanged += latBox_TextChanged;
		}

		/// <summary>
		/// Reset street and town box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void latBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			streetBox.TextChanged -= streetBox_TextChanged;
			townBox.TextChanged -= streetBox_TextChanged;
			streetBox.Text = "";
			townBox.Text = "";
			streetBox.TextChanged += streetBox_TextChanged;
			townBox.TextChanged += streetBox_TextChanged;
		}
		#endregion

		#region Pivot
		private void rootPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Pivot a = sender as Pivot;
			switch (a.SelectedIndex)
			{
				case 0:
					SetCommandBarMap();
					break;
				case 1:
					SetCommandBarYelp();
					break;
				case 2:
					break;
			}
		}

		private void SetCommandBarMap()
		{
			//<AppBarToggleButton x:Name="toggle" Label="Aerial view" Icon="Globe" Checked="AppBarToggleButton_Checked" Unchecked="AppBarToggleButton_Checked" />
			//<AppBarToggleButton x:Name="followToggle" Label="Follow me" Click="followToggle_Click" IsChecked="False" Icon="Bullets">
			//	<FontIcon Glyph="&#xE7E7;"/>
			//</AppBarToggleButton>
			//<AppBarToggleButton x:Name="trafficToggle" Label="Traffic" Click="trafficToggle_Click" IsChecked="False">
			//	<FontIcon Glyph="&#xE7EC;" />
			//</AppBarToggleButton>
			//<AppBarSeparator/>
			//<AppBarButton Label="Filter by" Icon="Filter" >
			//	<AppBarButton.Flyout>
			//		<MenuFlyout>
			//			<MenuFlyoutItem Click="FlyoutSelectBar" Text="Bar" />
			//			<MenuFlyoutItem Click="FlyoutSelectClub" Text="Club" />
			//			<MenuFlyoutItem Click="FlyoutSelectRest" Text="Restaurant" />
			//			<MenuFlyoutItem Click="FlyoutSelectAll" Text="All" />
			//		</MenuFlyout>
			//	</AppBarButton.Flyout>
			//</AppBarButton>
		}

		private void SetCommandBarYelp()
		{
			
		}

		#endregion

		#region foursquare

		/*
		https://api.foursquare.com/v2/venues/search
		?client_id=CLIENT_ID
		&client_secret=CLIENT_SECRET
		&v=20130815
		&ll=40.7,-74
		&query=sushi
		*/

		/// <summary>
		/// Internal Object repressenting a Venue
		/// </summary>
		internal class Venue
		{
			/// <summary>
			/// Gets or sets the identifier.
			/// </summary>
			[PrimaryKey]
			public string Id { get; set; }

			/// <summary>
			/// Gets or sets the name.
			/// </summary>
			[MaxLength(128)]
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
			public string Categorie { get; set; }

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
		/// Check if there is an internet connection
		/// </summary>
		/// <returns>nullable bool</returns>
		public static bool IsInternet()
		{
			ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
			bool internet = connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
			return internet;
		}

		/// <summary>
		/// Take care to call great functions to load Foursquare
		/// </summary>
		/// <remarks>Need to be done more elegant</remarks>
		/// <returns>True if it performed correct</returns>
		private async Task<bool?> LoadFoursquare()
		{
			if(IsInternet() == false)
			{
				var d = new MessageDialog("No internet access", "Sorry");
				return null;
			}
			JObject json = new JObject();
			if (mapIconME == null)
				return false;
			json = await GetJson(1000,"",50,0,"","",false,false,false,0,mapIconME.Location.Position.Latitude,mapIconME.Location.Position.Longitude);
			ring.Visibility = Visibility.Collapsed;

			UpdateFoursquareList(json);
			DisplayFoursquareList();
			return true;
		}

		private void DisplayFoursquareList()
		{
			//foursquareGrid.Children.Clear();
			var i = 0;
			foreach (Venue venue in ListVenue)
			{
				var rd = new RowDefinition();
				//foursquareGrid.RowDefinitions.Add(rd);

				if (i % 2 == 0)
				{
					Rectangle background = new Rectangle();
					background.Fill = new SolidColorBrush(Color.FromArgb(Convert.ToByte(90), Convert.ToByte(230), Convert.ToByte(230), Convert.ToByte(230)));
					Grid.SetRow(background, i);
					Grid.SetColumn(background, 0);
					Grid.SetColumnSpan(background, 5);
					//foursquareGrid.Children.Add(background);
				}

				TextBlock tbId = new TextBlock()
				{
					Text = venue.Id.ToString(),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(6)
				};

				Grid.SetRow(tbId, i);
				Grid.SetColumn(tbId, 0);
				//foursquareGrid.Children.Add(tbId);

				TextBlock tbName = new TextBlock()
				{
					Text = venue.Name,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(6)
				};

				Grid.SetRow(tbName, i);
				Grid.SetColumn(tbName, 1);
				//foursquareGrid.Children.Add(tbName);

				TextBlock tbDescr = new TextBlock()
				{
					Text = venue.Description,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(6)
				};

				Grid.SetRow(tbDescr, i);
				Grid.SetColumn(tbDescr, 2);
				//foursquareGrid.Children.Add(tbDescr);

				TextBlock tbStreet = new TextBlock()
				{
					Text = venue.Street + "\n" + venue.Town,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(6)
				};

				Grid.SetRow(tbStreet, i);
				Grid.SetColumn(tbStreet, 3);
				//foursquareGrid.Children.Add(tbStreet);

				ToggleButton toggleButton = new ToggleButton()
				{
					Content = new FontIcon()
					{
						FontFamily = new FontFamily("Segoe MDL2 Assets"),
						Glyph = "\uE1D2"
					},
					IsChecked = false,
					Padding = new Thickness(6),
					Background = new SolidColorBrush(Colors.Transparent),
					Tag = venue.Id
				};
				

				Grid.SetRow(toggleButton, i);
				Grid.SetColumn(toggleButton, 4);
				//foursquareGrid.Children.Add(toggleButton);

				i++;
			}
		}

		/// <summary>
		/// Update the List of foursquare point
		/// </summary>
		/// <param name="json"></param>
		private async void UpdateFoursquareList(JObject json)
		{
			var item = json["response"]["groups"][0]["items"];
			int itemCount = item.Count();

			for (int i = 0; i < itemCount; i++)
			{
				try
				{
					Venue v = new Venue();
					var venue = json["response"]["groups"][0]["items"][i];
					v.Id = venue["venue"]["id"].ToString();
					v.Name = venue["venue"]["name"].ToString();
					string tmp = venue["venue"]["location"]["lat"].ToString();
					v.Latitude = Convert.ToDouble(tmp);
					tmp = venue["venue"]["location"]["lng"].ToString();
					v.Longitude = Convert.ToDouble(tmp);
					v.Description = venue["venue"]["categories"][0]["name"].ToString();
					string address = await GetAdressFromPoint(new Point(v.Latitude, v.Longitude));
					string[] add = address.Split(',');
					v.Street = add[0];
					v.Town = add[1] + ", " + add[2];
					ListVenue.Add(v);
					
				}
				catch (Exception e)
				{
					e.ToString();
				}
			}
		}

		/// <summary>
		/// Get the query of server from different parameters
		/// </summary>
		/// <param name="Radius">Radius research zone</param>
		/// <param name="Section">Type de résultats [food, drinks, coffee, shops, arts, outdoors, sights, trending or specials, nextVenues]</param>
		/// <param name="NbItem">Number a item requested</param>
		/// <param name="Offset">Offset for the item requested</param>
		/// <param name="Time">[Morning, Lunch, Dinner, Night]</param>
		/// <param name="Day">[Lundi, Mardi, Mercredi, Jeudi, Vendredi, Samedi, Dimanche]</param>
		/// <param name="Photo">Retrieve picture too</param>
		/// <param name="IsOpenNow">Check only if open</param>
		/// <param name="sortByDistance">Sort things by distance from point</param>
		/// <param name="Price">Price from low to high [1,2,3,4]</param>
		/// <param name="Latitude">double representing latitude</param>
		/// <param name="Longitude">double representing lonitude</param>
		/// <returns>JsonObject</returns>
		private async Task<JObject> GetJson(int Radius = 0, string Section = "trending", int NbItem = 20, int Offset = 0, string Time = "", string Day = "", 
										bool Photo = false, bool IsOpenNow = false, bool sortByDistance = false, int Price = 0, double Latitude = 0, double Longitude = 0)
		{
			string web = @"https://api.foursquare.com/v2/venues/explore?client_id=" + FOURSQUARECLIENTID + @"&client_secret=" + FOURSQUARESECRETID;
			web += @"&v=20130815";
			web += @"&ll=" + Latitude + "," + Longitude;
			if (Radius > 0)
				web += @"&radius=" + Radius;
			if (Section != "")
				web += @"&section=" + Section;
			if (NbItem > 0)
				web += @"&limit=" + NbItem;
			if (Offset > 0)
				web += @"&offset=" + Offset;
			if(Time != "")
			{
				//TODO
				;
			}
			if(Day != "")
			{
				//TODO
				;
			}
			if (Photo)
				web += @"&venuePhoto=1";
			if (IsOpenNow)
				web += @"&openNow=1";
			if (sortByDistance)
				web += @"&sortByDistance=1";
			if (Price > 1)
				web += @"&price=" + Price;
				
			var uri = new Uri(web);

			var httpClient = new HttpClient();
			var content = await httpClient.GetStringAsync(uri);
			return await Task.Run(() => JObject.Parse(content));
		}

		#endregion

	}
}
