using ChessChallenge.API;
using System;
using System.Collections;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    #region Piece and Square Values
    static int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };
    // Pawn
    static short[][] squareValues = { new short[]{ 0,  0,  0,  0,  0,  0,  0,  0,
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

    int maxDepth;
    public bool isWhite;
    //int budget = 1000000;
    MoveComp moveComp;

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

    public class MoveComp : IComparer<Move>
    {
        public static MyBot Bot;
        public static Board Board;

        public int Compare(Move x, Move y)
        {     
            Board.MakeMove(x);
            int v1 = MyBot.StaticEval(Board, Bot.isWhite);
            Board.UndoMove(x);

            Board.MakeMove(y);
            int v2 = MyBot.StaticEval(Board, Bot.isWhite);
            Board.UndoMove(y);
            
            return v1.CompareTo(v2);
        }
    }

    public MyBot()
    {
        MoveComp.Bot = this;
        maxDepth = 3;
        moveComp = new MoveComp();
    }

    public Move Think(Board board, Timer timer)
    {
        isWhite = board.IsWhiteToMove;

        // Determine recursion depth
        /*
        Move[] legalMoves = board.GetLegalMoves();
        board.MakeMove(legalMoves[0]);
        int opponentMovesCount = board.GetLegalMoves().Length;
        board.UndoMove(legalMoves[0]);

        maxDepth = (int)(Math.Log2(budget) / Math.Log2(Math.Max(legalMoves.Length, opponentMovesCount)));
        if (board.IsInCheck())
            maxDepth = Math.Min(maxDepth, 4);
        maxDepth = Math.Max(maxDepth, 2);

        maxDepth = 3; // DEBUG, TODO: REMOVE
        Console.WriteLine("MyBot | Recursion depth: " + maxDepth);
        */
        
        return PickMove(board, timer, isWhite, 0, int.MaxValue).Move;
    }

    private int EvaluateBoard(Board board, Timer timer, bool isWhite, int depth, int bestPrev)
    {

        // Base cases
        if (board.IsInCheckmate())
            return isWhite != board.IsWhiteToMove ? int.MaxValue : int.MinValue;
        
        if (board.IsDraw()) 
            return 0;

        if (depth >= maxDepth)
            return StaticEval(board, isWhite);

        // Recursion case
        return PickMove(board, timer, isWhite, depth, bestPrev).Value;
    }

    private EvalMove PickMove(Board board, Timer timer, bool isWhite, int depth, int bestPrev) {

        List<Move> allMoves = new List<Move>(board.GetLegalMoves());
        bool ownMove = isWhite == board.IsWhiteToMove;
        
        if (depth < maxDepth - 1)
        {
            MoveComp.Board = board;
            allMoves.Sort(moveComp);
            if (ownMove)
                allMoves.Reverse();
        }

        List<Move> movesToPlay = new List<Move>(allMoves);
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
            if (ownMove ? bestValue >= bestPrev : bestValue <= bestPrev)
            {
                bestValue = ownMove ? int.MaxValue : int.MinValue;
                break;
            }
            
        }

        // Pick random move from list of best moves (equal evaluation)
        return new EvalMove(movesToPlay[new Random().Next(movesToPlay.Count)], bestValue);
    }

    // Helper Functions
    public static int StaticEval(Board board, bool isWhite)
    {
        int totalValue = 0;

        if (board.IsInCheckmate())
            return (isWhite == !board.IsWhiteToMove) ? int.MaxValue : int.MinValue;

        else if (board.IsDraw())
            return 0;

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

        for(int i = 0; i < 3; i++)
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

    private static int ConvInd(int index, bool isWhite)
    {
        if (!isWhite)
            return index;

        int x = index % 8;
        int y = index / 8;
        y = 7 - y;
        return 8 * y + x;
    }
    
}