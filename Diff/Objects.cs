using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace DiffApp.Objects
{
    public class Snake
    {
		public int XStart;
		public int YStart;
		public int ADeleted;
		public int BInserted;
		public int DiagonalLength;
		// public bool IsForward = true;
		public int DELTA = 0;

		// public bool IsMiddle = false;
		public int D = -1;

		public Point StartPoint { get { return new Point( XStart, YStart ); } }

		public int XMid { get { return (XStart + ADeleted); } }
		public int YMid { get { return (YStart + BInserted); } }
		public Point MidPoint { get { return new Point( XMid, YMid ); } }

		public int XEnd { get { return (XStart + ADeleted + DiagonalLength); } }
		public int YEnd { get { return (YStart + BInserted + DiagonalLength); } }
		public Point EndPoint { get { return new Point( XEnd, YEnd ); } }

        public Snake(int a0, int N, int b0, int M, bool isForward, int xStart, int yStart, bool down, int diagonal)
        {
            XStart = xStart;
            YStart = yStart;
            ADeleted = down ? 0 : 1;
            BInserted = down ? 1 : 0;
            DiagonalLength = diagonal;

            RemoveStubs(a0, N, b0, M);
        }

		public Snake( int a0, int N, int b0, int M, bool forward, int delta, VState VState, int k, int d, string[] strA, string[] strB )
		{
			DELTA = delta;

			Calculate( VState, k, d, strA, a0, N, strB, b0, M );
		}

		Snake Calculate( VState VState, int k, int d, string[] strA, int a0, int N, string[] strB, int b0, int M )
		{
			bool down = ( k == -d || ( k != d && VState[ k - 1 ] < VState[ k + 1 ] ) );

			int xStart = down ? VState[ k + 1 ] : VState[ k - 1 ];
			int yStart = xStart - ( down ? k + 1 : k - 1 );

			int xEnd = down ? xStart : xStart + 1;
			int yEnd = xEnd - k;

			int snake = 0;
			while ( xEnd < N && yEnd < M && strA[ xEnd + a0 ] == strB[ yEnd + b0 ] )
				{ xEnd++; yEnd++; snake++; }

			XStart = xStart + a0;
			YStart = yStart + b0;
			ADeleted = down ? 0 : 1;
			BInserted = down ? 1 : 0;
			DiagonalLength = snake;

			RemoveStubs( a0, N, b0, M );

			return this;
		}
        

		void RemoveStubs( int a0, int N, int b0, int M )
		{
			int aMin, aMax, bMin, bMax;

            if ( XStart == a0 && YStart == b0 - 1 && BInserted == 1 )
            {
                // Debug.WriteLine( "Stub before:" + this );

                YStart = b0;
                BInserted = 0;

                // Debug.WriteLine( "Stub after:" + this );
            }

            aMin = a0; aMax = a0 + N + M;
            bMin = b0; bMax = b0 + N + M;


			Debug.Assert( aMin <= XStart && XStart <= aMax );
			Debug.Assert( aMin <= XMid && XMid <= aMax );
			Debug.Assert( aMin <= XEnd && XEnd <= aMax );

			Debug.Assert( bMin <= YStart && YStart <= bMax );
			Debug.Assert( bMin <= YMid && YMid <= bMax );
			Debug.Assert( bMin <= YEnd && YEnd <= bMax );

			Debug.Assert( XMid - YMid == XEnd - YEnd ); // k
		}
        
		public override string ToString()
		{
			return "Snake " + ": Start( " + XStart + ", " + YStart + " ), Changes( " +
				( ADeleted > 0 ? "D" + ADeleted : "" ) +
				"," +
				( BInserted > 0 ? " I" + BInserted : "" ) +
				" ) + " + DiagonalLength + " -> Stop( " + XEnd + ", " + YEnd + " )" +
				" k=" + ( XMid - YMid );
		}


    }


    public class VState
    {
        public int m_N { get; private set; }
        public int m_M { get; private set; }

        public int m_max;
        public int m_delta;
        public int[] m_array;

        public int this[ int k ]
		{
			get
			{
				return m_array[ k - m_delta + m_max ];
			}

			set
			{
				m_array[ k - m_delta + m_max ] = value;
			}
		}
        VState(){ }
        public VState(int n, int m)
        {
            Debug.Assert(n >= 0 && m >= 0, "V.ctor N:" + n + " M:" + m);
            m_N = n;
            m_M = m;

            m_max = n + m;

            if (m_max <= 0)
                m_max = 1;

            m_array = new int[2 * m_max + 1];

            InitStub(n, m, m_max);
        }

		public void InitStub( int n, int m, int max )
		{
			this[ 1 ] = 0; // stub for forward
		}

        public VState CreateCopy( int d, int delta )
		{
			// Debug.Assert( !( isForward && delta != 0 ) );

			if ( d == 0 ) d++;

			VState localV = new VState();

			localV.m_max = d;
			localV.m_delta = delta;
			localV.m_array = new int[ 2 * d + 1 ];

			if ( d <= m_max )
			{
				Array.Copy( m_array, ( m_max - m_delta ) - ( localV.m_max - localV.m_delta ), localV.m_array, 0, localV.m_array.Length );
			}
			else
			{
				throw new NotImplementedException( "V.CreateCopy" );
				//Debug.Assert( false );
				//Debug.Assert( _Delta == 0 );
				//Marshal.Copy( _Array, 0, new IntPtr( po + d - _Max ), _Array.Length );
			}

			return localV;
		}

		public override string ToString()
		{
			return "V " + m_array.Length + " [ " + ( m_delta - m_max ) + " .. " + m_delta + " .. " + ( m_delta + m_max ) + " ]";
		}

    }


    public struct Point
    {
        public int X;
        public int Y;

        public Point(int x, int y) { X = x; Y = y; }
    } 

	public enum DiffMode
	{
		FastByLine,
		SlowByChar
	}
}
