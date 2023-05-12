
// imports for WPF app
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// imports for other things
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
//using SeleniumExtras.WaitHelpers;
//using NUnit.Framework;
//using OpenQA.Selenium;
//using OpenQA.Selenium.Chrome;
//using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

/* 
 * to release a new version: 
 * do Build > Clean solution to clean up the file structure
 * change the build configuration from Debug to Release (dropdown on the toolbar that says "debug")
 * build solution by choosing Build > Build solution
 * the exe is found in ...\appname\appname\bin\release
 */

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

        public TimeSheetDate(string sheetDate, string tIn, string tOut, string h, bool b) {
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
            if(day == 0)
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
            if(m < 10)
            {
                newmonth = "0" + m.ToString();
            }
            else
            {
                newmonth = m.ToString();
            }
            if(d < 10)
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
            // separate time into hh:mm and am/pm
            string hour = "";
            int ind = time.Length - 2;
            for(int i = 0; i < ind; i++)
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


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // testing values
        //private string eid = "7247156";
        //private string asemail = "logan.ashbaugh7156";
        //private string sid = "007247156";
        //private string uni = "18";
        //private DateTime begTimeSheet = new DateTime(2023, 3, 1);
        //private DateTime endTimeSheet = new DateTime(2023, 3, 30);
        //private bool useFirefox = false;

        private string eid = "";
        private string asemail = "";
        private string sid = "";
        private string uni = "";
        private DateTime begTimeSheet;
        private DateTime endTimeSheet;
        private bool useFirefox = false;

        private int checkDate(string dateRange)
        {
            // split date with " "
            string[] dates = dateRange.Split(" ");

            // get the month and day for each date
            string[] firstDate = dates[0].Split("/");
            string[] secondDate = dates[2].Split("/");

            // initialize 2 datetimes, with year being the time sheet years, month is [0], day is [1]
            DateTime begDate = new DateTime(begTimeSheet.Year, Convert.ToInt32(firstDate[0]), Convert.ToInt32(firstDate[1]));
            DateTime endDate = new DateTime(endTimeSheet.Year, Convert.ToInt32(secondDate[0]), Convert.ToInt32(secondDate[1]));

            // big ol if statement, returning 0, -1, or 1 (check old code for that)
            if (begTimeSheet >= begDate && begTimeSheet <= endDate)
            {
                return 0;
            }
            else if(begTimeSheet < begDate || begDate.Month == 1 && begTimeSheet.Month == 12) // bad, we are too far forward, need to move back
            {
                return -1; // covers edge case where current month is january but timesheet dates are in december and need to move back
            }
            else // too far back and need to move forward
            {
                return 1;
            }
        }

        private void pageBack(WebDriverWait w)
        {
            IWebElement prevButton = w.Until(driver => driver.FindElement(By.Id("Previous")));
            prevButton.Click();
        }

        private void pageForward(WebDriverWait w)
        {
            IWebElement forwButton = w.Until(driver => driver.FindElement(By.Id("Next")));
            forwButton.Click();
        }

        private bool isDateInRange(string inDate)
        {
            string[] dateList = inDate.Split(" "); // index 0 is m/d/yyyy, 1 is time, 2 is am/pm
            string[] currDate = dateList[0].Split("/");
            DateTime dateCheck = new DateTime(Convert.ToInt32(currDate[2]), Convert.ToInt32(currDate[0]), Convert.ToInt32(currDate[1]));
            return begTimeSheet <= dateCheck && dateCheck <= endTimeSheet;
        }

        private bool checkSecondDate(string inDate)
        {
            string[] dateList = inDate.Split(" ");
            string[] second = dateList[2].Split("/"); // this gives us the second date
            DateTime dateOn = new DateTime(endTimeSheet.Year, Convert.ToInt32(second[0]), Convert.ToInt32(second[1]));

            if(dateOn.Month > endTimeSheet.Month)
            {
                return true;
            }
            return false;
        }

        private string getMonthFromNumber(int month)
        {
            IDictionary<int, string> switcher = new Dictionary<int, string>()
            {
                {1, "January"},
                {2, "February"},
                {3, "March"},
                {4, "April"},
                {5, "May"},
                {6, "June"},
                {7, "July"},
                {8, "August"},
                {9, "September"},
                {10, "October"},
                {11, "November"},
                {12, "December"}
            };

            return switcher[month];
        }

        private string dayIntToString(int num)
        {
            IDictionary<int, string> switcher = new Dictionary<int, string>()
            {
                {0, "MON"},
                {1, "TUE"},
                {2, "WED"},
                {3, "THU"},
                {4, "FRI"},
                {5, "SAT"},
                {6, "SUN"}
            };

            return switcher[num];
        }

        private int findCorrespondingDay(int day, int inc, List<IWebElement> sheet)
        {
            string strDay = dayIntToString(day);
            for(int i = 0; i < sheet.Count; i++)
            {
                string attribute = sheet[i].GetAttribute("data-fieldname");
                if (attribute.Length > 6 && strDay == attribute.Substring(4, 3)) // substring is (from, length)
                {
                    if(inc == 0)
                    {
                        return i;
                    }
                    inc--;
                }
            }
            // should never get here but c# gets mad when there isnt a path that returns this
            return -1;
        }

        // link for using Selenium https://www.javatpoint.com/selenium-csharp
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            // get text from input boxes
            // Result.Text = eid;
            eid = EmployeeID.Text;
            asemail = ASEmail.Text;
            sid = StuEmail.Text;
            uni = Units.Text;

            // process the entered dates and make datetimes out of them
            begTimeSheet = StartDate.SelectedDate ?? DateTime.Now;
            endTimeSheet = EndDate.SelectedDate ?? DateTime.Now;

            // check for update and install if necessary
            IWebDriver driver;

            // in the future i want to add firefox support, but that isnt working at the moment
            //if(useFirefox)
            //{
            //    string file = new DriverManager().SetUpDriver(new FirefoxConfig());
            //    string dir = System.IO.Path.GetDirectoryName(file); // Redundant!
            //    driver = new FirefoxDriver(dir);
            //}
            //else
            //{
            //    string file = new DriverManager().SetUpDriver(new ChromeConfig());
            //    string dir = System.IO.Path.GetDirectoryName(file); // Redundant!
            //    driver = new ChromeDriver(dir);
            //}

            string file = new DriverManager().SetUpDriver(new ChromeConfig());
            string dir = System.IO.Path.GetDirectoryName(file); // Redundant!
            driver = new ChromeDriver(dir);

            driver.Navigate().GoToUrl("https://61783.tcplusondemand.com/app/webclock/#/EmployeeLogOn/61783");

            // wait for element to load
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            IWebElement loginBox = wait.Until(driver => driver.FindElement(By.Id("LogOnEmployeeId")));

            // need to wait a bit so that the element can be clicked
            System.Threading.Thread.Sleep(1000);

            loginBox.SendKeys(eid);
            loginBox.SendKeys(Keys.Return);

            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(3));
            IWebElement viewButton = wait.Until(driver => driver.FindElement(By.Id("View")));
            viewButton.Click();

            IWebElement hoursButton = wait.Until(driver => driver.FindElement(By.Id("ViewHours")));
            hoursButton.Click();

            int rangeNum = 2;
            while(rangeNum != 0)
            {
                System.Threading.Thread.Sleep(100);
                string dateRange = wait.Until(driver => driver.FindElement(By.ClassName("PeriodTotal")).Text);
                rangeNum = checkDate(dateRange);
                if(rangeNum == -1)
                {
                    pageBack(wait);
                }
                if(rangeNum == 1)
                {
                    pageForward(wait);
                }
            }

            // now we are on the page that contains the data we want to scrape
            // TimeSheetDate[] listOfDates = new TimeSheetDate[] { };
            List<TimeSheetDate> listOfDates = new List<TimeSheetDate>();

            bool bGatherDates = true;
            while(bGatherDates)
            {
                System.Threading.Thread.Sleep(100); // sleep a little bit so that the right dates are gathered, otherwise we can have some issues in loop
                ReadOnlyCollection<IWebElement> rows = wait.Until(driver => driver.FindElements(By.TagName("tr"))); // list of all timestamps
                // since there are more than 1 tr elements we have to find the specific one we need
                // 3 is the first tr element we need to scrape, and the last 12 are not needed
                for(int i = 0; i < rows.Count; i++)
                {
                    System.Threading.Thread.Sleep(100); // some elements not loaded without this
                    if(i >= 3 && i < rows.Count - 12)
                    {
                        ReadOnlyCollection<IWebElement> items = rows[i].FindElements(By.TagName("td")); // index 6 = timein, 7 = timeout, 8 = hours

                        // need to make a check for when the weird hidden element is there or not, and if not then subtract index by 1
                        int checkedIndex = 0;
                        if (items[10].Text == "5 - ATI Student Assistant")
                        {
                            checkedIndex = -1;
                        }

                        string dateToCheck = items[6 + checkedIndex].Text;
                        if(isDateInRange(dateToCheck))
                        {
                            // store this value in the list of timesheet objects
                            // first seperate the dates from the times
                            string[] dateIn = dateToCheck.Split(" "); // grab date from here and not from the time out, formatted mm/dd/yyyy hh:mm am/pm
                            string[] dateOut = items[7 + checkedIndex].Text.Split(" ");

                            // create object and add to list
                            listOfDates.Add(new TimeSheetDate(dateIn[0], dateIn[1] + dateIn[2], dateOut[1] + dateOut[2], items[8 + checkedIndex].Text, i == rows.Count - 14));
                        }
                        else
                        {
                            string secDate = wait.Until(driver => driver.FindElement(By.ClassName("PeriodTotal")).Text); // need top date range
                            if(checkSecondDate(secDate))
                            {
                                // if this is false then we are on the first page and should not break loop
                                // but if its true then we should break bc we are out of the range of dates we need to gather
                                // this should break the loop and start putting the dates in the timesheet
                                bGatherDates = false;
                                break;
                            }
                        }
                    }
                    else if (rows[i].Text == "No records found")
                    {
                        string secDate = wait.Until(driver => driver.FindElement(By.ClassName("PeriodTotal")).Text); // need top date range
                        if (checkSecondDate(secDate))
                        {
                            // if this is false then we are on the first page and should not break loop
                            // but if its true then we should break bc we are out of the range of dates we need to gather
                            // this should break the loop and start putting the dates in the timesheet
                            bGatherDates = false;
                            break;
                        }
                    }
                }
                pageForward(wait);
            }

            // since at this point, listOfDates has all the data we need, we can now start putting stuff in the timesheet
            driver.Navigate().GoToUrl("https://csusbsign.na2.documents.adobe.com/account/customComposeJs?workflowid=CBJCHBCAABAAIfLxRZ7xj7zi7uLegFYwKypc4N7ZcGop");

            // sign into adobe sign
            IWebElement user = wait.Until(driver => driver.FindElement(By.Id("EmailPage-EmailField")));
            user.SendKeys(asemail + "@coyote.csusb.edu"); // email goes here
            user.SendKeys(Keys.Return);

            System.Diagnostics.Debug.WriteLine("Please sign in to your MyCoyote to continue..."); // i would like to avoid users putting passwords into the app

            // enters the needed items for the form before the timesheet
            // the xpath is kinda annoying but its what consistently works (:
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(1000)); // the form takes a bit to load sometimes, and we need to wait for sign-in
            System.Threading.Thread.Sleep(100);
            IWebElement reviewerBox = wait.Until(driver => driver.FindElement(By.XPath("//*[@id=\"mainContent\"]/div[2]/div/div[4]/div[2]/div[2]/div/div[2]/div[3]/div/div/div[1]/textarea")));
            reviewerBox.SendKeys("marcy.iniguez@csusb.edu"); // reviewer box

            // supervisor box
            IWebElement supervisorBox = driver.FindElement(By.XPath("//*[@id=\"mainContent\"]/div[2]/div/div[4]/div[2]/div[3]/div/div[2]/div[3]/div/div/div[1]/textarea"));
            supervisorBox.SendKeys("bobby.laudeman@csusb.edu");

            // admin box
            IWebElement adminBox = driver.FindElement(By.XPath("//*[@id=\"mainContent\"]/div[2]/div/div[4]/div[2]/div[4]/div/div[2]/div[3]/div/div/div[1]/textarea"));
            adminBox.SendKeys("jamest@csusb.edu");

            // cc box
            IWebElement ccBox = driver.FindElement(By.XPath("//*[@id=\"mainContent\"]/div[2]/div/div[4]/div[4]/div/div/div[2]/div/div[1]/textarea"));
            ccBox.SendKeys("ashleea.holloway@csusb.edu");

            // need to scroll down to sign button
            IWebElement signButton = driver.FindElement(By.XPath("//*[@id=\"mainContent\"]/div[2]/div/div[6]/div/ul/li/button"));
            signButton.SendKeys(Keys.PageDown);
            signButton.Click();

            // fill out our timesheet now

            // first field: coyote id
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            IWebElement cid = wait.Until(driver => driver.FindElement(By.Name("COYOTE ID")));
            cid.SendKeys(sid);

            // second field: rate of pay
            IWebElement payrate = driver.FindElement(By.Name("PAY_RATE"));
            payrate.SendKeys("15.50");

            // third field: month and year of timesheet
            IWebElement mon = driver.FindElement(By.Name("MONTH  YEAR OF TIMESHEET"));
            mon.SendKeys(getMonthFromNumber(begTimeSheet.Month) + " " + begTimeSheet.Year);

            // fourth field: student job title
            IWebElement jobTitle = driver.FindElement(By.Name("STUDENT JOB TITLE"));
            jobTitle.SendKeys("Student Assistant");

            // fifth field: department
            IWebElement dept = driver.FindElement(By.Name("DEPARTMENT"));
            dept.SendKeys("ITS ATI");

            // sixth field: unit enrollment
            IWebElement units = driver.FindElement(By.Name("Current Unit Enrollment"));
            units.SendKeys(uni);

            // now we fill out the times and dates
            // first get all elements of class 'pdfFormField text_field todo-done'
            List<IWebElement> sheetRange = new List<IWebElement>();
            sheetRange.AddRange(driver.FindElements(By.ClassName("todo-done")));

            // need to fix the goofy ahh tuesday that doesnt show up in the pdf, so we fix it here in the list of elements
            IWebElement badTues = driver.FindElement(By.XPath("//*[@id=\"document\"]/ul/li/div[191]"));

            // get index of item before it to know where to insert it
            IWebElement preBadTues = driver.FindElement(By.XPath("//*[@id=\"document\"]/ul/li/div[188]"));
            int badindex = sheetRange.IndexOf(preBadTues);
            sheetRange.Insert(badindex + 1, badTues);

            // now insert data into the timesheet
            int[] dayCounterList = { 0, 0, 0, 0, 0, 0, 0 };
            for(int j = 0; j < listOfDates.Count; j++)
            {
                int dayOfWeek = listOfDates[j].getDayWeek();
                
                // check if the date we are on matches the previous date
                if(j != 0 && listOfDates[j].getPrintableDate() == listOfDates[j - 1].getPrintableDate())
                {
                    // get child so that element is interactable
                    int ranInt = findCorrespondingDay(dayOfWeek, dayCounterList[dayOfWeek] - 1, sheetRange);
                    IWebElement tiBox2 = sheetRange[ranInt + 3].FindElement(By.ClassName("text_field_input"));
                    IWebElement toBox2 = sheetRange[ranInt + 4].FindElement(By.ClassName("text_field_input"));

                    // put hours into the boxes
                    tiBox2.SendKeys(listOfDates[j].getTimeSheetTimeIn());
                    toBox2.SendKeys(listOfDates[j].getTimeSheetTimeOut());
                }
                else
                {
                    // get child of each box so we can input into it
                    int ranInt = findCorrespondingDay(dayOfWeek, dayCounterList[dayOfWeek], sheetRange);
                    IWebElement dateBox = sheetRange[ranInt].FindElement(By.ClassName("text_field_input"));
                    IWebElement tiBox = sheetRange[ranInt + 1].FindElement(By.ClassName("text_field_input"));
                    IWebElement toBox = sheetRange[ranInt + 2].FindElement(By.ClassName("text_field_input"));

                    // put new date and hours in
                    dateBox.SendKeys(listOfDates[j].getPrintableDate());
                    tiBox.SendKeys(listOfDates[j].getTimeSheetTimeIn());
                    toBox.SendKeys(listOfDates[j].getTimeSheetTimeOut());

                    // increment number in day counter list
                    dayCounterList[dayOfWeek]++;
                }

                // make sure days are put on the right week
                if (listOfDates[j].isEndOfWeek)
                {
                    for(int t = 0; t < dayCounterList.Length; t++)
                    {
                        if(t <= dayOfWeek)
                        {
                            if (dayCounterList[t] < dayCounterList[dayOfWeek])
                            {
                                dayCounterList[t]++;
                            }
                        }
                        else
                        {
                            // here, the day is in the future so we check if its behind by 2, since being behind by 1 is normal
                            if (dayCounterList[t] > dayCounterList[dayOfWeek] + 1)
                            {
                                dayCounterList[t]++;
                            }
                        }
                    }
                }
            }
        }
    }
}


// TODO: fix starting day and military time