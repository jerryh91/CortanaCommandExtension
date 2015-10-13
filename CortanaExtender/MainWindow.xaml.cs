using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using Windows.ApplicationModel.VoiceCommands;
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
        private List<string> constraints;
        private SpeechRecognizer backgroundListener;
        private TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs> BLResultGenerated;
        private NotifyIcon notifyIcon;
        private string filePath;

        public MainWindow()
        {
            InitializeComponent();

            string fileName = @"Stored_Constraints.txt";
            filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fileName);

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

        private List<string> readInConstraintsFromFile()
        {
            List<string> existingConstraints = new List<string>();
            string line;

            try
            {
                if (!File.Exists(filePath))
                {
                    File.Create(filePath);
                }

                StreamReader reader = new StreamReader(filePath);
                while ((line = reader.ReadLine()) != null)
                {
                    existingConstraints.Add(line);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return existingConstraints;
        }

        private void WriteConstraintsToFile()
        {
            IEnumerable<string> NewConstraintsToSave = constraints.Except(currentlyStoredConstraints);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                foreach (string str in NewConstraintsToSave)
                {
                    writer.WriteLine(str);
                }
                writer.Close();
            }
        }

        private void OnAppClosing(Object sender, EventArgs args)
        {
            notifyIcon.Visible = false;
            WriteConstraintsToFile();
        }

        private void blResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if (args.Result.Constraint != null)
            {
                InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_S);
            }
        }

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
                updateConstraintsWindow(constraints);
                backgroundListener.Constraints.Clear();
                backgroundListener.Constraints.Add(new SpeechRecognitionListConstraint(constraints));
                var blCompileConstraints = backgroundListener.CompileConstraintsAsync();
                while (blCompileConstraints.Status != AsyncStatus.Completed) { }
                var leavemealone = backgroundListener.ContinuousRecognitionSession.StartAsync();
            }
        }

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
                    DeleteConstraintFromFile(removePhrase.Text);
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

        private void updateConstraintsWindow(List<string> cons)
        {
            StringBuilder builder = new StringBuilder();
            foreach (String str in cons)
            {
                builder.Append("\n" + str);
            }

            TextBlockSavedConstraints.Text = builder.ToString();
        }

        private IAsyncOperation<SpeechRecognitionCompilationResult> loadOnStart()
        {
            backgroundListener.Constraints.Clear();
            backgroundListener.Constraints.Add(new SpeechRecognitionListConstraint(constraints));
            return backgroundListener.CompileConstraintsAsync();
        }
    }
}
