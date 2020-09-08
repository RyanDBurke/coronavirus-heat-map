using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

/*
 * [CORONAVIRUS-HEAT-MAP]
 * 
 * I avoided using the MVVM-Framework because
      I figured it would be sort of overkill for what
      I consider a pretty straight-forward application
 * 
 * So, instead its broken into two main C# files
    * StateData.cs
        * Fetches data for each state
        * Performs ~some~ functions for the data collected (probably shouldve split it up for clarity, but I'll do that in the future)
    * MainWindow.xaml.cs
        * Manages UI
        * Evaluates and performs ~most~ functions for the data collected
        * communicates with the XAML tags </>
        * 
  * Future Goals
    * CLARITY
    * How can I make this code easy to follow to the extent that someone could read, understand, and build upon what's already written
    * 
  * NOTE
    * I use the word "state" alot here. Sometimes to refer to actual states (Maryland, New York, etc)
        and other times to refer to the state of the UI (heat map or interactive map). I hope
        context-clues negate any semantic ambiguity.
*/

namespace coronavirus_heat_map {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        // STATE SELECTED
        private string STATE = null;

        // CURRENT MAP STATE (HEAT OR INTERACTIVE)
        // App was built on interactive being the default view, so DO NOT change
        private string MAP_STATE = "interactive";

        // different options for the heat map
        // default option is "tested"
        private string heatMapOption = "tested";

        // percentiles
        private Dictionary<string, int> testedPercentiles;
        private Dictionary<string, int> positivePercentiles;
        private Dictionary<string, int> deathsPercentiles;

        // <State Abbreviation : Full State Name>
        private Dictionary<string, string> statePairs = new Dictionary<string, string>() {
            {"AL","Alabama"}, {"AK","Alaska"}, {"AZ","Arizona"}, {"AR","Arkansas"}, {"CA","California"}, {"CO","Colorado"}, {"CT","Connecticut"}
            , {"DE","Delaware"}, {"FL","Florida"}, {"GA","Georgia"}, {"HI","Hawaii"}, {"ID","Idaho"}, {"IL","Illinois"}, {"IN","Indiana"}, {"IA","IOWA"}
            , {"KS","Kansas"}, {"KY","Kentucky"}, {"LA","Louisiana"}, {"ME","Maine"}, {"MD","Maryland"}, {"MA","Massachusetts"}, {"MI","Michigan"}, {"MN","Minnesota"}
            , {"MS","Mississippi"}, {"MO","Missouri"}, {"MT","Montana"}, {"NE","Nebraska"}, {"NV","Nevada"}, {"NH","New Hampshire"}, {"NJ","New Jersey"}, {"NM","New Mexico"}
            , {"NY","New York"}, {"NC","North Carolina" }, {"ND","North Dakota"}, {"OH","Ohio"}, {"OK","Oklahoma"}, {"OR","Oregon"}, {"PA","Pennslyvania"}, {"RI","Rhode Island"}
            , {"SC","South Carolina"}, {"SD","South Dakota"}, {"TN","Tennessee"}, {"TX","Texas"}, {"UT","Utah"}, {"VT","Vermont"}, {"VA","Virginia"}, {"WA","Washington"}
            , {"WV","West Virginia"}, {"WI","Wisconsin"}, {"WY","Wyoming"}, {"DC","District of Columbia"}
        };

        // list of valid month names
        private List<string> monthStrings = new List<string>() { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };


        public MainWindow() {

            // Sleep window to allow for longer splash--screen
            System.Threading.Thread.Sleep(0);
            InitializeComponent();

            // Screen-Size Properties
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // Minimize, Maximize (disabled), Open Github, Open CDC, and Close Window
            MinimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;
            Github.Click += (s, e) => System.Diagnostics.Process.Start("https://github.com/RyanDBurke/coronavirus-heat-map");
            CDC.Click += (s, e) => System.Diagnostics.Process.Start("https://www.cdc.gov/coronavirus/2019-ncov/index.html?CDC_AA_refVal=https%3A%2F%2Fwww.cdc.gov%2Fcoronavirus%2Findex.html");
            // MaximizeButton.Click += (s, e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            CloseButton.Click += (s, e) => Close();

            // calculate current percentiles on startup to avoid slowing UI controls
            testedPercentiles = statePercentile("tested");
            positivePercentiles = statePercentile("positive");
            deathsPercentiles = statePercentile("deaths");
        }

