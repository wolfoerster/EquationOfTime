//******************************************************************************************
// Copyright © 2016 Wolfgang Foerster (wolfoerster@gmx.de)
//
// This file is part of the EquationOfTime project which can be found on github.com
//
// EquationOfTime is free software: you can redistribute it and/or modify it under the terms 
// of the GNU General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version.
// 
// EquationOfTime is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
//******************************************************************************************
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using WFTools3D;

namespace EquationOfTime
{
    public enum ViewModes
	{
		FixOverview, FixEclipitcPole, FixNorthPole, FixLocation, FixEarth, FreeLocation, FreeOverview2, FreeOverview3, Freeze
	}

	public partial class SimulatorView : UserControl, INotifyPropertyChanged
	{
		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void FirePropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion INotifyPropertyChanged

		public SimulatorView()
		{
			InitializeComponent();
			DataContext = this;
			Background = null;
			InitScene();

			StartDay = 21;
			StartMonth = 12;
            Speed = 8;

			timer.Tick += TimerTick;
			timer.Interval = TimeSpan.FromMilliseconds(30);
			mainWindow = Application.Current.MainWindow;
		}
		Simulator simulator = new Simulator();
		DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render);
		Brush defBrush = new SolidColorBrush(Color.FromRgb(64, 64, 32));
		Cylinder latitude, meridian, shadowBorder;
		Sphere earth, location;
		Disk horizon, xyPlane;
		Object3D axes;
		Window mainWindow;

		void InitScene()
		{
			//--- use scene.Lighting.DirectionalLight2 only:
			scene.Lighting.LightingGroup.Children.Remove(scene.Lighting.AmbientLight);
			scene.Lighting.LightingGroup.Children.Remove(scene.Lighting.DirectionalLight1);

			scene.ActivateCamera(2);
			scene.Camera.Position = new Point3D(-12, 2, 2);
			scene.Camera.LookAtOrigin();

			scene.ActivateCamera(1);
			scene.Camera.Position = new Point3D(8, -12, 2);
			scene.Camera.LookAtOrigin();

			scene.ActivateCamera(0);
			scene.Camera.FarPlaneDistance = 100;
			scene.Camera.NearPlaneDistance = 0.01;

			InitAxes();
			InitSun();
			InitEarth();
			InitLocation();
			InitPoleAxis();
			InitNorthPole();
			InitLatitude();
			InitHorizon();
			//--- wegen WPF Fehler transparente Objekte immer hinten anfuegen
			InitMeridian();
			InitShadowBorder();
			InitEcliptic();
		}

		void InitAxes()
		{
			if (axes != null)
			{
				scene.Models.Children.Remove(axes);
				axes = null;
			}

			if (!showAxes)
				return;

			axes = new Object3D();
            InitAxis(Math3D.UnitX, Brushes.DarkRed);
            InitAxis(Math3D.UnitY, Brushes.DarkGreen);
            InitAxis(Math3D.UnitZ, Brushes.Blue);
            scene.Models.Children.Add(axes);
		}

        void InitAxis(Vector3D v, Brush brush)
        {
            CreateLine(v, brush, true);
            CreateLine(v, brush, false);
        }

        void CreateLine(Vector3D v, Brush brush, bool mode)
        {
            v *= mode ? 20 : 0.4;
            Cylinder line = new Cylinder();
            line.DiffuseMaterial.Brush = brush;
            line.EmissiveMaterial.Brush = brush;
            line.Radius = mode ? 0.005 : 0.012;
            line.From = mode ? (Point3D)(-v) : new Point3D(0, 0, 0);
            line.To = (Point3D)v;
            axes.Children.Add(line);
        }

        void InitSun()
		{
			Sphere sun = new Sphere { Radius = 0.1 };
			sun.DiffuseMaterial.Brush = Brushes.Gold;
			sun.EmissiveMaterial.Brush = Brushes.Gold;
			scene.Models.Children.Add(sun);
		}

