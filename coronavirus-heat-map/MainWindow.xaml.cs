using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
using Microsoft.Maps.MapControl.WPF;

namespace coronavirus_heat_map {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        // STATE SELECTED
        public string STATE = "md";

        public MainWindow() {

            // Sleep window to allow for longer splash-screen
            System.Threading.Thread.Sleep(5000);
            InitializeComponent();

            // Screen-Size Properties
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // Minimize, Maximize (disabled), Open Github, and Close Window
            MinimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;
            Github.Click += (s, e) => System.Diagnostics.Process.Start("https://github.com/RyanDBurke/coronavirus-heat-map");
            // MaximizeButton.Click += (s, e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            CloseButton.Click += (s, e) => Close();

            // Left Side (would change via right-side input)
            // leftSide(STATE);

            // Right-Side & Map
        }

        // displays the data for the state on the left side
        public void leftSide(string state) {
            StateData st = new StateData(state);
            List<int> tested = st.getNumTested();
            List<int> positive = st.getNumPositive();
            List<int> deaths = st.getNumDeaths();
            List<int> time = st.getUnixTimes();

            // display data
            numTested.Text = (tested[tested.Count - 1] > 1000000) ? ((double)tested[tested.Count - 1] / 1000000).ToString("0.##") + "M" : tested[tested.Count - 1].ToString("#,##0");
            numPositive.Text = (positive[positive.Count - 1] > 1000000) ? ((double)positive[positive.Count - 1] / 1000000).ToString("0.##") + "M" : positive[positive.Count - 1].ToString("#,##0");
            numDeaths.Text = (deaths[deaths.Count - 1] > 1000000) ? ((double)deaths[deaths.Count - 1] / 1000000).ToString("0.##") + "M" : deaths[deaths.Count - 1].ToString("#,##0");

            // display percents
            percentTestedIncrease.Text = "+" + st.percentageDifference("TESTED").ToString("0") + "%";
            percentPositiveIncrease.Text = "+" + st.percentageDifference("positive").ToString("0") + "%";
            percentDeathsIncrease.Text = "+" + st.percentageDifference("deaths").ToString("0") + "%";
        }

        public void buildBarGraph(string state, string dataType) {

            // pull state data
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

            // list of valid month names
            List<string> monthStrings = new List<string>() { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

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


        /* use to select which data you'd like to see for the bar graph */
        public void barGraphTextTested(object sender, MouseButtonEventArgs e) {
            testedBar.Foreground = Brushes.White;
            positiveBar.Foreground = Brushes.Gray;
            deathsBar.Foreground = Brushes.Gray;

            buildBarGraph(STATE, "tested");
        }
        public void barGraphTextPositive(object sender, MouseButtonEventArgs e) {
            testedBar.Foreground = Brushes.Gray;
            positiveBar.Foreground = Brushes.White;
            deathsBar.Foreground = Brushes.Gray;

            buildBarGraph(STATE, "positive");
        }

        public void barGraphTextDeaths(object sender, MouseButtonEventArgs e) {
            testedBar.Foreground = Brushes.Gray;
            positiveBar.Foreground = Brushes.Gray;
            deathsBar.Foreground = Brushes.White;

            buildBarGraph(STATE, "deaths");
        }


        /* this needs to handle all states being clicked! */
        public void clickedState(object sender, MouseButtonEventArgs e) {
            
        }

    }
}
