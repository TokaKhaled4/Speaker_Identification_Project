using Accord.Math;
using Recorder.MFCC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Recorder.MainFuctions
{
    public class DTW
    {
        #region DTW Without Pruning

        public static double DTW_without_pruning(Sequence seq1, Sequence seq2)
        {
            int N = seq1.Frames.Count(); //number of frames in seq1
            int M = seq2.Frames.Count(); //number of frames in seq2

            double[,] dtw = new double[2, M + 1]; // we use 2 * (M+1) matrix to optimize time and space and we only need current and previous anyway

            for (int j = 0; j <= M; j++)
                dtw[0, j] = double.PositiveInfinity; //intialize first row to be zero cost except 0,0 because there is no distance between 0 and it self 
            dtw[0, 0] = 0; //distance between 0,0 and is 0

            for (int i = 1; i <= N; i++)
            {
                int curr = i % 2; //alternate between 1 and 0
                int prev = (i - 1) % 2; //alternate between 1 and 0

                dtw[curr, 0] = double.PositiveInfinity; // Intialize current row to have cost of infinity to start comparing

                for (int j = 1; j <= M; j++)
                {
                    double cost = EuclideanDistance(seq1.Frames[i - 1], seq2.Frames[j - 1]); //calculate distance between the frames
                    double stretch = dtw[prev, j]; // move forward in seq2 and stay in seq1
                                                   // take minimum of previous frame (this is like repeating a previous frame)
                    double exact = dtw[prev, j - 1]; //we took previous with previous
                    double shrink = (j >= 2) ? dtw[prev, j - 2] : double.PositiveInfinity; //skip frame if j > 2 
                    dtw[curr, j] = cost + Math.Min(stretch, Math.Min(exact, shrink));
                }
            }

            return dtw[N % 2, M];
        }



        #endregion

        #region Euclidean Distance
        private static double EuclideanDistance(MFCCFrame frame1, MFCCFrame frame2)
        {
            double sum = 0;
            for (int k = 0; k < 13; k++)
            {
                double diff = frame1.Features[k] - frame2.Features[k];
                sum += diff * diff;
            }
            double cost = Math.Sqrt(sum);
            return cost;
        }

        #endregion

        #region DTW With Pruning
        public static double DTW_pruning(Sequence seq1, Sequence seq2, int w)
        {

            int N = seq1.Frames.Count();
            int M = seq2.Frames.Count();
            if (N == 0 || M == 0)
                return double.PositiveInfinity;

            w = Math.Max(w, 2 * Math.Abs(N - M));
            double[,] dtw = new double[2, M + 1];

            for (int j = 0; j <= M; j++)
                dtw[0, j] = double.PositiveInfinity;
            dtw[0, 0] = 0;

            for (int i = 1; i <= N; i++)
            {
                int curr = i % 2;
                int prev = (i - 1) % 2;
                for (int j = 0; j <= M; j++)
                {
          
                    dtw[curr, j] = double.PositiveInfinity;
                }
                int jStart = Math.Max(1, i - w / 2);
                int jEnd = Math.Min(M, i + w / 2);

                for (int j = jStart; j <= jEnd; j++)
                {
                    double cost = EuclideanDistance(seq1.Frames[i - 1], seq2.Frames[j - 1]);

                    double stretch = dtw[prev, j];
                    double exact = dtw[prev, j - 1];
                    double shrink = (j >= 2) ? dtw[prev, j - 2] : double.PositiveInfinity;

                    dtw[curr, j] = cost + Math.Min(stretch, Math.Min(exact, shrink));
                }
            }

            return dtw[N % 2, M];
        }


        #endregion

        #region DTW beam search pruning
        public class BeamEntry
        {
            public double Cost;
            public int J;
        }
        public static double DTW_pruning_with_beam(Sequence seq1, Sequence seq2, int w)
        {
            int N = seq1.Frames.Count();
            int M = seq2.Frames.Count();
            if (N == 0 || M == 0)
                return double.PositiveInfinity;

            w = Math.Max(w / 2, Math.Abs(N - M));
            int beamSize = Math.Max(10, w / 2);

            double[,] dtw = new double[2, M + 1];
            for (int j = 0; j <= M; j++)
                dtw[0, j] = double.PositiveInfinity;
            dtw[0, 0] = 0;

            for (int i = 1; i <= N; i++)
            {
                int curr = i % 2;
                int prev = (i - 1) % 2;

                for (int j = 0; j <= M; j++)
                    dtw[curr, j] = double.PositiveInfinity;

                int jStart = Math.Max(1, i - w);
                int jEnd = Math.Min(M, i + w);

                List<BeamEntry> beam = new List<BeamEntry>();

                for (int j = jStart; j <= jEnd; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < 13; k++)
                    {
                        double diff = seq1.Frames[i - 1].Features[k] - seq2.Frames[j - 1].Features[k];
                        sum += diff * diff;
                    }
                    double cost = Math.Sqrt(sum);

                    double stretch = dtw[prev, j];
                    double exact = dtw[prev, j - 1];
                    double shrink = (j >= 2) ? dtw[prev, j - 2] : double.PositiveInfinity;

                    dtw[N % 2, M] = cost + Math.Min(stretch, Math.Min(exact, shrink));

                }
            }

            return dtw[N % 2, M];
        }

        #endregion



    }
}