        // displays the data for the state on the left side
        public void leftSide(string state) {
            StateData st = new StateData(state);
            List<int> tested = st.getNumTested();
            List<int> positive = st.getNumPositive();
            List<int> deaths = st.getNumDeaths();
            List<int> unixTimes = st.getUnixTimes();

            // display data
            numTested.Text = (tested[tested.Count - 1] > 1000000) ? ((double)tested[tested.Count - 1] / 1000000).ToString("0.##") + "M" : tested[tested.Count - 1].ToString("#,##0");
            numPositive.Text = (positive[positive.Count - 1] > 1000000) ? ((double)positive[positive.Count - 1] / 1000000).ToString("0.##") + "M" : positive[positive.Count - 1].ToString("#,##0");
            numDeaths.Text = (deaths[deaths.Count - 1] > 1000000) ? ((double)deaths[deaths.Count - 1] / 1000000).ToString("0.##") + "M" : deaths[deaths.Count - 1].ToString("#,##0");

            // display percents
            percentTestedIncrease.Text = "+" + st.percentageDifference("tested").ToString("0") + "%";
            percentPositiveIncrease.Text = "+" + st.percentageDifference("positive").ToString("0") + "%";
            percentDeathsIncrease.Text = "+" + st.percentageDifference("deaths").ToString("0") + "%";

            // ToolTip for percents
            int currentMonth = st.UnixTimeStampToDateTime(unixTimes[unixTimes.Count - 1]).Month;
            int previousMonth = (currentMonth == 1) ? 12 : currentMonth - 1; // handles January/December
            percentTestedIncrease.ToolTip = " up " + st.percentageDifference("tested").ToString("0") + "% since " + monthStrings[previousMonth - 1];
            percentPositiveIncrease.ToolTip = " up " + st.percentageDifference("positive").ToString("0") + "% since " + monthStrings[previousMonth - 1];
            percentDeathsIncrease.ToolTip = " up " + st.percentageDifference("deaths").ToString("0") + "% since " + monthStrings[previousMonth - 1];
        }

