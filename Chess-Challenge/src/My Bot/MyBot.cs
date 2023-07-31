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
    const int rookValue = 600;
    const int queenValue = 950;
    const int kingValue = 10000;

    const int castlingBonus = 300;
    const int castlingAvailableBonus = 500;
    const int promotionBonus = 200;
    const int captureBonus = 500;
    const int centerBonus = 40;
    const int checkmateBonus = 100000;
    const int checkBonus = 500;
    const int coveredSquareMoveBonus = 5000;

    const int doublePawnPenalty = 60;
    const int isolatedPawnPenalty = 10;
    const int edgeKnightPenalty = 5;
    const int kingMovePenalty = 200;
    const int attackedSquareMovePenalty = 10000;
    const int revertMovePenalty = 150;

    const int drawPenalty = 10000;

    Board board = null!;
    Timer timer = null!;
    bool iAmWhite;
    Move lastMove = Move.NullMove;

    public Move Think(Board b, Timer t)
    {
        board = b;
        timer = t;
        iAmWhite = board.IsWhiteToMove;

        var (bestScore, bestMove) = GetBestMove(maxDepth);

        Console.WriteLine("Best move score: " + bestScore + ", took " + timer.MillisecondsElapsedThisTurn + "ms");

        return lastMove = bestMove;
    }

    (int score, Move move) GetBestMove(int remainingDepth)
    {
        var legalMoves = board.GetLegalMoves();
        if (legalMoves.Length == 0)
        {
            if (board.IsDraw() || board.IsInsufficientMaterial())
                return (drawPenalty, new());

            if (board.IsInCheck()) // if legal moves are empty, this already means checkmate
                return (-checkmateBonus, new());

            throw new InvalidOperationException("No legal moves, but not in checkmate or draw?");
        }

        var bestMoves = new List<Move>(legalMoves);
        var bestScore = int.MinValue;
        foreach (var move in legalMoves)
        {
            var score = EvaluateMove(move, remainingDepth);
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

    int EvaluateMove(Move move, int remainingDepth = 0)
    {
        var score = 0;

        // pre move evaluation
        // score += EvaluateBoard();

        board.MakeMove(move);

        if (remainingDepth > 0)
        {
            var (enemyBestMoveScore, _) = GetBestMove(remainingDepth - 1);

            score -= enemyBestMoveScore;
        }

        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            score += coveredSquareMoveBonus;

        // post move evaluation
        score += EvaluateBoard();

        board.UndoMove(move);

        if (move.IsCastles)
            score += castlingBonus;

        if (move.MovePieceType is PieceType.King)
            score -= kingMovePenalty;

        if (move.IsCapture)
            score += captureBonus + PieceTypeSingleValue(move.CapturePieceType);

        if (move.IsPromotion)
            score += promotionBonus + PieceTypeSingleValue(move.PromotionPieceType);

        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            score -= attackedSquareMovePenalty;

        if (lastMove.MovePieceType == move.MovePieceType && lastMove.TargetSquare == move.StartSquare)
            score -= revertMovePenalty;

        return score;
    }
    int EvaluateBoard()
    {
        var pawnsValue = PieceValuesDiff(PieceType.Pawn, iAmWhite);
        var knightsValue = PieceValuesDiff(PieceType.Knight, iAmWhite);
        var bishopsValue = PieceValuesDiff(PieceType.Bishop, iAmWhite);
        var rooksValue = PieceValuesDiff(PieceType.Rook, iAmWhite);
        var queensValue = PieceValuesDiff(PieceType.Queen, iAmWhite);

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

    int PieceValuesDiff(PieceType type, bool white) => PieceTypeValues(type, white) - PieceTypeValues(type, !white);

    int PawnValues(bool white)
    {
        var pawns = board.GetPieceList(PieceType.Pawn, white);
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
    }

    int KnightValues(bool white)
    {
        var knights = board.GetPieceList(PieceType.Knight, white);
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
    }

    int RookValues(bool white)
    {
        var rooks = board.GetPieceList(PieceType.Rook, white);
        var rooksSum = 0;

        foreach (var rook in rooks)
        {
            rooksSum += rookValue;

            if (board.HasKingsideCastleRight(white))
                rooksSum += castlingAvailableBonus;

            if (board.HasQueensideCastleRight(white))
                rooksSum += castlingAvailableBonus;
        }

        return rooksSum;
    }

    int PieceTypeValues(PieceType type, bool white)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return PawnValues(white);
            case PieceType.Knight:
                return KnightValues(white);
            case PieceType.Bishop:
                return board.GetPieceList(type, white).Count * bishopValue;
            case PieceType.Rook:
                return RookValues(white);
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