using DungeonSolver;

Setup[] setups =
    {
        new(new[] { 1, 4, 2, 7, 0, 4, 4, 4 }, new[] { 3, 2, 5, 3, 4, 1, 4, 4 }, new[] { (8, 2), (3, 3), (8, 4), (8, 6), (8, 8) }, new[] { (2, 6) }),
        new(new[] { 6, 2, 4, 1, 5, 4, 4, 5 }, new[] { 4, 4, 4, 4, 3, 4, 2, 6 }, new[] { (7, 1), (2, 5), (8, 6), (1, 8), (5, 8) }, new[] { (2, 3) }),
        new(new[] { 4, 2, 5, 0, 6, 2, 4, 2 }, new[] { 5, 2, 2, 1, 5, 3, 2, 5 }, new[] { (3, 3), (1, 7), (4, 8), (6, 8), (8, 8) }, new[] { (7, 3) }),
        new(new[] { 6, 2, 4, 3, 4, 4, 2, 6 }, new[] { 6, 2, 5, 3, 2, 5, 2, 6 }, new[] { (3, 1), (5, 1), (8, 3), (1, 4), (8, 5), (1, 6), (4, 8), (6, 8) }, new (int, int)[0])
    };

//SolvePuzzle(setups[2], true);

DeepNet net = new(256, new[] { 238 }, 64);
var iterations = 0;
var lastBadIteration = 0;
for (var wrong = 999; wrong > 0;)
{
    wrong = 0;
    foreach (var setup in setups.Take(3))
    {
        var result = ProcessSetup(net, setup, false);
        //Console.WriteLine(result);
        iterations += 8;
        if (result > 0)
        {
            lastBadIteration = iterations;
            wrong += result;
        }
        if (lastBadIteration < iterations - 16 )
            break;
    }
}

Console.WriteLine(lastBadIteration);

