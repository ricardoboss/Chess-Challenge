using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    Random rng = new();

    public Move Think(Board board, Timer timer)
    {
        var moves = GetBestMoves(board, 2);

        var moveToMake = moves.MinBy(_ => rng.NextDouble());

        Console.WriteLine($"Chose move: {moveToMake}");

        return moveToMake;
    }

    /// Calculates the best moves based on an evaluation function
    private IEnumerable<Move> GetBestMoves(Board board, int checkDepth)
    {
        var bestMoveValue = int.MinValue;
        var bestMoves = new List<Move>();
        var moves = board.GetLegalMoves();

        do
        {
            foreach (var move in moves)
            {
                var moveValue = ScoreMove(board, move, checkDepth);
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

            Console.WriteLine($"Depth: {checkDepth}, max score: {bestMoveValue}, moves with that score: {bestMoves.Count}");

            checkDepth++;
        } while (bestMoveValue <= 0 && checkDepth < 5);

        return bestMoves;
    }

    /// Evaluates a move based on the value of the piece that is captured
    /// The higher the better the move
    private int ScoreMove(Board board, Move move, int maxDepth, int depth = 0)
    {
        board.MakeMove(move);

        var score = 0;

        if (board.IsInCheckmate())
        {
            score += 100;
        }
        else if (board.IsInCheck())
        {
            score += 10;
        }
        else if (board.IsDraw())
        {
            score -= 5;
        }

        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            score -= 5;
        }

        if (move.IsCapture)
        {
            var capturedPiece = board.GetPiece(move.TargetSquare);
            score += capturedPiece.PieceType switch
            {
                PieceType.Pawn => 1,
                PieceType.Knight => 5,
                PieceType.Bishop => 7,
                PieceType.Rook => 9,
                PieceType.Queen => 10,
                PieceType.King => 20,
                _ => 0,
            } * 2;
        }

        if (move.IsPromotion)
        {
            score += move.PromotionPieceType switch
            {
                PieceType.Knight => 3,
                PieceType.Bishop => 5,
                PieceType.Rook => 7,
                PieceType.Queen => 9,
                _ => 0,
            };
        }

        if (move.MovePieceType == PieceType.King)
        {
            score -= 5;
        }

        if (depth < maxDepth)
        {
            var bestOpponentMoveScores = board
                .GetLegalMoves()
                .Select(m => ScoreMove(board, m, maxDepth, depth + 1))
                .ToList();

            if (bestOpponentMoveScores.Count > 0)
                score -= bestOpponentMoveScores.Max();
            else
                score += 100; // opponent has no legal moves => checkmate
        }

        board.UndoMove(move);

        var isMyMove = depth % 2 == 0;
        return isMyMove ? score : -score;
    }
}