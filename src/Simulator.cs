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
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.ComponentModel;
using System.Diagnostics;
using WFTools3D;

namespace EquationOfTime
{
    public enum Phases
    {
        /// <summary>
        /// Between sunrise and noon
        /// </summary>
        Forenoon,

        /// <summary>
        /// Between noon and sunset
        /// </summary>
        Afternoon,

        /// <summary>
        /// Between sunset and midnight
        /// </summary>
        ForeMidnight,

        /// <summary>
        /// Between midnight and sunrise
        /// </summary>
        AfterMidnight
    }

    public class Simulator
    {
        /// <summary>
        /// A sidereal day in seconds.
        /// </summary>
        static double SiderealDay = MathUtils.ToSeconds(0, 23, 56, 4.1);

        /// <summary>
        /// Angle between the ecliptic and the celestial equator.
        /// </summary>
        public double Obliquity = 23.44;

        /// <summary>
        /// Latitude of an example location on earth, e.g. Greenwich.
        /// </summary>
        public double Latitude = 51;

        /// <summary>
        /// Eccentricity of the earth's orbit.
        /// </summary>
        public double Eccentricity = 0.0167;

        /// <summary>
        /// Angular velocity of the earth rotating around itself.
        /// </summary>
        double wEarth = MathUtils.PIx2 / SiderealDay;

        /// <summary>
        /// Mean angular velocity of the earth orbiting the sun.
        /// </summary>
        double wSun;

        /// <summary>
        /// Rotation of the earth around itself.
        /// </summary>
        public Quaternion EarthRotation;

        /// <summary>
        /// Tilt of the earth's rotation axis.
        /// </summary>
        public Quaternion AxialTilt;

        /// <summary>
        /// Position of earth in cartesian coordinates.
        /// </summary>
        public Point3D EarthPosition;

        /// <summary>
        /// Position of earth in radians.
        /// </summary>
        public double EarthAngle;

        /// <summary>
        /// Text showing the times and time differences of sunrise, noon and sunset for each day.
        /// </summary>
        public string Text;

        /// <summary>
        /// Performance counter.
        /// </summary>
        public long Count;

        /// <summary>
        /// A reference time used for date calculation.
        /// </summary>
        DateTime refTime = new DateTime(2013, 6, 21);

        /// <summary>
        /// The actual simulation time in seconds after refTime.
        /// </summary>
        double time;

        /// <summary>
        /// Time step of the simulation in seconds.
        /// </summary>
        double dt = 0.01;

        /// <summary>
        /// Number of seconds in a solar day.
        /// </summary>
        double oneDay;

        public Simulator()
        {
            DemoMode = false;
            InitTime(20, 6);
            worker.DoWork += DoWork;
            worker.WorkerSupportsCancellation = true;
        }
        BackgroundWorker worker = new BackgroundWorker();

        public void InitTime(int day, int month)
        {
            time = (new DateTime(2014, month, day) - refTime).TotalSeconds;
            time -= demoMode ? 200 : 1800; //--- correct to something before midnight
            phase = Phases.ForeMidnight;
            Update();
        }

        public void Revolute(int numDays)
        {
            time += numDays * SiderealDay;
            Update();
        }

        public void Start(bool stopNextNoon = false)
        {
            if (!worker.IsBusy)
            {
                StopNextNoon = stopNextNoon;
                worker.RunWorkerAsync();
            }
        }

        public void Stop()
        {
            StopNextNoon = false;
            if (worker.IsBusy)
            {
                worker.CancelAsync();
                while (worker.IsBusy)
                {
                    //Application.DoEvents();
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
                }
            }
        }

        public bool IsBusy
        {
            get { return worker.IsBusy; }
        }

        void DoWork(object sender, DoWorkEventArgs e)
        {
            Text = "";
            lastRise = lastNoon = lastSet = lastCheck = oldAngle = oldDist = Count = 0;

            while (true)
            {
                ++Count;
                time += dt;
                Update();

                if (time - lastCheck >= 1) //--- check for phases every second
                {
                    if (!CheckPhases())
                    {
                        e.Cancel = true;
                        break;
                    }
                    lastCheck = time;
                }

                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
            }
        }
        double lastCheck;