		void InitEarth()
		{
			earth = new Sphere(64);
			earth.DiffuseMaterial.Brush = (ImageBrush)Resources["earth"];
			earth.SpecularMaterial.Brush = Brushes.Black;
			scene.Models.Children.Add(earth);
		}

		void InitLocation()
		{
			if (location != null)
			{
				earth.Children.Remove(location);
				simulator.SetLocation(location = null);
			}

			if (!showLocation)
				return;

			location = CreateLocation();
			earth.Children.Add(location);
			simulator.SetLocation(location);
		}

		Sphere CreateLocation()
		{
			Sphere sphere = new Sphere { Radius = 0.03 };
			sphere.Position = new Point3D(1, 0, 0);
			sphere.Rotation3 = Math3D.RotationY(-simulator.Latitude);
			sphere.DiffuseMaterial.Brush = Brushes.Green;
			return sphere;
		}

		void InitPoleAxis()
		{
			Cylinder axis = new Cylinder { Radius = 0.01, ScaleZ = 6.6 };
			axis.Position = new Point3D(0, 0, -3.3);
			axis.DiffuseMaterial.Brush = axis.EmissiveMaterial.Brush = defBrush;
			earth.Children.Add(axis);
		}

		void InitNorthPole()
		{
			Sphere pole = new Sphere { Radius = 0.02 };
			pole.Position = new Point3D(0, 0, 1);
			earth.Children.Add(pole);
		}

		void InitLatitude()
		{
			if (latitude != null)
			{
				earth.Children.Remove(latitude);
				latitude = null;
			}

			latitude = new Cylinder { IsClosed = true, Divisions = 64 };
			double l = MathUtils.ToRadians(simulator.Latitude);
			latitude.Position = new Point3D(0, 0, Math.Sin(l));
			latitude.Radius = Math.Cos(l) * 1.003;
			latitude.ScaleZ = 0.003;
			latitude.DiffuseMaterial.Brush = latitude.EmissiveMaterial.Brush = defBrush;
			earth.Children.Add(latitude);
		}

		void InitHorizon()
		{
			if (horizon != null)
			{
				earth.Children.Remove(horizon);
				horizon = null;
			}

			if (!showHorizon)
				return;

			horizon = new Disk(128) { Scale = 0.2 };
			AddCompassPoint(0, Brushes.Yellow);
			AddCompassPoint(90, Brushes.Green);
			AddCompassPoint(-90, Brushes.Blue);
			horizon.Rotation1 = Math3D.RotationY(90);
			horizon.Position = new Point3D(0.98, 0, 0);
			horizon.Rotation3 = Math3D.RotationY(-simulator.Latitude);
			horizon.BackMaterial = horizon.Material;
			horizon.EmissiveMaterial.Brush = defBrush;
			earth.Children.Add(horizon);
		}

		void AddCompassPoint(double angleToSouth, Brush brush)
		{
			Sphere sphere = new Sphere(4) { Radius = 0.02 };
			sphere.EmissiveMaterial.Brush = sphere.DiffuseMaterial.Brush = brush;
			sphere.Position = new Point3D(1, 0, 0);
			sphere.Rotation3 = Math3D.RotationZ(angleToSouth);
			horizon.Children.Add(sphere);
		}

		void CheckCompassPoints()
		{
			if (horizon == null || location == null)
				return;

			foreach (var child in horizon.Children)
			{
				Sphere sphere = child as Sphere;
				if (sphere != null)
				{
					sphere.DiffuseMaterial.Brush = location.DiffuseMaterial.Brush;
					sphere.EmissiveMaterial.Brush = location.EmissiveMaterial.Brush;
				}
			}
		}