        // builds bar-graph based on state selected and dataType ("tested", "positive", or "deaths")
        public void buildBarGraph(string state, string dataType) {

            // pull rleevant state data
            StateData st = new StateData(state);

            // returns max value of all keys in dictionary (helpful for creating bargraph bounds)
            int maxValue = st.barGraphData(dataType).Values.Max();

            // these are the 14 different value stops our data could "attach" to
            // the bar graph is from 0 - maxValue, and theres 14 valid "heights" for each bar
            double barGraphIncrement = maxValue / 14;

            // List of the literal number at each "stop"
            List<double> barStops = new List<double>();

            // fill barStops with those 14 numbers
            double stopNum = barGraphIncrement; // lowest value for graph would be the first stop (wont be zero)
            for (int i = 0; i < 14; i++) {
                barStops.Add(stopNum);
                stopNum += barGraphIncrement;
            }

            // I choose to dump my dictionary into 2 Lists, associated by their index
            List<int> months = new List<int>();
            List<int> dataNum = new List<int>();
            foreach (KeyValuePair<int, int> kvp in st.barGraphData(dataType)) {
                months.Add(kvp.Key);
                dataNum.Add(kvp.Value);
            }

            // assigned month names in bar graph
            month6Name.Text = monthStrings[months[0] - 1];
            month5Name.Text = monthStrings[months[1] - 1];
            month4Name.Text = monthStrings[months[2] - 1];
            month3Name.Text = monthStrings[months[3] - 1];
            month2Name.Text = monthStrings[months[4] - 1];
            month1Name.Text = monthStrings[months[5] - 1];

            // adjust margin-tops for each bar
            int indexBarStop = 0;
            int currentMonth = 6;
            foreach (int d in dataNum) {

                double lowestDifference = Double.PositiveInfinity;

                for (int i = 0; i < barStops.Count; i++) {
                    double difference = Math.Abs(d - barStops[i]);

                    if (difference < lowestDifference) {
                        lowestDifference = difference;
                        indexBarStop = i;
                    }
                }


                // marginTop for each bar in barGraph
                int marginTop = (((barStops.Count - indexBarStop + 1) * 5) + 5 >= 75) ? 70 : ((barStops.Count - indexBarStop + 1) * 5) + 5;

                // change color of each bar
                var bc = new BrushConverter();

                // adjust margin-tops for each bar
                if (currentMonth == 6) {
                    currentMonth -= 1;
                    month6.Margin = new Thickness(185, marginTop, 0, 0);
                    month6.Background = (Brush)bc.ConvertFrom("#bb86fc");
                    month6Name.Margin = new Thickness(10, 55 - marginTop, 10, 0);
                } else if (currentMonth == 5) {
                    currentMonth -= 1;
                    month5.Margin = new Thickness(111, marginTop, 0, 0);
                    month5.Background = (Brush)bc.ConvertFrom("#bb86fc");
                    month5Name.Margin = new Thickness(10, 55 - marginTop, 10, 0);
                } else if (currentMonth == 4) {
                    currentMonth -= 1;
                    month4.Margin = new Thickness(37, marginTop, 0, 0);
                    month4.Background = (Brush)bc.ConvertFrom("#bb86fc");
                    month4Name.Margin = new Thickness(10, 55 - marginTop, 10, 0);
                } else if (currentMonth == 3) {
                    currentMonth -= 1;
                    month3.Margin = new Thickness(-37, marginTop, 0, 0);
                    month3.Background = (Brush)bc.ConvertFrom("#bb86fc");
                    month3Name.Margin = new Thickness(10, 55 - marginTop, 10, 0);
                } else if (currentMonth == 2) {
                    currentMonth -= 1;
                    month2.Margin = new Thickness(-111, marginTop, 0, 0);
                    month2.Background = (Brush)bc.ConvertFrom("#bb86fc");
                    month2Name.Margin = new Thickness(10, 55 - marginTop, 10, 0);
                } else {
                    currentMonth -= 1;
                    month1.Margin = new Thickness(-185, marginTop, 0, 0);
                    month1.Background = (Brush)bc.ConvertFrom("#bb86fc");
                    month1Name.Margin = new Thickness(10, 55 - marginTop, 10, 0);
                }
            }
        }


        // use to select which data you'd like to see for the bar graph 
        public void barGraphText(object sender, MouseButtonEventArgs e) {

            if (STATE == null) {
                return;
            }

            string dataType = ((string)(((TextBlock)sender).Tag)).ToLower();

            switch (dataType) {
                case "tested":
                    testedBar.Foreground = Brushes.White;
                    positiveBar.Foreground = Brushes.Gray;
                    deathsBar.Foreground = Brushes.Gray;
                    break;
                case "positive":
                    testedBar.Foreground = Brushes.Gray;
                    positiveBar.Foreground = Brushes.White;
                    deathsBar.Foreground = Brushes.Gray;
                    break;
                case "deaths":
                    testedBar.Foreground = Brushes.Gray;
                    positiveBar.Foreground = Brushes.Gray;
                    deathsBar.Foreground = Brushes.White;
                    break;
            }
            
            buildBarGraph(STATE, dataType);
            

        }

        // handles all states being clicked in "interactive" UI state
        public void clickedState(object sender, MouseButtonEventArgs e) {

            string state = (string)((Path)sender).Tag;
            STATE = state;
            leftSide(STATE);
            currentStateClicked.Text = statePairs[STATE.ToUpper()];

            // default bar graph view
            buildBarGraph(STATE, "tested");
            testedBar.Foreground = Brushes.White;
            positiveBar.Foreground = Brushes.Gray;
            deathsBar.Foreground = Brushes.Gray;
        }

