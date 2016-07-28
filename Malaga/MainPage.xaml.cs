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
using Windows.UI.Core;

namespace Malaga
{

	/*36.718715, -4.421998 la taberna del pinxto larios*/
	/// <summary>
	/// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		MapIcon mapIconME = null;
		Geolocator myPosition = null;
		MapPoint SelectedPoint = null;
		#region Database
		private static string dbPath = string.Empty;
		List<MapPoint> ListMapPoint = null;
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

		#endregion

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
					myPosition = new Geolocator { ReportInterval = 2000 };
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

		private async void MyPosition_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
		{
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				setMyPosition(args.Position);
			});

		}

		private void setMyPosition(Geoposition position)
		{
			if (mapIconME == null)
				mapIconME = new MapIcon();
			else
				mainMap.MapElements.Remove(mapIconME);
			mapIconME.Location = new Geopoint(new BasicGeoposition()
			{ Latitude = position.Coordinate.Point.Position.Latitude, Longitude = position.Coordinate.Point.Position.Longitude });
			mapIconME.NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 1.0);
			mapIconME.ZIndex = 100;
			mapIconME.CollisionBehaviorDesired = MapElementCollisionBehavior.RemainVisible;
			mapIconME.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/loc.png"));
			mainMap.MapElements.Add(mapIconME);
		}

		/// <summary>
		/// Set POI (Point Of Interest) on the map
		/// </summary>
		/// <remarks>Use the ZIndex to mark the Id of the point</remarks>
		private void setPOI()
		{
			clearPOI();
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
			}
			typeSelect.SelectionChanged += typeSelect_SelectionChanged;
			SelectedPoint = point;
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
			var select = sender as Button;
			var point = GetPointById(Convert.ToInt32(select.Tag));
			EditPointUI(point);
		}

		/// <summary>
		/// Event called when user click on a map element
		/// </summary>
		/// <remarks>not implemented yet</remarks>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void mainMap_MapElementClick(MapControl sender, MapElementClickEventArgs args)
		{
			MapPoint point = GetPointById(args.MapElements[0].ZIndex);
			CenterMap(point);
			EditPointUI(point);
		}

		/// <summary>
		/// Event called when user click on the map
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void mainMap_Tapped(object sender, TappedRoutedEventArgs e)
		{
			//Geopoint geoPt = this.mainMap.Layers[0].ScreenToGeoPoint(e.GetPosition(this.mainMap));
		}

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

		private void typeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var combo = sender as ComboBoxItem;
			string type = combo.Content.ToString();
			SelectedPoint.Type = type;
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			DeleteMapPoint(SelectedPoint);
			HideEditUI(true);
		}

		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			SelectedPoint.Name = boxName.Text;
			SelectedPoint.Description = boxDesc.Text;
			SelectedPoint.Latitude = Convert.ToDouble(latBox.Text);
			SelectedPoint.Longitude = Convert.ToDouble(LonBox.Text);
			SaveMapPoint(SelectedPoint);
			HideEditUI(true);
		}

		private void HideButton_Click(object sender, RoutedEventArgs e)
		{
			HideEditUI(true);
		}

		private void HideEditUI(bool state)
		{
			typeSelect.SelectionChanged -= typeSelect_SelectionChanged;
			if (state)
				Grid.SetColumnSpan(scrollview, 2);
			else
				Grid.SetColumnSpan(scrollview, 1);

			ListMapPoint = GetAllPoints();
			setPOI();
			setGridView();
		}


		#region coordAdressConverter
		private string getAdressFromPoint(Point point)
		{
			string address = String.Empty;
			/*https://msdn.microsoft.com/windows/uwp/maps-and-location/geocoding */

			return address;
		}

		private Point getPointFromAddress(string adress)
		{
			Point point = new Point(0.00, 0.00);
			return point;
		}
		#endregion

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
	}
}
