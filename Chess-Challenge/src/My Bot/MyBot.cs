using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    const int pawnValue = 100;
    const int knightValue = 300;
    const int bishopValue = 320;
    const int rookValue = 500;
    const int queenValue = 900;

    const float endgameMaterialStart = rookValue * 2 + bishopValue + knightValue;

    int checkDepth = 2;

    Random rng = new();
    Board board = null!;

    public Move Think(Board currentBoard, Timer timer)
    {
        board = currentBoard;

        var moves = GetBestMoves();

        return moves.MinBy(_ => rng.NextDouble());
    }

    /// Calculates the best moves based on an evaluation function
    private IEnumerable<Move> GetBestMoves()
    {
        var bestMoveValue = int.MinValue;
        var bestMoves = new List<Move>();
        var moves = board.GetLegalMoves();

        if (moves.Length == 1) return moves;

        Console.Write($"available moves: {moves.Length}");

        do
        {
            foreach (var move in moves)
            {
                var moveValue = EvaluateMove(move, checkDepth);
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

            if (checkDepth < 6)
                checkDepth++;
            else if (bestMoveValue == 0)
            {
                Console.Write(" (max depth reached)");
                break;
            }
        } while (bestMoveValue <= 0);

        if (checkDepth > 1)
            checkDepth--;

        Console.WriteLine($", max score: {bestMoveValue}, moves with that score: {bestMoves.Count}");

        return bestMoves;
    }

    static Dictionary<ulong, Dictionary<ushort, int>> moveCache = new();

    /// Evaluates a move. The higher the score, the better the move.
    private int EvaluateMove(Move move, int depth = 0)
    {
        var isMyMove = depth % 2 == 0;

        Dictionary<ushort, int> boardCache;
        if (moveCache.TryGetValue(board.ZobristKey, out var nullableBoardCache))
        {
            boardCache = nullableBoardCache;
            if (boardCache.TryGetValue(move.RawValue, out var precomputed))
            {
                return isMyMove ? precomputed : -precomputed;
            }
        }
        else
        {
            boardCache = new();
            moveCache[board.ZobristKey] = boardCache;
        }

        board.MakeMove(move);

        var score = StaticEvaluate();

        if (board.IsInCheckmate())
        {
            score += 100000;
        }
        else
        {
            if (depth < checkDepth)
            {
                var bestOpponentMoveScores = board
                    .GetLegalMoves()
                    .Select(m => EvaluateMove(m, depth + 1))
                    .ToList();

                if (bestOpponentMoveScores.Count > 0)
                    score -= bestOpponentMoveScores.Max(); // discourage moves that lead to a good opponent move
                else
                    score += 100000; // opponent has no legal moves => checkmate
            }
        }

        board.UndoMove(move);

        boardCache[move.RawValue] = score;
        Debug.WriteLine($"cache miss: {depth}, {move}, {score}");

        return isMyMove ? score : -score;
    }

    /// Performs static analysis of the current board and returns a higher score for the current players perspective
    int StaticEvaluate()
    {
        var whiteScore = 0;
        var blackScore = 0;

        var whiteMaterial = CountMaterial(true, false);
        var blackMaterial = CountMaterial(false, false);
        // var whiteMaterialWithoutPawns = CountMaterial(true, true);
        // var blackMaterialWithoutPawns = CountMaterial(true, true);
        // var whiteEndgamePhaseWeight = EndgamePhaseWeight(whiteMaterialWithoutPawns);
        // var blackEndgamePhaseWeight = EndgamePhaseWeight(blackMaterialWithoutPawns);

        whiteScore += whiteMaterial;
        blackScore += blackMaterial;
        // whiteScore += MopUpEval(false, whiteMaterial, blackMaterial, blackEndgamePhaseWeight);
        // blackScore += MopUpEval(true, blackMaterial, whiteMaterial, whiteEndgamePhaseWeight);

        // whiteScore += EvaluatePieceSquareTables(true, blackEndgamePhaseWeight);
        // blackScore += EvaluatePieceSquareTables(false, whiteEndgamePhaseWeight);

        var score = whiteScore - blackScore;

        return board.IsWhiteToMove ? score : -score;
    }

    float EndgamePhaseWeight(int materialCountWithoutPawns)
    {
        const float multiplier = 1 / endgameMaterialStart;
        return 1 - Math.Min(1, materialCountWithoutPawns * multiplier);
    }

    // int MopUpEval(bool friendly, int friendlyIndex, int opponentIndex, int myMaterial, int opponentMaterial, float endgameWeight)
    // {
    //     var mopUpScore = 0;
    //     if (myMaterial <= opponentMaterial + pawnValue * 2 || !(endgameWeight > 0))
    //         return 0;
    //
    //     var friendlyKingSquare = board.GetKingSquare(friendly);
    //     var opponentKingSquare = board.GetKingSquare(!friendly);
    //     mopUpScore += PrecomputedMoveData.centreManhattanDistance[opponentKingSquare] * 10;
    //     // use ortho dst to promote direct opposition
    //     mopUpScore += (14 - PrecomputedMoveData.NumRookMovesToReachSquare(friendlyKingSquare, opponentKingSquare)) * 4;
    //
    //     return (int)(mopUpScore * endgameWeight);
    // }

    int CountMaterial(bool white, bool excludePawns)
    {
        var material = 0;

        if (!excludePawns)
            material += board.GetPieceList(PieceType.Pawn, white).Count * pawnValue;

        material += board.GetPieceList(PieceType.Knight, white).Count * knightValue;
        material += board.GetPieceList(PieceType.Bishop, white).Count * bishopValue;
        material += board.GetPieceList(PieceType.Rook, white).Count * rookValue;
        material += board.GetPieceList(PieceType.Queen, white).Count * queenValue;

        return material;
    }

    // int EvaluatePieceSquareTables(bool isWhite, float endgamePhaseWeight)
    // {
    //     var value = 0;
    //     value += EvaluatePieceSquareTable(PieceSquareTable.pawns, board.pawns[colourIndex], isWhite);
    //     value += EvaluatePieceSquareTable(PieceSquareTable.rooks, board.rooks[colourIndex], isWhite);
    //     value += EvaluatePieceSquareTable(PieceSquareTable.knights, board.knights[colourIndex], isWhite);
    //     value += EvaluatePieceSquareTable(PieceSquareTable.bishops, board.bishops[colourIndex], isWhite);
    //     value += EvaluatePieceSquareTable(PieceSquareTable.queens, board.queens[colourIndex], isWhite);
    //     int kingEarlyPhase = PieceSquareTable.Read(PieceSquareTable.kingMiddle, board.KingSquare[colourIndex], isWhite);
    //     value += (int)(kingEarlyPhase * (1 - endgamePhaseWeight));
    //     //value += PieceSquareTable.Read (PieceSquareTable.kingMiddle, board.KingSquare[colourIndex], isWhite);
    //
    //     return value;
    // }
    //
    // static int EvaluatePieceSquareTable(int[] table, PieceList pieceList, bool isWhite)
    // {
    //     var value = 0;
    //     for (var i = 0; i < pieceList.Count; i++)
    //     {
    //         value += PieceSquareTable.Read(table, pieceList[i], isWhite);
    //     }
    //
    //     return value;
    // }
}