        // changes between heat and interactive map UI state
        public void changeMapState(object sender, MouseButtonEventArgs e) {

            // set UI MAP_STATE
            string map_state = ((string)((TextBlock)sender).Tag).ToLower();

            // to avoid re-loading current UI State
            if (map_state == MAP_STATE) {
                return;
            } else {
                MAP_STATE = map_state;
            }
            

            // color depending on state's percentile!
            string[] colors = new string[] { "#ffb3b3", "#ff6666", "#ff0000", "#b30000", "#4d0000" };

            // revert STATE to null
            STATE = null;

            // clear stats on left-sidebar
            numTested.Text = "--";
            numPositive.Text = "--";
            numDeaths.Text = "--";

            // reset percentage increases
            percentTestedIncrease.Text = "";
            percentPositiveIncrease.Text = "";
            percentDeathsIncrease.Text = "";

            // clear bar-graph
            var bc = new BrushConverter();
            month6.Background = (Brush)bc.ConvertFrom("#292929");
            month5.Background = (Brush)bc.ConvertFrom("#292929");
            month4.Background = (Brush)bc.ConvertFrom("#292929");
            month3.Background = (Brush)bc.ConvertFrom("#292929");
            month2.Background = (Brush)bc.ConvertFrom("#292929");
            month1.Background = (Brush)bc.ConvertFrom("#292929");

            // reset bar-graph text
            month6Name.Text = "";
            month5Name.Text = "";
            month4Name.Text = "";
            month3Name.Text = "";
            month2Name.Text = "";
            month1Name.Text = "";

            // reset bar-graph data selection
            testedBar.Foreground = Brushes.Gray;
            positiveBar.Foreground = Brushes.Gray;
            deathsBar.Foreground = Brushes.Gray;

            // reset current map clicked text
            currentStateClicked.Text = "--";

            // rearrange UI according to MAP_STATE
            switch (MAP_STATE) {
                case "heat":

                    // change heat/interactive button opacity, for clarity
                    heatButton.Opacity = 1;
                    interactiveButton.Opacity = .5;

                    // enable color blocks
                    colorBlocks.Opacity = 1;

                    // enable heat-map options
                    testedHeatMapOption.Opacity = 1;
                    deathsHeatMapOption.Opacity = .5;

                    // enables heat-map options (positive option)
                    // kinda confusing cause currentStateClicked is now the "positive" button
                    // So, what I do is rearrange currentStateClicked to match the size of other heat-map options
                    currentStateClicked.Width = 80;
                    currentStateClicked.Margin = new Thickness(5,15,0,5);
                    currentStateClicked.Text = "Positive";
                    currentStateClicked.Background = (Brush)bc.ConvertFrom("#121212");
                    currentStateClicked.Opacity = .5;
                    currentStateClicked.FontSize = 12;

                    // change map interactions
                    MAP.IsEnabled = false;

                    // assign percentiles
                    Dictionary<string, int> percentiles;
                    switch (heatMapOption) {
                        case "tested":
                            percentiles = testedPercentiles;
                            break;
                        case "positive":
                            percentiles = positivePercentiles;
                            break;
                        case "deaths":
                            percentiles = deathsPercentiles;
                            break;
                        default:
                            percentiles = testedPercentiles;
                            break;
                    }

                    // change map colors
                    foreach (KeyValuePair<string, string> states in statePairs) {
                        var name = (Path)this.FindName(states.Key);
                        name.Fill = (Brush)bc.ConvertFrom(colors[percentiles[states.Key] - 1]);
                    }

                    break;
                case "interactive":

                    // change button opacity, for clarity
                    interactiveButton.Opacity = 1;
                    heatButton.Opacity = .5;

                    // disable color blocks
                    colorBlocks.Opacity = 0;

                    // enable heat-map options
                    testedHeatMapOption.Opacity = 0;
                    deathsHeatMapOption.Opacity = 0;

                    // enables heat-map options (positive option)
                    // kinda confusing cause currentStateClicked is now the "positive" button
                    currentStateClicked.Width = 250;
                    currentStateClicked.Margin = new Thickness(-80, -2, 0, 4);
                    currentStateClicked.Text = "--";
                    currentStateClicked.Background = (Brush)bc.ConvertFrom("#292929");
                    currentStateClicked.Opacity = 1;
                    currentStateClicked.FontSize = 25;

                    // change map interactions
                    MAP.IsEnabled = true;

                    // reset map colors
                    foreach (KeyValuePair<string, string> states in statePairs) {
                        var name = (Path)this.FindName(states.Key);
                        name.Fill = (Brush)bc.ConvertFrom("#ffd2d2d2");
                    }

                    break;
            }
        }