        public void Update()
        {
            //--- Update earth rotation around itself
            double angle = MathUtils.ToDegrees(wEarth * time);
            EarthRotation = new Quaternion(Math3D.UnitZ, angle);

            //--- Update axial tilt
            AxialTilt = new Quaternion(Math3D.UnitY, -Obliquity);// ==> max. 15 Minuten Unterschied

            //--- Update earth rotation angle around sun
            angle = wSun * time;
            EarthAngle = angle - 2 * Eccentricity * Math.Sin(angle - 15 * wSun * oneDay);
#if comment
            //--- Ellipse mit Perihel am 3. Januar und Aphel am 6 Juli ==> max. 8 Minuten Unterschied
            //--- Winkelabhängige Winkelgeschwindigkeit, Winkelkorrektur um 15 Tage nach hinten verschoben
            //--- Winkel zwischen grosser Halbachse und SommerWinterAchse (unserer x Achse) ist 12.25 Grad.
            //--- Siehe auch http://info.ifpan.edu.pl/firststep/aw-works/fsII/mul/mueller.html
            //--- The above formula is an approximation for very small eccentricity values only (< 0.1).
#endif
            //--- Update earth position
            EarthPosition = new Point3D(3 * Math.Cos(EarthAngle), 3 * Math.Sin(EarthAngle), 0);
        }

        //--- Calculate the angle between location-sun and location-zenith
        //--- Calculate the distance between sun and the meridian plane
        (double, double) GetAngleAndDist()
        {
            //--- Calculate the absolute position of the example location on earth
            Matrix3D earthMatrix = new Matrix3D();
            earthMatrix.Rotate(EarthRotation);
            earthMatrix.Rotate(AxialTilt);
            earthMatrix.Translate((Vector3D)EarthPosition);
            Point3D location = (locationMatrix * earthMatrix).Transform(new Point3D(0, 0, 0));

            //--- Calculate the angle between location-sun and location-zenith
            Vector3D dirSun = -(Vector3D)EarthPosition;
            Vector3D zenith = location - EarthPosition;
            double angle = dirSun.AngleTo(zenith);

            //--- Calculate the Hesse normal form of the meridian plane: (n0 * r) = d
            Point3D earthUnitY = earthMatrix.Transform(new Point3D(0, 1, 0));
            Vector3D normalOfMeridian = EarthPosition - earthUnitY; // has a length of 1!
            double d = normalOfMeridian.Dot((Vector3D)location);

            //--- Since the sun is located at the origin, its distance to the meridian is simply -d
            return (angle, -d);
        }

        bool CheckPhases()
        {
            if (locationMatrix.IsIdentity)
                return true;

            //--- Calculate the angle between location-sun and location-zenith
            //--- Calculate the distance between sun and the meridian plane
            var (angle, dist) = GetAngleAndDist();
            var result = true;

            if (lastCheck > 0)
            {
                if (angle <= 90 && oldAngle > 90) //--- sunrise
                {
                    var t = new LinearTransform(angle, oldAngle, time, lastCheck);
                    corrTime = t.Transform(90);
                    Phase = Phases.Forenoon;
                }

                if (dist >= 0 && oldDist < 0) //--- noon
                {
                    var t = new LinearTransform(dist, oldDist, time, lastCheck);
                    corrTime = t.Transform(0);

                    if (StopNextNoon)
                    {
                        StopNextNoon = false;
                        time = corrTime;
                        Update();
                        (angle, dist) = GetAngleAndDist();
                        result = false;
                    }

                    Phase = Phases.Afternoon;
                }

                if (angle >= 90 && oldAngle < 90) //--- sunset
                {
                    var t = new LinearTransform(angle, oldAngle, time, lastCheck);
                    corrTime = t.Transform(90);
                    Phase = Phases.ForeMidnight;
                }

                if (dist <= 0 && oldDist > 0) //--- midnight
                {
                    var t = new LinearTransform(dist, oldDist, time, lastCheck);
                    corrTime = t.Transform(0);
                    Phase = Phases.AfterMidnight;
                }
            }

            oldDist = dist;
            oldAngle = angle;
            return result;
        }
        double oldAngle, oldDist, corrTime;
        public bool StopNextNoon;

