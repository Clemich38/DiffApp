using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;

using DiffApp.Objects;

[assembly: InternalsVisibleToAttribute("DiffApp.Tests")]

namespace DiffApp.Diff
{
    public static class Diff
    {
        public static bool Dumy()
        {
            return true;
        }
        
        public static int GetSimilarityValue(string fileA, string fileB, bool forward, DiffMode mode)
        {
            int retVal = 0;

            string[] strArrayA;
            string[] strArrayB;

            int diagLength = 0;

            var snakes = new List<Snake>();
            var vstates = new List<VState>();

            if(mode == DiffMode.SlowByChar)
            {
                string strA = System.IO.File.ReadAllText(fileA);
                string strB = System.IO.File.ReadAllText(fileB);

                strA = strA.RemoveWhitespace();
                strB = strB.RemoveWhitespace();

                strArrayA = strA.ToCharArray().Select(c => c.ToString()).ToArray();
                strArrayB = strB.ToCharArray().Select(c => c.ToString()).ToArray();
            }

            else
            {
                strArrayA = System.IO.File.ReadAllLines(fileA);
                strArrayB = System.IO.File.ReadAllLines(fileB);
            }
            
            var VState = new VState(strArrayA.Length, strArrayB.Length);

            diagLength = CompareStr(snakes, vstates, strArrayA, strArrayA.Length, strArrayB, strArrayB.Length, VState, forward);

            retVal = ComputeSimilarityPercentage2(strArrayA.Length, strArrayB.Length, diagLength);
            

            // return new Results(V.Memory, snakes, forward, vs);
            return retVal;
        }

        internal static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }


        static int CompareStr(List<Snake> snakes, IList<VState> vStates, string[] pa, int N, string[] pb, int M, VState VState, bool forward)
        {
            // Debug.WriteLine("\n\nDiffGreedy " + (forward ? "FORWARD" : "REVERSE") + " ( " + N + ", " + M + " )");

            Snake last = null;

            int DELTA = N - M;

            int diagLength = 0;

            int d = 0;
            for (; d <= (N + M); d++)
            {
                // Debug.WriteLine( "Calculating d: " + d );

                last = DAlgoStr(pa, N, pb, M, VState, d);

                vStates.Add(VState.CreateCopy(d, 0));

                if (last != null) break;
            }

            if (last == null) throw new InvalidOperationException("No solution was found!");

            // Debug.WriteLine( "Solution D: " + d + " " + last );

            diagLength = SolveForwardStr(snakes, vStates, pa, pb, N, M);

            return diagLength;
        }

        static Snake DAlgoStr(string[] pa, int N, string[] pb, int M, VState VState, int d )
		{
			int MAX = N + M;

			for ( int k = -d ; k <= d ; k += 2 )
			{
				bool down = ( k == -d || ( k != d && VState[ k - 1 ] < VState[ k + 1 ] ) );

				int xStart = down ? VState[ k + 1 ] : VState[ k - 1 ];
				int yStart = xStart - ( down ? k + 1 : k - 1 );

				int xEnd = down ? xStart : xStart + 1;
				int yEnd = xEnd - k;

				int snake = 0;
				while ( xEnd < N && yEnd < M && pa[ xEnd ].Equals(pb[ yEnd ]) ) { xEnd++; yEnd++; snake++; }

				VState[ k ] = xEnd;

				// Debug.WriteLine( "  k:" + k + " V[k-1]:" + ( k - 1 >= -MAX ? VState[ k - 1 ].ToString() : "X" ) + " V[k+1]:" + ( k + 1 <= MAX ? VState[ k + 1 ].ToString() : "X" ) + ( down ? " down" : " right" ) + " Start:" + xStart + "," + yStart + " End:" + xEnd + "," + yEnd );

				if ( xEnd >= N && yEnd >= M )
					return new Snake( 0, N, 0, M, true, xStart, yStart, down, snake );
			}

			return null;
		}

        static int SolveForwardStr( List<Snake> snakes, IList<VState> vStates, string[] strA, string[] strB, int N, int M )
		{
            int diagLength = 0;
			Point p = new Point( N, M );

			for ( int d = vStates.Count - 1 ; p.X > 0 || p.Y > 0 ; d-- )
			{
				var V = vStates[ d ];

				int k = p.X - p.Y;

				int xEnd = V[ k ];
				int yEnd = xEnd - k;

				if ( xEnd != p.X || yEnd != p.Y )
                    break;
					// throw new ApplicationException( "No solution for " +
					// 	"d:" + d + " k:" + k + " p:( " + p.X + ", " + p.Y + " ) V:( " + xEnd + ", " + yEnd + " )" );

				Snake solution = new Snake( 0, p.X, 0, p.Y, true, 0, V, k, d, strA, strB );

				if ( solution.XEnd != p.X || solution.YEnd != p.Y )
                    break;
					// throw new ApplicationException( "Missed solution for " +
					// 	"d:" + d + " k:" + k + " p:( " + p.X + ", " + p.Y + " ) V:( " + xEnd + ", " + yEnd + " )" );

				//Debug.WriteLine( "D: " + d + " " + solution );

				snakes.Add( solution );
                diagLength += solution.DiagonalLength;

				p.X = solution.XStart;
				p.Y = solution.YStart;
			}

            return diagLength;
		}

        static int ComputeSimilarityPercentage(int strALength, int strBLength, int d)
        {
            float floatDiff = 0;
            int res;

            float sizeRatio = (strALength > strBLength)? (float)strBLength/strALength : (float)strALength / strBLength;
            int refSize = (strALength > strBLength) ? strALength : strBLength;

            float divRatio = (2 * sizeRatio) > 1? (2 * sizeRatio) : 1;
            if(strALength > 0)
                floatDiff = d * 100 / refSize / 2;
            else
                floatDiff = d*100;

            res = (floatDiff >= 100)? 0 : (100 - (int)Math.Floor(floatDiff));
            return res;
        }

        static int ComputeSimilarityPercentage2(int strALength, int strBLength, int diagLength)
        {
            int res;

            int refSize = (strALength > strBLength) ? strALength : strBLength;

            if(refSize > 0)
                res = diagLength * 100 / refSize;
            else
                res = 100;

            return res;
        }
    }

}