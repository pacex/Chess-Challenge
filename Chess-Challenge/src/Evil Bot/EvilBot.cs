using ChessChallenge.API;
using System;
using System.Collections.Generic;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        #region Piece and Square Values
        int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };
        // Pawn
        short[][] squareValues = { new short[]{ 0,  0,  0,  0,  0,  0,  0,  0,
                                            50, 50, 50, 50, 50, 50, 50, 50,
                                            10, 10, 20, 30, 30, 20, 10, 10,
                                             5,  5, 10, 25, 25, 10,  5,  5,
                                             0,  0,  0, 22, 22,  0,  0,  0,
                                             5, -5,-10,  0,  0,-10, -5,  5,
                                             5, 10, 10,-23,-23, 10, 10,  5,
                                             0,  0,  0,  0,  0,  0,  0,  0},
        // Knight
        new short[] { -50,-35,-30,-30,-30,-30,-35,-50,
                    -40,-20,  0,  0,  0,  0,-20,-40,
                    -30,  0, 10, 15, 15, 10,  0,-30,
                    -30,  5, 15, 20, 20, 15,  5,-30,
                    -30,  0, 15, 20, 20, 15,  0,-30,
                    -30,  5, 10, 15, 15, 10,  5,-30,
                    -40,-20,  0,  5,  5,  0,-20,-40,
                    -50,-35,-30,-30,-30,-30,-35,-50},
        // Bishop
        new short[] { -20,-10,-10,-10,-10,-10,-10,-20,
                    -10,  0,  0,  0,  0,  0,  0,-10,
                    -10,  0,  5, 10, 10,  5,  0,-10,
                    -10,  5,  5, 10, 10,  5,  5,-10,
                    -10,  0, 10, 10, 10, 10,  0,-10,
                    -10, 10, 10, 10, 10, 10, 10,-10,
                    -10,  5,  0,  0,  0,  0,  5,-10,
                    -20,-10,-10,-10,-10,-10,-10,-20},};

        #endregion

        int maxDepth = 0;
        int budget = 1000000;

        public struct EvalMove
        {
            public EvalMove(Move m, int v)
            {
                Move = m;
                Value = v;
            }
            public Move Move;
            public int Value;
        }

        public Move Think(Board board, Timer timer)
        {
            bool isWhite = board.IsWhiteToMove;

            // Determine recursion depth
            Move[] legalMoves = board.GetLegalMoves();
            board.MakeMove(legalMoves[0]);
            int opponentMovesCount = board.GetLegalMoves().Length;
            board.UndoMove(legalMoves[0]);

            maxDepth = (int)(Math.Log2(budget) / Math.Log2(Math.Max(legalMoves.Length, opponentMovesCount)));
            if (board.IsInCheck())
                maxDepth = Math.Min(maxDepth, 4);
            maxDepth = Math.Max(maxDepth, 2);

            maxDepth = 3; // DEBUG, TODO: REMOVE
            Console.WriteLine("EvilBot | Recursion depth: " + maxDepth);



            return PickMove(board, timer, isWhite, 0, int.MaxValue).Move;
        }

        private int EvaluateBoard(Board board, Timer timer, bool isWhite, int depth, int bestPrev)
        {
            bool boardAfterOwnMove = isWhite != board.IsWhiteToMove;

            // Base cases
            if (board.IsInCheckmate())
                return boardAfterOwnMove ? int.MaxValue : int.MinValue;

            if (board.IsDraw())
                return 0;

            if (depth >= maxDepth)
            {
                // Board evaluation
                return StaticEval(board, isWhite);
            }

            // Recursion case
            return PickMove(board, timer, isWhite, depth, bestPrev).Value;
        }

        private EvalMove PickMove(Board board, Timer timer, bool isWhite, int depth, int bestPrev)
        {

            Move[] allMoves = board.GetLegalMoves();
            List<Move> movesToPlay = new List<Move>(allMoves);
            bool ownMove = isWhite == board.IsWhiteToMove;

            int bestValue = ownMove ? int.MinValue : int.MaxValue;

            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                int moveValue = EvaluateBoard(board, timer, isWhite, depth + 1, bestValue);
                if (ownMove ? moveValue >= bestValue : moveValue <= bestValue)
                {
                    if (bestValue != moveValue)
                        movesToPlay.Clear();
                    bestValue = moveValue;
                    movesToPlay.Add(move);
                }
                board.UndoMove(move);
                /*
                if (ownMove ? bestValue > bestPrev : bestValue < bestPrev)
                    break;
                */
            }

            // Pick random move from list of best moves (equal evaluation)
            Random rng = new Random();

            return new EvalMove(movesToPlay[rng.Next(movesToPlay.Count)], bestValue);
        }

        // Helper Functions
        private int StaticEval(Board board, bool isWhite)
        {
            int totalValue = 0;

            // Piece Value
            PieceList[] allPieceLists = board.GetAllPieceLists();
            foreach (PieceList pieceList in allPieceLists)
            {
                int pieceListValue = 0;
                foreach (Piece piece in pieceList)
                {
                    pieceListValue += pieceValues[(int)piece.PieceType];
                }
                totalValue += pieceList.IsWhitePieceList == isWhite ? pieceListValue : -pieceListValue;
            }

            // Square Value
            ulong[] pieceBitboards = { board.GetPieceBitboard(PieceType.Pawn, isWhite),
            board.GetPieceBitboard(PieceType.Knight, isWhite),
            board.GetPieceBitboard(PieceType.Bishop, isWhite) };

            for (int i = 0; i < 3; i++)
            {
                int len = BitboardHelper.GetNumberOfSetBits(pieceBitboards[i]);
                for (int j = 0; j < len; j++)
                {
                    int index = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBitboards[i]);
                    int convertedIndex = ConvInd(index, isWhite);
                    totalValue += squareValues[i][convertedIndex];
                }
            }

            return totalValue;
        }

        private int ConvInd(int index, bool isWhite)
        {
            if (!isWhite)
                return index;

            int x = index % 8;
            int y = index / 8;
            y = 7 - y;
            return 8 * y + x;
        }
    }
}