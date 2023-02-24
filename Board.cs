public class Board
{
    public readonly CellState[,] State = new CellState[10, 10];
    public readonly int[] ColumnCounts = new int[8];
    public readonly int[] RowCounts = new int[8];

    public Board(Setup setup)
    {
        Array.Copy(setup.ColumnCounts, this.ColumnCounts, 8);
        Array.Copy(setup.RowCounts, this.RowCounts, 8);
        for (var x = 0; x < 10; x++)
        {
            this.State[0, x] = CellState.Wall;
            this.State[x, 9] = CellState.Wall;
            this.State[9, x] = CellState.Wall;
            this.State[x, 0] = CellState.Wall;
        }

        foreach (var setupMonster in setup.Monsters)
            this.State[setupMonster.Item1, setupMonster.Item2] = CellState.Monster;
        foreach (var setupTreasure in setup.Treasures)
            this.State[setupTreasure.Item1, setupTreasure.Item2] = CellState.Treasure;
    }

    public int CountNeighbors(int column, int row, CellState cellState)
    {
        var count = 0;
        if (this.State[column - 1, row] == cellState)
            count++;
        if (this.State[column + 1, row] == cellState)
            count++;
        if (this.State[column, row - 1] == cellState)
            count++;
        if (this.State[column, row + 1] == cellState)
            count++;
        return count;
    }

    public List<(int X, int Y)> GetNeighbors(int column, int row, CellState cellState)
    {
        List<(int X, int Y)> neighbors = new();
        if (this.State[column - 1, row] == cellState)
            neighbors.Add((column - 1, row));
        if (this.State[column + 1, row] == cellState)
            neighbors.Add((column + 1, row));
        if (this.State[column, row - 1] == cellState)
            neighbors.Add((column, row - 1));
        if (this.State[column, row + 1] == cellState)
            neighbors.Add((column, row + 1));
        return neighbors;
    }

    public CellState[] GetArea(int column, int row, int width, int height)
    {
        var cells = new CellState[width * height];
        for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                cells[x * height + y] = this.State[column + x, row + y];
        return cells;
    }

    public int CountColumn(int column, CellState cellState)
    {
        var count = 0;
        for (var j = 1; j <= 8; j++)
            if (this.State[column, j] == cellState)
                count++;
        return count;
    }

    public int CountRow(int row, CellState cellState)
    {
        var count = 0;
        for (var j = 1; j <= 8; j++)
            if (this.State[j, row] == cellState)
                count++;
        return count;
    }

    public void Draw()
    {
        Console.SetCursorPosition(0, 0);
        Console.Write("  ");
        for (var x = 0; x < 8; x++)
            Console.Write(this.ColumnCounts[x]);
        Console.WriteLine();
        for (var y = 0; y < 10; y++)
        {
            if (y is > 0 and < 9)
                Console.Write(this.RowCounts[y - 1]);
            else
                Console.Write(' ');
            for (var x = 0; x < 10; x++)
                Console.Write(this.State[x, y] switch
                {
                    CellState.Wall => '█',
                    CellState.Empty => '·',
                    CellState.EmptyRoom => 'x',
                    CellState.Monster => '♠',
                    CellState.Treasure => '♦',
                    _ => ' '
                });
            Console.WriteLine();
        }
    }

    public ulong GetWallsAsNeuralNetOutput()
    {
        var result = 0ul;

        for (var i = 1; i <= 8; i++)
            for (var j = 1; j <= 8; j++)
                result |= this.State[j, i] == CellState.Wall ? (1ul << (8 - i) * 8 + 8 - j) : 0;

        return result;
    }
}

