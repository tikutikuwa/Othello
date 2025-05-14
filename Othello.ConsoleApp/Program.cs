using System;
using System.Collections.Generic;
using System.Linq;
using Othello.Core.Game;

namespace Othello.ConsoleApp
{
    /// <summary>
    /// GameState をラップして対話式に動かすコンソールアプリ
    /// </summary>
    internal static class Program
    {
        static void Main()
        {
            var game = new GameState();
            Console.WriteLine("=== オセロ CLI ===\nq で終了\n");

            while (true)
            {
                PrintBoard(game);

                if (game.IsFinished(out var winner))
                {
                    Console.WriteLine($"\nゲーム終了！ 勝者: {(winner?.ToString() ?? "引き分け")}\n");
                    break;
                }

                Console.Write($"{game.Turn} の手番 > ");
                var input = Console.ReadLine()?.Trim().ToLower();
                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input == "q") break;

                if (!TryParse(input, out var point))
                {
                    Console.WriteLine("  [!] 入力形式: a〜h + 1〜8  (例: d3)\n");
                    continue;
                }

                if (!game.TryMove(point))
                {
                    Console.WriteLine("  [!] 非合法な手です\n");
                }
            }
        }

        static bool TryParse(string s, out Point p)
        {
            p = default;
            if (s.Length != 2) return false;

            char file = s[0]; // a〜h
            char rank = s[1]; // 1〜8

            int col = file - 'a';
            int row = rank - '1';
            p = new Point(row, col);
            return p.IsValid;
        }

        static void PrintBoard(GameState g)
        {
            var snap = g.Board.Snapshot;
            var legalMoves = new HashSet<Point>(g.GetLegalMoves());

            Console.WriteLine("  a b c d e f g h");
            for (int r = 0; r < 8; r++)
            {
                Console.Write($"{r + 1} ");
                for (int c = 0; c < 8; c++)
                {
                    var point = new Point(r, c);
                    var s = snap[r][c];
                    char ch = s switch
                    {
                        Stone.Black => '●',
                        Stone.White => '○',
                        _ => legalMoves.Contains(point) ? '+' : '-'
                    };
                    Console.Write(ch + " ");
                }
                Console.WriteLine();
            }

            var (b, w) = g.Board.Snapshot.SelectMany(row => row)
                .Aggregate((black: 0, white: 0), (acc, s) =>
                {
                    if (s == Stone.Black) acc.black++;
                    if (s == Stone.White) acc.white++;
                    return acc;
                });
            Console.WriteLine($"●: {b}   ○: {w}\n");
        }
    }
}