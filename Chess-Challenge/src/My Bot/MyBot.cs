using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    Random rng = new();

    public Move Think(Board board, Timer timer)
    {
        var moves = GetBestMoves(board, 3, 2000);

        var moveToMake = moves.MinBy(_ => rng.NextDouble());

        Console.WriteLine($"Chose move: {moveToMake}");

        return moveToMake;
    }

    /// Calculates the best moves based on an evaluation function
    private IEnumerable<Move> GetBestMoves(Board board, int minCheckDepth, long maxMillis)
    {
        var bestMoveValue = int.MinValue;
        var bestMoves = new List<Move>();
        var moves = board.GetLegalMoves();

        if (moves.Length == 1)
            return moves;

        var sw = new Stopwatch();
        sw.Start();
        do
        {
            Console.Write($"Depth: {minCheckDepth}, available moves: {moves.Length}");

            foreach (var move in moves.OrderBy(_ => rng.NextDouble()))
            {
                var moveValue = EvaluateMove(board, move, sw, maxMillis, minCheckDepth);
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

            Console.WriteLine($", max score: {bestMoveValue}, moves with that score: {bestMoves.Count}, time: {sw.ElapsedMilliseconds}ms");

            minCheckDepth++;
        } while (sw.ElapsedMilliseconds < maxMillis && minCheckDepth < 10);

        return bestMoves;
    }

    static Dictionary<ulong, Dictionary<ushort, int>> moveCache = new();

    /// Evaluates a move based on the value of the piece that is captured
    /// The higher the better the move
    private int EvaluateMove(Board board, Move move, Stopwatch sw, long maxMillis, int maxDepth, int depth = 0)
    {
        var isMyMove = depth % 2 == 0;
        var score = 0;

        Dictionary<ushort, int> boardCache;
        if (moveCache.TryGetValue(board.ZobristKey, out var nullableBoardCache))
        {
            boardCache = nullableBoardCache;
            if (boardCache.TryGetValue(move.RawValue, out score))
            {
                score = isMyMove ? score : -score;
            }
        }
        else
        {
            boardCache = new();
            moveCache[board.ZobristKey] = boardCache;
        }

        if (move.IsCapture)
        {
            var capturedPiece = board.GetPiece(move.TargetSquare);
            score += capturedPiece.PieceType switch
            {
                PieceType.Pawn => 1,
                PieceType.Knight => 10,
                PieceType.Bishop => 100,
                PieceType.Rook => 1000,
                PieceType.Queen => 10000,
                PieceType.King => 100000,
                _ => 0,
            };
        }

        if (move.IsPromotion)
        {
            score += move.PromotionPieceType switch
            {
                PieceType.Knight => 1,
                PieceType.Bishop => 10,
                PieceType.Rook => 100,
                PieceType.Queen => 1000,
                _ => 0,
            };
        }

        if (board.SquareIsAttackedByOpponent(move.StartSquare))
        {
            score += 10; // encourage moving away from attacked squares
        }

        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            score -= 10; // discourage moving to attacked squares

            board.MakeMove(move);

            // can we attack the attacker?
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                score += 10; // encourage attacking the attacker

                var baitType = move.MovePieceType;
                var attackerType = board.GetPiece(move.TargetSquare).PieceType;

                score += baitType switch
                {
                    PieceType.Pawn => attackerType switch
                    {
                        PieceType.Pawn => -10,
                        PieceType.Knight => 10,
                        PieceType.Bishop => 100,
                        PieceType.Rook => 1000,
                        PieceType.Queen => 10000,
                        PieceType.King => 100000, // leads to checkmate?
                        _ => 0,
                    },
                    PieceType.Knight => attackerType switch
                    {
                        PieceType.Pawn => -100,
                        PieceType.Knight => -10,
                        PieceType.Bishop => 10,
                        PieceType.Rook => 100,
                        PieceType.Queen => 1000,
                        PieceType.King => 10000, // leads to checkmate?
                        _ => 0,
                    },
                    PieceType.Bishop => attackerType switch
                    {
                        PieceType.Pawn => -1000,
                        PieceType.Knight => -100,
                        PieceType.Bishop => -10,
                        PieceType.Rook => 10,
                        PieceType.Queen => 100,
                        PieceType.King => 1000, // leads to checkmate?
                        _ => 0,
                    },
                    PieceType.Rook => attackerType switch
                    {
                        PieceType.Pawn => -1000,
                        PieceType.Knight => -100,
                        PieceType.Bishop => -10,
                        PieceType.Rook => -10,
                        PieceType.Queen => 10,
                        PieceType.King => 100, // leads to checkmate?
                        _ => 0,
                    },
                    PieceType.Queen => attackerType switch
                    {
                        PieceType.Pawn => -10000,
                        PieceType.Knight => -1000,
                        PieceType.Bishop => -100,
                        PieceType.Rook => -10,
                        PieceType.Queen => -1, // queen exchange when possible
                        PieceType.King => 1000, // leads to checkmate?
                        _ => 0,
                    },
                    PieceType.King => -100000,
                    _ => 0,
                };
            }

            board.UndoMove(move);
        }

        switch (move.MovePieceType)
        {
            case PieceType.Pawn:
                // TODO: increase score if reaching the other side
                score -= 1;
                break;
            case PieceType.Knight:
                // TODO: check for knight specific improvements
                break;
            case PieceType.Bishop:
                // TODO: check for bishop specific improvements
                break;
            case PieceType.Rook:
                // TODO: check for rook specific improvements
                break;
            case PieceType.Queen:
                // TODO: check for queen specific improvements
                break;
            case PieceType.King:
                score -= 10; // discourage king from moving
                if (board.IsInCheck() && !board.GetPiece(move.TargetSquare).IsNull)
                {
                    score += 100; // make the king capture the attacker
                }

                break;
        }

        if (move.TargetSquare.Index is >= 27 and <= 28 or >= 35 and <= 36)
        {
            score += 1; // encourage moving to the opponent's side
        }

        board.MakeMove(move);

        if (board.IsInCheckmate())
        {
            score += 100000; // make the move if it leads to checkmate
        }
        else if (board.IsInCheck())
        {
            score += 1000; // encourage checking the opponent

            board.UndoMove(move);

            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                score -= 1000; // discourage checking the opponent if the attacker is attacked
            }

            board.MakeMove(move);
        }
        // else if (board.IsDraw())
        // {
        //     score -= 5;
        // }

        if (depth < maxDepth && sw.ElapsedMilliseconds < maxMillis)
        {
            var bestOpponentMoveScores = board
                .GetLegalMoves()
                .Select(m => EvaluateMove(board, m, sw, maxMillis, maxDepth, depth + 1))
                .ToList();

            if (bestOpponentMoveScores.Count > 0)
                score -= bestOpponentMoveScores.Max(); // discourage moves that lead to a good opponent move
            else
                score += 100000; // opponent has no legal moves => checkmate
        }

        board.UndoMove(move);

        // only cache if fully calculated
        if (sw.ElapsedMilliseconds > maxMillis)
            boardCache[move.RawValue] = score;

        return isMyMove ? score : -score;
    }
}