using Recorder.MFCC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Recorder.UserDataBase
{
    public class MatchingSeq
    {
        private static Dictionary<string, Sequence> userDatabase = new Dictionary<string, Sequence>();

        public static void LoadUsers(string folderPath)
        {
            userDatabase.Clear();

            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show("Folder does not exist");
                return;
            }

            string[] files = Directory.GetFiles(folderPath, "*.txt");

            foreach (string filePath in files)
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                   
                    string userName = Path.GetFileNameWithoutExtension(filePath);
                    List<MFCCFrame> frames = new List<MFCCFrame>();

                  
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

                   
                    Sequence sequence = new Sequence
                    {
                        Frames = frames.ToArray()
                    };

                    userDatabase[userName] = sequence;
                }
            }
        }

        public static Dictionary<string, Sequence> GetDatabase()
        {
            return userDatabase;
        }

    }
}
