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
using System.Windows;

namespace EquationOfTime
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string theme = "PresentationFramework.Aero;V3.0.0.0;31bf3856ad364e35;component\\themes/aero.normalcolor.xaml";
            //string theme = "/PresentationFramework.Classic;v3.0.0.0;31bf3856ad364e35;Component/themes/classic.xaml";
            //string theme = "/PresentationFramework.Royale;v3.0.0.0;31bf3856ad364e35;Component/themes/royale.normalcolor.xaml";
            //string theme = "/PresentationFramework.Luna;v3.0.0.0;31bf3856ad364e35;Component/themes/luna.normalcolor.xaml";
            //string theme = "/PresentationFramework.Luna;v3.0.0.0;31bf3856ad364e35;Component/themes/luna.homestead.xaml";
            //string theme = "/PresentationFramework.Luna;v3.0.0.0;31bf3856ad364e35;Component/themes/luna.metallic.xaml";
            Uri uri = new Uri(theme, UriKind.Relative);
            Resources.MergedDictionaries.Add(Application.LoadComponent(uri) as ResourceDictionary);
        }
    }
}