        public Phases Phase
        {
            get { return phase; }
            protected set
            {
                if (!demoMode && Text.Length > 0)
                {
                    var nothing = "  -------- ----";
                    if (phase == Phases.AfterMidnight && value == Phases.Afternoon)
                    {
                        Text += nothing;
                    }
                    else if (phase == Phases.Afternoon && value == Phases.AfterMidnight)
                    {
                        Text += nothing;
                    }
                }

                phase = value;
                switch (phase)
                {
                    case Phases.Forenoon: AddText(ref lastRise); break;
                    case Phases.Afternoon: AddText(ref lastNoon); break;
                    case Phases.ForeMidnight: AddText(ref lastSet); break;
                    case Phases.AfterMidnight:
                        {
                            if (demoMode)
                            {
                                if (Text.Length == 0) Text = "Orbit   Noon      Diff";
                                double angle = MathUtils.ToDegrees(EarthAngle) % 360;
                                Text += string.Format("\n{0:000}°: ", angle);
                            }
                            else
                            {
                                if (Text.Length == 0) Text = "Date    Sunrise  Diff  Noon     Diff  Sunset   Diff";
                                DateTime now = refTime.AddSeconds(corrTime);
                                if (now.Hour == 23) now = now.AddHours(1);
                                Text += string.Format("\n{0:D2}.{1:D2}.", now.Day, now.Month);
                            }
                        }
                        break;
                }
            }
        }
        Phases phase;
        double lastRise, lastNoon, lastSet;

        void AddText(ref double prevTime)
        {
            if (demoMode)
            {
                if (Phase != Phases.Afternoon)//--- only add text for noon
                    return;
            }

            bool firstTime = prevTime == 0;
            double delta = corrTime - prevTime - oneDay;
            prevTime = corrTime;

            if (Text.Length == 0)//--- wait for first midnight
                return;

            string str = firstTime ? " ---" : DiffToString(delta);
            Text += string.Format("  {0} {1}", TimeToString(corrTime), str);
        }

        string DiffToString(double delta)
        {
            int diff = (int)Math.Round(delta);
            string sign = diff == 0 ? " " : diff < 0 ? "-" : "+";
            return sign + Math.Abs(diff).ToString(demoMode ? "D4" : "D3");
        }

        string TimeToString(double seconds)
        {
            //--- convert rounded seconds to days
            double t = Math.Round(seconds) / oneDay;

            int d = (int)t;
            t -= d;

            t *= 24;
            int h = (int)t;
            t -= h;

            t *= 60;
            int m = (int)t;
            t -= m;

            t *= 60;
            int s = (int)t;

            return string.Format("{0:D2}:{1:D2}:{2:D2}", h, m, s);
        }

        public void SetLocation(Object3D value)
        {
            locationMatrix = new Matrix3D();
            if (value != null && value.Transform is MatrixTransform3D)
                locationMatrix = (value.Transform as MatrixTransform3D).Matrix;
        }
        private Matrix3D locationMatrix;

        public void SetSpeed(int index)
        {
            dt = 0.01 * Math.Pow(2, index);
        }

        public void InvertTime()
        {
            dt *= -1;
            lastCheck = 0;
        }

        public bool DemoMode
        {
            get => demoMode;

            set
            {
                demoMode = value;
                if (demoMode)
                {
                    // To get the number of additional seconds in a day, just set it to 0 first.
                    // The table of page 18 of the demo will then show this number as 'Diff'.
                    wSun = MathUtils.PIx2 / MathUtils.ToSeconds(36, 0, 0, 0);
                    oneDay = MathUtils.ToSeconds(1, 0, 0, 2219);
                }
                else
                {
                    wSun = MathUtils.PIx2 / MathUtils.ToSeconds(365, 6, 9, 9.54); //--- sidereal year
                    oneDay = MathUtils.ToSeconds(1, 0, 0, 0);
                }
            }
        }
        bool demoMode;
    }
}
