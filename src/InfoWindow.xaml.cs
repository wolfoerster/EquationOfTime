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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using WFTools3D;
using System;

namespace EquationOfTime
{
	/// <summary>
	/// Interaction logic for InfoWindow.xaml
	/// </summary>
	public partial class InfoWindow : Window, INotifyPropertyChanged
	{
		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void FirePropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion INotifyPropertyChanged

		public InfoWindow(string language)
		{
			InitializeComponent();
			subDir = language + "\\";
		}
		string subDir;

		public int PageIndex
		{
			get { return pageIndex; }
			set
			{
				if (value > 0 && pageIndex != value && ShowText(value))
				{
					pageIndex = value;
					pageText.Text = string.Format("Page {0}/27", pageIndex);
					FirePropertyChanged("PageIndex");
				}
			}
		}
		private int pageIndex;

		bool ShowText(int index)
		{
			string dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			string name = dir + "\\" + subDir + index.ToString("D2") + ".txt";
			if (File.Exists(name))
			{
                bool firstTime = textBlock.Text.Length == 0;
				textBlock.Text = File.ReadAllText(name, Encoding.Default);
                if (firstTime)
				    UpdatePosition();
				return true;
			}
			FirePropertyChanged("Finished");
			return false;
		}

		private void UpdatePosition()
		{
			textBlock.Measure(new Size(1234, 1234));
			Size size = textBlock.DesiredSize;
			double width = size.Width + 17;
			double height = size.Height + 63;
			Screen screen = WFUtils.GetScreenByPixel(0, 0);
			Left = screen.WorkArea.Right - width;
			Top = (screen.WorkArea.Bottom - height) * 0.5;
		}

		void OnButtonClick(object sender, RoutedEventArgs e)
		{
			int i = ((sender as Button).Content as string).Equals("Prev") ? -1 : 1;
			PageIndex += i;
		}
    }
}
