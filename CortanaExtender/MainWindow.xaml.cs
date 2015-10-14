using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using WindowsInput;
using System.IO;
using System.Windows.Forms;

namespace CortanaExtender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> currentlyStoredConstraints;
        private Dictionary<string, string> dateTimesForConstraints;
        private List<string> constraints;
        private SpeechRecognizer backgroundListener;
        private TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs> BLResultGenerated;
        private NotifyIcon notifyIcon;
        private string filePath;

        /// <summary>
        /// Constructor initializes necessary variables and reads in saved constraints from text file.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            string fileName = @"Stored_Constraints.txt";
            Debug.WriteLine(DateTime.Now.ToString());
            filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fileName);
            dateTimesForConstraints = new Dictionary<string, string>();

            backgroundListener = new SpeechRecognizer();
            constraints = new List<string>();
            BLResultGenerated = new TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs>(blResultGenerated);
            backgroundListener.ContinuousRecognitionSession.ResultGenerated += BLResultGenerated;

            constraints = readInConstraintsFromFile();
            currentlyStoredConstraints = constraints.ToList();
            updateConstraintsWindow(constraints);

            this.Closing += OnAppClosing;

            var waitOn = loadOnStart();
            while (waitOn.Status != AsyncStatus.Completed) { }
            var ff = backgroundListener.ContinuousRecognitionSession.StartAsync();

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new System.Drawing.Icon("trayImage.ico");
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
        }

        /// <summary>
        /// Reads in saved constraints from text file.
        /// </summary>
        /// <returns>List of constraint strings.</returns>
        private List<string> readInConstraintsFromFile()
        {
            List<string> existingConstraints = new List<string>();
            string line;

            char[] delimeter = new char[1] { ',' };
            string[] result = new string[2];

            try
            {
                if (!File.Exists(filePath))
                {
                    File.Create(filePath);
                }

                StreamReader reader = new StreamReader(filePath);
                while ((line = reader.ReadLine()) != null)
                {
                    result = line.Split(delimeter);
                    existingConstraints.Add(result[0]);
                    dateTimesForConstraints.Add(result[0], result[1]);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return existingConstraints;
        }
        
        /// <summary>
        /// Writes all current constraints in "constraints" variable to the saved text file if they are not already there.
        /// </summary>
        private void WriteConstraintsToFile()
        {
            IEnumerable<string> NewConstraintsToSave = constraints.Except(currentlyStoredConstraints);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                foreach (string str in NewConstraintsToSave)
                {
                    writer.WriteLine(str + "," + dateTimesForConstraints[str]);
                }
                writer.Close();
            }
        }

        /// <summary>
        /// Event to run on application closing. Turns off the notify icon and saves constraints to storage file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAppClosing(Object sender, EventArgs args)
        {
            notifyIcon.Visible = false;
            WriteConstraintsToFile();
        }

        /// <summary>
        /// Triggers cortana with simulated key combo if constraint is heard.
        /// </summary>
        /// <param name="session">backgroundListener's continuous recognition session.</param>
        /// <param name="args">Result arguments</param>
        private void blResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if (args.Result.Constraint != null)
            {
                switch (args.Result.Text)
                {
                    case "Open Netflix now":
                        Process.Start(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", @"http:\\www.netflix.com");
                        break;
                    default:
                        InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_S);
                        break;
                }
            }
        }

        /// <summary>
        /// Add's value of custom phrase textbox to constraints list and recompiles the listener's constraints.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Add_Constraint(Object sender, RoutedEventArgs e)
        {
            if (customPhrase.Text != "" && customPhrase.Text != null)
            {
                if (backgroundListener.State != SpeechRecognizerState.Idle && backgroundListener.State != SpeechRecognizerState.Paused)
                {
                    var async = backgroundListener.ContinuousRecognitionSession.StopAsync();
                    while (async.Status != AsyncStatus.Completed) { }
                }

                constraints.Add(customPhrase.Text);
                dateTimesForConstraints.Add(customPhrase.Text, DateTime.Now.ToString());
                updateConstraintsWindow(constraints);
                customPhrase.Text = string.Empty;
                backgroundListener.Constraints.Clear();
                backgroundListener.Constraints.Add(new SpeechRecognitionListConstraint(constraints));
                var blCompileConstraints = backgroundListener.CompileConstraintsAsync();
                while (blCompileConstraints.Status != AsyncStatus.Completed) { }
                var leavemealone = backgroundListener.ContinuousRecognitionSession.StartAsync();
            }
        }

        /// <summary>
        /// Uses removePhrase text box's value to remove value from constraints and if it exists deletes it from storage as well.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Remove_Constraint(Object sender, RoutedEventArgs e)
        {
            if (removePhrase.Text != "" && removePhrase.Text != null)
            {
                if (backgroundListener.State != SpeechRecognizerState.Idle && backgroundListener.State != SpeechRecognizerState.Paused)
                {
                    var async = backgroundListener.ContinuousRecognitionSession.StopAsync();
                    while (async.Status != AsyncStatus.Completed) { }
                }

                if (constraints.Remove(removePhrase.Text))
                {
                    DeleteConstraintFromFile(removePhrase.Text + "," + dateTimesForConstraints[removePhrase.Text]);
                    dateTimesForConstraints.Remove(removePhrase.Text);
                }

                updateConstraintsWindow(constraints);
                removePhrase.Text = string.Empty;
                backgroundListener.Constraints.Clear();
                backgroundListener.Constraints.Add(new SpeechRecognitionListConstraint(constraints));
                var blCompileConstraints = backgroundListener.CompileConstraintsAsync();
                while (blCompileConstraints.Status != AsyncStatus.Completed) { }
                var useless = backgroundListener.ContinuousRecognitionSession.StartAsync();
            }
        }

        /// <summary>
        /// Delete's given value from the constraint's text file storage.
        /// </summary>
        /// <param name="toDelete">Constraint to delete.</param>
        private void DeleteConstraintFromFile(string toDelete)
        {
            StringBuilder builder = new StringBuilder();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = "";
                while ((line = reader.ReadLine()) != null && line != toDelete)
                {
                    builder.AppendLine(line);
                }
                reader.Close();
            }
            File.WriteAllText(filePath, builder.ToString());
        }

        /// <summary>
        /// Updates the text block showing constraints to match the current Constraint list variable.
        /// </summary>
        /// <param name="cons"></param>
        private void updateConstraintsWindow(List<string> cons)
        {
            StringBuilder builder = new StringBuilder();
            foreach (String str in cons)
            {
                builder.Append("\n" + str + " - " + dateTimesForConstraints[str]);
            }

            TextBlockSavedConstraints.Text = builder.ToString();
        }

        /// <summary>
        /// Starts the background listener on start of the application with current constraint values.
        /// </summary>
        /// <returns>IAsyncOperation to hold until it completes.</returns>
        private IAsyncOperation<SpeechRecognitionCompilationResult> loadOnStart()
        {
            backgroundListener.Constraints.Clear();
            backgroundListener.Constraints.Add(new SpeechRecognitionListConstraint(constraints));
            return backgroundListener.CompileConstraintsAsync();
        }
    }
}
