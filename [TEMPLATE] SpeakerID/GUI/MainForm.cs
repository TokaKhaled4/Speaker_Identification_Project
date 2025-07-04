using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Audio.Filters;
using Recorder.Recorder;
using Recorder.MFCC;
using System.Linq;
using SharpDX.Multimedia;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Recorder.UserDataBase;
using Recorder.MainFuctions;

// added by toka
using System.Collections.Generic; // for dictionay and list
using System.Diagnostics; // stop watch


namespace Recorder
{
    /// <summary>
    ///   Speaker Identification application.
    /// </summary>
    /// 
    public partial class MainForm : Form
    {
        /// <summary>
        /// Data of the opened audio file, contains:
        ///     1. signal data
        ///     2. sample rate
        ///     3. signal length in ms
        /// </summary>
        private AudioSignal signal = null;
        Sequence seq = null;
       
        private string path;

        private Encoder encoder;
        private Decoder decoder;

        private bool isRecorded;

        string currentFileName;
        public MainForm()
        {
            InitializeComponent();

            // Configure the wavechart
            chart.SimpleMode = true;
            chart.AddWaveform("wave", Color.Green, 1, false);
            updateButtons();
        }


        /// <summary>
        ///   Starts recording audio from the sound card
        /// </summary>
        /// 
        private void btnRecord_Click(object sender, EventArgs e)
        {
            isRecorded = true;
            this.encoder = new Encoder(source_NewFrame, source_AudioSourceError);
            this.encoder.Start();
            updateButtons();
        }

        /// <summary>
        ///   Plays the recorded audio stream.
        /// </summary>
        /// 
        private void btnPlay_Click(object sender, EventArgs e)
        {
            InitializeDecoder();
            // Configure the track bar so the cursor
            // can show the proper current position
            if (trackBar1.Value < this.decoder.frames)
                this.decoder.Seek(trackBar1.Value);
            trackBar1.Maximum = this.decoder.samples;
            this.decoder.Start();
            updateButtons();
        }