        // returns a dictionary of <state, percentile #>
        // percentile is a number is 1-5
        /*
             * 1 => < 20th percentile
             * 2 => 20th percentile > but < 40th percentile
             * 3 => 40th percentile > but < 60th percentile
             * 4 => 60th percentile > but < 80th percentile
             * 5 => > 80th percentile
        */
        public Dictionary<string, int> statePercentile(string dataType) {

            // hold each states percentile
            Dictionary<string, int> percentile = new Dictionary<string, int>();

            // holds each states relevant data (tested, positive, deaths)
            Dictionary<string, int> dataForState = new Dictionary<string, int>();

            foreach (string state in statePairs.Keys) {

                // fetch data for each state
                StateData currentState = new StateData(state);

                // add states to percentile
                percentile[state] = -1;

                // data
                List<int> data;

                // fetch data
                if (dataType == "tested") {
                    data = currentState.getNumTested();
                } else if (dataType == "positive") {
                    data = currentState.getNumPositive();
                } else if (dataType == "deaths") {
                    data = currentState.getNumDeaths();
                } else {
                    Console.WriteLine("Invalid Parameter in MainWindow.cs statePercentile");
                    return null;
                }

                // add <state, relevant data> pairs for most recent available data
                dataForState[state] = data[data.Count - 1];
            }

            // order states by value
            var sortedData = from pair in dataForState orderby pair.Value ascending select pair;
            List<string> statesSorted = new List<string>();
            foreach (KeyValuePair<string, int> kvp in sortedData) {
                statesSorted.Add(kvp.Key);
            }

            //  return percentile;
            int numStates = statesSorted.Count;
            int numStatesInPercentile = numStates / 5;

            for (int i = 0; i < numStates; i++) {
                if (i < numStatesInPercentile * 1) {
                    percentile[statesSorted[i]] = 1;
                } else if (i >= numStatesInPercentile * 1 && i < numStatesInPercentile * 2) {
                    percentile[statesSorted[i]] = 2;
                } else if (i >= numStatesInPercentile * 2 && i < numStatesInPercentile * 3) {
                    percentile[statesSorted[i]] = 3;
                } else if (i >= numStatesInPercentile * 3 && i < numStatesInPercentile * 4) {
                    percentile[statesSorted[i]] = 4;
                } else {
                    percentile[statesSorted[i]] = 5;
                }
            }
            return percentile;
        }


        // handles when a heat-map option is selected
        public void heatMapOptions(object sender, MouseButtonEventArgs e) {

            // only register event if MAP_STATE is "heat"
            if (MAP_STATE == "interactive") {
                return;
            } else {

                // color depending on state's percentile 
                string[] colors = new string[] { "#ffb3b3", "#ff6666", "#ff0000", "#b30000", "#4d0000" };

                // option selected
                string optionSelected = (string)((TextBlock)sender).Tag;

                switch (optionSelected) {
                    case "testedOption":
                        heatMapOption = "tested";
                        testedHeatMapOption.Opacity = 1;
                        deathsHeatMapOption.Opacity = .5;
                        currentStateClicked.Opacity = .5;
                        break;
                    case "positiveOption":
                        heatMapOption = "positive";
                        testedHeatMapOption.Opacity = .5;
                        deathsHeatMapOption.Opacity = .5;
                        currentStateClicked.Opacity = 1;
                        break;
                    case "deathsOption":
                        heatMapOption = "deaths";
                        testedHeatMapOption.Opacity = .5;
                        deathsHeatMapOption.Opacity = 1;
                        currentStateClicked.Opacity = .5;
                        break;
                }

                // assign percentiles
                Dictionary<string, int> percentiles;
                switch (heatMapOption) {
                    case "tested":
                        percentiles = testedPercentiles;
                        break;
                    case "positive":
                        percentiles = positivePercentiles;
                        break;
                    case "deaths":
                        percentiles = deathsPercentiles;
                        break;
                    default:
                        percentiles = testedPercentiles;
                        break;
                }

                // change map colors
                var bc = new BrushConverter();
                foreach (KeyValuePair<string, string> states in statePairs) {
                    var name = (Path)this.FindName(states.Key);
                    name.Fill = (Brush)bc.ConvertFrom(colors[percentiles[states.Key] - 1]);
                }
            }

        }



    }
}
