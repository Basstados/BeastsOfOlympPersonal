//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
//
//  Email:  gustavo_franco@hotmail.com
//
//  Copyright (C) 2006 Franco, Gustavo 
//
#define DEBUGON

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Algorithms
{
    #region Structs
    public struct PathFinderNode
    {
        #region Variables Declaration
        public int     F;
        public int     G;
        public int     H;  // f = gone + heuristic
        public int     X;
        public int     Y;
        public int     PX; // Parent
        public int     PY;
        #endregion
    }
    #endregion

    #region Enum
    public enum PathFinderNodeType
    {
        Start   = 1,
        End     = 2,
        Open    = 4,
        Close   = 8,
        Current = 16,
        Path    = 32
    }

    public enum HeuristicFormula
    {
        Manhattan           = 1,
        MaxDXDY             = 2,
        DiagonalShortCut    = 3,
        Euclidean           = 4,
        EuclideanNoSQR      = 5,
        Custom1             = 6
    }
    #endregion

    #region Delegates
    public delegate void PathFinderDebugHandler(int fromX, int fromY, int x, int y, PathFinderNodeType type, int totalCost, int cost);
    #endregion
	
    public class PathFinder : IPathFinder
    {
        #region Events
        public event PathFinderDebugHandler PathFinderDebug;
        #endregion

        #region Variables Declaration
        private byte[,]                         mGrid                   = null;
        private PriorityQueueB<PathFinderNode>  mOpen                   = new PriorityQueueB<PathFinderNode>(new ComparePFNode());
        private List<PathFinderNode>            mClose                  = new List<PathFinderNode>();
        private bool                            mStop                   = false;
        private bool                            mStopped                = true;
        private int                             mHoriz                  = 0;
        private HeuristicFormula                mFormula                = HeuristicFormula.Manhattan;
        private bool                            mDiagonals              = true;
        private int                             mHEstimate              = 2;
        private bool                            mPunishChangeDirection  = false;
        private bool                            mTieBreaker             = false;
        private bool                            mHeavyDiagonals         = false;
        private int                             mSearchLimit            = 2000;
        private double                          mCompletedTime          = 0;
        private bool                            mDebugProgress          = false;
        private bool                            mDebugFoundPath         = false;
        #endregion

        #region Constructors
        public PathFinder(byte[,] grid)
        {
            if (grid == null)
                throw new Exception("Grid cannot be null");

            mGrid = grid;
        }
        #endregion

        #region Properties
        public bool Stopped
        {
            get { return mStopped; }
        }

        public HeuristicFormula Formula
        {
            get { return mFormula; }
            set { mFormula = value; }
        }

        public bool Diagonals
        {
            get { return mDiagonals; }
            set { mDiagonals = value; }
        }

        public bool HeavyDiagonals
        {
            get { return mHeavyDiagonals; }
            set { mHeavyDiagonals = value; }
        }

        public int HeuristicEstimate
        {
            get { return mHEstimate; }
            set { mHEstimate = value; }
        }

        public bool PunishChangeDirection
        {
            get { return mPunishChangeDirection; }
            set { mPunishChangeDirection = value; }
        }

        public bool TieBreaker
        {
            get { return mTieBreaker; }
            set { mTieBreaker = value; }
        }

        public int SearchLimit
        {
            get { return mSearchLimit; }
            set { mSearchLimit = value; }
        }

        public double CompletedTime
        {
            get { return mCompletedTime; }
            set { mCompletedTime = value; }
        }

        public bool DebugProgress
        {
            get { return mDebugProgress; }
            set { mDebugProgress = value; }
        }

        public bool DebugFoundPath
        {
            get { return mDebugFoundPath; }
            set { mDebugFoundPath = value; }
        }
        #endregion

        #region Methods
        public void FindPathStop()
		{
            mStop = true;
        }

        public List<PathFinderNode> FindPath(Vector start, Vector end)
        {
           // HighResolutionTime.Start();

            PathFinderNode parentNode;
            bool found  = false;
			int  gridX  = mGrid.GetUpperBound(0);
            int  gridY  = mGrid.GetUpperBound(1);

            mStop       = false;
            mStopped    = false;
            mOpen.Clear();
            mClose.Clear();

            #if DEBUGON
            if (mDebugProgress && PathFinderDebug != null)
                PathFinderDebug(0, 0, start.x, start.y, PathFinderNodeType.Start, -1, -1);
            if (mDebugProgress && PathFinderDebug != null)
                PathFinderDebug(0, 0, end.x, end.y, PathFinderNodeType.End, -1, -1);
            #endif

            sbyte[,] direction;
            if (mDiagonals)
                direction = new sbyte[8,2]{ {0,-1} , {1,0}, {0,1}, {-1,0}, {1,-1}, {1,1}, {-1,1}, {-1,-1}};
            else
                direction = new sbyte[4,2]{ {0,-1} , {1,0}, {0,1}, {-1,0}};

            parentNode.G         = 0;
            parentNode.H         = mHEstimate;
            parentNode.F         = parentNode.G + parentNode.H;
            parentNode.X         = start.x;
            parentNode.Y         = start.y;
            parentNode.PX        = parentNode.X;
            parentNode.PY        = parentNode.Y;
            mOpen.Push(parentNode);
            while(mOpen.Count > 0 && !mStop)
            {
                parentNode = mOpen.Pop();

                #if DEBUGON
                if (mDebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, parentNode.X, parentNode.Y, PathFinderNodeType.Current, -1, -1);
                #endif

                if (parentNode.X == end.x && parentNode.Y == end.y)
                {
                    mClose.Add(parentNode);
                    found = true;
                    break;
                }

                if (mClose.Count > mSearchLimit)
                {
                    mStopped = true;
                    return null;
                }

                if (mPunishChangeDirection)
                    mHoriz = (parentNode.X - parentNode.PX); 

                //Lets calculate each successors
                for (int i=0; i<(mDiagonals ? 8 : 4); i++)
                {
                    PathFinderNode newNode;
                    newNode.X = parentNode.X + direction[i,0];
                    newNode.Y = parentNode.Y + direction[i,1];

                    if (newNode.X < 0 || newNode.Y < 0 || newNode.X > gridX || newNode.Y > gridY)
                        continue;

                    int newG;
                    if (mHeavyDiagonals && i>3)
                        newG = parentNode.G + (int) (mGrid[newNode.X, newNode.Y] * 2.41);
                    else
                        newG = parentNode.G + mGrid[newNode.X, newNode.Y];


                    if (newG == parentNode.G)
                    {
                        //Unbrekeable
                        continue;
                    }

                    if (mPunishChangeDirection)
                    {
                        if ((newNode.X - parentNode.X) != 0)
                        {
                            if (mHoriz == 0)
                                newG += 20;
                        }
                        if ((newNode.Y - parentNode.Y) != 0)
                        {
                            if (mHoriz != 0)
                                newG += 20;

                        }
                    }

                    int     foundInOpenIndex = -1;
                    for(int j=0; j<mOpen.Count; j++)
                    {
                        if (mOpen[j].X == newNode.X && mOpen[j].Y == newNode.Y)
                        {
                            foundInOpenIndex = j;
                            break;
                        }
                    }
                    if (foundInOpenIndex != -1 && mOpen[foundInOpenIndex].G <= newG)
                        continue;

                    int     foundInCloseIndex = -1;
                    for(int j=0; j<mClose.Count; j++)
                    {
                        if (mClose[j].X == newNode.X && mClose[j].Y == newNode.Y)
                        {
                            foundInCloseIndex = j;
                            break;
                        }
                    }
                    if (foundInCloseIndex != -1 && mClose[foundInCloseIndex].G <= newG)
                        continue;

                    newNode.PX      = parentNode.X;
                    newNode.PY      = parentNode.Y;
                    newNode.G       = newG;

                    switch(mFormula)
                    {
                        default:
                        case HeuristicFormula.Manhattan:
                            newNode.H       = mHEstimate * (Math.Abs(newNode.X - end.x) + Math.Abs(newNode.Y - end.y));
                            break;
                        case HeuristicFormula.MaxDXDY:
                            newNode.H       = mHEstimate * (Math.Max(Math.Abs(newNode.X - end.x), Math.Abs(newNode.Y - end.y)));
                            break;
                        case HeuristicFormula.DiagonalShortCut:
                            int h_diagonal  = Math.Min(Math.Abs(newNode.X - end.x), Math.Abs(newNode.Y - end.y));
                            int h_straight  = (Math.Abs(newNode.X - end.x) + Math.Abs(newNode.Y - end.y));
                            newNode.H       = (mHEstimate * 2) * h_diagonal + mHEstimate * (h_straight - 2 * h_diagonal);
                            break;
                        case HeuristicFormula.Euclidean:
                            newNode.H       = (int) (mHEstimate * Math.Sqrt(Math.Pow((newNode.X - end.x) , 2) + Math.Pow((newNode.Y - end.y), 2)));
                            break;
                        case HeuristicFormula.EuclideanNoSQR:
                            newNode.H       = (int) (mHEstimate * (Math.Pow((newNode.X - end.x) , 2) + Math.Pow((newNode.Y - end.y), 2)));
                            break;
                        case HeuristicFormula.Custom1:
                            Vector dxy       = new Vector(Math.Abs(end.x - newNode.X), Math.Abs(end.y - newNode.Y));
                            int Orthogonal  = Math.Abs(dxy.x - dxy.y);
                            int Diagonal    = Math.Abs(((dxy.x + dxy.y) - Orthogonal) / 2);
                            newNode.H       = mHEstimate * (Diagonal + Orthogonal + dxy.x + dxy.y);
                            break;
                    }
                    if (mTieBreaker)
                    {
                        int dx1 = parentNode.X - end.x;
                        int dy1 = parentNode.Y - end.y;
                        int dx2 = start.x - end.x;
                        int dy2 = start.y - end.y;
                        int cross = Math.Abs(dx1 * dy2 - dx2 * dy1);
                        newNode.H = (int) (newNode.H + cross * 0.001);
                    }
                    newNode.F       = newNode.G + newNode.H;

                    #if DEBUGON
                    if (mDebugProgress && PathFinderDebug != null)
                        PathFinderDebug(parentNode.X, parentNode.Y, newNode.X, newNode.Y, PathFinderNodeType.Open, newNode.F, newNode.G);
                    #endif
                    

                    //It is faster if we leave the open node in the priority queue
                    //When it is removed, all nodes around will be closed, it will be ignored automatically
                    //if (foundInOpenIndex != -1)
                    //    mOpen.RemoveAt(foundInOpenIndex);

                    //if (foundInOpenIndex == -1)
                        mOpen.Push(newNode);
                }

                mClose.Add(parentNode);

                #if DEBUGON
                if (mDebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, parentNode.X, parentNode.Y, PathFinderNodeType.Close, parentNode.F, parentNode.G);
                #endif
            }

           // mCompletedTime = HighResolutionTime.GetTime();
            if (found)
            {
                PathFinderNode fNode = mClose[mClose.Count - 1];
                for(int i=mClose.Count - 1; i>=0; i--)
                {
                    if (fNode.PX == mClose[i].X && fNode.PY == mClose[i].Y || i == mClose.Count - 1)
                    {
                        #if DEBUGON
                        if (mDebugFoundPath && PathFinderDebug != null)
                            PathFinderDebug(fNode.X, fNode.Y, mClose[i].X, mClose[i].Y, PathFinderNodeType.Path, mClose[i].F, mClose[i].G);
                        #endif
                        fNode = mClose[i];
                    }
                    else
                        mClose.RemoveAt(i);
                }
                mStopped = true;
                return mClose;
            }
            mStopped = true;
            return null;
        }

		/// <summary>
		/// Calculate an distance matrix for a limited range.
		/// The distance matrix tells you which fields are in range and how far they are.
		/// </summary>
		/// <returns>The distance matrix.</returns>
		/// <param name="position">The position the distancematrix should be calulated</param>
		/// <param name="range">The range limit</param>
		public byte[][] GetDistanceMatrix(Vector position, int range)
		{
			// init distance matrix
			byte[][] distMatrix = new byte[mGrid.GetLength(0)][];

			for (int i = 0; i < distMatrix.Length; i++) {
				distMatrix[i] = new byte[mGrid.GetLength(1)];
				for (int j = 0; j < distMatrix[i].Length; j++) {
					distMatrix[i][j] = byte.MaxValue;
				}
			}
			// move to current position is always free
			distMatrix[position.x][position.y] = 0;

			Queue<Vector> checkOpen = new Queue<Vector>();
			List<Vector> neighbours = new List<Vector>();
			Vector currentPoint;

			checkOpen.Enqueue(position);

			while(checkOpen.Count > 0) {
				currentPoint = checkOpen.Dequeue();
				neighbours = GetNeighbours(currentPoint);
				foreach(Vector p in neighbours) {
					// save cost for neighbour (= cost current point + penalty neighbour point)
					byte cost = (byte) (distMatrix[currentPoint.x][currentPoint.y] + mGrid[p.x,p.y]);
					// check if position is free; if not...
					if(mGrid[p.x,p.y] == 0)
						cost = byte.MaxValue;
					// continue with this neighbour if
					// 		cost is less then current know shortest distance OR current know shortest distance is not set
					//		AND cost is in range
					if(cost < distMatrix[p.x][p.y] && cost <= range && cost > 0) {
						distMatrix[p.x][p.y] = cost;
						checkOpen.Enqueue(p);
					}
				}
			}

			return distMatrix;
		}

		private List<Vector> GetNeighbours(Vector position)
		{
			List<Vector> neighbours = new List<Vector>();
			sbyte[][] direction = new sbyte[4][]{ 
				new sbyte[2]{0,-1},
				new sbyte[2]{1,0}, 
				new sbyte[2]{0,1}, 
				new sbyte[2]{-1,0}
			};

			foreach(sbyte[] dir in direction) {
				// check if neighbour coordinates are on the grid
				if(position.x + dir[0] >= 0 && position.x + dir[0] < mGrid.GetLength(0)
				   && position.y + dir[1] >= 0 && position.y + dir[1] < mGrid.GetLength(1)) {
					neighbours.Add(new Vector(position.x + dir[0], position.y + dir[1]));
				}
			}

			return neighbours;
		}
        #endregion

        #region Inner Classes
        internal class ComparePFNode : IComparer<PathFinderNode>
        {
            #region IComparer Members
            public int Compare(PathFinderNode x, PathFinderNode y)
            {
                if (x.F > y.F)
                    return 1;
                else if (x.F < y.F)
                    return -1;
                return 0;
            }
            #endregion
        }
        #endregion
    }
}
