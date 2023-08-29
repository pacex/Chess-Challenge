using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Random rnd = new Random();
        Move[] moves = board.GetLegalMoves();
        
        return moves[rnd.Next(moves.Length)];
    }
}