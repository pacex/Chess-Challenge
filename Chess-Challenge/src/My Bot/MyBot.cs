using ChessChallenge.API;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 0 };
    int maxDepth = 4;
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
        maxDepth = (int)(Math.Log2(budget) / Math.Log2(board.GetLegalMoves().Length));
        if (board.IsInCheck())
            maxDepth = Math.Min(maxDepth, 4);
        maxDepth = Math.Max(maxDepth, 2);
        Console.WriteLine("Recursion depth: " + maxDepth);

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
            return PieceValue(board, isWhite);
        }

        // Recursion case
        return PickMove(board, timer, isWhite, depth, bestPrev).Value;
    }

    private EvalMove PickMove(Board board, Timer timer, bool isWhite, int depth, int bestPrev) {

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
            if (ownMove ? bestValue > bestPrev : bestValue < bestPrev)
                break;
        }

        // Pick random move from list of best moves (equal evaluation)
        Random rng = new Random();

        return new EvalMove(movesToPlay[rng.Next(movesToPlay.Count)], bestValue);
    }

    // Evaluation Helper Functions
    private int PieceValue(Board board, bool isWhite)
    {
        int totalPieceValue = 0;

        PieceList[] allPieceLists = board.GetAllPieceLists();
        foreach (PieceList pieceList in allPieceLists)
        {
            int pieceListValue = 0;
            foreach (Piece piece in pieceList)
            {
                pieceListValue += pieceValues[(int)piece.PieceType];
            }
            totalPieceValue += pieceList.IsWhitePieceList == isWhite ? pieceListValue : -pieceListValue;
        }
        return totalPieceValue;
    }
    
}