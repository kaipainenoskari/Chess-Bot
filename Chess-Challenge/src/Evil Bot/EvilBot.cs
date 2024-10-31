using ChessChallenge.API;
using System;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        private int GetPieceValue(PieceType pieceType)
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

        private int GetMoveScore(Move move, Board board)
        {
            int score = 0;

            // MVV-LVA: Most Valuable Victim - Least Valuable Attacker
            if (move.IsCapture)
            {
                int victimValue = GetPieceValue(move.CapturePieceType);
                int attackerValue = GetPieceValue(move.MovePieceType);
                score += (victimValue * 10) - attackerValue;
            }

            // Promotions
            if (move.IsPromotion)
            {
                int promotionValue = GetPieceValue(move.PromotionPieceType);
                score += promotionValue * 10;
            }

            // Checks
            board.MakeMove(move);
            if (board.IsInCheck())
            {
                score += 10000; // Arbitrary high value to prioritize checks
            }
            board.UndoMove(move);

            return score;
        }

        private Move[] OrderMoves(Move[] moves, Board board)
        {
            //return moves;
            return moves.OrderByDescending(move => GetMoveScore(move, board)).ToArray();
        }

        private static int GetScore(Board board)
        {
            if (board.IsInCheckmate())
            {
                return board.IsWhiteToMove ? int.MinValue : int.MaxValue;
            }
            if (board.IsInStalemate())
            {
                return 0;
            }
            int score = 0;
            PieceList[] allPieces = board.GetAllPieceLists();

            for (int i = 0; i < allPieces.Length; i++)
            {
                int multiplier = (i < 6) ? 1 : -1;
                foreach(Piece piece in allPieces[i])
                {
                    score += multiplier * piece.PieceType switch
                    {
                        PieceType.Pawn => 100,
                        PieceType.Knight => 320,
                        PieceType.Bishop => 330,
                        PieceType.Rook => 500,
                        PieceType.Queen => 900,
                        PieceType.King => 20000,
                        _ => 0
                    };
                }
            }
            return score;
        }

        private (Move, int) AlphaBeta(Board board, int depth, bool isMaximizingPlayer, int alpha = int.MinValue, int beta = int.MaxValue)
        {
            // Base case: if depth is 0, evaluate and return the score of the current board
            if (depth == 0)
            {
                return (board.GetLegalMoves().FirstOrDefault(), GetScore(board));
            }

            Move[] moves = OrderMoves(board.GetLegalMoves(), board);

            int bestScore = isMaximizingPlayer ? int.MinValue : int.MaxValue;
            Move bestMove = moves.FirstOrDefault();

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score;

                if (board.IsInCheckmate())
                {
                    score = isMaximizingPlayer ? int.MaxValue : int.MinValue;
                }
                else if (board.IsInStalemate())
                {
                    score = 0;
                }
                else
                {
                    // Recursively call MinMax with alpha-beta pruning, decreasing depth
                    score = AlphaBeta(board, depth - 1, !isMaximizingPlayer, alpha, beta).Item2;
                }

                // Update the best score and move based on maximizing or minimizing player
                if (isMaximizingPlayer)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    alpha = Math.Max(alpha, bestScore);

                    // Alpha-beta pruning: cut off exploration if the score is already worse than beta
                    if (beta <= alpha)
                    {
                        board.UndoMove(move);
                        break; // Prune the remaining branches
                    }
                }
                else
                {
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    beta = Math.Min(beta, bestScore);

                    // Alpha-beta pruning: cut off exploration if the score is already worse than alpha
                    if (beta <= alpha)
                    {
                        board.UndoMove(move);
                        break; // Prune the remaining branches
                    }
                }

                board.UndoMove(move);
            }

            return (bestMove, bestScore);
        }

        public Move Think(Board board, Timer timer)
        {
            int maxDepth = 5;
            double timeLimit = timer.MillisecondsRemaining * 0.1 / 20.0;
            Move bestMove = board.GetLegalMoves().FirstOrDefault();
            int i = 1;
            DateTime startTime = DateTime.Now;
            while (i <= maxDepth)
            {
                (Move move, int score) = AlphaBeta(board, i, board.IsWhiteToMove);
                bestMove = move;
                double elapsedTime = (DateTime.Now - startTime).TotalMilliseconds / 1000.0;
                //Console.WriteLine($"Depth: {i}, Time: {Math.Round(elapsedTime, 2, MidpointRounding.AwayFromZero)}s");
                if (elapsedTime > timeLimit)
                {
                    break;
                }
                i++;
            }
            return bestMove;
        }
    }
}
