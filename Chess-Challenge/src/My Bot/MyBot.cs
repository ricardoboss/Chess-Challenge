using System.Collections.Generic;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Random rng = new();
    private string[][] openingMoves = new[]
    {
        new []
        {
            ""
        }
    };

    public Move Think(Board board, Timer timer)
    {
        // var moves = board.GetLegalMoves().OrderBy(_ => rng.NextDouble()).ToArray();
        var moves = GetBestMoves(board);

        // return GetMoveThatCaptures(board, moves) ?? GetNotAttackedMove(board, moves) ?? moves[0];
        return moves[0];
    }

    /// Calculates the best moves based on an evaluation function
    private List<Move> GetBestMoves(Board board)
    {
        board.GetPieceList()

        var moves = board.GetLegalMoves();
        var bestMoves = new List<Move>();
        var bestMoveValue = int.MinValue;

        foreach (var move in moves)
        {
            var moveValue = EvaluateMove(board, move);
            if (moveValue > bestMoveValue)
            {
                bestMoves.Clear();
                bestMoves.Add(move);
                bestMoveValue = moveValue;
            }
            else if (moveValue == bestMoveValue)
            {
                bestMoves.Add(move);
            }
        }

        return bestMoves;
    }

    /// Evaluates a move based on the value of the piece that is captured
    /// The higher the better the move
    private int EvaluateMove(Board board, Move move)
    {
        var capturedPiece = board.GetPiece(move.TargetSquare);
        return (int)capturedPiece.PieceType;
    }

    // Move? GetNotAttackedMove(Board board, Move[] moves)
    // {
    //     var allMoves = new Queue<Move>(moves);
    //
    //     // IDEA: check multiple moves into the future if the bot can get attacked after x moves after this move
    //
    //     Move moveToPlay;
    //     do
    //     {
    //         if (!allMoves.TryDequeue(out moveToPlay))
    //             return null;
    //     } while (board.SquareIsAttackedByOpponent(moveToPlay.TargetSquare));
    //
    //     return moveToPlay;
    // }

    // Move? GetMoveThatCaptures(Board board, Move[] moves)
    // {
    //     var highestValueCapture = 0;
    //     Move? moveToPlay = null;
    //     foreach (var move in moves)
    //     {
    //         // shortcut to checkmate
    //         if (MoveIsCheckmate(board, move))
    //         {
    //             moveToPlay = move;
    //             break;
    //         }
    //
    //         // only update moveToPlay if the target piece is of higher value
    //         var capturedPiece = board.GetPiece(move.TargetSquare);
    //         if ((int)capturedPiece.PieceType <= highestValueCapture)
    //             continue;
    //
    //         // don't update if the attack would give the opponent the opportunity to capture
    //         if (board.SquareIsAttackedByOpponent(move.TargetSquare))
    //             continue;
    //
    //         moveToPlay = move;
    //         highestValueCapture = (int)capturedPiece.PieceType;
    //     }
    //
    //     if (highestValueCapture == 0) return null;
    //
    //     return moveToPlay;
    // }

    // bool MoveIsCheckmate(Board board, Move move)
    // {
    //     board.MakeMove(move);
    //     var isMate = board.IsInCheckmate();
    //     board.UndoMove(move);
    //     return isMate;
    // }

    // bool EnablesBait(Board board, Move move)
    // {
    //     board.MakeMove(move);
    //     var opponentMovesToTargetSquare = board.GetLegalMoves().Where(m => m.TargetSquare == move.TargetSquare).ToArray();
    //
    // }
}