int ProcessSetup(DeepNet net, Setup s, bool showOutput)
{
    var result = RunNet(net, s, showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight().Mirror(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    return result;
}

int RunNet(DeepNet net, Setup setup, bool showOutput)
{
    if (showOutput)
        Console.Clear();
    var board = SolvePuzzle(setup, showOutput);
    var result = board.GetWallsAsNeuralNetOutput();
    var input = setup.ConvertToNeuralNetInput();
    var wrong = TestSetup(net, input, result);

    if (showOutput)
        Console.WriteLine(wrong);

    return wrong;
}

Board SolvePuzzle(Setup setup, bool showOutput)
{
    Board b = new(setup);
    if (showOutput)
        b.Draw();
    for (; ; )
    {
        if (CheckForCompletedLines(b, showOutput))
            continue;
        if (CheckForPathOut(b, showOutput))
            continue;
        if (CheckQuads(b, showOutput))
            continue;
        if (CheckForTreasureRoom(b, showOutput))
            continue;
        break;
    }
    return b;
}

int TestSetup(DeepNet net, float[] input, ulong solution)
{
    var solutionVector = new float[64];

    for (var i = 0; i < 64; i++)
        solutionVector[i] = ((solution >> (63 - i)) & 1) == 1 ? 1 : -1;

    var result = net.Train(input, solutionVector);
    var wrong = 0;
    for (var i = 0; i < 64; i++)
        if (MathF.Sign(result[i]) != MathF.Sign(solutionVector[i]))
            wrong++;
    return wrong;
}

static void Redraw(Board b)
{
    var cursorPosition = Console.GetCursorPosition();
    b.Draw();
    Console.SetCursorPosition(cursorPosition.Left, cursorPosition.Top);
}

static bool CheckForTreasureRoom(Board b, bool showOutput)
{
    var changed = false;

    for (var i = 1; i <= 8; i++)
        for (var j = 1; j <= 8; j++)
        {
            if (b.State[i, j] != CellState.Treasure)
                continue;
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
                        switch (b.State[l, m])
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

                var topWall = CountBorder(b.GetArea(xp, yp - 1, 3, 1));
                if (topWall == null)
                    continue;
                var bottomWall = CountBorder(b.GetArea(xp, yp + 3, 3, 1));
                if (bottomWall == null)
                    continue;
                var leftWall = CountBorder(b.GetArea(xp - 1, yp, 1, 3));
                if (leftWall == null)
                    continue;
                var rightWall = CountBorder(b.GetArea(xp + 3, yp, 1, 3));
                if (rightWall == null)
                    continue;

                var canBeWalls = topWall.Value.Item1 + bottomWall.Value.Item1 + leftWall.Value.Item1 + rightWall.Value.Item1;
                var canBeExits = topWall.Value.Item2 + bottomWall.Value.Item2 + leftWall.Value.Item2 + rightWall.Value.Item2;
                var exits = topWall.Value.Item3 + bottomWall.Value.Item3 + leftWall.Value.Item3 + rightWall.Value.Item3;


                if (exits > 1 || canBeExits < 1 || canBeWalls < 11)
                    continue;

                if (yp > 1 && b.RowCounts[yp - 2] - b.CountRow(yp - 1, CellState.Wall) < ((topWall.Value.Item3 == 0 ? 2 : 3) - topWall.Value.Item4))
                    continue;
                if (yp < 6 && b.RowCounts[yp + 2] - b.CountRow(yp + 3, CellState.Wall) < ((topWall.Value.Item3 == 0 ? 2 : 3) - bottomWall.Value.Item4))
                    continue;
                if (xp > 1 && b.ColumnCounts[xp - 2] - b.CountColumn(xp - 1, CellState.Wall) < ((topWall.Value.Item3 == 0 ? 2 : 3) - leftWall.Value.Item4))
                    continue;
                if (xp < 6 && b.ColumnCounts[xp + 2] - b.CountColumn(xp + 3, CellState.Wall) < ((topWall.Value.Item3 == 0 ? 2 : 3) - rightWall.Value.Item4))
                    continue;

                // add checks for monsters in dead ends 2 away
                canBeRoom.Add((xp, yp, exits == 1));
            }
            if (canBeRoom.Count == 0)
                continue;
            if (canBeRoom.Count == 1)
            {
                var changedAny = false;
                for (var l = 0; l < 3; l++)
                    for (var m = 0; m < 3; m++)
                    {
                        var xx = canBeRoom[0].X + l;
                        var yy = canBeRoom[0].Y + m;
                        if (xx == i && yy == j)
                            continue;
                        if (b.State[xx, yy] != CellState.EmptyRoom)
                        {
                            b.State[xx, yy] = CellState.EmptyRoom;
                            changedAny = true;
                        }
                    }
                if (canBeRoom[0].HasExit)
                {
                    for (var l = 0; l < 3; l++)
                    {
                        if (b.State[canBeRoom[0].X + l, canBeRoom[0].Y - 1] == CellState.Unknown)
                        {
                            b.State[canBeRoom[0].X + l, canBeRoom[0].Y - 1] = CellState.Wall;
                            changedAny = true;
                        }
                        if (b.State[canBeRoom[0].X + l, canBeRoom[0].Y + 3] == CellState.Unknown)
                        {
                            b.State[canBeRoom[0].X + l, canBeRoom[0].Y + 3] = CellState.Wall;
                            changedAny = true;
                        }
                    }
                    for (var m = 0; m < 3; m++)
                    {
                        if (b.State[canBeRoom[0].X - 1, canBeRoom[0].Y + m] == CellState.Unknown)
                        {
                            b.State[canBeRoom[0].X - 1, canBeRoom[0].Y + m] = CellState.Wall;
                            changedAny = true;
                        }
                        if (b.State[canBeRoom[0].X + 3, canBeRoom[0].Y + m] == CellState.Unknown)
                        {
                            b.State[canBeRoom[0].X + 3, canBeRoom[0].Y + m] = CellState.Wall;
                            changedAny = true;
                        }
                    }
                }
                if (changedAny)
                {
                    if (showOutput)
                    {
                        Console.WriteLine($"Found the room for treasure at ({i},{j})");
                        Redraw(b);
                    }
                    return true;
                }
            }
        }
    return false;
}

static bool CheckQuads(Board b, bool showOutput)
{
    for (var i = 1; i < 8; i++)
        for (var j = 1; j < 8; j++)
        {
            (int X, int Y)? unk = null;
            var fail = false;
            for (var k = 0; k < 4; k++)
            {
                var xp = i + (k >> 1);
                var yp = j + (k & 1);
                switch (b.State[xp, yp])
                {
                    case CellState.Unknown:
                        if (unk == null)
                            unk = (xp, yp);
                        else
                            fail = true;
                        break;
                    case CellState.Empty:
                        break;
                    default:
                        fail = true;
                        break;
                }
            }
            if (fail || unk == null)
                continue;
            b.State[unk.Value.X, unk.Value.Y] = CellState.Wall;
            if (showOutput)
            {
                Console.WriteLine($"Cell ({unk.Value.X},{unk.Value.Y}) must be a wall, paths can't be wider than 1");
                Redraw(b);
            }
            return true;
        }
    return false;
}

static bool CheckForCompletedLines(Board b, bool showOutput)
{
    for (var i = 1; i <= 8; i++)
    {
        if (b.ColumnCounts[i - 1] - b.CountColumn(i, CellState.Wall) == 0 && b.CountColumn(i, CellState.Unknown) > 0)
        {
            for (var j = 1; j <= 8; j++)
                if (b.State[i, j] == CellState.Unknown)
                    b.State[i, j] = CellState.Empty;
            if (showOutput)
            {
                Console.WriteLine($"Column {i} has no more walls, marking as empty");
                Redraw(b);
            }
            return true;
        }

        if (b.ColumnCounts[i - 1] - b.CountColumn(i, CellState.Wall) == b.CountColumn(i, CellState.Unknown) && b.CountColumn(i, CellState.Unknown) > 0)
        {
            for (var j = 1; j <= 8; j++)
                if (b.State[i, j] == CellState.Unknown)
                    b.State[i, j] = CellState.Wall;
            if (showOutput)
            {
                Console.WriteLine($"Column {i} has remaining unknowns equal to wall count, marking as walls");
                Redraw(b);
            }
            return true;
        }

        if (b.RowCounts[i - 1] - b.CountRow(i, CellState.Wall) == 0 && b.CountRow(i, CellState.Unknown) > 0)
        {
            for (var j = 1; j <= 8; j++)
                if (b.State[j, i] == CellState.Unknown)
                    b.State[j, i] = CellState.Empty;
            if (showOutput)
            {
                Console.WriteLine($"Row {i} has no more walls, marking as empty");
                Redraw(b);
            }
            return true;
        }

        if (b.RowCounts[i - 1] - b.CountRow(i, CellState.Wall) == b.CountRow(i, CellState.Unknown) && b.CountRow(i, CellState.Unknown) > 0)
        {
            for (var j = 1; j <= 8; j++)
                if (b.State[j, i] == CellState.Unknown)
                    b.State[j, i] = CellState.Wall;
            if (showOutput)
            {
                Console.WriteLine($"Row {i} has remaining unknowns equal to wall count, marking as walls");
                Redraw(b);
            }
            return true;
        }
    }
    return false;
}

static bool CheckForPathOut(Board b, bool showOutput)
{
    for (var i = 1; i <= 8; i++)
        for (var j = 1; j <= 8; j++)
            switch (b.State[i, j])
            {
                case CellState.Monster:
                    var mn = b.GetNeighbors(i, j, CellState.Unknown);
                    if (mn.Count == 1 && b.CountNeighbors(i, j, CellState.Wall) == 3)
                    {
                        b.State[mn[0].X, mn[0].Y] = CellState.Empty;
                        if (showOutput)
                        {
                            Console.WriteLine($"Cell ({i},{j}) needs a way out, marking as empty");
                            Redraw(b);
                        }
                        return true;
                    }
                    else
                    {
                        mn = b.GetNeighbors(i, j, CellState.Empty);
                        if (mn.Count == 1)
                        {
                            mn = b.GetNeighbors(i, j, CellState.Unknown);

                            if (mn.Count > 0)
                            {
                                foreach (var m in mn)
                                    b.State[m.X, m.Y] = CellState.Wall;

                                if (showOutput)
                                {
                                    Console.WriteLine($"Cell ({i},{j}) can only have one way out, marking rest as walls");
                                    Redraw(b);
                                }
                                return true;
                            }
                        }
                    }
                    break;
                case CellState.Empty:
                    var en = b.GetNeighbors(i, j, CellState.Unknown);
                    if (en.Count == 1 && b.CountNeighbors(i, j, CellState.Wall) == 2)
                    {
                        b.State[en[0].X, en[0].Y] = CellState.Empty;
                        if (showOutput)
                        {
                            Console.WriteLine($"Cell ({i},{j}) needs a way out, marking as empty");
                            Redraw(b);
                        }
                        return true;
                    }
                    break;
                case CellState.Unknown:
                    var un = b.GetNeighbors(i, j, CellState.Wall);
                    if (un.Count == 3)
                    {
                        b.State[i, j] = CellState.Wall;
                        if (showOutput)
                        {
                            Console.WriteLine($"Cell ({i},{j}) is a dead end, fill it in");
                            Redraw(b);
                        }
                        return true;
                    }
                    break;
            }
    return false;
}

