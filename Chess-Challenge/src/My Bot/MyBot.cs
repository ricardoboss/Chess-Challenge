using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    Random rng = new();

    public Move Think(Board board, Timer timer)
    {
        var moves = board.GetLegalMoves().OrderBy(_ => rng.NextDouble()).ToArray();

        return GetMoveThatCaptures(board, moves) ?? GetNotAttackedMove(board, moves) ?? moves[0];
    }

    Move? GetNotAttackedMove(Board board, Move[] moves)
    {
        var allMoves = new Queue<Move>(moves);

        Move moveToPlay;
        do
        {
            if (!allMoves.TryDequeue(out moveToPlay))
                return null;
        } while (board.SquareIsAttackedByOpponent(moveToPlay.TargetSquare));

        return moveToPlay;
    }

    Move? GetMoveThatCaptures(Board board, Move[] moves)
    {
        var highestValueCapture = 0;
        Move? moveToPlay = null;
        foreach (var move in moves)
        {
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            var capturedPiece = board.GetPiece(move.TargetSquare);
            if ((int)capturedPiece.PieceType <= highestValueCapture)
                continue;

            moveToPlay = move;
            highestValueCapture = (int)capturedPiece.PieceType;
        }

        if (highestValueCapture == 0) return null;

        return moveToPlay;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        var isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}