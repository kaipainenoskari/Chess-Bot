using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    List<int[,]> middlegamePieceSquareTable;
    List<int[,]> endgamePieceSquareTable;
    int[] middlegamePieceValues = { 0, 82, 337, 365, 477, 1025, 20000 };
    int[] endgamePieceValues = { 0, 94, 281, 297, 512, 936, 20000 };
    bool isWhite;
    Move moveToPlay;
    int depth;
    int R = 2;
    int LMR_THRESHOLD = 4;
    int REDUCTION_LIMIT = 3;
    int LMR_REDUCTION = 1;
    Board boardRef;
    Timer timerRef;
    int maxTime;
    int evaluateVisits = 0;

    public MyBot()
    {
        middlegamePieceSquareTable = new List<int[,]>(){new int[1,1]{ { 0 } }, //Blank
        new int[8, 8] //Middlegame Pawn
        {
            {   0,   0,   0,   0,   0,   0,  0,   0 },
            {  98, 134,  61,  95,  68, 126, 34, -11 },
            {  -6,   7,  26,  31,  65,  56, 25, -20 },
            { -14,  13,   6,  21,  23,  12, 17, -23 },
            { -27,  -2,  -5,  12,  17,   6, 10, -25 },
            { -26,  -4,  -4, -10,   3,   3, 33, -12 },
            { -35,  -1, -20, -23, -15,  24, 38, -22 },
            {   0,   0,   0,   0,   0,   0,  0,   0 }
        },
        new int[8, 8] //Middlegame Knight
        {
            { -167, -89, -34, -49,  61, -97, -15, -107 },
            {  -73, -41,  72,  36,  23,  62,   7,  -17 },
            {  -47,  60,  37,  65,  84, 129,  73,   44 },
            {   -9,  17,  19,  53,  37,  69,  18,   22 },
            {  -13,   4,  16,  13,  28,  19,  21,   -8 },
            {  -23,  -9,  12,  10,  19,  17,  25,  -16 },
            {  -29, -53, -12,  -3,  -1,  18, -14,  -19 },
            { -105, -21, -58, -33, -17, -28, -19,  -23 }
        },
        new int[8, 8] //Middlegame Bishop
        {
            { -29,   4, -82, -37, -25, -42,   7,  -8 },
            { -26,  16, -18, -13,  30,  59,  18, -47 },
            { -16,  37,  43,  40,  35,  50,  37,  -2 },
            {  -4,   5,  19,  50,  37,  37,   7,  -2 },
            {  -6,  13,  13,  26,  34,  12,  10,   4 },
            {   0,  15,  15,  15,  14,  27,  18,  10 },
            {   4,  15,  16,   0,   7,  21,  33,   1 },
            { -33,  -3, -14, -21, -13, -12, -39, -21 }
        },
        new int[8, 8] //Middlegame Rook
        {
            {  32,  42,  32,  51,  63,   9,  31,  43 },
            {  27,  32,  58,  62,  80,  67,  26,  44 },
            {  -5,  19,  26,  36,  17,  45,  61,  16 },
            { -24, -11,   7,  26,  24,  35,  -8, -20 },
            { -36, -26, -12,  -1,   9,  -7,   6, -23 },
            { -45, -25, -16, -17,   3,   0,  -5, -33 },
            { -44, -16, -20,  -9,  -1,  11,  -6, -71 },
            { -19, -13,   1,  17,  16,   7, -37, -26 }
        },
        new int[8, 8] //Middlegame Queen
        {
            { -28,   0,  29,  12,  59,  44,  43,  45 },
            { -24, -39,  -5,   1, -16,  57,  28,  54 },
            { -13, -17,   7,   8,  29,  56,  47,  57 },
            { -27, -27, -16, -16,  -1,  17,  -2,   1 },
            {  -9, -26,  -9, -10,  -2,  -4,   3,  -3 },
            { -14,   2, -11,  -2,  -5,   2,  14,   5 },
            { -35,  -8,  11,   2,   8,  15,  -3,   1 },
            {  -1, -18,  -9,  10, -15, -25, -31, -50 }
        },
        new int[8, 8] //Middlegame King
        {
            { -65,  23,  16, -15, -56, -34,   2,  13 },
            {  29,  -1, -20,  -7,  -8,  -4, -38, -29 },
            {  -9,  24,   2, -16, -20,   6,  22, -22 },
            { -17, -20, -12, -27, -30, -25, -14, -36 },
            { -49,  -1, -27, -39, -46, -44, -33, -51 },
            { -14, -14, -22, -46, -44, -30, -15, -27 },
            {   1,   7,  -8, -64, -43, -16,   9,   8 },
            { -15,  36,  12, -54,   8, -28,  24,  14 }
        }};
        endgamePieceSquareTable = new List<int[,]>(){new int[1,1]{ { 0 } }, //Blank
        new int[8, 8] //Endgame Pawn
        {
            {   0,   0,   0,   0,   0,   0,   0,   0 },
            { 178, 173, 158, 134, 147, 132, 165, 187 },
            {  94, 100,  85,  67,  56,  53,  82,  84 },
            {  32,  24,  13,   5,  -2,   4,  17,  17 },
            {  13,   9,  -3,  -7,  -7,  -8,   3,  -1 },
            {   4,   7,  -6,   1,   0,  -5,  -1,  -8 },
            {  13,   8,   8,  10,  13,   0,   2,  -7 },
            {   0,   0,   0,   0,   0,   0,   0,   0 }
        },
        new int[8, 8] //Endgame Knight
        {
            { -58, -38, -13, -28, -31, -27, -63, -99 },
            { -25,  -8, -25,  -2,  -9, -25, -24, -52 },
            { -24, -20,  10,   9,  -1,  -9, -19, -41 },
            { -17,   3,  22,  22,  22,  11,   8, -18 },
            { -18,  -6,  16,  25,  16,  17,   4, -18 },
            { -23,  -3,  -1,  15,  10,  -3, -20, -22 },
            { -42, -20, -10,  -5,  -2, -20, -23, -44 },
            { -29, -51, -23, -15, -22, -18, -50, -64 }
        },
        new int[8, 8] //Endgame Bishop
        {
            { -14, -21, -11,  -8,  -7,  -9, -17, -24 },
            {  -8,  -4,   7, -12,  -3, -13,  -4, -14 },
            {   2,  -8,   0,  -1,  -2,   6,   0,   4 },
            {  -3,   9,  12,   9,  14,  10,   3,   2 },
            {  -6,   3,  13,  19,   7,  10,  -3,  -9 },
            { -12,  -3,   8,  10,  13,   3,  -7, -15 },
            { -14, -18,  -7,  -1,   4,  -9, -15, -27 },
            { -23,  -9, -23,  -5,  -9, -16,  -5, -17 }
        },
        new int[8, 8] //Endgame Rook
        {
            {  13,  10,  18,  15,  12,  12,   8,   5 },
            {  11,  13,  13,  11,  -3,   3,   8,   3 },
            {   7,   7,   7,   5,   4,  -3,  -5,  -3 },
            {   4,   3,  13,   1,   2,   1,  -1,   2 },
            {   3,   5,   8,   4,  -5,  -6,  -8, -11 },
            {  -4,   0,  -5,  -1,  -7, -12,  -8, -16 },
            {  -6,  -6,   0,   2,  -9,  -9, -11,  -3 },
            {  -9,   2,   3,  -1,  -5, -13,   4, -20 }
        },
        new int[8, 8] //Endgame Queen
        {
            {  -9,  22,  22,  27,  27,  19,  10,  20 },
            { -17,  20,  32,  41,  58,  25,  30,   0 },
            { -20,   6,   9,  49,  47,  35,  19,   9 },
            {   3,  22,  24,  45,  57,  40,  57,  36 },
            { -18,  28,  19,  47,  31,  34,  39,  23 },
            { -16, -27,  15,   6,   9,  17,  10,   5 },
            { -22, -23, -30, -16, -16, -23, -36, -32 },
            { -33, -28, -22, -43,  -5, -32, -20, -41 }
        },
        new int[8, 8] //Endgame King
        {
            { -74, -35, -18, -18, -11,  15,   4, -17 },
            { -12,  17,  14,  17,  17,  38,  23,  11 },
            {  10,  17,  23,  15,  20,  45,  44,  13 },
            {  -8,  22,  24,  27,  26,  33,  26,   3 },
            { -18,  -4,  21,  24,  27,  23,   9, -11 },
            { -19,  -3,  11,  21,  23,  16,   7,  -9 },
            { -27, -11,   4,  13,  14,   4,  -5, -17 },
            { -53, -34, -21, -11, -28, -14, -24, -43 }
        }
        };
    }
    public Move Think(Board board, Timer timer)
    {
        boardRef = board;
        timerRef = timer;
        isWhite = boardRef.IsWhiteToMove;

        // As there are less pieces search to higher depths
        int pieceCount = 0;
        Array.ForEach(boardRef.GetAllPieceLists(), list => pieceCount += list.Count);
        depth = pieceCount < 5 ? 8 : pieceCount < 10 ? 7 : 6;

        maxTime = timerRef.MillisecondsRemaining / 10;
        maxTime = timerRef.MillisecondsRemaining;
        if (timerRef.MillisecondsRemaining < 20000)
        {
            depth = 5;
        }
        if (timerRef.MillisecondsRemaining < 8000)
        {
            maxTime = 500;
            depth = 4;
        }

        int evaluation = NegaMax(depth, isWhite ? 1 : -1, -600000, 600000);

        Console.WriteLine($"Move #{boardRef.PlyCount / 2 + 1} MyBot   evaluated {evaluateVisits} positions");
        Console.WriteLine("Evaluation: " + evaluation);
        if (!boardRef.IsWhiteToMove)
            Console.WriteLine();
        //Move[] moves = OrderMoves();
        //Console.WriteLine("Moves: " + string.Join(", ", moves.Select(move => move.ToString())));
        return moveToPlay;
    }
    int NegaMax(int currentDepth, int color, int alpha, int beta)
    {
        if (timerRef.MillisecondsElapsedThisTurn > maxTime)
            return 500000;
        if (boardRef.IsInCheckmate() || boardRef.IsDraw())
            return Evaluate(color, currentDepth);
        if (currentDepth == 0)// If depth is 0, perform quiescence search
            return QuiescenceSearch(currentDepth, color, alpha, beta);

        if (currentDepth > R && boardRef.TrySkipTurn())
        {
            int nullMoveScore = -NegaMax(currentDepth - R - 1, -color, -beta, -beta + 1);
            boardRef.UndoSkipTurn();
            if (nullMoveScore >= beta)
                return beta;
        }

        Move[] moves = OrderMoves(false);
 
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];

            boardRef.MakeMove(move);
            int score;
            if (i == 0)
                score = -NegaMax(currentDepth - 1, -color, -beta, -alpha);
            else
            {
                bool okToReduce = !move.IsCapture && !move.IsPromotion && !boardRef.IsInCheck();
                if (i >= LMR_THRESHOLD && currentDepth >= REDUCTION_LIMIT && okToReduce)
                    score = -NegaMax(currentDepth - 1 - LMR_REDUCTION, -color, -alpha - 1, -alpha);
                else score = alpha + 1;

                if (score > alpha)
                {
                    score = -NegaMax(currentDepth - 1, -color, -alpha - 1, -alpha);
                    if (score > alpha && score < beta)
                        score = -NegaMax(currentDepth - 1, -color, -beta, -alpha);
                }
            }
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
        Move[] moves = OrderMoves(true);
        foreach (Move move in moves)
        {
            boardRef.MakeMove(move);
            standPat = -QuiescenceSearch(currentDepth - 1, -color, -beta, -alpha);
            boardRef.UndoMove(move);

            if (standPat >= beta)
                return beta;
            if (alpha < standPat)
                alpha = standPat;
        }

        return alpha;
    }
    int Evaluate(int color, int currentDepth)
    {
        evaluateVisits++;

        if (boardRef.IsInCheckmate())
            return -500000 - currentDepth; // Prioritize quicker checkmates
        if (boardRef.IsDraw())
            return 0;

        int score = 0;
        PieceList[] allPieceLists = boardRef.GetAllPieceLists();

        foreach (PieceList pieceList in allPieceLists)
        {
            int pieceValue;
            if (IsEndGame())
                pieceValue = endgamePieceValues[(int)pieceList.TypeOfPieceInList];
            else pieceValue = middlegamePieceValues[(int)pieceList.TypeOfPieceInList];
            foreach(Piece piece in pieceList)
            {
                int value = GetPieceSquareValue(piece.PieceType, piece.IsWhite, piece.Square) + pieceValue;
                if (!piece.IsWhite)
                    value *= -1;
                score += value;
            }
        }
        int mobilityBonus = boardRef.GetLegalMoves().Length;
        score += mobilityBonus;

        //Prioritize attacking
        //int attackBonus = (boardRef.IsWhiteToMove ? 1 : -1) * boardRef.GetLegalMoves(capturesOnly: true).Length * 30;
        //score += attackBonus;

        return score * color;
    }
    bool IsEndGame()
    {
        bool[] sides = { true, false };
        var GetPieces = boardRef.GetPieceList;
        foreach (bool side in sides)
        {
            int queenCount = GetPieces(PieceType.Queen, side).Count;
            int minorPieceCount = GetPieces(PieceType.Rook, side).Count + GetPieces(PieceType.Bishop, side).Count + GetPieces(PieceType.Knight, side).Count;
            if ((queenCount == 0 && minorPieceCount > 3) || (queenCount == 1 && minorPieceCount > 1))
                return false;
        }
        return true;
    }
    int GetMoveScore(Move move)
    {
        int score = 0;
        bool isEndGame = IsEndGame();
        int movePieceType = (int)move.MovePieceType;

        // MVV-LVA: Most Valuable Victim - Least Valuable Attacker
        if (move.IsCapture)
        {
            int capturePieceType = (int)move.CapturePieceType;

            int victimValue = isEndGame ? endgamePieceValues[capturePieceType] : middlegamePieceValues[capturePieceType];
            int attackerValue = isEndGame ? endgamePieceValues[movePieceType] : middlegamePieceValues[movePieceType];
            score = 10 * victimValue - attackerValue;
        }
        // Promotions
        if (move.IsPromotion)
        {
            int promotionPieceType = (int)move.PromotionPieceType;

            int promotionPieceValue = isEndGame ? endgamePieceValues[promotionPieceType] : middlegamePieceValues[promotionPieceType];
            int movedPieceValue = isEndGame ? endgamePieceValues[movePieceType] : middlegamePieceValues[movePieceType];
            score += promotionPieceValue - movedPieceValue;
        }
        // Piece square tables, prioritize moving to better squares
        score += GetPieceSquareValue(move.MovePieceType, boardRef.IsWhiteToMove, move.TargetSquare);
        score -= GetPieceSquareValue(move.MovePieceType, boardRef.IsWhiteToMove, move.StartSquare);

        // Checks
        boardRef.MakeMove(move);
        if (boardRef.IsInCheck())
        {
            score += 5000; // Arbitrary high value to prioritize checks
            if (boardRef.IsInCheckmate())
            {
                score = 500000; // Arbitrary high value to prioritize checkmates
            }
        }
        boardRef.UndoMove(move);

        return score;
    }
    Move[] OrderMoves(bool capturesOnly)
    {
        Move[] moves = boardRef.GetLegalMoves(capturesOnly);
        int[] scores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
            scores[i] = GetMoveScore(moves[i]);
        
        //Reorder list of moves based on the guess scores
        for (int i = 0; i < moves.Length; i++)
            for (int j = i + 1; j < moves.Length; j++)
                if (scores[i] < scores[j])
                {
                    (scores[j], scores[i]) = (scores[i], scores[j]);
                    (moves[j], moves[i]) = (moves[i], moves[j]);
                }

        return moves;
    }
    int GetPieceSquareValue(PieceType pieceType, bool isWhite, Square square)
    {
        int file = square.File;
        int rank = square.Rank;

        // Invert rank for white pieces and file for black pieces
        if (isWhite)
            rank = 7 - rank;
        else file = 7 - file;

        if (IsEndGame())
            return endgamePieceSquareTable[(int)pieceType][rank, file];
        else return middlegamePieceSquareTable[(int)pieceType][rank, file];
    }
}
