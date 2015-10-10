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

namespace CortanaExtender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> constraints;
        private SpeechRecognizer backgroundListener;
        private TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs> BLResultGenerated;

        public MainWindow()
        {
            InitializeComponent();

            backgroundListener = new SpeechRecognizer();
            constraints = new List<string>();
            BLResultGenerated = new TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs>(blResultGenerated);
            backgroundListener.ContinuousRecognitionSession.ResultGenerated += BLResultGenerated;

            constraints = readInConstraintsFromFile();
            updateConstraintsWindow(constraints);

            var waitOn = loadOnStart();
            while (waitOn.Status != AsyncStatus.Completed) { }
            var ff = backgroundListener.ContinuousRecognitionSession.StartAsync();

            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("trayImage.ico");
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
        }

        private List<string> readInConstraintsFromFile()
        {
            string fileName = @"Stored_Constraints.txt";
            string currPath = Directory.GetCurrentDirectory();
            string filePath = System.IO.Path.Combine(currPath, fileName);

            List<string> existingConstraints = new List<string>();
            string line;

            try
            {
                if (!File.Exists(filePath))
                {
                    File.Create(filePath);
                }

                System.IO.StreamReader file1 = new System.IO.StreamReader(filePath);
                while ((line = file1.ReadLine()) != null)
                {
                    existingConstraints.Add(line);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return existingConstraints;
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

        private void updateConstraintsWindow(List<string> cons)
        {
            StringBuilder builder = new StringBuilder();
            foreach (String str in cons)
            {
                builder.Append("\n" + str);
            }

            TextBlockSavedConstraints.Text = builder.ToString();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

        private IAsyncOperation<SpeechRecognitionCompilationResult> loadOnStart()
        {
            backgroundListener.Constraints.Clear();
            backgroundListener.Constraints.Add(new SpeechRecognitionListConstraint(constraints));
            return backgroundListener.CompileConstraintsAsync();
        }
    }
}
