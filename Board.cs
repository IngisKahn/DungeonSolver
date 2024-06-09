namespace DungeonSolver;

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

    private Board(Board copy)
    {
        Array.Copy(copy.ColumnCounts, this.ColumnCounts, 8);
        Array.Copy(copy.RowCounts, this.RowCounts, 8);
        Array.Copy(copy.State, this.State, 100);
    }

    public Board Clone() => new(this);

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

    public SolutionState SolutionState
    {
        get
        {
            var result = SolutionState.Solved;

            for (var i = 1; i <= 8; i++)
            {
                var deltaColumns = this.CountColumn(i, CellState.Wall) - this.ColumnCounts[i - 1];
                if (deltaColumns > 0)
                    return SolutionState.Invalid;
                if (deltaColumns != 0)
                    result = SolutionState.None;
                var deltaRows = this.CountRow(i, CellState.Wall) - this.RowCounts[i - 1];
                if (deltaRows > 0)
                    return SolutionState.Invalid;
                if (deltaRows != 0)
                    result = SolutionState.None;

                for (var j = 1; j <= 8; j++)
                {
                    switch (this.State[i, j])
                    {
                        case CellState.Empty:
                            if (this.GetNeighbors(i, j, CellState.Wall).Count > 2)
                                return SolutionState.Invalid;
                            break;
                        case CellState.Monster:
                            if (this.GetNeighbors(i, j, CellState.Empty).Count > 1)
                                return SolutionState.Invalid;
                            break;
                        case CellState.Treasure:
                            if (this.CanBeRoom(i, j).Count < 1)
                                return SolutionState.Invalid;
                            break;
                    }
                    if (i < 8 && j < 8 && this.GetArea(i, j, 2, 2).All(c => c == CellState.Empty) && this.GetArea(i - 1, j - 1, 4, 4).All(c => c != CellState.Treasure))
                        return SolutionState.Invalid;
                }
            }

            return result;
        }
    }      

    public List<(int X, int Y, bool HasExit)> CanBeRoom(int i, int j)
    {
        List<(int X, int Y, bool HasExit)> canBeRoom = new();
        for (var k = 0; k < 9; k++)
        {
            var xp = i - Math.DivRem(k, 3, out var yp);
            yp = j - yp;
            var noGood = false;
            for (var l = xp; l < xp + 3 && !noGood; l++)
            for (var m = yp; m < yp + 3 && !noGood; m++)
            {
                if (l == i && m == j)
                    continue;
                switch (this.State[l, m])
                {
                    case CellState.Unknown:
                    case CellState.Empty:
                    case CellState.EmptyRoom:
                        break;
                    default:
                        noGood = true;
                        break;
                }
            }
            if (noGood)
                continue;
            // ok, room is clear, make sure no monsters or treasure are touching
            // and that only one exit is possible
            static (int, int, int, int)? CountBorder(CellState[] cellState)
            {
                var canBeWalls = 0;
                var canBeExits = 0;
                var exits = 0;
                var walls = 0;
                foreach (var cell in cellState)
                    switch (cell)
                    {
                        case CellState.Unknown:
                            canBeExits++;
                            canBeWalls++;
                            break;
                        case CellState.Monster:
                        case CellState.Treasure:
                            return null;
                        case CellState.Wall:
                            canBeWalls++;
                            walls++;
                            break;
                        case CellState.Empty:
                            canBeExits++;
                            exits++;
                            break;
                    }
                return (canBeWalls, canBeExits, exits, walls);
            }

            var topWall = CountBorder(this.GetArea(xp, yp - 1, 3, 1));
            if (topWall == null)
                continue;
            var bottomWall = CountBorder(this.GetArea(xp, yp + 3, 3, 1));
            if (bottomWall == null)
                continue;
            var leftWall = CountBorder(this.GetArea(xp - 1, yp, 1, 3));
            if (leftWall == null)
                continue;
            var rightWall = CountBorder(this.GetArea(xp + 3, yp, 1, 3));
            if (rightWall == null)
                continue;

            var canBeWalls = topWall.Value.Item1 + bottomWall.Value.Item1 + leftWall.Value.Item1 + rightWall.Value.Item1;
            var canBeExits = topWall.Value.Item2 + bottomWall.Value.Item2 + leftWall.Value.Item2 + rightWall.Value.Item2;
            var exits = topWall.Value.Item3 + bottomWall.Value.Item3 + leftWall.Value.Item3 + rightWall.Value.Item3;


            if (exits > 1 || canBeExits < 1 || canBeWalls < 11)
                continue;

            if (yp > 1 && this.RowCounts[yp - 2] - this.CountRow(yp - 1, CellState.Wall) < ((topWall.Value.Item3 == 0 ? 2 : 3) - topWall.Value.Item4))
                continue;
            if (yp < 6 && this.RowCounts[yp + 2] - this.CountRow(yp + 3, CellState.Wall) < ((topWall.Value.Item3 == 0 ? 2 : 3) - bottomWall.Value.Item4))
                continue;
            if (xp > 1 && this.ColumnCounts[xp - 2] - this.CountColumn(xp - 1, CellState.Wall) < ((topWall.Value.Item3 == 0 ? 2 : 3) - leftWall.Value.Item4))
                continue;
            if (xp < 6 && this.ColumnCounts[xp + 2] - this.CountColumn(xp + 3, CellState.Wall) < ((topWall.Value.Item3 == 0 ? 2 : 3) - rightWall.Value.Item4))
                continue;

            // add checks for monsters in dead ends 2 away
            canBeRoom.Add((xp, yp, exits == 1));
        }
        return canBeRoom;
    }
}

public enum SolutionState
{
    None,
    Invalid,
    Solved
}