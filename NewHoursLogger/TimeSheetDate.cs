using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace NewHoursLogger
{
    // class for handling dates in the timesheet
    // object represents what will be put on the timesheet
    public class TimeSheetDate
    {
        // class attributes
        private DateTime useDate;
        private string timeIn;
        private string timeOut;
        // private int hours;

        public bool isEndOfWeek;

        public TimeSheetDate(string sheetDate, string tIn, string tOut, string h, bool b)
        {
            string[] newDate = sheetDate.Split("/"); // passed in as m/d/yyyy
            useDate = new DateTime(Convert.ToInt32(newDate[2]), Convert.ToInt32(newDate[0]), Convert.ToInt32(newDate[1])); // make sure its a proper date
            timeIn = tIn;
            timeOut = tOut;
            // hours = Convert.ToInt32(h.Replace(":", ""));
            isEndOfWeek = b;
        }

        public int getDayWeek()
        {
            // 0 = sunday, 1 = monday, ... 6 = saturday
            int day = (int)useDate.DayOfWeek;
            if (day == 0)
            {
                return 6;
            }
            return day - 1;
        }

        public string getPrintableDate()
        {
            int m = useDate.Month;
            int d = useDate.Day;
            string newmonth;
            string newday;
            if (m < 10)
            {
                newmonth = "0" + m.ToString();
            }
            else
            {
                newmonth = m.ToString();
            }
            if (d < 10)
            {
                newday = "0" + d.ToString();
            }
            else
            {
                newday = d.ToString();
            }
            return newmonth + "/" + newday;
        }
        private string getMilitaryTime(string time)
        {
            // make sure that time passed in isnt the clocked in string
            if (time == "")
            {
                return "";
            }
            // separate time into hh:mm and am/pm
            string hour = "";
            int ind = time.Length - 2;
            for (int i = 0; i < ind; i++)
            {
                hour += time[i].ToString();
            }
            // above solution is weird, but i cant think of anything better

            string[] t = hour.Split(':');
            if (time[ind] == 'P' && t[0] != "12")
            {
                t[0] = (Convert.ToInt32(t[0]) + 12).ToString();
            }

            return t[0] + "." + t[1]; // returned as hh:mm
        }

        // when copying code i noticed that getMinuteRatio was unused, so ill leave it out for now

        public string getTimeSheetTimeIn()
        {
            return getMilitaryTime(timeIn);
        }

        public string getTimeSheetTimeOut()
        {
            return getMilitaryTime(timeOut);
        }

        public void printObj()
        {
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("Day: " + getDayWeek());
            System.Diagnostics.Debug.WriteLine("Date: " + getPrintableDate());
            System.Diagnostics.Debug.WriteLine("Time in: " + getMilitaryTime(timeIn));
            System.Diagnostics.Debug.WriteLine("Time out: " + getMilitaryTime(timeOut));
            // System.Diagnostics.Debug.WriteLine("Total hours: " + hours);
            System.Diagnostics.Debug.WriteLine("");
        }
    }

}