		void InitMeridian()
		{
			if (meridian != null)
			{
				earth.Children.Remove(meridian);
				meridian = null;
			}

			if (!showMeridian)
				return;

			meridian = CreateDisk(Math3D.RotationX(90));
			meridian.StartDegrees = -90;
			meridian.StopDegrees = 90;
			meridian.Radius = 3.3;

			meridian.SpecularMaterial.Brush = null;
			Brush brush = Brushes.MidnightBlue.Clone();
			brush.Opacity = 0.3;
			meridian.DiffuseMaterial.Brush = meridian.EmissiveMaterial.Brush = brush;
			meridian.BackMaterial = meridian.Material;
			earth.Children.Add(meridian);
		}

		Cylinder CreateDisk(Quaternion rotation)
		{
			Cylinder disk = new Cylinder(128) { IsClosed = true, Radius = 1.5, ScaleZ = 0.005 };
			disk.Position = new Point3D(0, disk.ScaleZ * 0.5, 0);
			disk.Rotation1 = rotation;
			Brush brush = Brushes.Yellow.Clone();
			brush.Opacity = 0.1;
			disk.DiffuseMaterial.Brush = disk.EmissiveMaterial.Brush = brush;
			return disk;
		}

		void InitShadowBorder()
		{
			if (shadowBorder != null)
			{
				scene.Models.Children.Remove(shadowBorder);
				shadowBorder = null;
			}

			if (!showShadowBorder)
				return;

			shadowBorder = CreateDisk(Math3D.RotationY(90));
			scene.Models.Children.Add(shadowBorder);
		}

		void InitEcliptic()
		{
			if (xyPlane != null)
			{
				scene.Models.Children.Remove(xyPlane);
				xyPlane = null;
			}

			if (!showEcliptic)
				return;

			xyPlane = new Disk(128) { Radius = 6 };
			Brush brush = Brushes.Green.Clone();
			brush.Opacity = 0.12;
			xyPlane.DiffuseMaterial.Brush = brush;
			xyPlane.EmissiveMaterial.Brush = brush;
			xyPlane.SpecularMaterial.Brush = null;
			xyPlane.BackMaterial = xyPlane.Material;
			scene.Models.Children.Add(xyPlane);
		}

