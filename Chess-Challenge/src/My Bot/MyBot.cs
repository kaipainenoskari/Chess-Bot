using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    bool isWhite;
    Move moveToPlay;
    int depth = 5;
    Board boardRef;
    Timer timerRef;
    int maxTime;
    public Move Think(Board board, Timer timer)
    {
        boardRef = board;
        timerRef = timer;
        isWhite = boardRef.IsWhiteToMove;
        // As there are less pieces search to higher depths
        int pieceCount = 0;
        Array.ForEach(boardRef.GetAllPieceLists(), list => pieceCount += list.Count);
        depth = pieceCount < 5 ? 7 : pieceCount < 10 ? 5 : 3;

        maxTime = timerRef.MillisecondsRemaining / 20;
        if (timerRef.MillisecondsRemaining < 5000)
        {
            maxTime = 500;
            depth = 3;
        }
        depth = 3;

        int evaluation = NegaMax(depth, isWhite ? 1 : -1);
        Console.WriteLine("Evaluation: " + evaluation);
        return moveToPlay;
    }
    private int NegaMax(int currentDepth, int color, int alpha = -600000, int beta = 600000)
    {
        if (timerRef.MillisecondsElapsedThisTurn > maxTime)
            return 500000;
        if (boardRef.IsInCheckmate() || boardRef.IsDraw())
            return Evaluate(color, currentDepth);

        // If depth is 0, perform quiescence search
        if (currentDepth == 0)
            return QuiescenceSearch(currentDepth, color, alpha, beta);

        Move[] moves = OrderMoves();
        foreach (Move move in moves)
        {
            boardRef.MakeMove(move);
            int score = -NegaMax(currentDepth - 1, -color, -beta, -alpha);
            boardRef.UndoMove(move);

            if (score >= beta)
                return beta;
            if (score > alpha)
            {
                alpha = score;
                if (currentDepth == depth)
                    moveToPlay = move;
            }
        }
        return alpha;
    }
    int QuiescenceSearch(int currentDepth, int color, int alpha, int beta)
    {
        int standPat = Evaluate(color, currentDepth);
        if (standPat >= beta)
            return beta;
        if (alpha < standPat)
            alpha = standPat;

        /*
        List<Move> moveList = OrderMoves(capturesOnly: true).ToList();
        Move[] allMoves = boardRef.GetLegalMoves();
        foreach (Move move in allMoves)
        {
            boardRef.MakeMove(move);
            if (boardRef.IsInCheck())
                moveList.Insert(0, move);
            boardRef.UndoMove(move);
        }
        Move[] moves = moveList.ToArray();
        */
        Move[] moves = OrderMoves(capturesOnly: true);
        foreach (Move move in moves)
        {
            boardRef.MakeMove(move);
            int score = -QuiescenceSearch(currentDepth - 1, -color, -beta, -alpha);
            boardRef.UndoMove(move);

            if (score >= beta)
                return beta;
            if (alpha < score)
                alpha = score;
        }
        return alpha;
    }
    private int Evaluate(int color, int currentDepth)
    {
        if (boardRef.IsInCheckmate())
            return -500000 - currentDepth; // Prioritize quicker checkmates
        if (boardRef.IsDraw())
            return 100; // Arbitrary value to discourage draws
        int score = 0;
        PieceList[] allPieceLists = boardRef.GetAllPieceLists();

        foreach (PieceList pieceList in allPieceLists)
        {
            int pieceValue = GetPieceValue(pieceList.TypeOfPieceInList);
            foreach(Piece piece in pieceList)
            {
                int pieceSquareValue = GetPieceSquareValue(piece) + pieceValue;
                score += (piece.IsWhite ? 1 : -1) * pieceSquareValue;
            }
        }
        int attackBonus = (boardRef.IsWhiteToMove ? 1 : -1) * boardRef.GetLegalMoves(capturesOnly: true).Length * 30;
        score += attackBonus;

        return score * color;
    }
    private static int GetPieceValue(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 350,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => 20000,
            _ => 0
        };
    }
    private bool IsEndGame()
    {
        int totalMaterial = 0;
        PieceList[] allPieces = boardRef.GetAllPieceLists();

        foreach (var pieceList in allPieces)
        {
            foreach (Piece piece in pieceList)
            {
                // Exclude kings and pawns from material count
                if (piece.PieceType != PieceType.King && piece.PieceType != PieceType.Pawn)
                    totalMaterial += GetPieceValue(piece.PieceType);
            }
        }
        // If total material (excluding kings and pawns) is below or equal to 1300 centipawns
        return totalMaterial <= 1300;
    }
    private int GetMoveScore(Move move)
    {
        int score = 0;

        // MVV-LVA: Most Valuable Victim - Least Valuable Attacker
        if (move.IsCapture)
        {
            int victimValue = GetPieceValue(move.CapturePieceType);
            int attackerValue = GetPieceValue(move.MovePieceType);
            score += 10 * (victimValue - attackerValue);
        }

        // Promotions
        if (move.IsPromotion)
        {
            int promotionValue = GetPieceValue(move.PromotionPieceType) - GetPieceValue(move.MovePieceType);
            score += promotionValue;
        }

        /*
        // Checks
        boardRef.MakeMove(move);
        if (boardRef.IsInCheck())
        {
            score += 5000; // Arbitrary high value to prioritize checks
            if (boardRef.IsInCheckmate())
            {
                score += 100000; // Arbitrary high value to prioritize checkmates
            }
        }
        boardRef.UndoMove(move);
        */

        return score;
    }
    private Move[] OrderMoves(bool capturesOnly = false)
    {
        Move[] moves = boardRef.GetLegalMoves(capturesOnly: capturesOnly);
        return moves.OrderByDescending(move => GetMoveScore(move)).ToArray();
    }
    private int GetPieceSquareValue(Piece piece)
    {
        int file = piece.Square.File;
        int rank = piece.Square.Rank;

        // Mirror the file if it's on the right half
        if (file > 3)
            file = 7 - file;

        // Invert rank for black pieces
        if (!piece.IsWhite)
            rank = 7 - rank;
        bool isEndGame = IsEndGame();

        return piece.PieceType switch
        {
            PieceType.Pawn => isEndGame ? HalfFilePawnEndgameTable[rank, file] : HalfFilePawnMiddleTable[rank, file],
            PieceType.Knight => HalfFileKnightTable[rank, file],
            PieceType.Bishop => HalfFileBishopTable[rank, file],
            PieceType.Rook => HalfFileRookTable[rank, file],
            PieceType.Queen => HalfFileQueenTable[rank, file],
            PieceType.King => IsEndGame() ? HalfFileKingEndgameTable[rank, file] : HalfFileKingMiddleTable[rank, file],
            _ => 0,
        };
    }
    private static readonly int[,] HalfFilePawnMiddleTable = new int[8, 4]
    {
        { 0,   0,   0,   0 },
        { 5,  10,  10, -20 },
        { 5,  -5, -10,   0 },
        { 0,   0,   0,  20 },
        { 5,   5,  10,  25 },
        { 10, 10,  20,  30 },
        { 50, 50,  50,  50 },
        { 0,   0,   0,   0 }
    };
    private static readonly int[,] HalfFilePawnEndgameTable = new int[8, 4]
    {
        { 0,   0,   0,   0   },
        { 15,  5,   5,  10   },
        { 5,   5,  -5,   0   },
        { 10,  10, -5,  -5   },
        { 30,  25,  10,  5   },
        { 90,  100, 85,  65  },
        { 180, 170, 160, 130 },
        { 0,   0,   0,   0   }
    };
    private static readonly int[,] HalfFileKnightTable = new int[8, 4]
    {
        { -50, -40, -30, -30 },
        { -40, -20,   0,   5 },
        { -30,   5,  10,  15 },
        { -30,   0,  15,  20 },
        { -30,   5,  15,  20 },
        { -30,   0,  10,  15 },
        { -40, -20,   0,   0 },
        { -50, -40, -30, -30 }
    };
    private static readonly int[,] HalfFileBishopTable = new int[8, 4]
    {
        { -20, -10, -10, -10 },
        { -10,   5,   0,   0 },
        { -10,  10,  10,  10 },
        { -10,   0,  10,  10 },
        { -10,   5,   5,  10 },
        { -10,   0,   5,  10 },
        { -10,   0,   0,   0 },
        { -20, -10, -10, -10 }
    };
    private static readonly int[,] HalfFileRookTable = new int[8, 4]
    {
        {   0,   0,   0,   5 },
        {  -5,   0,   0,   0 },
        {  -5,   0,   0,   0 },
        {  -5,   0,   0,   0 },
        {  -5,   0,   0,   0 },
        {  -5,   0,   0,   0 },
        {   5,  10,  10,  10 },
        {   0,   0,   0,   0 }
    };
    private static readonly int[,] HalfFileQueenTable = new int[8, 4]
    {
        { -20, -10, -10,  -5 },
        { -10,   0,   5,   0 },
        { -10,   5,   5,   5 },
        {  -5,   0,   5,   5 },
        {   0,   0,   5,   5 },
        { -10,   0,   5,   5 },
        { -10,   0,   0,   0 },
        { -20, -10, -10,  -5 }
    };
    private static readonly int[,] HalfFileKingMiddleTable = new int[8, 4]
    {
        {  60,  80,  50,   0 },
        {  20,  20,   0,   0 },
        { -10, -20, -20, -20 },
        { -20, -30, -30, -40 },
        { -30, -40, -40, -50 },
        { -30, -40, -40, -50 },
        { -30, -40, -40, -50 },
        { -30, -40, -40, -50 },
    };
    private static readonly int[,] HalfFileKingEndgameTable = new int[8, 4]
    {
        { -50, -40, -30, -20 },
        { -30, -20, -10,   0 },
        { -30, -10,  20,  30 },
        { -30, -10,  30,  40 },
        { -30, -10,  30,  40 },
        { -30, -10,  20,  30 },
        { -30, -30,   0,   0 },
        { -50, -30, -30, -30 }
    };
}
