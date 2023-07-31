using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class AresV1 : IChessBot
{
    bool _iamWhite;
    Board _board;
    Move _move;
    Move? _lastMove;
    Random _random = new();
    
    class MoveWorthy : IComparable<MoveWorthy>
    {
        public double Value;
        public Move Move;

        public int CompareTo(MoveWorthy? other) => other.Value.CompareTo(Value);
    }

    public Move Think(Board board, Timer timer)
    {
      
        _iamWhite = board.IsWhiteToMove;
        _board = board;

        var best = ThinkDeeper(2);
        _lastMove = best;
        return best;
    }

    Move ThinkDeeper(int maxdeep)
    {
        // ich bin Weiß bzw.. meine Farbe
        var n = _board.GetLegalMoves();
        List<MoveWorthy> posibleMoves = new();
        foreach (var mymove in n)
        {
            _board.MakeMove(mymove);
            var wval = GetWorstValue();
            _move = mymove;
            wval += MoveQuallifier();

            posibleMoves.Add(new()
            {
                Move = mymove,
                Value = wval
                //+ (_board.IsInCheck() ? 1 : 0)

                //ThinkEvenDeeper(1, 2)
            });
            
            _board.UndoMove(mymove);
        }
        posibleMoves.Sort();

        List<MoveWorthy> Moves = new();
        var val = posibleMoves[0].Value;
        Moves.Add(posibleMoves[0]);
        for (int i = 1; i < posibleMoves.Count; i++)
            if (posibleMoves[i].Value == val)
                Moves.Add(posibleMoves[i]);

        return Moves[_random.Next(0,Moves.Count)].Move;
    }

    //double ThinkEvenDeeper(int deep, int maxdeep)
    //{
    //    // Gegenzug
    //    // nur den für mich schlechtesten Wert des Gegenzuges
    //    if (deep >= maxdeep)
    //        return GetWorstValue();

    //    double worst = 9999;
    //    foreach (var theremove in _board.GetLegalMoves()) // Gegen Zug
    //    {
    //        _board.MakeMove(theremove);
    //        foreach (var mymove in _board.GetLegalMoves()) // Mein Zug
    //        {
    //            _board.MakeMove(mymove);
    //            var value = ThinkEvenDeeper(deep+1,maxdeep);
    //            if (worst > value) worst = value;
    //            _board.UndoMove(mymove);
    //        }
    //        _board.UndoMove(theremove);
    //    }
    //    return worst;
    //}
    //double GetBestValue()
    //{
    //    var n = _board.GetLegalMoves();
    //    double bestvalue = -9999;
    //    foreach (var mymove in n)
    //    {
    //        _board.MakeMove(mymove);
    //        _move = mymove;
    //        var value = ff();
    //        if (value > bestvalue)
    //            bestvalue = value;
    //        _board.UndoMove(mymove);
    //    }
    //    return bestvalue;
    //}
    double GetWorstValue()
    {
        var n = _board.GetLegalMoves();
        if (n.Length == 0 && !_board.IsInCheck())
            return -9999;

        double worstvalue = 9999;
        foreach (var mymove in n)
        {
            _board.MakeMove(mymove);
            _move = mymove;
            var value = Evaluate();
            if (value < worstvalue)
                worstvalue = value;
            _board.UndoMove(mymove);
        }
        return worstvalue;
    }

    PieceType Pawn => PieceType.Pawn;
    PieceType King => PieceType.King;
    PieceType Knight => PieceType.Knight;

    PieceList[] AP => _board.GetAllPieceLists();
    bool Between(int a, int b, int c) => a >= b && a <= c;
    bool IsDoublePawn(Piece piece, PieceList pieces)
        => pieces.Any(x => x != piece && x.Square.File == piece.Square.File);

    bool IsCorner(Square square) => square.Rank == 1 || square.Rank == 8 || square.File == 0 || square.File == 8;

    double PieceListCount(PieceType pt, bool white) 
        => _board.GetPieceList(pt, white).Count - _board.GetPieceList(pt, !white).Count;

    double PawnValue(bool white)
    {
        var pawns = _board.GetPieceList(Pawn, white);

        double sum = 0;
        foreach (var p in pawns)
        {
            sum++;
            var isDoublePawn = IsDoublePawn(p, pawns);
            var bestRank = Between(p.Square.Rank, 2, 5);
            var bestFile = Between(p.Square.File, 3, 6);
            if (isDoublePawn) sum -= 0.3;
            if (bestRank && bestFile) sum += 0.2;
        }
        return sum;
        //return pawns.Select(p => (IsDoublePawn(p, pawns) ? -0.3 : 0) + (Between(p.Square.Rank, 2, 5) && Between(p.Square.File, 3, 6) ? 1.2 : 1)).Sum();
    }

    double KnightValue(bool white)
    {
        var pawns = _board.GetPieceList(Knight, white);
        return pawns.Select(p => (IsCorner(p.Square) ? -0.1 : 0) + 3).Sum();
    }
    double MoveQuallifier()
    {
        double hh = 0;
        if (_move.IsCastles)
            hh += 2;

        if (_board.IsInCheck())
            hh += 0.5;

        if (_board.IsDraw())
            hh -= 5;

        if (_move.MovePieceType == King)
            hh -= 1.5;

        if (_lastMove != null && _lastMove.Value.TargetSquare == _move.StartSquare)
            hh -= 1.5;
        return hh;
    }
    double Evaluate()
    {
        var pawnsvalue = PawnValue(_iamWhite) - PawnValue(!_iamWhite);
        var knightvalue = KnightValue(_iamWhite) - KnightValue(!_iamWhite);
        var hh = pawnsvalue + knightvalue
            + PieceListCount(PieceType.Bishop, _iamWhite) * 3
            + PieceListCount(PieceType.Rook, _iamWhite) * 5
            + PieceListCount(PieceType.Queen, _iamWhite) * 9;


        if (_iamWhite != _board.IsWhiteToMove)
        {
            if (_move.IsCastles)
                hh += 2;

            if (_board.IsInCheck())
                hh += 4;

            if (_board.IsDraw())
                hh -= 5;

            if (_move.MovePieceType == King)
                hh -= 2.5;

            if (_lastMove != null && _lastMove.Value.TargetSquare == _move.StartSquare)
                hh -= 0.5;
        }
        else
        {
            if (_board.IsInCheck())
                hh -= 5;
            if (_board.IsInCheckmate())
                hh = -9999;
        }
        return hh;
    }


}