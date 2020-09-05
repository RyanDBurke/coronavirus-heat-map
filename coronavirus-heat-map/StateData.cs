using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Documents;

namespace coronavirus_heat_map {
    class StateData {
        // data is linked via their indices within their respected structures
        readonly List<int> unixTimes = new List<int>();
        readonly List<int> numTested = new List<int>();
        readonly List<int> numPositive = new List<int>();
        readonly List<int> numDeaths = new List<int>();
        readonly string webData;

        public StateData(string state) {
            System.Net.WebClient wc = new System.Net.WebClient();
            webData = wc.DownloadString("http://coronavirusapi.com/getTimeSeries/" + state);
            parseData();
        }

        public void parseData() {
            var sr = new StringReader(webData);

            int firstLine = 0;
            string line;

            // for each line
            while ((line = sr.ReadLine()) != null) {
                // skip the first line
                if (firstLine == 0) {
                    firstLine = 1;
                } else {
                    List<int> data = line.Split(',').Select(int.Parse).ToList();
                    unixTimes.Add(data[0]);
                    numTested.Add(data[1]);
                    numPositive.Add(data[2]);
                    numDeaths.Add(data[3]);
                }
            }
        }

        public DateTime UnixTimeStampToDateTime(double unixTimeStamp) {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


        /* returns the percentage-difference between current and previous month of tested, positive, or deaths */
        public double percentageDifference(string dataType) {
            List<int> data;
            dataType = dataType.ToLower();

            if (dataType == "tested") {
                data = new List<int>(numTested);
            } else if (dataType == "positive") {
                data = new List<int>(numPositive);
            } else if (dataType == "deaths") {
                data = new List<int>(numDeaths);
            } else {
                Console.WriteLine("Invalid Parameter in StateData.percentageIncrease()");
                return -1;
            }

            int currentMonth = UnixTimeStampToDateTime(unixTimes[unixTimes.Count - 1]).Month;
            int currentMonthNum = data[data.Count - 1];

            int previousMonth = (currentMonth == 1) ? 12 : currentMonth - 1; // handles January/December
            int previousMonthNum = -1;

            int breakLoop = 0;

            // get previous month's data
            for (int i = unixTimes.Count - 1; i >= 0; i--) {
                if (UnixTimeStampToDateTime(unixTimes[i]).Month == previousMonth) {
                    for (int j = i; j >= 0; j--) {
                        if (UnixTimeStampToDateTime(unixTimes[j]).Month == previousMonth) {
                            previousMonthNum = data[j];
                        } else {
                            breakLoop = 1;
                            break;
                        }
                    }
                }

                if (breakLoop == 1) {
                    break;
                }
            }

            if (currentMonthNum > previousMonthNum) {
                double numIncrease = currentMonthNum - previousMonthNum;
                return (numIncrease / previousMonthNum) * 100;
            } else {
                double numDecrease = previousMonthNum - currentMonthNum;
                return (numDecrease / previousMonthNum) * 100;
            }
        }

        /* returns necessary data to build line Graph (only records last 6 months) */
        /* worth nothing keys are sorted in reverse order (most recent month first) */
        public Dictionary<int, int> barGraphData(string dataType) {

            // <month, numData>
            Dictionary<int, int> graphData = new Dictionary<int, int>();
            List<int> data;

            if (dataType == "tested") {
                data = new List<int>(numTested);
            } else if (dataType == "positive") {
                data = new List<int>(numPositive);
            } else if (dataType == "deaths") {
                data = new List<int>(numDeaths);
            } else {
                Console.WriteLine("Invalid Parameter in StateData.percentageIncrease()");
                return null;
            }

            int monthsAdded = 0;

            for (int i = unixTimes.Count - 1; i >= 0; i--) {
                int month = UnixTimeStampToDateTime(unixTimes[i]).Month;
                if (!graphData.ContainsKey(month)) {
                    graphData[month] = data[i];
                    monthsAdded++;

                    // only add last 6 months
                    if (monthsAdded == 6) { return graphData; }
                }
            }

            /* should never reach here */

            /*  PROBLEM IS HERE!!, WHAT IF ONLY LAST 5 MONTHS ARE RECORDED */
            return null;
        }

        /* getters */
        public List<int> getUnixTimes() { return unixTimes; }
        public List<int> getNumTested() { return numTested; }
        public List<int> getNumPositive() { return numPositive; }
        public List<int> getNumDeaths() { return numDeaths; }
    }
}
