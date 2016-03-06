﻿//******************************************************************************************
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
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using WFTools3D;

namespace EquationOfTime
{
	public static class Utils
	{
		#region GetAllScreens

		[DllImport("user32.dll")]
		static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

		delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

		// size of a device name string
		const int CCHDEVICENAME = 32;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct MonitorInfoEx
		{
			public int Size;

			public RectStruct Monitor;

			public RectStruct WorkArea;

			public uint Flags;//--- first bit = MONITORINFOF_PRIMARY

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
			public string DeviceName;

			public void Init()
			{
				this.Size = 40 + 2 * CCHDEVICENAME;
				this.DeviceName = string.Empty;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RectStruct
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		static public List<Screen> GetAllScreens()
		{
			List<Screen> screens = new List<Screen>();

			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
					delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
					{
						MonitorInfoEx mi = new MonitorInfoEx();
						mi.Size = (int)Marshal.SizeOf(mi);
						bool success = GetMonitorInfo(hMonitor, ref mi);
						if (success)
						{
							Screen screen = new Screen()
							{
								ScreenArea = new Rect(mi.Monitor.Left, mi.Monitor.Top, mi.Monitor.Right - mi.Monitor.Left, mi.Monitor.Bottom - mi.Monitor.Top),
								WorkArea = new Rect(mi.WorkArea.Left, mi.WorkArea.Top, mi.WorkArea.Right - mi.WorkArea.Left, mi.WorkArea.Bottom - mi.WorkArea.Top),
								IsPrimary = (mi.Flags & 1) == 1,
								Name = mi.DeviceName
							};
							screens.Add(screen);
						}
						return true;
					}, IntPtr.Zero);

			return screens;
		}

		static public Screen GetScreenByName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return null;

			List<Screen> screens = GetAllScreens();
			foreach (var screen in screens)
			{
				if (screen.Name == name)
					return screen;
			}
			return null;
		}

		static public Screen GetScreenByPixel(Point pt)
		{
			List<Screen> screens = GetAllScreens();
			foreach (var screen in screens)
			{
				if (screen.WorkArea.Contains(pt))
					return screen;
			}
			return null;
		}

		static public Screen GetScreenByPixel(double x, double y)
		{
			return GetScreenByPixel(new Point(x, y));
		}

		static public Screen GetPrimaryScreen()
		{
			List<Screen> screens = GetAllScreens();
			foreach (var screen in screens)
			{
				if (screen.IsPrimary)
					return screen;
			}
			return null;
		}

		#endregion GetAllScreens
	}

	public class Screen
	{
		public Rect ScreenArea;
		public Rect WorkArea;
		public bool IsPrimary;
		public string Name;
	}

	public class StackPanelH : StackPanel
	{
		public StackPanelH()
		{
			Orientation = Orientation.Horizontal;
		}

		protected override Size ArrangeOverride(Size arrangeSize)
		{
			double x = 0;

			foreach (UIElement child in base.InternalChildren)
			{
				if (child != null)
				{
					double width = child.DesiredSize.Width;
					double height = child.DesiredSize.Height;
					double y = (arrangeSize.Height - height) * 0.5;

					Rect rect = new Rect(x, y, width, height);
					child.Arrange(rect);
					x += width;
				}
			}
			return arrangeSize;
		}
	}

	public class PerformanceChecker
	{
		public string GetResult(bool altMode = false)
		{
			if (!watch.IsRunning)
			{
				Reset();
				return "";
			}

			long elapsed = watch.ElapsedMilliseconds;
			if (elapsed < 3)
				return "";

			watch.Reset();
			watch.Start();

			average = (average * count + elapsed) / ++count;

			if (altMode)
				return string.Format("Average: {0:F0} ms, {1:F0} fps", average, 1e3 / average);

			return string.Format("Average: {0:F0} ms, Actual: {1} ms", average, elapsed);
		}

		public double Average
		{
			get { return average; }
		}

		public void Reset()
		{
			count = 0;
			average = 0;
			watch.Reset();
			watch.Start();
		}

		long count;
		double average;
		Stopwatch watch = new Stopwatch();
	}

	/// <summary>
	/// A spin box for numbers. Composed of a Label, a TextBox and a ScrollBar.
	/// </summary>
	public class NumberBox : Grid
	{
		#region DependencyProperty Number

		/// <summary>
		/// Gets or sets the number.
		/// </summary>
		public double Number
		{
			get { return (double)GetValue(NumberProperty); }
			set { SetValue(NumberProperty, value); }
		}

		/// <summary>
		/// The NumberProperty.
		/// </summary>
		public static readonly DependencyProperty NumberProperty =
			DependencyProperty.Register("Number", typeof(double), typeof(NumberBox),
			new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
				OnNumberChanged, OnNumberCoerce));

