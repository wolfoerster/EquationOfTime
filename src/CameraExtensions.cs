using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using WFTools3D;

namespace EquationOfTime
{

    public static class CameraExtensions
    {
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
                if (c > 0)
                    upDirection = -Math3D.UnitX;
                else
                    upDirection = Math3D.UnitX;
            }
        }
    }
}
