﻿using LolloGPS.Data;
using LolloGPS.Data.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilz;

namespace LolloGPS.Core
{
    public sealed class AltitudeProfiles_VM : IMapApController
    {
        //public PersistentData MyPersistentData { get { return App.PersistentData; } }
        //public RuntimeData MyRuntimeData { get { return App.MyRuntimeData; } }
        public Main_VM MyMainVM { get { return Main_VM.GetInstance(); } }
        private IMapApController _altitudeProfilesController = null;

        private const double ALTITUDE_SCALE_MARGIN_WHEN_ALL_EQUAL = 50.0;

        internal AltitudeProfiles_VM(IMapApController altitudeProfilesController)
        {
            _altitudeProfilesController = altitudeProfilesController;
            MyMainVM.MyAltitudeProfiles_VM = this;
        }

        internal void InitialiseChartData(Collection<PointRecord> coll, bool respectDatesAndTimes, bool sortIfRespectingDatesAndTimes,
            ref double maxAltitude, ref double minAltitude, ref double maxTime, ref double minTime, ref double[,] outPoints)
        {
            if (coll == null || coll.Count == 0) return;

            Stopwatch sw0 = new Stopwatch(); sw0.Start();
            double[,] points = new double[coll.Count, 2];

            bool isDateTimeAlwaysPresent = GetDatesAndTimesAlwaysPresent(coll, respectDatesAndTimes);

            // set first point, maxes and mins
            if (isDateTimeAlwaysPresent) points[0, 0] = coll[0].TimePoint.ToBinary();
            else points[0, 0] = 0.0;
            points[0, 1] = Main_VM.RoundAndRangeAltitude(coll[0].Altitude);

            minTime = maxTime = points[0, 0];
            minAltitude = maxAltitude = points[0, 1];
            // set next points, maxes and mins - if any further points
            for (int i = 1; i < coll.Count; i++)
            {
                if (isDateTimeAlwaysPresent) points[i, 0] = coll[i].TimePoint.ToBinary();
                else points[i, 0] = i;
                points[i, 1] = Main_VM.RoundAndRangeAltitude(coll[i].Altitude);

                if (points[i, 0] > maxTime) maxTime = points[i, 0];
                if (points[i, 0] < minTime) minTime = points[i, 0];
                if (points[i, 1] > maxAltitude) maxAltitude = points[i, 1];
                if (points[i, 1] < minAltitude) minAltitude = points[i, 1];
            }
            // if we have one piece of data only, make a max and a min, which are far apart enough, 
            // to get the X grid lines left and right and the Y grid lines top and bottom
            if (minTime == maxTime)
            {
                if (minTime == 0.0) // ticks
                {
                    minTime = -.5;
                    maxTime = .5;
                }
                else // DateTime.ToBinary()
                {
                    minTime /= 2; // reduce it: if you increase it, you may get an overflow
                }
                if (minTime > maxTime) LolloMath.Swap(ref minTime, ref maxTime);
            }
            if (minAltitude == maxAltitude)
            {
                minAltitude -= ALTITUDE_SCALE_MARGIN_WHEN_ALL_EQUAL;
                maxAltitude += ALTITUDE_SCALE_MARGIN_WHEN_ALL_EQUAL;
            }

            sw0.Stop();
            Debug.WriteLine(string.Format("Initialising chart data (excluding sorting) took {0} msec", sw0.ElapsedMilliseconds));

            if (isDateTimeAlwaysPresent && sortIfRespectingDatesAndTimes && points.GetUpperBound(0) > 0) outPoints = Sort(points);
            else outPoints = points;
        }
        private static bool GetDatesAndTimesAlwaysPresent(Collection<PointRecord> coll, bool respectDatesAndTimes)
        {
            bool isDateTimeAlwaysPresent = true;
            if (respectDatesAndTimes) 
            {
                for (int i = 0; i < coll.Count; i++)
                {
                    if (coll[i].TimePoint == null || coll[i].TimePoint == default(DateTime))
                    {
                        isDateTimeAlwaysPresent = false;
                        break;
                    }
                }
            }
            else // series like landmarks do not get real date and time: even if some may have it, they are really only a sequence.
            {
                isDateTimeAlwaysPresent = false;
            }
            return isDateTimeAlwaysPresent;
        }
        private static double[,] Sort(double[,] points)
        {
            Stopwatch sw0 = new Stopwatch(); sw0.Start();
            int size = points.GetUpperBound(0) + 1;
            double[,] sortedPoints = new double[size, 2];

            var indexArray = new int[size];
            for (int i = 0; i < size; i++)
            {
                indexArray[i] = i;
            }

            Array.Sort(indexArray, new RectangularComparer(points));

            for (int i = 0; i < size; i++)
            {
                sortedPoints[i, 0] = points[indexArray[i], 0];
                sortedPoints[i, 1] = points[indexArray[i], 1];
            }

            sw0.Stop();
            Debug.WriteLine(string.Format("SORTING took {0} msec", sw0.ElapsedMilliseconds));

            return sortedPoints;
        }
        class RectangularComparer : IComparer
        {
            // maintain a reference to the 2-dimensional array being sorted
            private double[,] myArray;

            // constructor initializes the sortArray reference
            public RectangularComparer(double[,] arrayToBeSorted)
            {
                myArray = arrayToBeSorted;
            }

            public int Compare(object one, object two)
            {
                // one and two are row indexes into the sortArray
                //int iOne = (int)one;
                //int iTwo = (int)two;

                // compare the items in the sortArray
                return myArray[(int)one, 0].CompareTo(myArray[(int)two, 0]);
            }
        }

        #region IAltitudeProfilesController
        public Task CentreOnHistoryAsync()
        {
            return _altitudeProfilesController?.CentreOnHistoryAsync();
        }
        public Task CentreOnRoute0Async()
        {
            return _altitudeProfilesController?.CentreOnRoute0Async();
        }
        public Task CentreOnLandmarksAsync()
        {
            return _altitudeProfilesController?.CentreOnLandmarksAsync();
        }
        public Task CentreOnTargetAsync()
        {
            return _altitudeProfilesController?.CentreOnTargetAsync();
        }
        public void Goto2D()
        {
            _altitudeProfilesController.Goto2D();
        }
        #endregion IAltitudeProfilesController
    }
}