		static void OnNumberChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			NumberBox numberBox = obj as NumberBox;
			if (numberBox != null)
				numberBox.MyNumberChanged();
		}

		internal void MyNumberChanged()
		{
			textBox.Text = Number.ToString(FormatString);
			scrollBar.Value = Invert(Number);

			if (NumberChanged != null)
				NumberChanged(this, null);
		}

		static object OnNumberCoerce(DependencyObject d, object baseValue)
		{
			NumberBox numberBox = (NumberBox)d;
			double value = (double)baseValue;
			value = MathUtils.Clamp(value, numberBox.Minimum, numberBox.Maximum);
			return value;
		}

		#endregion DependencyProperty Number

		/// <summary>
		/// Initializes a new instance of the <see cref="NumberBox"/> class.
		/// </summary>
		public NumberBox()
		{
			Initialize();
		}

		/// <summary>
		/// Initializes this instance.
		/// </summary>
		protected virtual void Initialize()
		{
			ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
			ColumnDefinitions.Add(new ColumnDefinition());
			ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

			label = new TextBlock();
			label.VerticalAlignment = VerticalAlignment.Center;
			SetColumn(label, 0);
			Children.Add(label);

			textBox = new TextBox();
			textBox.TextChanged += TextBoxTextChanged;
			SetColumn(textBox, 1);
			Children.Add(textBox);

			scrollBar = new ScrollBar();
			scrollBar.Focusable = true;
			scrollBar.ContextMenu = null;
			scrollBar.Scroll += ScrollBarScroll;
			scrollBar.Margin = new Thickness(0, 1, 0, 0);
			scrollBar.MouseRightButtonDown += ScrollBarMouseRightButtonDown;
			SetColumn(scrollBar, 2);
			Children.Add(scrollBar);

			FormatString = "F0";
			Minimum = 0;
			Maximum = 100;
			SmallChange = 1;
			LargeChange = 10;
		}
		TextBlock label;
		TextBox textBox;
		ScrollBar scrollBar;

		void TextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
			double number;
			if (double.TryParse(textBox.Text, out number))
				Number = number;
		}

		void ScrollBarScroll(object sender, ScrollEventArgs e)
		{
			scrollBar.Focus();
			Number = Invert(scrollBar.Value);
		}

		void ScrollBarMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			Point pt = e.GetPosition(scrollBar);
			if (pt.Y > scrollBar.ActualHeight * 0.5)
				Number -= scrollBar.LargeChange;
			else
				Number += scrollBar.LargeChange;
		}

		/// <summary>
		/// Gets or sets the value to be added to or subtracted from the Number property when the scroll box is moved a small distance.
		/// </summary>
		public double SmallChange
		{
			get { return scrollBar.SmallChange; }
			set { scrollBar.SmallChange = value; }
		}

		/// <summary>
		/// Gets or sets a value to be added to or subtracted from the Number property when the scroll box is moved a large distance.
		/// </summary>
		public double LargeChange
		{
			get { return scrollBar.LargeChange; }
			set { scrollBar.LargeChange = value; }
		}

		/// <summary>
		/// Gets or sets the lower limit of values of the scrollable range.
		/// </summary>
		public double Minimum
		{
			get { return scrollBar.Minimum; }
			set
			{
				if (scrollBar.Minimum != value)
				{
					scrollBar.Minimum = value;
					MyNumberChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets the upper limit of values of the scrollable range.
		/// </summary>
		public double Maximum
		{
			get { return scrollBar.Maximum; }
			set
			{
				if (scrollBar.Maximum != value)
				{
					scrollBar.Maximum = value;
					MyNumberChanged();
				}
			}
		}

		/// <summary>
		/// The format string is used to create the text representation of the number.
		/// </summary>
		public string FormatString
		{
			get { return formatString; }
			set
			{
				if (formatString != value)
				{
					formatString = value;
					MyNumberChanged();
				}
			}
		}
		string formatString;

		/// <summary>
		/// Gets or sets the label text.
		/// </summary>
		public string Label
		{
			get { return label.Text; }
			set
			{
				label.Text = value;
				label.Margin = string.IsNullOrEmpty(value) ? new Thickness(0) : new Thickness(5, 0, 5, 0);
			}
		}

		/// <summary>
		/// Get or set the MinWidth of the textBox.
		/// </summary>
		public double TBMinWidth
		{
			get { return textBox.MinWidth; }
			set { textBox.MinWidth = value; }
		}

		/// <summary>
		/// Need to invert the scrollBar values because if the scrollBar value is max we want our number to be min and v.v.
		/// </summary>
		double Invert(double number)
		{
			return Maximum + Minimum - number;
		}

		/// <summary>
		/// Occurs when the number has changed.
		/// </summary>
		public event EventHandler NumberChanged;
	}
}