		void Update()
		{
			earth.LockUpdates(true);
			earth.Position = simulator.EarthPosition;
			earth.Rotation1 = simulator.EarthRotation;
			earth.Rotation2 = simulator.AxialTilt;
			earth.LockUpdates(false);

			if (shadowBorder != null)
			{
				shadowBorder.LockUpdates(true);
				shadowBorder.Position = earth.Position;
				shadowBorder.Rotation2 = Math3D.RotationZ(simulator.EarthAngle, false);
				shadowBorder.LockUpdates(false);
			}

			if (location != null)
			{
				switch (simulator.Phase)
				{
					case Phases.Forenoon: location.DiffuseMaterial.Brush = Brushes.Green; break;
					case Phases.Afternoon: location.DiffuseMaterial.Brush = Brushes.Yellow; break;
					default: location.DiffuseMaterial.Brush = Brushes.Blue; break;
				}
				location.EmissiveMaterial.Brush = location.DiffuseMaterial.Brush;
				CheckCompassPoints();
			}

			scene.ActivateCamera(0);
			scene.IsInteractive = ViewMode > 4;
			scene.Camera.FieldOfView = 45;
			scene.Lighting.DirectionalLight2.Direction = (Vector3D)earth.Position;

			switch (viewMode)
			{
				case ViewModes.FixOverview:
					scene.Camera.Position = new Point3D(0.5, -1, 12);
					scene.Camera.LookAtOrigin();
					break;

				case ViewModes.FixEclipitcPole:
					scene.Camera.Position = earth.Position + 10 * Math3D.UnitZ;
					scene.Camera.LookDirection = -Math3D.UnitZ;
					scene.Camera.UpDirection = Math3D.UnitY;
					break;

				case ViewModes.FixNorthPole:
					{
						Point3D pt = earth.TranslatePoint(new Point3D(0, 0, 10));
						Vector3D v = pt.DirectionTo(earth.Position);

						scene.Camera.Position = pt;
						scene.Camera.LookDirection = v;
						scene.Camera.UpDirection = Math3D.UnitY;
					}
					break;

				case ViewModes.FixLocation:
				case ViewModes.FreeLocation:
					if (location != null)
					{
						Point3D p0 = location.TranslatePoint(new Point3D(0, 0, 0));
						Point3D px = location.TranslatePoint(new Point3D(1, 0, 0));
						Point3D pz = location.TranslatePoint(new Point3D(0, 0, 1));

						if (viewMode == ViewModes.FixLocation)
						{
							Vector3D v = p0.DirectionTo(px);
							scene.Camera.Position = px + 9 * v;
							scene.Camera.LookDirection = -v;

							v = p0.DirectionTo(pz);
							scene.Camera.UpDirection = v;
						}
						else
						{
							scene.Camera.FieldOfView = 80;

							Vector3D v = p0.DirectionTo(px);
							scene.Camera.Position = px;
							scene.Camera.UpDirection = v;

							Point3D pt = location.TranslatePoint(new Point3D(0, 0, -1));
							v = p0.DirectionTo(pt);
							scene.Camera.LookDirection = v;

							scene.Camera.ChangePitch(-altitude);

							Vector3D axis = p0.DirectionTo(px);
							Quaternion q = Math3D.Rotation(axis, -azimuth);
							scene.Camera.UpDirection = q.Transform(scene.Camera.UpDirection);
							scene.Camera.LookDirection = q.Transform(scene.Camera.LookDirection);
						}
					}
					break;

				case ViewModes.FixEarth:
					scene.Camera.Position = (Point3D)((Vector3D)earth.Position.Inverse());
					scene.Camera.LookAtOrigin();
					break;

				case ViewModes.FreeOverview2:
					scene.ActivateCamera(1);
					break;

				case ViewModes.FreeOverview3:
					scene.ActivateCamera(2);
					break;

				case ViewModes.Freeze:
					scene.IsInteractive = false;
					break;
			}
		}
		double altitude, azimuth;

		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			base.OnPreviewMouseMove(e);

			if (viewMode != ViewModes.FreeLocation)
				return;

			//--- don't handle mousemove in scene:
			e.Handled = true;

			Point mousePosition = e.GetPosition(this);
			if (e.LeftButton == MouseButtonState.Released)
			{
				oldPosition = mousePosition;
				return;
			}

			Vector diff = (mousePosition - oldPosition) * 0.15;
			oldPosition = mousePosition;

			azimuth += diff.X;
			altitude += diff.Y;

