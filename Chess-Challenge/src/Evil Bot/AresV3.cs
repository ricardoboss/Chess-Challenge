using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public class AresV3 : IChessBot
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

        //public int CompareTo(MoveWorthy? other) => Value.CompareTo(other.Value);

        public int CompareTo(MoveWorthy? other) => other.Value.CompareTo(Value);

        public override string ToString()
        {
            return $"{Move}: {Value}";
        }
    }

    public Move Think(Board board, Timer timer)
    {

        _iamWhite = board.IsWhiteToMove;
        _board = board;

        var best = ThinkDeeper2(2);
        _lastMove = best;
        return best;
    }

    Move ThinkDeeper(int maxdeep)
    {
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

        return Moves[_random.Next(0, Moves.Count)].Move;
    }

    Move ThinkDeeper2(int maxdeep)
    {
        List<MoveWorthy> posibleMoves = new();
        foreach (var mymove in _board.GetLegalMoves())
        {
            _board.MakeMove(mymove);

            var nf = new MoveWorthy()
            {
                Move = mymove,
                Value = 9999
            };

            foreach (var theremove in _board.GetLegalMoves())
            {
                _board.MakeMove(theremove);
                var val2 = GetBestValue();
                if (nf.Value > val2) nf.Value = val2;

                _board.UndoMove(theremove);
            }
             
            _move = mymove;
            nf.Value += MoveQuallifier();

            posibleMoves.Add(nf);

            _board.UndoMove(mymove);
        }
        posibleMoves.Sort();

        List<MoveWorthy> Moves = new();
        var val = posibleMoves[0].Value;
        Moves.Add(posibleMoves[0]);
        for (int i = 1; i < posibleMoves.Count; i++)
            if (posibleMoves[i].Value == val)
                Moves.Add(posibleMoves[i]);

        return Moves[_random.Next(0, Moves.Count)].Move;
    }


    double GetBestValue()
    {
        var n = _board.GetLegalMoves();
        if (n.Length == 0 && !_board.IsInCheck())
            return 9999;

        double worstvalue = -9999;
        foreach (var mymove in n)
        {
            _board.MakeMove(mymove);
            _move = mymove;
            var value = Evaluate();
            if (value > worstvalue)
                worstvalue = value;
            _board.UndoMove(mymove);
        }
        return worstvalue;
    }
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
            var bestRank = Between(p.Square.Rank, 3, 4);
            var bestFile = Between(p.Square.File, 3, 4);
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
            hh += 1.5;

        if (_board.IsDraw())
            hh -= 5;

        if (_board.PlyCount < 10 && _move.MovePieceType == PieceType.Queen)
            hh -= 0.5;

        if (_move.MovePieceType == King)
            hh -= 1.5;

        if (_lastMove != null && _lastMove.Value.TargetSquare == _move.StartSquare)
            if (_board.FiftyMoveCounter > 10)
                hh -= 5.5;
            else
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