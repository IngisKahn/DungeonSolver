﻿namespace DungeonSolver;

public record Setup(int[] ColumnCounts, int[] RowCounts, (int, int)[] Monsters, (int, int)[] Treasures)
{
    public float[] ConvertToNeuralNetInput()
    {
        var input = new float[256];
        Array.Fill(input, -1f);
        for (var i = 0; i < 8; i++)
        {
            for (var j = 0; j < ColumnCounts[i]; j++)
                input[8 * i + j] = 1;
            for (var j = 0; j < RowCounts[i]; j++)
                input[8 * i + 64 + j] = 1;
        }
        foreach (var m in Monsters)
            input[(m.Item1 - 1) * 8 + m.Item2 + 127] = 1;
        foreach (var t in Treasures)
            input[(t.Item1 - 1) * 8 + t.Item2 + 191] = 1;
        return input;
    }

    public Setup Mirror() =>
        new(ColumnCounts.Reverse().ToArray(),
            RowCounts,
            Monsters.Select(m => m with { Item1 = 9 - m.Item1 }).ToArray(),
            Treasures.Select(t => t with { Item1 = 9 - t.Item1 }).ToArray());

    public Setup RotateRight() =>
        new(RowCounts.Reverse().ToArray(),
            ColumnCounts,
            Monsters.Select(m => (9 - m.Item2, m.Item1)).ToArray(),
            Treasures.Select(t => (9 - t.Item2, t.Item1)).ToArray());
}