			if (!simulator.IsBusy)
				Update();
		}
		Point oldPosition;

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Space:
				case Key.Return: OnButtonStart(null, null); return;
                case Key.Add: Speed += 1; return;
                case Key.Subtract: Speed -= 1; return;
                case Key.Multiply: Speed *= 2; return;
                case Key.Divide: Speed /= 2; return;
                case Key.Back: simulator.InvertTime(); return;
                case Key.NumPad5: ViewMode = 5; return;
                case Key.NumPad6: ViewMode = 6; return;
            }
            e.Handled = false;
		}

		void OnButtonInvert(object sender, RoutedEventArgs e)
		{
			simulator.InvertTime();
		}

		void OnButtonStart(object sender, RoutedEventArgs e)
		{
			if (simulator.IsBusy)
				Stop();
			else
				Start();
		}

		void OnButtonNoon(object sender, RoutedEventArgs e)
		{
			if (simulator.IsBusy)
				simulator.StopNextNoon = true;
			else
				Start(true);
		}

		void Start(bool stopNextNoon = false)
		{
			if (simulator.IsBusy)
				return;

			startButton.Content = "Stop";
			simulator.Start(stopNextNoon);
			checker.Reset();
			timer.Start();
		}
		PerformanceChecker checker = new PerformanceChecker();

		public void Stop()
		{
			timer.Stop();
			simulator.Stop();
			startButton.Content = "Start";
		}

		void TimerTick(object sender, EventArgs e)
		{
			Update();
			if (!simulator.IsBusy)
				Stop();

			string msg = checker.GetResult(simulator.Count);
			DateTime t1 = DateTime.Now;
			if ((t1 - t0).TotalSeconds > 1)
			{
				t0 = t1;
				mainWindow.Title = string.Format("EquationOfTime ({0})", msg);
				if (textBox.Text.Length != simulator.Text.Length)
				{
					textBox.Text = simulator.Text;
					textBox.Select(textBox.Text.Length, 0);
				}
			}
		}
		DateTime t0;

		public int ViewMode
		{
			get { return (int)viewMode; }
			set
			{
				viewMode = (ViewModes)value;
				azimuth = 0;
				altitude = 0;
				FirePropertyChanged("ViewMode");
				Update();
			}
		}
		ViewModes viewMode;

		public double Obliquity
		{
			get { return simulator.Obliquity; }
			set
			{
				if (simulator.Obliquity != value)
				{
					simulator.Obliquity = value;
					ValueChanged("Obliquity");
				}
			}
		}

		void ValueChanged(string propertyName)
		{
			simulator.Update();
			Update();
			FirePropertyChanged(propertyName);
		}

		public double Latitude
		{
			get { return simulator.Latitude; }
			set
			{
				if (simulator.Latitude != value)
				{
					simulator.Latitude = value;
					ValueChanged("Latitude");
					InitLocation();
					InitLatitude();
				}
			}
		}

		public bool ShowTexture
		{
			get { return showTexture; }
			set
			{
				if (showTexture != value)
				{
					showTexture = value;
					if (showTexture)
						earth.DiffuseMaterial.Brush = (ImageBrush)Resources["earth"];
					else
						earth.DiffuseMaterial.Brush = Brushes.White;
					FirePropertyChanged("ShowTexture");
				}
			}
		}
		bool showTexture = true;

		public bool ShowAxes
		{
			get { return showAxes; }
			set
			{
				if (showAxes != value)
				{
					showAxes = value;
					InitAxes();
					FirePropertyChanged("ShowAxes");
				}
			}
		}
		bool showAxes = true;

		public bool ShowLocation
		{
			get { return showLocation; }
			set
			{
				if (showLocation != value)
				{
					showLocation = value;
					InitLocation();
					FirePropertyChanged("ShowLocation");
				}
			}
		}
		bool showLocation = true;

		public bool ShowMeridian
		{
			get { return showMeridian; }
			set
			{
				if (showMeridian != value)
				{
					showMeridian = value;
					InitMeridian();
					FirePropertyChanged("ShowMeridian");
				}
			}
		}
		bool showMeridian;

		public bool ShowEcliptic
		{
			get { return showEcliptic; }
			set
			{
				if (showEcliptic != value)
				{
					showEcliptic = value;
					InitEcliptic();
					FirePropertyChanged("ShowEcliptic");
				}
			}
		}
		bool showEcliptic;

		public bool ShowHorizon
		{
			get { return showHorizon; }
			set
			{
				if (showHorizon != value)
				{
					showHorizon = value;
					InitHorizon();
					FirePropertyChanged("ShowHorizon");
				}
			}
		}
		bool showHorizon;

		public bool ShowShadowBorder
		{
			get { return showShadowBorder; }
			set
			{
				if (showShadowBorder != value)
				{
					showShadowBorder = value;
					InitShadowBorder();
					Update();
					FirePropertyChanged("ShowShadowBorder");
				}
			}
		}
		bool showShadowBorder;

		public int Speed
		{
			get { return speed; }
			set
			{
				if (speed != value)
				{
					speed = value;
					simulator.SetSpeed(speed - 10);
					FirePropertyChanged("Speed");
				}
			}
		}
		int speed = 10;

		public int StartDay
		{
			get { return startDay; }
			set
			{
				if (true)//startDay != value)
				{
					startDay = value;
					Stop();
					if (IsValidDate())
					{
						simulator.InitTime(startDay, startMonth);
						Update();
					}
					FirePropertyChanged("StartDay");
				}
			}
		}
		int startDay = 1;

		public int StartMonth
		{
			get { return startMonth; }
			set
			{
				if (true)//startMonth != value)
				{
					startMonth = value;
					Stop();
					if (IsValidDate())
					{
						simulator.InitTime(startDay, startMonth);
						Update();
					}
					FirePropertyChanged("StartMonth");
				}
			}
		}
		int startMonth = 1;

		bool IsValidDate()
		{
			int maxDay = 31;
			switch (startMonth)
			{
				case 2: maxDay = 28; break;
				case 4:
				case 6:
				case 9:
				case 11: maxDay = 30; break;
			}

			if (startDay <= maxDay)
				return true;

			StartDay = maxDay;
			return false;
		}

		public int EccentricityIndex
		{
			get { return eccentricityIndex; }
			set
			{
				if (eccentricityIndex != value)
				{
					eccentricityIndex = value;
					switch (value)
					{
						case 0: simulator.Eccentricity = 0.0167; break;
						case 1: simulator.Eccentricity = 0.0; break;
						case 2: simulator.Eccentricity = 0.2; break;
						case 3: simulator.Eccentricity = 0.5; break;
					}
					FirePropertyChanged("EccentricityIndex");
				}
			}
		}
		int eccentricityIndex;

		#region Demo Mode

		void OnButtonDemo(object sender, RoutedEventArgs e)
		{
			InitDemo(1);
		}

		void InitDemo(int pageIndex)
		{
			Stop();

			if (pageIndex == 0)
			{
				Latitude = 51;
				Obliquity = 23.44;
				EccentricityIndex = 0;
				simulator.DemoMode = false;
				infoWindow = null;
				controlPanel.Visibility = Visibility.Visible;
				FocusManager.SetFocusedElement(mainWindow, mainWindow);
				return;
			}

			LanguageDialog dlg = new LanguageDialog();
			dlg.Owner = mainWindow;
			Point pt = PointToScreen(Mouse.GetPosition(this));
			dlg.Left = pt.X - dlg.Width * 0.5;
			dlg.Top = pt.Y;
			if (dlg.ShowDialog() == false)
				return;

			controlPanel.Visibility = Visibility.Collapsed;
			infoWindow = new InfoWindow(dlg.Choice);
			infoWindow.Owner = mainWindow;
			infoWindow.DataContext = DataContext;
			infoWindow.Closed += infoWindow_Closed;
			infoWindow.KeyDown += infoWindow_KeyDown;
			infoWindow.PropertyChanged += infoWindow_PropertyChanged;
			infoWindow.Show();
			infoWindow.PageIndex = pageIndex;
		}
		InfoWindow infoWindow;

		void infoWindow_Closed(object sender, EventArgs e)
		{
			infoWindow.Closed -= infoWindow_Closed;
			InitDemo(0);
		}

		void infoWindow_KeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Escape: infoWindow.Close(); return;
				case Key.Enter: infoWindow.PageIndex++; return;
			}
			e.Handled = false;
		}

		void infoWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Finished")
			{
				infoWindow.Close();
				return;
			}

			Latitude = infoWindow.PageIndex < 8 ? 0 : 51;
			Obliquity = 23.44;
			EccentricityIndex = infoWindow.PageIndex < 17 ? 0 : 1;
			ShowHorizon = false;
			startDay = startMonth = 1;

			switch (infoWindow.PageIndex)
			{
				case 5:
					StartDay = 20;
					StartMonth = 3;
					Speed = 16;
					ViewMode = (int)ViewModes.FreeOverview2;
					ShowAxes = true;
					ShowTexture = false;
					ShowEcliptic = true;
					ShowMeridian = false;
					ShowLocation = false;
                    ShowShadowBorder = false;
					return;

				case 6:
					Start();
					return;

				case 7:
					ViewMode = (int)ViewModes.FreeOverview2;
					return;

				case 8:
					ViewMode = (int)ViewModes.FixOverview;
					return;

				case 9:
					StartDay = 20;
					StartMonth = 3;
					Speed = 8;
					ViewMode = (int)ViewModes.FixOverview;
					ShowAxes = true;
					ShowTexture = true;
					ShowEcliptic = true;
					ShowMeridian = false;
					ShowLocation = true;
					Start();
					return;

				case 11:
					Speed = 8;
					ViewMode = (int)ViewModes.FixOverview;
					ShowMeridian = true;
					ShowEcliptic = false;
					ShowLocation = true;
					Start();
					return;

				case 12:
					StartDay = 20;
					StartMonth = 3;
					Speed = 6;
					ViewMode = (int)ViewModes.FreeLocation;
					ShowAxes = false;
					ShowEcliptic = false;
					ShowMeridian = true;
					ShowLocation = true;
					Start(true);
					return;

				case 13:
					Stop();
					ShowAxes = true;
					ViewMode = (int)ViewModes.FixOverview;
					return;

				case 14:
					Speed = 10;
					StartDay = 20;
					StartMonth = 3;
					ViewMode = (int)ViewModes.FixOverview;
					ShowAxes = true;
					ShowEcliptic = false;
					ShowMeridian = true;
					ShowLocation = true;
					Start();
					return;

				case 15:
					Speed = 10;
					StartDay = 9;
					StartMonth = 12;
					Start();
					return;

				case 16:
					Speed = 10;
					ViewMode = (int)ViewModes.FixOverview;
					return;

				case 17:
					simulator.DemoMode = false;
					Speed = 10;
					StartDay = 21;
					StartMonth = 12;
					Obliquity = 0;
					ViewMode = (int)ViewModes.Freeze;
					scene.Camera.Position = new Point3D(0, 0, 12);
					scene.Camera.LookAtOrigin();
					scene.Camera.UpDirection = Math3D.UnitY;
					ShowAxes = true;
					ShowEcliptic = false;
					ShowMeridian = true;
					ShowLocation = true;
					Start();
					return;

				case 18:
					simulator.DemoMode = true;
					Speed = 10;
					StartDay = 31;
					StartMonth = 12;
					Obliquity = 0;
					ViewMode = (int)ViewModes.Freeze;
					scene.Camera.Position = new Point3D(0, 0, 12);
					scene.Camera.LookAtOrigin();
					scene.Camera.UpDirection = Math3D.UnitY;
					ShowMeridian = true;
					ShowEcliptic = false;
					Start();
					return;

				case 19:
					Speed = 10;
					Obliquity = 0;
					ViewMode = (int)ViewModes.FixNorthPole;
					ShowEcliptic = true;
					return;

				case 20:
					Speed = 12;
					StartDay = 31;
					StartMonth = 12;
					Obliquity = 60;
					Start();
					return;

				case 21:
					Speed = 12;
					Obliquity = 60;
					return;

				case 22:
					simulator.DemoMode = true;
					Speed = 12;
					Obliquity = 60;
					ViewMode = (int)ViewModes.FixNorthPole;
					ShowEcliptic = true;
					return;

				case 23:
					simulator.DemoMode = false;
					Speed = 10;
					StartDay = 9;
					StartMonth = 12;
					ViewMode = (int)ViewModes.FixOverview;
					ShowEcliptic = false;
					Start();
					return;

				case 25:
					StartDay = 9;
					StartMonth = 12;
					EccentricityIndex = 0;
					Start();
					return;

				case 26:
				case 27:
					EccentricityIndex = 0;
					return;
			}
		}

		#endregion Demo Mode
	}
}
