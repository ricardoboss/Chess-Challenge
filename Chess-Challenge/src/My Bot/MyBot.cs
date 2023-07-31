using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

// ReSharper disable All
public class MyBot : IChessBot
{
    const int maxDepth = 2; // should be even for best scoring

    const int pawnValue = 100;
    const int knightValue = 300;
    const int bishopValue = 300;
    const int rookValue = 500;
    const int queenValue = 900;
    const int kingValue = 10000;

    const int castlingBonus = 200;
    const int promotionBonus = 50;
    const int captureBonus = 100;
    const int centerBonus = 20;
    const int pawnPromotionBonus = 1000;
    const int checkmateBonus = 10000;
    const int checkBonus = 1000;

    const int doublePawnPenalty = 30;
    const int isolatedPawnPenalty = 20;
    const int edgeKnightPenalty = 10;
    const int kingMovePenalty = 150;

    const int drawPenalty = 1000;

    Random rng = new();
    Board board = null!;
    bool iAmWhite;

    public Move Think(Board b, Timer timer)
    {
        board = b;
        iAmWhite = board.IsWhiteToMove;

        var (bestScore, bestMove) = GetBestMove();

        Console.WriteLine("Best move score: " + bestScore + ", took " + timer.MillisecondsElapsedThisTurn);

        return bestMove;
    }

    (int score, Move move) GetBestMove(int depth = 0)
    {
        var legalMoves = board.GetLegalMoves();
        if (legalMoves.Length == 0)
        {
            if (board.IsDraw() || board.IsInsufficientMaterial())
                return (drawPenalty, new());

            if (board.IsInCheckmate())
                return (-checkmateBonus, new());

            throw new InvalidOperationException("No legal moves, but not in checkmate or draw?");
        }

        var bestMoves = new List<Move>(legalMoves);
        var bestScore = int.MinValue;
        foreach (var move in legalMoves)
        {
            var score = EvaluateMove(move, depth);
            if (score > bestScore)
            {
                bestMoves.Clear();
                bestMoves.Add(move);
                bestScore = score;
            }
            else if (score == bestScore)
            {
                bestMoves.Add(move);
            }
        }

        Debug.Assert(bestMoves.Count > 0, "bestMoves.Count > 0");

        return (bestScore, bestMoves[0]);
    }

    int EvaluateMove(Move move, int depth = 0)
    {
        var score = 0;

        board.MakeMove(move);

        var preMoveEvaluation = EvaluateBoard();

        if (depth < maxDepth)
        {
            var (enemyBestMoveScore, _) = GetBestMove(depth + 1);

            score -= enemyBestMoveScore;
        }

        var postMoveEvaluation = EvaluateBoard();

        board.UndoMove(move);

        score += postMoveEvaluation - preMoveEvaluation;

        if (move.IsCastles)
            score += castlingBonus;
        
        if (move.IsPromotion)
            score += pawnPromotionBonus;

        if (move.MovePieceType is PieceType.King)
            score -= kingMovePenalty;

        if (move.IsCapture)
            score += captureBonus + PieceTypeSingleValue(move.CapturePieceType);

        if (move.IsPromotion)
            score += promotionBonus + PieceTypeSingleValue(move.PromotionPieceType);

        return score;
    }

    int EvaluateBoard()
    {
        var pawnsValue = PieceTypeValues(PieceType.Pawn, board, iAmWhite) - PieceTypeValues(PieceType.Pawn, board, !iAmWhite);
        var knightsValue = PieceTypeValues(PieceType.Knight, board, iAmWhite) - PieceTypeValues(PieceType.Knight, board, !iAmWhite);
        var bishopsValue = PieceTypeValues(PieceType.Bishop, board, iAmWhite) - PieceTypeValues(PieceType.Bishop, board, iAmWhite);
        var rooksValue = PieceTypeValues(PieceType.Rook, board, iAmWhite) - PieceTypeValues(PieceType.Rook, board, iAmWhite);
        var queensValue = PieceTypeValues(PieceType.Queen, board, iAmWhite) - PieceTypeValues(PieceType.Queen, board, iAmWhite);

        var sum = pawnsValue + knightsValue + bishopsValue + rooksValue + queensValue;

        if (board.IsInCheckmate())
            sum += checkmateBonus;
        else if (board.IsInCheck())
            sum += checkBonus;
        else if (board.IsDraw() || board.IsInsufficientMaterial() || board.IsRepeatedPosition())
            sum -= drawPenalty;

        return sum;
    }

    static int PieceTypeSingleValue(PieceType type) => type switch
    {
        PieceType.Pawn => pawnValue,
        PieceType.Knight => knightValue,
        PieceType.Bishop => bishopValue,
        PieceType.Rook => rookValue,
        PieceType.Queen => queenValue,
        PieceType.King => kingValue,
        PieceType.None => 0,
    };

    static int PieceTypeValues(PieceType type, Board board, bool white)
    {
        switch (type)
        {
            case PieceType.Pawn:
                var pawns = board.GetPieceList(type, white);
                var pawnSum = 0;

                foreach (var pawn in pawns)
                {
                    pawnSum += pawnValue;

                    if (IsInCenterSquares(pawn.Square))
                        pawnSum += centerBonus;

                    if (pawns.Any(p => p.Square.File == pawn.Square.File && p.Square.Rank != pawn.Square.Rank))
                        pawnSum -= doublePawnPenalty;

                    if (pawns.Any(p => p.Square.File == pawn.Square.File - 1 || p.Square.File == pawn.Square.File + 1))
                        pawnSum -= isolatedPawnPenalty;
                }

                return pawnSum;
            case PieceType.Knight:
                var knights = board.GetPieceList(type, white);
                var knightsSum = 0;

                foreach (var knight in knights)
                {
                    knightsSum += knightValue;

                    if (IsInCenterSquares(knight.Square))
                        knightsSum += centerBonus;

                    if (knight.Square.File is 0 or 7)
                        knightsSum -= edgeKnightPenalty;
                }

                return knightsSum;
            case PieceType.Bishop:
                return board.GetPieceList(type, white).Count * bishopValue;
            case PieceType.Rook:
                return board.GetPieceList(type, white).Count * rookValue;
            case PieceType.Queen:
                return board.GetPieceList(type, white).Count * queenValue;
            case PieceType.King:
                return kingValue;
            case PieceType.None:
            default:
                return 0;
        }
    }

    static bool IsInCenterSquares(Square square)
        => square.Rank is >= 2 and <= 5 && square.File is >= 3 and <= 6;
}