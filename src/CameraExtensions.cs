//******************************************************************************************
// Copyright © 2020 Wolfgang Foerster (wolfoerster@gmx.de)
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
using System.Windows.Media.Media3D;
using WFTools3D;

namespace EquationOfTime
{
    public static class CameraExtensions
    {
        /// <summary>
        /// Just to fix a problem in CameraBox.LookAtOrigin().
        /// </summary>
        public static void LookAtSun(this CameraBox camera)
        {
            LookAt(Math3D.Origin, camera.Position, out Vector3D targetLook, out Vector3D targetUp);
            camera.UpDirection = targetUp;
            camera.LookDirection = targetLook;
        }

        private static void LookAt(Point3D targetPoint, Point3D observerPosition, out Vector3D lookDirection, out Vector3D upDirection)
        {
            lookDirection = targetPoint - observerPosition;
            lookDirection.Normalize();

            double a = lookDirection.X;
            double b = lookDirection.Y;
            double c = lookDirection.Z;

            //--- Find the one and only up vector (x, y, z) which has a positive z value (1), 
            //--- which is perpendicular to the look vector (2) and and which ensures that 
            //--- the resulting roll angle is 0, i.e. the resulting left vector (= up cross look)
            //--- lies within the xy-plane (or has a z value of 0) (3). In other words: 
            //--- 1. z > 0 (e.g. 1)
            //--- 2. ax + by + cz = 0
            //--- 3. ay - bx = 0
            //--- If the observer position is right above or below the target point, i.e. a = b = 0 and c != 0, 
            //--- we set the up vector to UnitX for the first case (c < 0) and to -UnitX for the second one (c > 0).

            double length = (a * a + b * b);
            if (length > 1e-12)
            {
                upDirection = new Vector3D(-c * a / length, -c * b / length, 1);
                upDirection.Normalize();
            }
            else
            {
                //--- The problem in CameraBox.LookAtOrigin() is this:
                if (c > 0)
                    upDirection = -Math3D.UnitX;
                else
                    upDirection = Math3D.UnitX;
            }
        }
    }
}
