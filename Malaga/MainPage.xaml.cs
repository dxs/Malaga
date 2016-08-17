using Newtonsoft.Json.Linq;
using SQLite.Net;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.WinRT;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	/// 
	/// </summary>
    public sealed partial class MainPage : Page
    {
		Database DB;
		DispatcherTimer yelpTimer, settingsTimer;
		Yelp yelp = null;
		List<Business> selectedItems;
		ObservableCollection<Categories> listOfCategories;
		public ObservableCollection<Categories> ListOfCategories { get { return listOfCategories; } }
		ObservableCollection<MapPoint> collectionMapPoint;
		ObservableCollection<MapPoint> CollectionMapPoint { get { return collectionMapPoint; } }
		ObservableCollection<Business> collectionBusinessFood;
		ObservableCollection<Business> collectionBusinessDrinks;
		ObservableCollection<Business> collectionBusinessRestaurant;
		ObservableCollection<Business> collectionBusinessMuseum;
		ObservableCollection<Business> collectionBusinessPub;
		ObservableCollection<Business> collectionBusinessShopping;
		ObservableCollection<Business> collectionBusinessLocal;
		ObservableCollection<Business> collectionBusinessIceCream;
		ObservableCollection<Business> collectionBusinessSport;
		ObservableCollection<Business> collectionBusinessBeauty;
		ObservableCollection<Business> collectionBusinessEducation;
		ObservableCollection<ObservableCollection<Business>> collectionOfCollection;
		public ObservableCollection<ObservableCollection<Business>> CollectionOfCollection { get { return collectionOfCollection; } }
		List<int> numberOfQuery = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

		MapPoint SelectedPoint;
		MapIcon mapIconMe = null, tmpIcon = null;
		bool? follow;

		/// <summary>
		/// Point d'entrée
		/// </summary>
		public MainPage()
		{
			this.InitializeComponent();
			DB = new Database(ApplicationData.Current.LocalFolder.Path);
			SelectedPoint = new MapPoint();
			Setup();
			
			if (Settings.FirstBoot)
				DisplayFirstBoot();
			Settings.ReadSettings();
			SetupYelpCollection(Settings.YelpCode);
			setMap();
			yelpTimer.Start();
			settingsTimer.Start();
		}

		/// <summary>
		/// Setup de base pour le lancement de l'app
		/// </summary>
		private async void Setup()
		{
			await DB.setDB();
			collectionMapPoint = DB.GetAllPoints();
			setPOI();

			yelpTimer = new DispatcherTimer()
			{
				Interval = new TimeSpan(0, 0, 1)
			};
			yelpTimer.Tick += YelpTimer_Tick;
			settingsTimer = new DispatcherTimer()
			{
				Interval = new TimeSpan(0, 0, 20)
			};
			settingsTimer.Tick += SettingsTimer_Tick;

			collectionOfCollection = new ObservableCollection<ObservableCollection<Business>>();
			collectionBusinessFood = new ObservableCollection<Business>();
			collectionBusinessDrinks = new ObservableCollection<Business>();
			collectionBusinessRestaurant = new ObservableCollection<Business>();
			collectionBusinessMuseum = new ObservableCollection<Business>();
			collectionBusinessPub = new ObservableCollection<Business>();
			collectionBusinessShopping = new ObservableCollection<Business>();
			collectionBusinessLocal = new ObservableCollection<Business>();
			collectionBusinessSport = new ObservableCollection<Business>();
			collectionBusinessIceCream = new ObservableCollection<Business>();
			collectionBusinessBeauty = new ObservableCollection<Business>();
			collectionBusinessEducation = new ObservableCollection<Business>();
		}

		/// <summary>
		/// Timer called to start getting yelp data
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void YelpTimer_Tick(object sender, object e)
		{
			yelpTimer.Stop();
			bool? isConnected = false;
			isConnected = LoadYelp();
			if(isConnected == false)
			{
				yelpTimer.Interval = new TimeSpan(0, 0, 1);
				yelpTimer.Start();
			}
			if(isConnected == null)
			{
				yelpTimer.Interval = new TimeSpan(0, 0, 10);
				yelpTimer.Start();
			}
		 }

		/// <summary>
		/// Given a parameter, updates the List and POI and map
		/// </summary>
		/// <param name="type"></param>
		private void selectItemInView(string type)
		{
			collectionMapPoint = DB.GetPointsByType(type);
			setPOI();
		}

		/// <summary>
		/// Hide the UI that allows user to edit fields
		/// </summary>
		/// <param name="state">Closed of open</param>
		private void HideEditUI(bool state)
		{
			UpdateButton.Content = "Update";
			latBox.TextChanged -= latBox_TextChanged;
			LonBox.TextChanged -= latBox_TextChanged;
			streetBox.TextChanged -= streetBox_TextChanged;
			townBox.TextChanged -= streetBox_TextChanged;
			if (state)
			{
				EditScrollView.Visibility = Visibility.Collapsed;
				scrollview.Visibility = Visibility.Visible;
			}
			else
			{
				EditScrollView.Visibility = Visibility.Visible;
				scrollview.Visibility = Visibility.Collapsed;
			}

			collectionMapPoint = DB.GetAllPoints();
			setPOI();
		}

		#region YELP

		/// <summary>
		/// Perform Task to load Yelp
		/// </summary>
		/// <param name="queryNb"></param>
		/// <param name="query"></param>
		/// <returns></returns>
		private bool LoadYelp(int queryNb = 0, string query = "Food")
		{ 
			ring.Visibility = Visibility.Visible;
			if (mapIconMe == null)
				return false;
			if (yelp == null)
				yelp = new Yelp();
			Point p = new Point()
			{
				X = mapIconMe.Location.Position.Latitude,
				Y = mapIconMe.Location.Position.Longitude
			};
			int offset = 20;
			FillYelpCollection(p, offset);
			ring.Visibility = Visibility.Collapsed;
			yelp.GetAllBusiness();
			this.Bindings.Update();
			return true;
		}

		private async void FillYelpCollection(Point location, int offset)
		{
			string town = (await DB.GetAdressFromPoint(location)).Split(',')[2];
			List<char> key = new List<char>();
			foreach (char c in Settings.YelpCode)
				key.Add(c);

			listOfCategories = new ObservableCollection<Categories>();
			listOfCategories.Add(new Categories() { Name = "Food" });
			listOfCategories.Add(new Categories() { Name = "Drinks" });
			listOfCategories.Add(new Categories() { Name = "Restaurant" });
			listOfCategories.Add(new Categories() { Name = "Museum" });
			listOfCategories.Add(new Categories() { Name = "Pub" });
			listOfCategories.Add(new Categories() { Name = "Shopping" });
			listOfCategories.Add(new Categories() { Name = "Local Flavour" });
			listOfCategories.Add(new Categories() { Name = "Ice cream" });
			listOfCategories.Add(new Categories() { Name = "Sport" });
			listOfCategories.Add(new Categories() { Name = "Beauty" });
			listOfCategories.Add(new Categories() { Name = "Education" });
			int i = 0;
			foreach(ObservableCollection<Business> collection in collectionOfCollection)
			{
				if (i >= key.Count - 1)
					return;
				while (key[i] != '1')
					i++;
				await yelp.GetData(location, listOfCategories[i].Name, 100000, offset, 0, 0, town);
				ObservableCollection<Business> tmp = yelp.GetAllBusiness();
				foreach (Business b in tmp)
					collection.Add(b);
				yelp.ClearCollection();
				i++;
				Bindings.Update();
			}
		}

		private void SetupYelpCollection(string yelpCode)
		{
			if (yelpCode[0] == '1') collectionOfCollection.Add(collectionBusinessFood);
			if (yelpCode[1] == '1') collectionOfCollection.Add(collectionBusinessDrinks);
			if (yelpCode[2] == '1') collectionOfCollection.Add(collectionBusinessRestaurant);
			if (yelpCode[3] == '1') collectionOfCollection.Add(collectionBusinessMuseum);
			if (yelpCode[4] == '1') collectionOfCollection.Add(collectionBusinessPub);
			if (yelpCode[5] == '1') collectionOfCollection.Add(collectionBusinessShopping);
			if (yelpCode[6] == '1') collectionOfCollection.Add(collectionBusinessLocal);
			if (yelpCode[7] == '1') collectionOfCollection.Add(collectionBusinessIceCream);
			if (yelpCode[8] == '1') collectionOfCollection.Add(collectionBusinessSport);
			if (yelpCode[9] == '1') collectionOfCollection.Add(collectionBusinessBeauty);
			if (yelpCode[10] == '1') collectionOfCollection.Add(collectionBusinessEducation);
		}

		#endregion

		#region MapFunctions

		/// <summary>
		/// Set a MapElement on MapControl representing the user position
		/// </summary>
		/// <param name="position"></param>
		private void setMyPosition(Geoposition position)
		{
			if (mapIconMe == null)
				mapIconMe = new MapIcon();

			mapIconMe.Location = new Geopoint(new BasicGeoposition()
			{ Latitude = position.Coordinate.Point.Position.Latitude, Longitude = position.Coordinate.Point.Position.Longitude });
			mapIconMe.NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 1.0);
			mapIconMe.ZIndex = 100;
			mapIconMe.CollisionBehaviorDesired = MapElementCollisionBehavior.RemainVisible;
			mapIconMe.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/loc.png"));
			mainMap.MapElements.Remove(mapIconMe);
			mainMap.MapElements.Add(mapIconMe);
		}

		/// <summary>
		/// Set POI (Point Of Interest) on the map
		/// </summary>
		/// <remarks>Use the ZIndex to mark the Id of the point</remarks>
		private void setPOI()
		{
			clearPOI();
			if (collectionMapPoint == null)
				return;

			foreach (MapPoint point in collectionMapPoint)
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
		/// Do stuff when the map finished loading
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void mainMap_Loaded(object sender, RoutedEventArgs e)
		{
			var timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 1, 0) };
			timer.Tick += (tick, args) =>
			{
				timer.Stop();
				CenterMap(Settings.LastPosition);
			};
			timer.Start();
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
		/// Center the map to a given MapPoint
		/// </summary>
		/// <param name="point"></param>
		private async Task CenterMap(MapPoint point)
		{
			if (point == null)
				return;
			Geopoint loc = new Geopoint(new BasicGeoposition() { Latitude = point.Latitude, Longitude = point.Longitude });
			await mainMap.TrySetViewAsync(loc, 19, 0, 0, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);
		}

		/// <summary>
		/// Center the map to a given Point (Latitude = X Longitude = Y)
		/// </summary>
		/// <param name="point"></param>
		private async void CenterMap(Point point)
		{
			if (point == null)
				return;
			Geopoint loc = new Geopoint(new BasicGeoposition() { Latitude = point.X, Longitude = point.Y });
			await mainMap.TrySetViewAsync(loc, 19, 0, 0, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);
		}

		/// <summary>
		/// Event called when user click on a map element
		/// </summary>
		/// <remarks>not implemented yet</remarks>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private async void mainMap_MapElementClick(MapControl sender, MapElementClickEventArgs args)
		{
			MapPoint point = DB.GetPointById(args.MapElements[0].ZIndex);
			if (point == null)
				return;
			UpdateButton.Content = "Update";
			EditPointUI(point);
			mainMap.MapElements.Remove(tmpIcon);
			await CenterMap(point);
			mainMap.MapElements.Remove(tmpIcon);
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
			string s = await DB.GetAdressFromPoint(new Point(SelectedPoint.Latitude, SelectedPoint.Longitude));
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

		#region GetPosition

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
			Database.CurrentLatitude = p.X;
			Database.CurrentLongitude = p.Y;
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


		private void FirstBootSaveButton_Click(object sender, RoutedEventArgs e)
		{
			string setting = "";

			if (checkBoxFood.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxDrinks.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxRestaurant.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxMuseum.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxPub.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxShopping.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxLocal.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxIce.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxSport.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxBeauty.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			if (checkBoxEducation.IsChecked == true)
				setting += "1";
			else
				setting += "0";

			Settings.YelpCode = setting;
			SetupYelpCollection(Settings.YelpCode);
			yelpTimer.Start();
			popupChooseCategories.IsOpen = false;
		}

		/// <summary>
		/// Event called when user presses DeleteButton
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			DB.DeleteMapPoint(SelectedPoint);
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
				point = await DB.GetPointFromAddress(streetBox.Text + ", " + townBox);
			else
			{
				SelectedPoint.Latitude = Convert.ToDouble(latBox.Text);
				SelectedPoint.Longitude = Convert.ToDouble(LonBox.Text);
				address = await DB.GetAdressFromPoint(new Point(SelectedPoint.Latitude, SelectedPoint.Longitude));
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
			SelectedPoint.PhotoUrl = "ms-appx:///Assets/barBig.png";
			if (UpdateButton.Content.ToString() == "Create")
			{
				SelectedPoint.Id = Database.nextId++;
				mainMap.MapElements.Remove(tmpIcon);
			}
			DB.SaveMapPoint(SelectedPoint);
			collectionMapPoint = DB.GetAllPoints();
			Bindings.Update();
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
		/// Event called when user press edit button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void EditButton_Click(object sender, RoutedEventArgs e)
		{
			UpdateButton.Content = "Update";
			EditPointUI(SelectedPoint);
		}

		/// <summary>
		/// Event called to store to list
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SaveYelpButton_Click(object sender, RoutedEventArgs e)
		{
			foreach(Business item in selectedItems)
				ConvertBusinessToMapPoint(item);

			SaveYelpButton.Visibility = Visibility.Collapsed;
			collectionMapPoint = DB.GetAllPoints();
			setPOI();
			Bindings.Update();
			rootPivot.SelectedIndex = 0;
		}

		/// <summary>
		/// Convert a Business class to a MapPoint class and Save it
		/// </summary>
		/// <param name="business"></param>
		private async void ConvertBusinessToMapPoint(Business business)
		{
			MapPoint point = await DB.createMapPoint(Database.nextId++, business.Name, business.Description, business.Latitude, business.Longitude, "bar", business.PhotoUrl);
			DB.SaveMapPoint(point);
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
			var t = sender as ToggleButton;
			if (t.IsChecked == true)
			{
				if (mainMap.Is3DSupported)
					mainMap.Style = MapStyle.Aerial3D;
				else
					toggle.IsChecked = false;
			}
			else
			{
				mainMap.Style = MapStyle.Road;
			}
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
		private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			//var HorizontalOffset = yelp_scrollviewer.HorizontalOffset;
			//var maxHorizontalOffset = yelp_scrollviewer.ScrollableWidth; //sv.ExtentHeight - sv.ViewportHeight;

			//if (HorizontalOffset < 0 || HorizontalOffset == maxHorizontalOffset)// Scrolled to bottom
			//	LoadYelp();
			//else// Not scrolled to bottom
			//{
			//}
		}

		/// <summary>
		/// Event called when user press on the grid
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void scrollview_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			HideEditUI(true);
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
					lineTab1.Visibility = Visibility.Visible;
					lineTab2.Visibility = lineTab3.Visibility = Visibility.Collapsed;
					break;
				case 1:
					lineTab2.Visibility = Visibility.Visible;
					lineTab1.Visibility = lineTab3.Visibility = Visibility.Collapsed;
					break;
				case 2:
					lineTab3.Visibility = Visibility;
					lineTab1.Visibility = lineTab2.Visibility = Visibility.Collapsed;
					break;
			}
		}

		#endregion

		#region GridView

		/// <summary>
		/// Event called to show or not the save button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GridView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (selectedItems == null)
				selectedItems = new List<Business>();
			Business read = e.ClickedItem as Business;
			if (selectedItems.Contains(read))
				selectedItems.Remove(read);
			else
				selectedItems.Add(e.ClickedItem as Business);

			if (selectedItems.Count > 0)
				SaveYelpButton.Visibility = Visibility.Visible;
			else
				SaveYelpButton.Visibility = Visibility.Collapsed;
		}

		private async void pointGrid_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			GridView listView = (GridView)sender;
			var a = ((FrameworkElement)e.OriginalSource).DataContext as MapPoint;
			await CenterMap(a);
		}

		private void pointGrid_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
		{
			var g = sender as GridView;
			mapPointGridViewFlyout.ShowAt(g, e.GetPosition(g));
			SelectedPoint = ((FrameworkElement)e.OriginalSource).DataContext as MapPoint;
		}

		#endregion

		#region Settings

		private void SettingsTimer_Tick(object sender, object e)
		{
			WriteSettings();
			this.Bindings.Update();
		}


		private void WriteSettings()
		{
			if(mapIconMe != null)
				Settings.LastPosition = new Point
					(
						mapIconMe.Location.Position.Latitude,
						mapIconMe.Location.Position.Longitude
					);
		}



		private void DisplayFirstBoot()
		{
			yelpTimer.Stop();
			if (Settings.YelpCode[0] == '1')
				checkBoxFood.IsChecked = true;
			else
				checkBoxFood.IsChecked = false;

			if (Settings.YelpCode[1] == '1')
				checkBoxDrinks.IsChecked = true;
			else
				checkBoxDrinks.IsChecked = false;

			if (Settings.YelpCode[2] == '1')
				checkBoxRestaurant.IsChecked = true;
			else
				checkBoxRestaurant.IsChecked = false;

			if (Settings.YelpCode[3] == '1')
				checkBoxMuseum.IsChecked = true;
			else
				checkBoxMuseum.IsChecked = false;

			if (Settings.YelpCode[4] == '1')
				checkBoxPub.IsChecked = true;
			else
				checkBoxPub.IsChecked = false;

			if (Settings.YelpCode[5] == '1')
				checkBoxShopping.IsChecked = true;
			else
				checkBoxShopping.IsChecked = false;

			if (Settings.YelpCode[6] == '1')
				checkBoxLocal.IsChecked = true;
			else
				checkBoxLocal.IsChecked = false;

			if (Settings.YelpCode[7] == '1')
				checkBoxIce.IsChecked = true;
			else
				checkBoxIce.IsChecked = false;

			if (Settings.YelpCode[8] == '1')
				checkBoxSport.IsChecked = true;
			else
				checkBoxSport.IsChecked = false;

			if (Settings.YelpCode[9] == '1')
				checkBoxBeauty.IsChecked = true;
			else
				checkBoxBeauty.IsChecked = false;

			if (Settings.YelpCode[10] == '1')
				checkBoxEducation.IsChecked = true;
			else
				checkBoxEducation.IsChecked = false;

			if (!popupChooseCategories.IsOpen) { popupChooseCategories.IsOpen = true; }
		}

		#endregion

		#region SementicZoom


		private void SemanticZoom_ViewChangeStarted(object sender, SemanticZoomViewChangedEventArgs e)
		{
			if (e.IsSourceZoomedInView == false)
			{
				e.DestinationItem.Item = e.SourceItem.Item;
			}

		}

		#endregion

		#region foursquare

		///*
		//https://api.foursquare.com/v2/venues/search
		//?client_id=CLIENT_ID
		//&client_secret=CLIENT_SECRET
		//&v=20130815
		//&ll=40.7,-74
		//&query=sushi
		//*/

		///// <summary>
		///// Internal Object repressenting a Venue
		///// </summary>
		//internal class Venue
		//{
		//	/// <summary>
		//	/// Gets or sets the identifier.
		//	/// </summary>
		//	[PrimaryKey]
		//	public string Id { get; set; }

		//	/// <summary>
		//	/// Gets or sets the name.
		//	/// </summary>
		//	[MaxLength(128)]
		//	public string Name { get; set; }

		//	/// <summary>
		//	/// Gets or sets the description.
		//	/// </summary>
		//	public string Description { get; set; }

		//	/// <summary>
		//	/// Gets or sets the latitude
		//	/// </summary>
		//	public double Latitude { get; set; }

		//	/// <summary>
		//	/// Gets or sets the Longitude
		//	/// </summary>
		//	public double Longitude { get; set; }

		//	/// <summary>
		//	/// Gets or sets the type
		//	/// </summary>
		//	public string Categorie { get; set; }

		//	/// <summary>
		//	/// Gets or sets the Street address
		//	/// </summary>
		//	public string Street { get; set; }

		//	/// <summary>
		//	/// Gets or sets the Town
		//	/// </summary>
		//	public string Town { get; set; }
		//}

		///// <summary>
		///// Check if there is an internet connection
		///// </summary>
		///// <returns>nullable bool</returns>
		//public static bool IsInternet()
		//{
		//	ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
		//	bool internet = connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
		//	return internet;
		//}

		///// <summary>
		///// Take care to call great functions to load Foursquare
		///// </summary>
		///// <remarks>Need to be done more elegant</remarks>
		///// <returns>True if it performed correct</returns>
		//private async Task<bool?> LoadFoursquare()
		//{
		//	if (IsInternet() == false)
		//	{
		//		var d = new MessageDialog("No internet access", "Sorry");
		//		return null;
		//	}
		//	JObject json = new JObject();
		//	if (mapIconMe == null)
		//		return false;
		//	json = await GetJson(1000, "", 50, 0, "", "", false, false, false, 0, mapIconMe.Location.Position.Latitude, mapIconMe.Location.Position.Longitude);
		//	ring.Visibility = Visibility.Collapsed;

		//	UpdateFoursquareList(json);
		//	DisplayFoursquareList();
		//	return true;
		//}

		//private void DisplayFoursquareList()
		//{
		//	//foursquareGrid.Children.Clear();
		//	var i = 0;
		//	foreach (Venue venue in ListVenue)
		//	{
		//		var rd = new RowDefinition();
		//		//foursquareGrid.RowDefinitions.Add(rd);

		//		if (i % 2 == 0)
		//		{
		//			Rectangle background = new Rectangle();
		//			background.Fill = new SolidColorBrush(Color.FromArgb(Convert.ToByte(90), Convert.ToByte(230), Convert.ToByte(230), Convert.ToByte(230)));
		//			Grid.SetRow(background, i);
		//			Grid.SetColumn(background, 0);
		//			Grid.SetColumnSpan(background, 5);
		//			//foursquareGrid.Children.Add(background);
		//		}

		//		TextBlock tbId = new TextBlock()
		//		{
		//			Text = venue.Id.ToString(),
		//			HorizontalAlignment = HorizontalAlignment.Left,
		//			VerticalAlignment = VerticalAlignment.Center,
		//			TextWrapping = TextWrapping.Wrap,
		//			Margin = new Thickness(6)
		//		};

		//		Grid.SetRow(tbId, i);
		//		Grid.SetColumn(tbId, 0);
		//		//foursquareGrid.Children.Add(tbId);

		//		TextBlock tbName = new TextBlock()
		//		{
		//			Text = venue.Name,
		//			HorizontalAlignment = HorizontalAlignment.Left,
		//			VerticalAlignment = VerticalAlignment.Center,
		//			TextWrapping = TextWrapping.Wrap,
		//			Margin = new Thickness(6)
		//		};

		//		Grid.SetRow(tbName, i);
		//		Grid.SetColumn(tbName, 1);
		//		//foursquareGrid.Children.Add(tbName);

		//		TextBlock tbDescr = new TextBlock()
		//		{
		//			Text = venue.Description,
		//			HorizontalAlignment = HorizontalAlignment.Left,
		//			VerticalAlignment = VerticalAlignment.Center,
		//			TextWrapping = TextWrapping.Wrap,
		//			Margin = new Thickness(6)
		//		};

		//		Grid.SetRow(tbDescr, i);
		//		Grid.SetColumn(tbDescr, 2);
		//		//foursquareGrid.Children.Add(tbDescr);

		//		TextBlock tbStreet = new TextBlock()
		//		{
		//			Text = venue.Street + "\n" + venue.Town,
		//			HorizontalAlignment = HorizontalAlignment.Left,
		//			VerticalAlignment = VerticalAlignment.Center,
		//			TextWrapping = TextWrapping.Wrap,
		//			Margin = new Thickness(6)
		//		};

		//		Grid.SetRow(tbStreet, i);
		//		Grid.SetColumn(tbStreet, 3);
		//		//foursquareGrid.Children.Add(tbStreet);

		//		ToggleButton toggleButton = new ToggleButton()
		//		{
		//			Content = new FontIcon()
		//			{
		//				FontFamily = new FontFamily("Segoe MDL2 Assets"),
		//				Glyph = "\uE1D2"
		//			},
		//			IsChecked = false,
		//			Padding = new Thickness(6),
		//			Background = new SolidColorBrush(Colors.Transparent),
		//			Tag = venue.Id
		//		};


		//		Grid.SetRow(toggleButton, i);
		//		Grid.SetColumn(toggleButton, 4);
		//		//foursquareGrid.Children.Add(toggleButton);

		//		i++;
		//	}
		//}

		///// <summary>
		///// Update the List of foursquare point
		///// </summary>
		///// <param name="json"></param>
		//private async void UpdateFoursquareList(JObject json)
		//{
		//	var item = json["response"]["groups"][0]["items"];
		//	int itemCount = item.Count();

		//	for (int i = 0; i < itemCount; i++)
		//	{
		//		try
		//		{
		//			Venue v = new Venue();
		//			var venue = json["response"]["groups"][0]["items"][i];
		//			v.Id = venue["venue"]["id"].ToString();
		//			v.Name = venue["venue"]["name"].ToString();
		//			string tmp = venue["venue"]["location"]["lat"].ToString();
		//			v.Latitude = Convert.ToDouble(tmp);
		//			tmp = venue["venue"]["location"]["lng"].ToString();
		//			v.Longitude = Convert.ToDouble(tmp);
		//			v.Description = venue["venue"]["categories"][0]["name"].ToString();
		//			string address = await GetAdressFromPoint(new Point(v.Latitude, v.Longitude));
		//			string[] add = address.Split(',');
		//			v.Street = add[0];
		//			v.Town = add[1] + ", " + add[2];
		//			ListVenue.Add(v);

		//		}
		//		catch (Exception e)
		//		{
		//			e.ToString();
		//		}
		//	}
		//}

		///// <summary>
		///// Get the query of server from different parameters
		///// </summary>
		///// <param name="Radius">Radius research zone</param>
		///// <param name="Section">Type de résultats [food, drinks, coffee, shops, arts, outdoors, sights, trending or specials, nextVenues]</param>
		///// <param name="NbItem">Number a item requested</param>
		///// <param name="Offset">Offset for the item requested</param>
		///// <param name="Time">[Morning, Lunch, Dinner, Night]</param>
		///// <param name="Day">[Lundi, Mardi, Mercredi, Jeudi, Vendredi, Samedi, Dimanche]</param>
		///// <param name="Photo">Retrieve picture too</param>
		///// <param name="IsOpenNow">Check only if open</param>
		///// <param name="sortByDistance">Sort things by distance from point</param>
		///// <param name="Price">Price from low to high [1,2,3,4]</param>
		///// <param name="Latitude">double representing latitude</param>
		///// <param name="Longitude">double representing lonitude</param>
		///// <returns>JsonObject</returns>
		//private async Task<JObject> GetJson(int Radius = 0, string Section = "trending", int NbItem = 20, int Offset = 0, string Time = "", string Day = "",
		//								bool Photo = false, bool IsOpenNow = false, bool sortByDistance = false, int Price = 0, double Latitude = 0, double Longitude = 0)
		//{
		//	string web = @"https://api.foursquare.com/v2/venues/explore?client_id=" + FOURSQUARECLIENTID + @"&client_secret=" + FOURSQUARESECRETID;
		//	web += @"&v=20130815";
		//	web += @"&ll=" + Latitude + "," + Longitude;
		//	if (Radius > 0)
		//		web += @"&radius=" + Radius;
		//	if (Section != "")
		//		web += @"&section=" + Section;
		//	if (NbItem > 0)
		//		web += @"&limit=" + NbItem;
		//	if (Offset > 0)
		//		web += @"&offset=" + Offset;
		//	if (Time != "")
		//	{
		//		//TODO
		//		;
		//	}
		//	if (Day != "")
		//	{
		//		//TODO
		//		;
		//	}
		//	if (Photo)
		//		web += @"&venuePhoto=1";
		//	if (IsOpenNow)
		//		web += @"&openNow=1";
		//	if (sortByDistance)
		//		web += @"&sortByDistance=1";
		//	if (Price > 1)
		//		web += @"&price=" + Price;

		//	var uri = new Uri(web);

		//	var httpClient = new HttpClient();
		//	var content = await httpClient.GetStringAsync(uri);
		//	return await Task.Run(() => JObject.Parse(content));
		//}

		#endregion
	}
}