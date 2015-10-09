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

namespace CortanaExtender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpeechRecognizer recognizer;
        private TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs> resultGenerated;
        private TypedEventHandler<SpeechRecognizer, SpeechRecognizerStateChangedEventArgs> OnStateChanged;
        private List<string> constraints;

        public MainWindow()
        {
            InitializeComponent();

            recognizer = new SpeechRecognizer();
            List<String> constraints = new List<string>();
            //recognizer.Constraints.Add(new SpeechRecognitionListConstraint(constraints));
            IAsyncOperation<SpeechRecognitionCompilationResult> op = recognizer.CompileConstraintsAsync();
            resultGenerated = new TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs>(UpdateTextBox);
            recognizer.ContinuousRecognitionSession.ResultGenerated += resultGenerated;
            OnStateChanged = new TypedEventHandler<SpeechRecognizer, SpeechRecognizerStateChangedEventArgs>(onStateChanged);
            recognizer.StateChanged += OnStateChanged;
            op.Completed += HandleCompilationCompleted;
        }

        private void Start_Cortana(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("add constraint clicked");
            if (customPhrase.Text != null)
            {
                List<string> cons = new List<string>();
                cons.Add(customPhrase.Text);
                //var op = recognizer.ContinuousRecognitionSession.PauseAsync();
                recognizer.Constraints.Add(new SpeechRecognitionListConstraint(cons));
                IAsyncOperation<SpeechRecognitionCompilationResult> addConstraintOp = recognizer.CompileConstraintsAsync();
            }
            //IAsyncAction startPauseRecAction = recognizer.ContinuousRecognitionSession.StartAsync(SpeechContinuousRecognitionMode.PauseOnRecognition);
        }

        private void submitCustomPhrase(object sender, RoutedEventArgs e)
        {
            IAsyncAction startRec = recognizer.ContinuousRecognitionSession.StartAsync();
            System.Diagnostics.Debug.WriteLine("recognition started");
            System.Diagnostics.Debug.WriteLine(recognizer.State.ToString());
            System.Threading.Thread.Sleep(1000);
            IAsyncAction stopRec = recognizer.ContinuousRecognitionSession.StopAsync();
            
            System.Diagnostics.Debug.WriteLine("recognition stopped");
            //stopRec.
        }

        private async void UpdateTextBox(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {

            System.Diagnostics.Debug.WriteLine("updated box with recognizer results");
            await Dispatcher.InvokeAsync(() =>
            {
                customPhrase.Text = args.Result.Text;
            });
        }

        private async void onStateChanged(SpeechRecognizer rec, SpeechRecognizerStateChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("in onStateChanged");
            await Dispatcher.InvokeAsync(() =>
            {
                if (args.State == SpeechRecognizerState.Paused)
                {
                    System.Diagnostics.Debug.WriteLine("state was paused");
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_S);
                }
            });
            
        }

        public void HandleCompilationCompleted(IAsyncOperation<SpeechRecognitionCompilationResult> opInfo, AsyncStatus status)
        {
            if (status == AsyncStatus.Completed)
            {
                System.Diagnostics.Debug.WriteLine("Compilation Complete");
                var result = opInfo.GetResults();
                System.Diagnostics.Debug.WriteLine(result.Status.ToString());
            }
        }
    }
}