        private void InitializeDecoder()
        {
            if (isRecorded)
            {
                // First, we rewind the stream
                this.encoder.stream.Seek(0, SeekOrigin.Begin);
                this.decoder = new Decoder(this.encoder.stream, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
            else
            {
                this.decoder = new Decoder(this.path, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
        }

        /// <summary>
        ///   Stops recording or playing a stream.
        /// </summary>
        /// 
        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();   
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This callback will be called when there is some error with the audio 
        ///   source. It can be used to route exceptions so they don't compromise 
        ///   the audio processing pipeline.
        /// </summary>
        /// 
        private void source_AudioSourceError(object sender, AudioSourceErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   This method will be called whenever there is a new input audio frame 
        ///   to be processed. This would be the case for samples arriving at the 
        ///   computer's microphone
        /// </summary>
        /// 
        private void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            this.encoder.addNewFrame(eventArgs.Signal);
            updateWaveform(this.encoder.current, eventArgs.Signal.Length);
       }


        /// <summary>
        ///   This event will be triggered as soon as the audio starts playing in the 
        ///   computer speakers. It can be used to update the UI and to notify that soon
        ///   we will be requesting additional frames.
        /// </summary>
        /// 
        private void output_FramePlayingStarted(object sender, PlayFrameEventArgs e)
        {
            updateTrackbar(e.FrameIndex);

            if (e.FrameIndex + e.Count < this.decoder.frames)
            {
                int previous = this.decoder.Position;
                decoder.Seek(e.FrameIndex);

                Signal s = this.decoder.Decode(e.Count);
                decoder.Seek(previous);

                updateWaveform(s.ToFloat(), s.Length);
            }
        }

        /// <summary>
        ///   This event will be triggered when the output device finishes
        ///   playing the audio stream. Again we can use it to update the UI.
        /// </summary>
        /// 
        private void output_PlayingFinished(object sender, EventArgs e)
        {
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This event is triggered when the sound card needs more samples to be
        ///   played. When this happens, we have to feed it additional frames so it
        ///   can continue playing.
        /// </summary>
        /// 
        private void output_NewFrameRequested(object sender, NewFrameRequestedEventArgs e)
        {
            this.decoder.FillNewFrame(e);
        }


        void output_AudioOutputError(object sender, AudioOutputErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   Updates the audio display in the wave chart
        /// </summary>
        /// 
        private void updateWaveform(float[] samples, int length)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    chart.UpdateWaveform("wave", samples, length);
                }));
            }
            else
            {
                if (this.encoder != null) { chart.UpdateWaveform("wave", this.encoder.current, length); }
            }
        }

        /// <summary>
        ///   Updates the current position at the trackbar.
        /// </summary>
        /// 
        private void updateTrackbar(int value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
                }));
            }
            else
            {
                trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
            }
        }

        private void updateButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(updateButtons));
                return;
            }

            if (this.encoder != null && this.encoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = false;
            }
            else if (this.decoder != null && this.decoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = this.path != null || this.encoder != null;
                btnIdentify.Enabled = this.path != null || this.encoder != null;
                btnPlay.Enabled = this.path != null || this.encoder != null;//stream != null;
                btnStop.Enabled = false;
                btnRecord.Enabled = true;
                trackBar1.Enabled = this.decoder != null;
                trackBar1.Value = 0;
            }
        }

        private void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }

        
        private void saveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.encoder != null)
            {
                Stream fileStream = saveFileDialog1.OpenFile();
                this.encoder.Save(fileStream);
                currentFileName = saveFileDialog1.FileName;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog(this);
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (this.encoder != null) { lbLength.Text = String.Format("Length: {0:00.00} sec.", this.encoder.duration / 1000.0); }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                isRecorded = false;
                path = open.FileName;
                currentFileName = path;
                //Open the selected audio file
                signal = AudioOperations.OpenAudioFile(path);
              //  signal = AudioOperations.RemoveSilence(signal);
                 seq = AudioOperations.ExtractFeatures(signal);
                for (int i = 0; i < seq.Frames.Length; i++)
                {
                    for (int j = 0; j < 13; j++)
                    {

                        if (double.IsNaN(seq.Frames[i].Features[j]) || double.IsInfinity(seq.Frames[i].Features[j]))
                            throw new Exception("NaN");
                    }
                }
                updateButtons();

            }
        }

        private void Stop()
        {
            if (this.encoder != null) { this.encoder.Stop(); }
            if (this.decoder != null) { this.decoder.Stop(); }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Please Enter Your Name");
                return;
            }

            if (string.IsNullOrEmpty(currentFileName) || !File.Exists(currentFileName))
            {
                MessageBox.Show("Please save your Voice");
                return;
            }

            try
            {

                AudioSignal input = AudioOperations.OpenAudioFile(currentFileName);

                DialogResult silenceResult = MessageBox.Show("Remove silence from audio?", "Silence Removal", MessageBoxButtons.YesNo);
                if (silenceResult == DialogResult.Yes)
                {
                    input = AudioOperations.RemoveSilence(input);
                    MessageBox.Show("Silence Removed Successfully");
                }


                Sequence seq = AudioOperations.ExtractFeatures(input);

                string folderPath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\Database";

                //Directory.CreateDirectory(folderPath);

                string fileName = textBox1.Text.Trim() + ".txt";
                string fullPath = Path.Combine(folderPath, fileName);

                using (StreamWriter writer = new StreamWriter(fullPath))
                {
                  //  writer.WriteLine(textBox1.Text);

                    foreach (var frame in seq.Frames)
                    {
                        string featuresLine = string.Join(" ", frame.Features.Select(f => f.ToString("R")));
                        writer.WriteLine(featuresLine);
                    }
                }

                MessageBox.Show("Your Data Has Been Recorded");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void loadTrain1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.ShowDialog();

            var hobba = TestcaseLoader.LoadTestcase2Training(fileDialog.FileName);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void btnIdentify_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (string.IsNullOrEmpty(currentFileName) || !File.Exists(currentFileName))
            {
                MessageBox.Show("Please save your Voice");
                return;
            }

            AudioSignal input = AudioOperations.OpenAudioFile(currentFileName);

            DialogResult silenceResult = MessageBox.Show("Remove silence from audio?", "Silence Removal", MessageBoxButtons.YesNo);
            if (silenceResult == DialogResult.Yes)
            {
                input = AudioOperations.RemoveSilence(input);
                //MessageBox.Show("Silence Removed Successfully");
            }

            Sequence CurrentSeq = AudioOperations.ExtractFeatures(input);

            if (CurrentSeq == null || CurrentSeq.Frames.Length == 0)
            {
                MessageBox.Show("Failed to Extract Features");
                return;
            }

            string folderpath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\Database";
            MatchingSeq.LoadUsers(folderpath);
            var userDatabase = MatchingSeq.GetDatabase();

            double minDistance = double.PositiveInfinity;
            string identifiedUser = " ";

            // Define variables to store time taken by each method
            TimeSpan timeWithoutPruning = TimeSpan.Zero;
            TimeSpan timeWithPruning = TimeSpan.Zero;
            TimeSpan timeWithBeamSearch = TimeSpan.Zero;

            foreach (var user in userDatabase)
            {
                double distance;

                if (radioButton1.Checked) // without pruning
                {
                    Stopwatch pruningStopwatch = new Stopwatch();
                    pruningStopwatch.Start();
                    distance = DTW.DTW_without_pruning(CurrentSeq, user.Value);
                    pruningStopwatch.Stop();
                    timeWithoutPruning += pruningStopwatch.Elapsed;
                }
                else if (radioButton2.Checked) // with pruning
                {
                    Stopwatch pruningStopwatch = new Stopwatch();
                    pruningStopwatch.Start();
                    if (string.IsNullOrWhiteSpace(textBox2.Text))
                    {
                        MessageBox.Show("Please Enter Pruning Width");
                        return;
                    }

                    distance = DTW.DTW_pruning(CurrentSeq, user.Value, int.Parse(textBox2.Text));
                    pruningStopwatch.Stop();
                    timeWithPruning += pruningStopwatch.Elapsed;
                }
                else if (radioButton3.Checked) // beam search
                {
                    Stopwatch pruningStopwatch = new Stopwatch();
                    pruningStopwatch.Start();
                    if (string.IsNullOrWhiteSpace(textBox2.Text))
                    {
                        MessageBox.Show("Please Enter Pruning Width");
                        return;
                    }
                    distance = DTW.DTW_pruning_with_beam(CurrentSeq, user.Value, int.Parse(textBox2.Text));
                    pruningStopwatch.Stop();
                    timeWithBeamSearch += pruningStopwatch.Elapsed;
                }
                else
                {
                    MessageBox.Show("You Must Choose With Pruning or Without Pruning or Beam search First");
                    return;
                }

                if (distance < minDistance)
                {
                    minDistance = distance;
                    identifiedUser = user.Key;
                }
            }

            stopwatch.Stop();
            var totalExecutionTime = stopwatch.Elapsed.TotalMilliseconds;

            MessageBox.Show($"Expected User: {identifiedUser} (Distance: {minDistance})\n" +
                             $"Time Without Pruning: {timeWithoutPruning.TotalMilliseconds:F2} ms\n" +
                             $"Time With Pruning: {timeWithPruning.TotalMilliseconds:F2} ms\n" +
                             $"Time With Beam Search: {timeWithBeamSearch.TotalMilliseconds:F2} ms\n" +
                             $"Total Execution Time: {totalExecutionTime:F2} ms");

        }

        private void button1_Click(object sender, EventArgs e)
        {

            string testpath = @"D:\[2] SPEAKER IDENTIFICATION\TEST CASES\[2] COMPLETE\Complete SpeakerID Dataset\TestingList.txt";
            string trainpath = @"D:\[2] SPEAKER IDENTIFICATION\TEST CASES\[2] COMPLETE\Complete SpeakerID Dataset\TrainingList.txt";

            var trainingUsers = TestcaseLoader.LoadTestcase1Training(trainpath);
            MessageBox.Show("1) training data completed");

            var trainingDB = new Dictionary<string, List<Sequence>>();
            //string folderPath = @"C:\Users\janae\OneDrive\Desktop\[TEMPLATE] SpeakerID\UserDataBase\Database";

            foreach (var user in trainingUsers)
            {
                trainingDB[user.UserName] = new List<Sequence>();
                foreach (var signal in user.UserTemplates)
                {
                    var seq = AudioOperations.ExtractFeatures(signal);
                    trainingDB[user.UserName].Add(seq);
                }
            }
            MessageBox.Show("2) Extracting training features completed");


            var testingUsers = TestcaseLoader.LoadTestcase1Testing(testpath);
            MessageBox.Show("3) testing data completed");

            var random = new Random();
            var testUser = testingUsers[random.Next(testingUsers.Count)];
            var testUserName = testUser.UserName;

            var testSignal = testUser.UserTemplates[random.Next(testUser.UserTemplates.Count)];
            var testSeq = AudioOperations.ExtractFeatures(testSignal);
           //var secondUser = testingUsers[1];
           //var secondTestSignal = secondUser.UserTemplates[1];
           //var testUserName = secondUser.UserName;
           //var testSeq = AudioOperations.ExtractFeatures(secondTestSignal);

            double minDist = double.MaxValue;
            string bestMatch = "";

            foreach (var trainUser in trainingDB)
            {
                foreach (var trainSeq in trainUser.Value)
                {
                    double dist = DTW.DTW_without_pruning(testSeq, trainSeq);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestMatch = trainUser.Key;
                    }
                }
            }

        MessageBox.Show($"Testing User: {testUserName}\nClosest match: {bestMatch}\nDTW Distance: {minDist:F2}");
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chart_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
 

            string trainpath = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select Training List File";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                trainpath = openFileDialog.FileName;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var users = TestcaseLoader.LoadTestcase1Training(trainpath);

            // project database path 
            //string folderPath = "";
            //using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            //{
            //    folderBrowserDialog.Description = "Select Folder Containing Training List";

            //    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        folderPath = folderBrowserDialog.SelectedPath;
            //    }
            //}
            string folderPath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\TrainingList";


            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);  
            }
            Directory.CreateDirectory(folderPath);


            foreach (var user in users)
            {

                string userFolder = Path.Combine(folderPath, user.UserName.Trim());
                Directory.CreateDirectory(userFolder);

                int counter = 1;
                foreach (var signal in user.UserTemplates)
                {

                    Sequence seq = AudioOperations.ExtractFeatures(signal);


                    string fileName = $"sample{counter}.txt";
                    string fullPath = Path.Combine(userFolder, fileName);


                    using (StreamWriter writer = new StreamWriter(fullPath))
                    {
                        foreach (var frame in seq.Frames)
                        {
                            string featuresLine = string.Join(" ", frame.Features.Select(f => f.ToString("R")));
                            writer.WriteLine(featuresLine);
                        }
                    }

                    counter++;
                }
            }
            stopwatch.Stop();

           
            TimeSpan totalTime = stopwatch.Elapsed;
            MessageBox.Show($"Total time for loading training set and extracting features: {totalTime.TotalMinutes} minutes");
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            string Testpath = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select Testing List File";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                Testpath = openFileDialog.FileName;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var users = TestcaseLoader.LoadTestcase1Testing(Testpath);

            string folderPath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\TestingList";

            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            Directory.CreateDirectory(folderPath);



            foreach (var user in users)
            {
                
                string userFolder = Path.Combine(folderPath, user.UserName.Trim());
                Directory.CreateDirectory(userFolder);

                int counter = 1;
                foreach (var signal in user.UserTemplates)
                {
                    
                    Sequence seq = AudioOperations.ExtractFeatures(signal);

                    
                    string fileName = $"sample{counter}.txt";
                    string fullPath = Path.Combine(userFolder, fileName);

                    
                    using (StreamWriter writer = new StreamWriter(fullPath))
                    {
                        foreach (var frame in seq.Frames)
                        {
                            string featuresLine = string.Join(" ", frame.Features.Select(f => f.ToString("R")));
                            writer.WriteLine(featuresLine);
                        }
                    }

                    counter++;
                }
            }

            MessageBox.Show("Testing data has been loaded and extracted successfully!");

            #region identification
            string trainFolderPath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\TrainingList";
            string testFolderPath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\TestingList";
            List<string> results = new List<string>();

            List<string> identifiedUsers = new List<string>();
            List<string> expectedUsers = new List<string>();
            int correctMatches = 0;
            int totalTestSamples = 0;

            string resultpath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\identification complete test results.txt";


            if (File.Exists(resultpath))
            {
                File.Delete(resultpath);
            }
            File.Create(resultpath).Close();

            Dictionary<string, List<Sequence>> trainingDatabase = new Dictionary<string, List<Sequence>>();

            foreach (string userFolder in Directory.GetDirectories(trainFolderPath))
            {
                string userName = Path.GetFileName(userFolder);
                trainingDatabase[userName] = new List<Sequence>();

                foreach (string sampleFile in Directory.GetFiles(userFolder, "*.txt"))
                {
                    List<MFCCFrame> frames = new List<MFCCFrame>();

                    using (StreamReader reader = new StreamReader(sampleFile))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            double[] features = Array.ConvertAll(line.Split(' '), double.Parse);

                            MFCCFrame frame = new MFCCFrame
                            {
                                Features = features
                            };

                            frames.Add(frame);
                        }
                    }

                    Sequence seq = new Sequence
                    {
                        Frames = frames.ToArray()
                    };

                    trainingDatabase[userName].Add(seq);
                }
            }


            foreach (string userFolder in Directory.GetDirectories(testFolderPath))
            {
                string userName = Path.GetFileName(userFolder);



                foreach (string sampleFile in Directory.GetFiles(userFolder, "*.txt"))
                {
                    totalTestSamples++;
                    List<MFCCFrame> frames = new List<MFCCFrame>();

                    using (StreamReader reader = new StreamReader(sampleFile))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            double[] features = Array.ConvertAll(line.Split(' '), double.Parse);

                            MFCCFrame frame = new MFCCFrame
                            {
                                Features = features
                            };

                            frames.Add(frame);
                        }
                    }

                    Sequence testSeq = new Sequence
                    {
                        Frames = frames.ToArray()
                    };

                    double minDistance = double.PositiveInfinity;
                    string identifiedUser = " ";

                    TimeSpan timeWithoutPruning = TimeSpan.Zero;
                    TimeSpan timeWithPruning = TimeSpan.Zero;
                    TimeSpan timeWithBeamSearch = TimeSpan.Zero;

                    foreach (var userEntry in trainingDatabase)
                    {
                        string trainUserName = userEntry.Key;
                        var sequences = userEntry.Value;

                        foreach (var trainSeq in sequences)
                        {
                            double distance;

                            if (radioButton1.Checked) // without pruning
                            {
                                Stopwatch pruningStopwatch = new Stopwatch();
                                pruningStopwatch.Start();
                                distance = DTW.DTW_without_pruning(testSeq, trainSeq);
                                pruningStopwatch.Stop();
                                timeWithoutPruning += pruningStopwatch.Elapsed;
                            }
                            else if (radioButton2.Checked) // with pruning
                            {
                                Stopwatch pruningStopwatch = new Stopwatch();
                                pruningStopwatch.Start();
                                if (string.IsNullOrWhiteSpace(textBox2.Text))
                                {
                                    MessageBox.Show("Please Enter Pruning Width");
                                    return;
                                }

                                distance = DTW.DTW_pruning(testSeq, trainSeq, int.Parse(textBox2.Text));
                                pruningStopwatch.Stop();
                                timeWithPruning += pruningStopwatch.Elapsed;
                            }
                            else if (radioButton3.Checked) // beam search
                            {
                                Stopwatch pruningStopwatch = new Stopwatch();
                                pruningStopwatch.Start();
                                if (string.IsNullOrWhiteSpace(textBox2.Text))
                                {
                                    MessageBox.Show("Please Enter Pruning Width");
                                    return;
                                }

                                distance = DTW.DTW_pruning_with_beam(testSeq, trainSeq, int.Parse(textBox2.Text));
                                pruningStopwatch.Stop();
                                timeWithBeamSearch += pruningStopwatch.Elapsed;
                            }
                            else
                            {
                                MessageBox.Show("Please select a DTW method.");
                                return;
                            }

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                identifiedUser = trainUserName;
                            }
                        }
                    }

                    string result = $"Expected: {userName}, Identified: {identifiedUser}, Distance: {minDistance}";
                    results.Add(result);
                    identifiedUsers.Add(identifiedUser);
                    expectedUsers.Add(userName);
                    if (userName == identifiedUser)
                    {
                        correctMatches++;
                    }

                }
            }
            double accuracy = (double)correctMatches / totalTestSamples * 100;

            File.WriteAllLines(resultpath, results);

            stopwatch.Stop();
            TimeSpan totalTime = stopwatch.Elapsed;
            MessageBox.Show($"Total time for loading training set and extracting features: {totalTime.TotalMinutes} minutes \n" +
               $"Total test samples: {totalTestSamples}\n" +
               $"Correct matches: {correctMatches}\n" +
               $"Accuracy: {accuracy:F2}%",
               "Results",
               MessageBoxButtons.OK,
               MessageBoxIcon.Information);

            //MessageBox.Show($"Testing completed!\n\n" +
            //   $"Total test samples: {totalTestSamples}\n" +
            //   $"Correct matches: {correctMatches}\n" +
            //   $"Accuracy: {accuracy:F2}%",
            //   "Results",
            //   MessageBoxButtons.OK,
            //   MessageBoxIcon.Information);

            #endregion
        }

        private void button3_Click(object sender, EventArgs e)
        {

            #region identification
            string trainFolderPath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\TrainingList";
            string testFolderPath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\TestingList";
            List<string> results = new List<string>();

            List<string> identifiedUsers = new List<string>();
            List<string> expectedUsers = new List<string>();
            int correctMatches = 0;
            int totalTestSamples = 0;

            string resultpath = @"C:\Users\Toka Khaled\Downloads\code\STARTUP CODE\Speaker Identification Startup Code\Speaker Identification Startup Code\[TEMPLATE] SpeakerID\UserDataBase\identification complete test results.txt";


            if (File.Exists(resultpath))
            {
                File.Delete(resultpath);
            }
            File.Create(resultpath).Close(); 

            Dictionary<string, List<Sequence>> trainingDatabase = new Dictionary<string, List<Sequence>>();

            foreach (string userFolder in Directory.GetDirectories(trainFolderPath))
            {
                string userName = Path.GetFileName(userFolder);
                trainingDatabase[userName] = new List<Sequence>();

                foreach (string sampleFile in Directory.GetFiles(userFolder, "*.txt"))
                {
                    List<MFCCFrame> frames = new List<MFCCFrame>();

                    using (StreamReader reader = new StreamReader(sampleFile))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            double[] features = Array.ConvertAll(line.Split(' '), double.Parse);

                            MFCCFrame frame = new MFCCFrame
                            {
                                Features = features
                            };

                            frames.Add(frame);
                        }
                    }

                    Sequence seq = new Sequence
                    {
                        Frames = frames.ToArray()
                    };

                    trainingDatabase[userName].Add(seq);
                }
            }

          
            foreach (string userFolder in Directory.GetDirectories(testFolderPath))
            {
                string userName = Path.GetFileName(userFolder);



                foreach (string sampleFile in Directory.GetFiles(userFolder, "*.txt"))
                {
                    totalTestSamples++;
                    List<MFCCFrame> frames = new List<MFCCFrame>();

                    using (StreamReader reader = new StreamReader(sampleFile))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            double[] features = Array.ConvertAll(line.Split(' '), double.Parse);

                            MFCCFrame frame = new MFCCFrame
                            {
                                Features = features
                            };

                            frames.Add(frame);
                        }
                    }

                    Sequence testSeq = new Sequence
                    {
                        Frames = frames.ToArray()
                    };

                    double minDistance = double.PositiveInfinity;
                    string identifiedUser = " ";

                    TimeSpan timeWithoutPruning = TimeSpan.Zero;
                    TimeSpan timeWithPruning = TimeSpan.Zero;
                    TimeSpan timeWithBeamSearch = TimeSpan.Zero;

                    foreach (var userEntry in trainingDatabase)
                    {
                        string trainUserName = userEntry.Key;
                        var sequences = userEntry.Value;

                        foreach (var trainSeq in sequences)
                        {
                            double distance;

                            if (radioButton1.Checked) // without pruning
                            {
                                Stopwatch pruningStopwatch = new Stopwatch();
                                pruningStopwatch.Start();
                                distance = DTW.DTW_without_pruning(testSeq, trainSeq);
                                pruningStopwatch.Stop();
                                timeWithoutPruning += pruningStopwatch.Elapsed;
                            }
                            else if (radioButton2.Checked) // with pruning
                            {
                                Stopwatch pruningStopwatch = new Stopwatch();
                                pruningStopwatch.Start();
                                if (string.IsNullOrWhiteSpace(textBox2.Text))
                                {
                                    MessageBox.Show("Please Enter Pruning Width");
                                    return;
                                }

                                distance = DTW.DTW_pruning(testSeq, trainSeq, int.Parse(textBox2.Text));
                                pruningStopwatch.Stop();
                                timeWithPruning += pruningStopwatch.Elapsed;
                            }
                            else if (radioButton3.Checked) // beam search
                            {
                                Stopwatch pruningStopwatch = new Stopwatch();
                                pruningStopwatch.Start();
                                if (string.IsNullOrWhiteSpace(textBox2.Text))
                                {
                                    MessageBox.Show("Please Enter Pruning Width");
                                    return;
                                }
                                distance = DTW.DTW_pruning_with_beam(testSeq, trainSeq, int.Parse(textBox2.Text));
                                pruningStopwatch.Stop();
                                timeWithBeamSearch += pruningStopwatch.Elapsed;
                            }
                            else
                            {
                                MessageBox.Show("Please select a DTW method.");
                                return;
                            }

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                identifiedUser = trainUserName;
                            }
                        }
                    }

                    string result = $"Expected: {userName}, Identified: {identifiedUser}, Distance: {minDistance}";
                    results.Add(result);
                    identifiedUsers.Add(identifiedUser);
                    expectedUsers.Add(userName);
                    if (userName == identifiedUser)
                    {
                        correctMatches++;
                    }

                }
            }
            double accuracy = (double)correctMatches / totalTestSamples * 100;

            File.WriteAllLines(resultpath, results);
           // MessageBox.Show("Testing completed. Results saved in Test_Results.txt");
            MessageBox.Show($"Testing completed!\n\n" +
               $"Total test samples: {totalTestSamples}\n" +
               $"Correct matches: {correctMatches}\n" +
               $"Accuracy: {accuracy:F2}%",
               "Results",
               MessageBoxButtons.OK,
               MessageBoxIcon.Information);

            #endregion
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
