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
        private SpeechRecognizer nonConstrainedRecognizer;
        private SpeechRecognizer backgroundListener;
        private TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs> resultGenerated;
        private TypedEventHandler<SpeechRecognizer, SpeechRecognizerStateChangedEventArgs> OnStateChanged;

        public MainWindow()
        {
            InitializeComponent();

            nonConstrainedRecognizer = new SpeechRecognizer();
            backgroundListener = new SpeechRecognizer();
            //recognizer.Constraints.Add(new SpeechRecognitionListConstraint(constraints));
            IAsyncOperation<SpeechRecognitionCompilationResult> op = nonConstrainedRecognizer.CompileConstraintsAsync();
            resultGenerated = new TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs>(UpdateTextBox);
            nonConstrainedRecognizer.ContinuousRecognitionSession.ResultGenerated += resultGenerated;
            OnStateChanged = new TypedEventHandler<SpeechRecognizer, SpeechRecognizerStateChangedEventArgs>(onStateChanged);
            backgroundListener.StateChanged += OnStateChanged;
            op.Completed += HandleCompilationCompleted;
        }

        private void Start_Cortana(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("add constraint clicked");
            if (customPhrase.Text != null)
            {
                List<string> cons = new List<string>();
                cons.Add(customPhrase.Text);
                backgroundListener.Constraints.Add(new SpeechRecognitionListConstraint(cons));
                IAsyncOperation<SpeechRecognitionCompilationResult> addConstraintOp = backgroundListener.CompileConstraintsAsync();
                while (addConstraintOp.Status != AsyncStatus.Completed) { }
                IAsyncOperation<SpeechRecognitionResult> startPauseRecAction = backgroundListener.RecognizeAsync();
                debugPauseResult(startPauseRecAction);
            }
        }

        private void debugPauseResult(IAsyncOperation<SpeechRecognitionResult> op)
        {
            while (op.Status != AsyncStatus.Completed) { }
            if (op.GetResults().Constraint != null)
            {
                InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_S);
            }
            Start_Cortana(null, null);
        }

        private void submitCustomPhrase(object sender, RoutedEventArgs e)
        {
            IAsyncAction startRec = nonConstrainedRecognizer.ContinuousRecognitionSession.StartAsync();
            System.Diagnostics.Debug.WriteLine("recognition started");
            System.Threading.Thread.Sleep(3000);
            IAsyncAction stopRec = nonConstrainedRecognizer.ContinuousRecognitionSession.StopAsync();
            
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

        private void onStateChanged(SpeechRecognizer rec, SpeechRecognizerStateChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("in onStateChanged: " + args.State);
            //await Dispatcher.InvokeAsync(() =>
            //{
            //System.Diagnostics.Debug.WriteLine("in async");
            if (args.State == SpeechRecognizerState.Paused)
            {
                System.Diagnostics.Debug.WriteLine("state was paused");
                InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_S);
            }
            else if (args.State == SpeechRecognizerState.Idle)
            {
                System.Diagnostics.Debug.WriteLine("Should restart here");
            }
            //});
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
